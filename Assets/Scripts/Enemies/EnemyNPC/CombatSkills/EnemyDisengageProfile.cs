using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Combat/Enemy Disengage Profile", fileName = "EnemyDisengageProfile_NewDisengage")]
public sealed class EnemyDisengageProfile : ScriptableObject
{
    [Header("Prioridad")]
    [Tooltip("Prioridad relativa frente a otras abilities. Para ranged/caster puede ser alta. Para melee defensivo puede ser menor que MeleeAttack.")]
    [SerializeField]
    private float priority = 60f;

    [Header("Activacion")]
    [Tooltip("La habilidad puede usarse si el target esta a esta distancia o menos.")]
    [SerializeField, Min(0.05f)]
    private float triggerDistance = 2f;

    [Tooltip("Distancia minima opcional. Normalmente 0.")]
    [SerializeField, Min(0f)]
    private float minTargetDistance = 0f;

    [SerializeField, Min(0f)]
    private float cooldown = 3f;

    [Header("Movimiento")]
    [Tooltip("Distancia ideal de retroceso.")]
    [SerializeField, Min(0.05f)]
    private float retreatDistance = 3f;

    [Tooltip("Si el retroceso completo esta bloqueado, intenta distancias menores hasta este minimo.")]
    [SerializeField, Min(0.05f)]
    private float minimumRetreatDistance = 0.8f;

    [SerializeField, Min(1)]
    private int distanceSearchSteps = 5;

    [Tooltip("Duracion del salto/retroceso.")]
    [SerializeField, Min(0.01f)]
    private float jumpDuration = 0.35f;

    [Tooltip("Altura visual del arco. No cambia la posicion fisica del Rigidbody, solo el visual root.")]
    [SerializeField, Min(0f)]
    private float jumpHeight = 0.35f;

    [Tooltip("Multiplicador conceptual por si mas adelante se quiere comparar con MoveSpeed. Esta habilidad usa posicion controlada.")]
    [SerializeField, Min(0f)]
    private float speedMultiplier = 1f;

    [Header("Timing")]
    [Tooltip("Delay antes de moverse. Para un disengage reactivo puede quedar en 0.")]
    [SerializeField, Min(0f)]
    private float startupTime = 0f;

    [Tooltip("Tiempo despues de aterrizar antes de devolver control al Brain.")]
    [SerializeField, Min(0f)]
    private float recoveryTime = 0.15f;

    [Header("Bloqueo")]
    [Tooltip("Layers que bloquean el retroceso. Recomendado: Obstacle, Boundary, Rock, LimitWall.")]
    [SerializeField]
    private LayerMask blockingLayers = 0;

    [Tooltip("Altura desde donde se evalua el path del cuerpo.")]
    [SerializeField, Min(0f)]
    private float pathCheckHeight = 0.5f;

    [Tooltip("Altura para revisar el punto de aterrizaje.")]
    [SerializeField, Min(0f)]
    private float landingCheckHeight = 0.5f;

    [Tooltip("Si es mayor a 0, reemplaza el BodyRadius del EnemyDefinition.")]
    [SerializeField, Min(0f)]
    private float bodyRadiusOverride = 0f;

    public float Priority => priority;
    public float TriggerDistance => triggerDistance;
    public float MinTargetDistance => minTargetDistance;
    public float Cooldown => cooldown;

    public float RetreatDistance => retreatDistance;
    public float MinimumRetreatDistance => minimumRetreatDistance;
    public int DistanceSearchSteps => distanceSearchSteps;
    public float JumpDuration => jumpDuration;
    public float JumpHeight => jumpHeight;
    public float SpeedMultiplier => speedMultiplier;

    public float StartupTime => startupTime;
    public float RecoveryTime => recoveryTime;

    public LayerMask BlockingLayers => blockingLayers;
    public float PathCheckHeight => pathCheckHeight;
    public float LandingCheckHeight => landingCheckHeight;
    public float BodyRadiusOverride => bodyRadiusOverride;

    public bool IsDistanceInRange(float horizontalDistance)
    {
        return horizontalDistance >= minTargetDistance && horizontalDistance <= triggerDistance;
    }

    private void OnValidate()
    {
        triggerDistance = Mathf.Max(0.05f, triggerDistance);
        minTargetDistance = Mathf.Max(0f, minTargetDistance);

        if (minTargetDistance > triggerDistance)
        {
            minTargetDistance = triggerDistance;
        }

        cooldown = Mathf.Max(0f, cooldown);

        retreatDistance = Mathf.Max(0.05f, retreatDistance);
        minimumRetreatDistance = Mathf.Max(0.05f, minimumRetreatDistance);

        if (minimumRetreatDistance > retreatDistance)
        {
            minimumRetreatDistance = retreatDistance;
        }

        distanceSearchSteps = Mathf.Max(1, distanceSearchSteps);
        jumpDuration = Mathf.Max(0.01f, jumpDuration);
        jumpHeight = Mathf.Max(0f, jumpHeight);
        speedMultiplier = Mathf.Max(0f, speedMultiplier);

        startupTime = Mathf.Max(0f, startupTime);
        recoveryTime = Mathf.Max(0f, recoveryTime);

        pathCheckHeight = Mathf.Max(0f, pathCheckHeight);
        landingCheckHeight = Mathf.Max(0f, landingCheckHeight);
        bodyRadiusOverride = Mathf.Max(0f, bodyRadiusOverride);
    }
}