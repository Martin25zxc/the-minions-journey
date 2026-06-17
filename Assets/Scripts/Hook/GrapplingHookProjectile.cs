using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class GrapplingHookProjectile : MonoBehaviour
{
    public event Action<Vector3> OnHookLanded;
    public event Action OnHookMissed;

    [Header("Movement")]
    [SerializeField, Min(1f)]
    private float speed = 30f;

    [SerializeField, Min(1f)]
    private float maxRange = 18f;

    [Tooltip("Tiempo máximo que el hook puede estar vivo sin engancharse.")]
    [SerializeField, Min(0.05f)]
    private float maxLifetime = 1.25f;

    [Tooltip("Después de este tiempo, si el hook casi no se mueve, se considera perdido.")]
    [SerializeField, Min(0f)]
    private float stuckCheckDelay = 0.1f;

    [Tooltip("Velocidad horizontal mínima antes de considerar que el hook quedó trabado.")]
    [SerializeField, Min(0.01f)]
    private float minPlanarSpeedBeforeMiss = 0.15f;

    [Header("Collision")]
    [Tooltip("Capas contra las que el hook puede engancharse. Ej: Rock, LimitWall.")]
    [SerializeField]
    private LayerMask attachableLayers;

    [Tooltip("Si el hook choca contra algo que no es enganchable, se cancela.")]
    [SerializeField]
    private bool missOnInvalidCollision = true;

    private Rigidbody body;
    private Collider hookCollider;
    private Transform owner;

    private Vector3 spawnPosition;
    private float launchTime;
    private bool launched;
    private bool completed;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        hookCollider = GetComponent<Collider>();

        ConfigureRigidbody();
    }

    private void ConfigureRigidbody()
    {
        body.useGravity = false;
        body.isKinematic = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
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
        launchTime = Time.time;
        launched = true;
        completed = false;

        body.isKinematic = false;
        body.useGravity = false;
        body.linearVelocity = planarDirection * speed;

        transform.rotation = Quaternion.LookRotation(planarDirection, Vector3.up);
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
            return;
        }

        if (Time.time - launchTime >= stuckCheckDelay && GetPlanarVelocitySqr() <= minPlanarSpeedBeforeMiss * minPlanarSpeedBeforeMiss)
        {
            Miss();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (completed)
        {
            return;
        }

        if (collision.collider == null)
        {
            return;
        }

        if (IsOwnerCollider(collision.transform))
        {
            return;
        }

        if (!IsInLayerMask(collision.gameObject.layer, attachableLayers))
        {
            if (missOnInvalidCollision)
            {
                Miss();
            }

            return;
        }

        Vector3 hitPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;

        Land(hitPoint);
    }

    private void Land(Vector3 hitPoint)
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
        completed = true;
        launched = false;

        StopBody();

        Destroy(gameObject);
    }

    private void StopBody()
    {
        if (body == null)
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

    private float GetPlanarVelocitySqr()
    {
        Vector3 velocity = body.linearVelocity;
        velocity.y = 0f;
        return velocity.sqrMagnitude;
    }

    private static float GetPlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}