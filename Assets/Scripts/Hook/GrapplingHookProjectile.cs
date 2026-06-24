using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class GrapplingHookProjectile : MonoBehaviour
{
    // Backward-compatible event kept for any existing listener that only needs the point.
    public event Action<Vector3> OnHookLanded;

    // Used by TopDownGrapple to distinguish world anchors from enemy anchors.
    public event Action<Vector3, Collider> OnHookLandedDetailed;

    public event Action OnHookMissed;

    [Header("Movement")]
    [SerializeField, Min(1f)]
    private float speed = 30f;

    [SerializeField, Min(1f)]
    private float maxRange = 18f;

    [Tooltip("Tiempo maximo que el hook puede estar vivo sin engancharse.")]
    [SerializeField, Min(0.05f)]
    private float maxLifetime = 1.25f;

    [Header("Collision")]
    [Tooltip("Capas contra las que el hook puede engancharse. Recomendado: Obstacle, Enemy, Rock legacy y LimitWall solo si se acepta enganchar paredes invisibles.")]
    [SerializeField]
    private LayerMask attachableLayers;

    [Tooltip("Capas que el proyectil sensor revisa durante el vuelo. Si queda en Nothing, se usa attachableLayers. Sirve para que Boundary bloquee el hook sin ser enganchable.")]
    [SerializeField]
    private LayerMask flightBlockingLayers;

    [Tooltip("Radio del barrido usado por el hook sensor. Debe aproximarse al radio visual de la punta.")]
    [SerializeField, Min(0.01f)]
    private float castRadius = 0.12f;

    [SerializeField]
    private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;

    [Tooltip("Si el hook detecta algo bloqueante que no es enganchable, se cancela.")]
    [SerializeField]
    private bool missOnInvalidCollision = true;

    private const int HitBufferSize = 12;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[HitBufferSize];
    private Rigidbody body;
    private Collider hookCollider;
    private Transform owner;

    private Vector3 spawnPosition;
    private Vector3 travelDirection;
    private float launchTime;
    private bool launched;
    private bool completed;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        hookCollider = GetComponent<Collider>();

        ConfigureSensorRigidbody();
    }

    private void ConfigureSensorRigidbody()
    {
        body.useGravity = false;
        body.isKinematic = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.constraints |= RigidbodyConstraints.FreezeRotation;

        // The hook is a sensor, not a physical projectile. This prevents it from pushing
        // or rotating enemies when it reaches an Enemy collider.
        hookCollider.isTrigger = true;
    }

    public void Launch(Vector3 direction, Transform ownerTransform = null)
    {
        owner = ownerTransform;
        IgnoreOwnerCollisions();

        Vector3 planarDirection = direction;
        planarDirection.y = 0f;

        if (planarDirection.sqrMagnitude <= 0.001f)
        {
            Miss();
            return;
        }

        planarDirection.Normalize();

        spawnPosition = transform.position;
        travelDirection = planarDirection;
        launchTime = Time.time;
        launched = true;
        completed = false;

        body.isKinematic = true;
        body.useGravity = false;
        transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);
    }

    private void Update()
    {
        if (!launched || completed)
        {
            return;
        }

        if (Time.time - launchTime >= maxLifetime)
        {
            Miss();
            return;
        }

        if (GetPlanarDistance(transform.position, spawnPosition) >= maxRange)
        {
            Miss();
        }
    }

    private void FixedUpdate()
    {
        if (!launched || completed)
        {
            return;
        }

        StepSensorProjectile(Time.fixedDeltaTime);
    }

    private void StepSensorProjectile(float deltaTime)
    {
        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = currentPosition + travelDirection * (speed * deltaTime);

        if (TryFindSensorHit(currentPosition, nextPosition, out RaycastHit hit))
        {
            transform.position = hit.point;
            ProcessDetectedCollider(hit.collider, hit.point);
            return;
        }

        body.MovePosition(nextPosition);
        transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);
    }

    private bool TryFindSensorHit(Vector3 from, Vector3 to, out RaycastHit bestHit)
    {
        bestHit = default;

        LayerMask detectionLayers = flightBlockingLayers.value != 0
            ? flightBlockingLayers
            : attachableLayers;

        if (detectionLayers.value == 0)
        {
            return false;
        }

        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.0001f)
        {
            return false;
        }

        Vector3 direction = delta / distance;
        int hitCount = Physics.SphereCastNonAlloc(
            from,
            castRadius,
            direction,
            hitBuffer,
            distance,
            detectionLayers,
            queryTriggerInteraction);

        float bestDistance = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = hitBuffer[i];
            Collider candidateCollider = candidate.collider;

            if (candidateCollider == null || candidateCollider == hookCollider)
            {
                continue;
            }

            if (IsOwnerCollider(candidateCollider.transform))
            {
                continue;
            }

            if (candidate.distance < bestDistance)
            {
                bestDistance = candidate.distance;
                bestHit = candidate;
                found = true;
            }
        }

        return found;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || completed)
        {
            return;
        }

        if (other == null || other == hookCollider || IsOwnerCollider(other.transform))
        {
            return;
        }

        LayerMask detectionLayers = flightBlockingLayers.value != 0
            ? flightBlockingLayers
            : attachableLayers;

        if (!IsInLayerMask(other.gameObject.layer, detectionLayers))
        {
            return;
        }

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        if (hitPoint == transform.position)
        {
            hitPoint = transform.position;
        }

        ProcessDetectedCollider(other, hitPoint);
    }

    private void ProcessDetectedCollider(Collider detectedCollider, Vector3 hitPoint)
    {
        if (detectedCollider == null || completed)
        {
            return;
        }

        if (!IsInLayerMask(detectedCollider.gameObject.layer, attachableLayers))
        {
            if (missOnInvalidCollision)
            {
                Miss();
            }

            return;
        }

        Land(hitPoint, detectedCollider);
    }

    private void Land(Vector3 hitPoint, Collider hitCollider)
    {
        if (completed)
        {
            return;
        }

        completed = true;
        launched = false;

        StopBody();

        body.isKinematic = true;
        transform.position = hitPoint;

        OnHookLanded?.Invoke(hitPoint);
        OnHookLandedDetailed?.Invoke(hitPoint, hitCollider);
    }

    private void Miss()
    {
        if (completed)
        {
            return;
        }

        completed = true;
        launched = false;

        StopBody();

        OnHookMissed?.Invoke();

        Destroy(gameObject);
    }

    public void CancelSilently()
    {
        if (completed)
        {
            Destroy(gameObject);
            return;
        }

        completed = true;
        launched = false;

        StopBody();

        Destroy(gameObject);
    }

    private void StopBody()
    {
        if (body == null || body.isKinematic)
        {
            return;
        }

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    private void IgnoreOwnerCollisions()
    {
        if (owner == null || hookCollider == null)
        {
            return;
        }

        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        for (int i = 0; i < ownerColliders.Length; i++)
        {
            if (ownerColliders[i] == null)
            {
                continue;
            }

            Physics.IgnoreCollision(hookCollider, ownerColliders[i], true);
        }
    }

    private bool IsOwnerCollider(Transform other)
    {
        if (owner == null || other == null)
        {
            return false;
        }

        return other == owner || other.IsChildOf(owner);
    }

    private float GetPlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, castRadius);
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
