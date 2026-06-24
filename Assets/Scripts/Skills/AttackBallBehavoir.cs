using UnityEngine;

public class AttackBallBehavoir : MonoBehaviour
{
    [Header("Attack Ball Settings")]
    [SerializeField] LayerMask targetLayer;   // Capa de los objetivos a los que puede dañar
    [SerializeField] LayerMask groundLayer;   // Capa del suelo para detectar distancia mínima
    [SerializeField] LayerMask obstacleLayer; // Capa de obstáculos para detectar colisiones
    [SerializeField] float lifeTime = 5f;     // Tiempo de vida de la bola de ataque antes de destruirse

    [Header("Ground following")]
    [SerializeField] float maxRayDistance = 20f; // Distancia máxima del raycast hacia el suelo
    [SerializeField] float heightFollowSpeed = 10f; // Qué tan rápido corrige la altura (suavizado)
    [SerializeField] private float minDistanceToGround = 0.5f; // Distancia mínima al suelo

    private float speed = 10f;   // Velocidad de la bola de ataque
    private float damage = 20f;  // Daño que inflige la bola de ataque

    private Vector3 moveDirection;

    public void Initialize(float speed, float damage)
    {
        this.speed = speed;
        this.damage = damage;
    }

    // Sobrecarga para poder definir también la dirección de movimiento al instanciar
    public void Initialize(float speed, float damage, Vector3 direction)
    {
        this.speed = speed;
        this.damage = damage;
        SetDirection(direction);
    }

    public void SetDirection(Vector3 direction)
    {
        direction.y = 0f; // Aseguramos que la dirección sea horizontal, la altura la maneja el raycast
        moveDirection = direction.normalized;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime); // Destruir la bola de ataque después de su tiempo de vida

        if (moveDirection == Vector3.zero)
        {
            SetDirection(transform.forward);
        }
    }

    private void Update()
    {
        MoveForward();
        MaintainGroundDistance();
    }

    private void MoveForward()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void MaintainGroundDistance()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, maxRayDistance, groundLayer))
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
        // Verificar si la bola de ataque colisiona con un objetivo
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            TMJ_DamageInfo damageInfo = new TMJ_DamageInfo(damage, transform.position, null, gameObject);
            TopDownHealth targetHealth = other.GetComponent<TopDownHealth>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damageInfo);
            }
            Debug.Log($"¡Bola de ataque golpeó a {other.gameObject.name} e infligió {damage} de daño!");
            Destroy(gameObject); // Destruir la bola de ataque después de impactar
        }

        // Verificar si la bola de ataque colisiona con un obstáculo
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            Destroy(gameObject); // Destruir la bola de ataque después de colisionar con un obstáculo
        }
    }
}