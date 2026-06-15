using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Combat/Enemy Ranged Attack Profile", fileName = "EnemyRangedAttackProfile_NewProjectile")]
public sealed class EnemyRangedAttackProfile : ScriptableObject
{
    [Header("Rango / Prioridad")]
    [SerializeField, Min(0f)]
    private float minAttackRange = 2.5f;

    [SerializeField, Min(0.05f)]
    private float maxAttackRange = 10f;

    [Tooltip("Distancia que el enemigo intenta mantener si no existe un componente de posicionamiento ranged.")]
    [SerializeField, Min(0.05f)]
    private float preferredDistance = 6f;

    [Tooltip("Prioridad relativa frente a otras abilities. Melee puede rondar 50, Leap 80, Ranged simple 40.")]
    [SerializeField]
    private float priority = 40f;

    [SerializeField, Min(0f)]
    private float cooldown = 1.4f;

    [Header("Timing")]
    [Tooltip("Tiempo en el que el enemigo apunta/telegraphea antes de disparar.")]
    [SerializeField, Min(0f)]
    private float aimTime = 0.25f;

    [Tooltip("Delay extra entre el trigger de animacion y el disparo. Dejar en 0 si Aim Time ya cubre la anticipacion.")]
    [SerializeField, Min(0f)]
    private float fireDelay = 0.15f;

    [SerializeField, Min(0f)]
    private float recoveryTime = 0.25f;

    [Header("Projectile")]
    [SerializeField]
    private EnemyProjectile projectilePrefab;

    [Tooltip("Si no se asigna FirePoint en la ability, se usa este offset local desde el root del enemigo.")]
    [SerializeField]
    private Vector3 firePointLocalOffset = new Vector3(0f, 0.8f, 0.8f);

    [Tooltip("Altura que se suma a la posicion del target para apuntar al cuerpo y no al piso.")]
    [SerializeField, Min(0f)]
    private float targetAimHeight = 0.7f;

    [SerializeField, Min(0.01f)]
    private float projectileSpeed = 8f;

    [SerializeField, Min(0f)]
    private float projectileDamage = 5f;

    [SerializeField, Min(0.05f)]
    private float projectileLifetime = 4f;

    [SerializeField]
    private LayerMask targetLayers = ~0;

    [Tooltip("Layers que destruyen/bloquean el proyectil. Ejemplo: Rock, LimitWall, escenario.")]
    [SerializeField]
    private LayerMask blockingLayers = 0;

    [Header("Impact opcional")]
    [SerializeField]
    private bool applyImpact;

    [SerializeField, Min(0f)]
    private float knockbackDistance;

    [SerializeField, Min(0f)]
    private float knockbackDuration;

    [SerializeField, Min(0f)]
    private float stunDuration;

    [SerializeField]
    private bool interruptCurrentAction;

    public float MinAttackRange => minAttackRange;
    public float MaxAttackRange => maxAttackRange;
    public float PreferredDistance => preferredDistance;
    public float Priority => priority;
    public float Cooldown => cooldown;

    public float AimTime => aimTime;
    public float FireDelay => fireDelay;
    public float RecoveryTime => recoveryTime;

    public EnemyProjectile ProjectilePrefab => projectilePrefab;
    public Vector3 FirePointLocalOffset => firePointLocalOffset;
    public float TargetAimHeight => targetAimHeight;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileDamage => projectileDamage;
    public float ProjectileLifetime => projectileLifetime;
    public LayerMask TargetLayers => targetLayers;
    public LayerMask BlockingLayers => blockingLayers;

    public bool ApplyImpact => applyImpact;
    public float KnockbackDistance => knockbackDistance;
    public float KnockbackDuration => knockbackDuration;
    public float StunDuration => stunDuration;
    public bool InterruptCurrentAction => interruptCurrentAction;

    public bool IsDistanceInRange(float horizontalDistance)
    {
        return horizontalDistance >= minAttackRange && horizontalDistance <= maxAttackRange;
    }

    private void OnValidate()
    {
        minAttackRange = Mathf.Max(0f, minAttackRange);
        maxAttackRange = Mathf.Max(0.05f, maxAttackRange);
        if (maxAttackRange < minAttackRange)
        {
            maxAttackRange = minAttackRange;
        }

        preferredDistance = Mathf.Clamp(preferredDistance, minAttackRange, maxAttackRange);
        cooldown = Mathf.Max(0f, cooldown);
        aimTime = Mathf.Max(0f, aimTime);
        fireDelay = Mathf.Max(0f, fireDelay);
        recoveryTime = Mathf.Max(0f, recoveryTime);

        projectileSpeed = Mathf.Max(0.01f, projectileSpeed);
        projectileDamage = Mathf.Max(0f, projectileDamage);
        projectileLifetime = Mathf.Max(0.05f, projectileLifetime);
        targetAimHeight = Mathf.Max(0f, targetAimHeight);

        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
        stunDuration = Mathf.Max(0f, stunDuration);
    }
}
