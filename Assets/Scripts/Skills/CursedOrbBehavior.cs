using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class CursedOrbBehavior : MonoBehaviour
{
    [Header("Orb lifecycle")]
    [SerializeField, Min(0f)]
    private float setupDelay = 0.5f; // Tiempo que tarda el orbe en estar activo después de ser lanzado

    [SerializeField, Min(0f), Tooltip("0 = no expira automáticamente. Mayor a 0 = el orbe se destruye solo después de esa cantidad de segundos.")]
    private float lifeTime = 0f;

    [Header("Ground distance settings")]
    [SerializeField]
    private LayerMask groundLayer; // Capa del suelo para detectar distancia mínima

    [SerializeField]
    private float maxRayDistance = 20f; // Distancia máxima del raycast hacia el suelo

    [SerializeField]
    private float heightFollowSpeed = 10f; // Qué tan rápido corrige la altura (suavizado)

    [SerializeField]
    private float minDistanceToGround = 1f; // Distancia mínima al suelo

    [Header("Obstacle detection")]
    [SerializeField, Tooltip("Capas que deben frenar al orbe. Ejemplo: paredes, rocas, límites del escenario.")]
    private LayerMask obstacleLayer;

    [SerializeField, Min(0.01f), Tooltip("Radio usado para anticipar paredes. Conviene que sea menor que el radio del trigger de curación.")]
    private float obstacleProbeRadius = 0.35f;

    [SerializeField, Min(0f), Tooltip("Pequeña separación para que el orbe no quede incrustado en la pared.")]
    private float obstacleSkin = 0.05f;

    public event Action<CursedOrbBehavior> OnOrbDestroyed; // Evento para notificar cuando el orbe se destruye o expira

    private float healAmount = 20f; // Cantidad de salud a curar al impactar
    private bool isSetupComplete; // Indica si el orbe ha completado su configuración inicial
    private bool hasEnded;

    private Rigidbody rb;
    private Coroutine setupCoroutine;
    private Coroutine lifetimeCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        setupCoroutine = StartCoroutine(SetupOrb());

        if (lifeTime > 0f)
        {
            lifetimeCoroutine = StartCoroutine(ExpireAfterLifetime());
        }
    }

    private void FixedUpdate()
    {
        if (hasEnded)
        {
            return;
        }

        StopBeforeObstacle();
        MaintainGroundDistance();
    }

    public void ChangeHealAmount(float newHealAmount)
    {
        healAmount = Mathf.Max(0f, newHealAmount);
    }

    public void DestroyOrb()
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        OnOrbDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    private IEnumerator SetupOrb()
    {
        yield return new WaitForSeconds(setupDelay);

        if (hasEnded)
        {
            yield break;
        }

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        isSetupComplete = true;
    }

    private IEnumerator ExpireAfterLifetime()
    {
        yield return new WaitForSeconds(lifeTime);
        DestroyOrb();
    }

    private void StopBeforeObstacle()
    {
        if (rb == null || rb.isKinematic || obstacleLayer.value == 0)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (planarVelocity.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 direction = planarVelocity.normalized;
        float distance = planarVelocity.magnitude * Time.fixedDeltaTime + obstacleSkin;

        if (Physics.SphereCast(
            rb.position,
            obstacleProbeRadius,
            direction,
            out RaycastHit hit,
            distance,
            obstacleLayer,
            QueryTriggerInteraction.Ignore))
        {
            Vector3 safePosition = rb.position + direction * Mathf.Max(0f, hit.distance - obstacleSkin);

            rb.MovePosition(safePosition);
            StopMovement();
        }
    }

    private void MaintainGroundDistance()
    {
        if (rb == null)
        {
            return;
        }

        Vector3 rayOrigin = rb.position + Vector3.up * 0.1f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, maxRayDistance, groundLayer))
        {
            float currentDistance = hit.distance;
            float heightDifference = minDistanceToGround - currentDistance;

            if (Mathf.Abs(heightDifference) > 0.001f)
            {
                Vector3 targetPosition = rb.position + Vector3.up * heightDifference;
                Vector3 newPosition = Vector3.Lerp(rb.position, targetPosition, heightFollowSpeed * Time.fixedDeltaTime);

                rb.MovePosition(newPosition);
            }
        }
    }

    private void StopMovement()
    {
        if (rb == null)
        {
            return;
        }

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.isKinematic = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasEnded)
        {
            return;
        }

        if (IsInLayerMask(other.gameObject.layer, obstacleLayer))
        {
            StopMovement();
            return;
        }

        if (!isSetupComplete)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        TopDownHealth playerHealth = other.GetComponent<TopDownHealth>();

        if (playerHealth == null)
        {
            playerHealth = other.GetComponentInParent<TopDownHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);
        }

        DestroyOrb();
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    private void OnDisable()
    {
        if (setupCoroutine != null)
        {
            StopCoroutine(setupCoroutine);
            setupCoroutine = null;
        }

        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }
    }

    private void OnValidate()
    {
        setupDelay = Mathf.Max(0f, setupDelay);
        lifeTime = Mathf.Max(0f, lifeTime);
        maxRayDistance = Mathf.Max(0f, maxRayDistance);
        heightFollowSpeed = Mathf.Max(0f, heightFollowSpeed);
        minDistanceToGround = Mathf.Max(0f, minDistanceToGround);
        obstacleProbeRadius = Mathf.Max(0.01f, obstacleProbeRadius);
        obstacleSkin = Mathf.Max(0f, obstacleSkin);
    }
}