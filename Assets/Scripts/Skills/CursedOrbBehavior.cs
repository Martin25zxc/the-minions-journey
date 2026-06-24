using UnityEngine;
using System.Collections;
using System;

public class CursedOrbBehavior : MonoBehaviour
{
    [SerializeField] private float setupDelay = 0.5f; // Tiempo que tarda el orbe en estar activo después de ser lanzado
    [Header("Ground distance settings")]
    [SerializeField] LayerMask groundLayer; // Capa del suelo para detectar distancia mínima
    [SerializeField] float maxRayDistance = 20f; // Distancia máxima del raycast hacia el suelo
    [SerializeField] float heightFollowSpeed = 10f; // Qué tan rápido corrige la altura (suavizado)
    [SerializeField] private float minDistanceToGround = 1f; // Distancia mínima al suelo

    public event Action<CursedOrbBehavior> OnOrbDestroyed; // Evento para notificar cuando el orbe se destruye o expira
    private float healAmount = 20f; // Cantidad de salud a curar al impactar
    private bool isSetupComplete = false; // Indica si el orbe ha completado su configuración inicial
    
    private Rigidbody rb;
    private Coroutine setupCoroutine;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Start()
    {
        // Iniciar la configuración del orbe
        setupCoroutine = StartCoroutine(SetupOrb());
    }

    private void Update()
    {
        MaintainGroundDistance();
    }

    public void ChangeHealAmount(float newHealAmount)
    {
        healAmount = newHealAmount;
    }

    private IEnumerator SetupOrb()
    {
        // Esperar el tiempo de configuración antes de activar el orbe
        yield return new WaitForSeconds(setupDelay);
        rb.linearVelocity = Vector3.zero; // Detener el movimiento del orbe después de la expulsión inicial
        isSetupComplete = true; // El orbe ahora está activo y puede interactuar con el jugador
    }

    private void MaintainGroundDistance()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f; // pequeño offset hacia arriba
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, maxRayDistance, groundLayer))
        {
            float currentDistance = hit.distance;
            float heightDifference = minDistanceToGround - currentDistance;

            if (Mathf.Abs(heightDifference) > 0.001f)
            {
                Vector3 targetPosition = transform.position + Vector3.up * heightDifference;
                transform.position = Vector3.Lerp(transform.position, targetPosition, heightFollowSpeed * Time.deltaTime);
            }
        }
        // Si no detecta suelo (por ejemplo sobre un precipicio), la bola sigue su altura actual.
    }    
    

    private void OnTriggerEnter(Collider other)
    {
        if (!isSetupComplete)
        {
            // Si el orbe aún no ha completado su configuración, ignora las colisiones
            return;
        }
        if (other.CompareTag("Player"))
        {
            TopDownHealth playerHealth = other.GetComponent<TopDownHealth>();
            if (playerHealth != null)
            {
                playerHealth.Heal(healAmount); // Curar al jugador
                //Debug.Log($"¡Jugador curado por {healAmount} puntos de salud!");
            }
            OnOrbDestroyed?.Invoke(this); // Notificar que el orbe ha sido destruido o ha impactado
            Destroy(gameObject); // Destruir el orbe después de curar al jugador
        }
       
    }
}
