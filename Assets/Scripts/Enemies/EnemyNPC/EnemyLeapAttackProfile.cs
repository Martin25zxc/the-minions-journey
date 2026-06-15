using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Combat/Enemy Leap Attack Profile", fileName = "EnemyLeapAttackProfile_NewLeap")]
public sealed class EnemyLeapAttackProfile : ScriptableObject
{
    [Header("Prioridad")]
    [Tooltip("Prioridad relativa frente a otras abilities. Leap recomendado: 80 para que gane frente a melee si esta a media distancia.")]
    [SerializeField]
    private float priority = 80f;

    [Header("Rango / Cooldown")]
    [SerializeField, Min(0f)]
    private float minRange = 3f;

    [SerializeField, Min(0.05f)]
    private float maxRange = 7f;

    [SerializeField, Min(0f)]
    private float cooldown = 5f;

    [Header("Timing")]
    [Tooltip("Anticipacion antes de moverse. Si el enemigo recibe impacto durante esta ventana, el Brain puede cancelar el leap.")]
    [SerializeField, Min(0f)]
    private float telegraphTime = 0.35f;

    [SerializeField, Min(0.01f)]
    private float jumpDuration = 0.65f;

    [SerializeField, Min(0f)]
    private float jumpHeight = 1.6f;

    [SerializeField, Min(0f)]
    private float recoveryTime = 0.45f;

    [Header("Landing seguro")]
    [Tooltip("Distancia a la que intenta aterrizar respecto al target. No debe caer en la posicion exacta del jugador.")]
    [SerializeField, Min(0.05f)]
    private float preferredLandingDistanceFromTarget = 1.2f;

    [Tooltip("Radio usado para validar si el cuerpo del enemigo cabe en el landing point. Si queda en 0, se usa EnemyDefinition.BodyRadius.")]
    [SerializeField, Min(0f)]
    private float landingBodyRadiusOverride;

    [Tooltip("Altura desde el piso para el CheckSphere de espacio libre.")]
    [SerializeField, Min(0f)]
    private float landingCheckHeight = 0.5f;

    [Tooltip("Radio adicional para probar posiciones cercanas al punto ideal si el landing directo esta bloqueado.")]
    [SerializeField, Min(0f)]
    private float landingSearchRadius = 1.2f;

    [SerializeField, Min(1)]
    private int landingSearchSteps = 8;

    [Tooltip("Layers que bloquean el aterrizaje. Recomendado: escenario/obstaculos + Player + Enemy si corresponde.")]
    [SerializeField]
    private LayerMask blockingLayers = ~0;

    [Header("Landing Damage")]
    [SerializeField, Min(0f)]
    private float landingDamage = 12f;

    [SerializeField, Min(0.05f)]
    private float landingDamageRadius = 1.3f;

    [Tooltip("Offset del centro del daño al aterrizar. Usar Y aprox 0.6/0.8 si los colliders del player estan a altura de cuerpo.")]
    [SerializeField]
    private Vector3 landingDamageOffset = new Vector3(0f, 0.7f, 0f);

    [SerializeField]
    private LayerMask targetLayers = ~0;

    [Header("Impact opcional en landing")]
    [SerializeField]
    private bool applyImpactOnLanding = true;

    [SerializeField, Min(0f)]
    private float landingKnockbackDistance = 0.8f;

    [SerializeField, Min(0f)]
    private float landingKnockbackDuration = 0.12f;

    [SerializeField, Min(0f)]
    private float landingStunDuration = 0.15f;

    [Tooltip("Preparado para futuro. Puede servir para cortar casteos/acciones especiales del objetivo.")]
    [SerializeField]
    private bool interruptCurrentAction;

    public float Priority => priority;
    public float MinRange => minRange;
    public float MaxRange => maxRange;
    public float Cooldown => cooldown;

    public float TelegraphTime => telegraphTime;
    public float JumpDuration => jumpDuration;
    public float JumpHeight => jumpHeight;
    public float RecoveryTime => recoveryTime;

    public float PreferredLandingDistanceFromTarget => preferredLandingDistanceFromTarget;
    public float LandingBodyRadiusOverride => landingBodyRadiusOverride;
    public float LandingCheckHeight => landingCheckHeight;
    public float LandingSearchRadius => landingSearchRadius;
    public int LandingSearchSteps => landingSearchSteps;
    public LayerMask BlockingLayers => blockingLayers;

    public float LandingDamage => landingDamage;
    public float LandingDamageRadius => landingDamageRadius;
    public Vector3 LandingDamageOffset => landingDamageOffset;
    public LayerMask TargetLayers => targetLayers;

    public bool ApplyImpactOnLanding => applyImpactOnLanding;
    public float LandingKnockbackDistance => landingKnockbackDistance;
    public float LandingKnockbackDuration => landingKnockbackDuration;
    public float LandingStunDuration => landingStunDuration;
    public bool InterruptCurrentAction => interruptCurrentAction;

    public bool IsDistanceInRange(float horizontalDistance)
    {
        return horizontalDistance >= minRange && horizontalDistance <= maxRange;
    }

    private void OnValidate()
    {
        minRange = Mathf.Max(0f, minRange);
        maxRange = Mathf.Max(0.05f, maxRange);
        if (maxRange < minRange)
        {
            maxRange = minRange;
        }

        cooldown = Mathf.Max(0f, cooldown);
        telegraphTime = Mathf.Max(0f, telegraphTime);
        jumpDuration = Mathf.Max(0.01f, jumpDuration);
        jumpHeight = Mathf.Max(0f, jumpHeight);
        recoveryTime = Mathf.Max(0f, recoveryTime);

        preferredLandingDistanceFromTarget = Mathf.Max(0.05f, preferredLandingDistanceFromTarget);
        landingBodyRadiusOverride = Mathf.Max(0f, landingBodyRadiusOverride);
        landingCheckHeight = Mathf.Max(0f, landingCheckHeight);
        landingSearchRadius = Mathf.Max(0f, landingSearchRadius);
        landingSearchSteps = Mathf.Max(1, landingSearchSteps);

        landingDamage = Mathf.Max(0f, landingDamage);
        landingDamageRadius = Mathf.Max(0.05f, landingDamageRadius);
        landingKnockbackDistance = Mathf.Max(0f, landingKnockbackDistance);
        landingKnockbackDuration = Mathf.Max(0f, landingKnockbackDuration);
        landingStunDuration = Mathf.Max(0f, landingStunDuration);
    }
}
