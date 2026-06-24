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

    private void Update()
    {
        if (hasEnded)
        {
            return;
        }

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

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        isSetupComplete = true;
    }

    private IEnumerator ExpireAfterLifetime()
    {
        yield return new WaitForSeconds(lifeTime);
        DestroyOrb();
    }

    private void MaintainGroundDistance()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, maxRayDistance, groundLayer))
        {
            float currentDistance = hit.distance;
            float heightDifference = minDistanceToGround - currentDistance;

            if (Mathf.Abs(heightDifference) > 0.001f)
            {
                Vector3 targetPosition = transform.position + Vector3.up * heightDifference;
                transform.position = Vector3.Lerp(transform.position, targetPosition, heightFollowSpeed * Time.deltaTime);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasEnded || !isSetupComplete)
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
}
