using UnityEngine;

public enum EnemyBarrageAimMode
{
    LockAtStart = 0,
    ReaimEachShot = 1
}

[CreateAssetMenu(menuName = "Enemies/Combat/Enemy Barrage Attack Profile", fileName = "EnemyBarrageAttackProfile_NewBarrage")]
public sealed class EnemyBarrageAttackProfile : ScriptableObject
{
    [Header("Rango / Prioridad")]
    [SerializeField, Min(0f)]
    private float minAttackRange = 3f;

    [SerializeField, Min(0.05f)]
    private float maxAttackRange = 10f;

    [Tooltip("Prioridad relativa frente a otras abilities. Debe ser mayor que Ranged simple si queremos que Barrage tenga preferencia cuando este disponible.")]
    [SerializeField]
    private float priority = 60f;

    [Header("Cooldown / Entrada en combate")]
    [SerializeField, Min(0f)]
    private float cooldown = 6f;

    [Tooltip("Delay antes de permitir el primer Barrage luego de detectar un target. Evita que el enemigo abra el combate instantaneamente con su especial.")]
    [SerializeField, Min(0f)]
    private float initialEngageDelay = 1.5f;

    [Tooltip("Si esta activo, el delay inicial se aplica solo la primera vez que esta ability ve un target. Si esta apagado, se reaplica cuando cambia el target.")]
    [SerializeField]
    private bool applyInitialDelayOnlyOnce = true;

    [Header("Timing")]
    [Tooltip("Anticipacion inicial antes de empezar la rafaga.")]
    [SerializeField, Min(0f)]
    private float telegraphTime = 0.35f;

    [SerializeField, Min(1)]
    private int shotCount = 5;

    [Tooltip("Tiempo entre disparos, despues de que sale cada flecha. No reemplaza el windup del gesto de disparo.")]
    [SerializeField, Min(0f)]
    private float delayBetweenShots = 0.18f;

    [Tooltip("Tiempo entre reproducir la animacion RangedAttack y soltar cada flecha. Permite que el cuerpo/arma acompañen el disparo.")]
    [SerializeField, Min(0f)]
    private float shotWindupTime = 0.12f;

    [SerializeField, Min(0f)]
    private float recoveryTime = 0.35f;

    [Header("Aim")]
    [SerializeField]
    private EnemyBarrageAimMode aimMode = EnemyBarrageAimMode.ReaimEachShot;

    [Tooltip("Altura que se suma a la posicion del target para apuntar al cuerpo y no al piso.")]
    [SerializeField, Min(0f)]
    private float targetAimHeight = 0.7f;

    [Header("Projectile")]
    [SerializeField]
    private EnemyProjectile projectilePrefab;

    [Tooltip("Si no se asigna FirePoint en la ability, se usa este offset local desde el root del enemigo.")]
    [SerializeField]
    private Vector3 firePointLocalOffset = new Vector3(0f, 0.8f, 0.8f);

    [SerializeField, Min(0.01f)]
    private float projectileSpeed = 8f;

    [SerializeField, Min(0f)]
    private float projectileDamage = 4f;

    [SerializeField, Min(0.05f)]
    private float projectileLifetime = 4f;

    [Header("Spread opcional")]
    [Tooltip("Cantidad de proyectiles por cada disparo de la rafaga. Para primera version usar 1.")]
    [SerializeField, Min(1)]
    private int projectilesPerShot = 1;

    [Tooltip("Angulo total de apertura si Projectiles Per Shot es mayor que 1.")]
    [SerializeField, Min(0f)]
    private float spreadAngle = 0f;

    [Header("Layers")]
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
    public float Priority => priority;
    public float Cooldown => cooldown;
    public float InitialEngageDelay => initialEngageDelay;
    public bool ApplyInitialDelayOnlyOnce => applyInitialDelayOnlyOnce;

    public float TelegraphTime => telegraphTime;
    public int ShotCount => shotCount;
    public float DelayBetweenShots => delayBetweenShots;
    public float ShotWindupTime => shotWindupTime;
    public float RecoveryTime => recoveryTime;

    public EnemyBarrageAimMode AimMode => aimMode;
    public float TargetAimHeight => targetAimHeight;

    public EnemyProjectile ProjectilePrefab => projectilePrefab;
    public Vector3 FirePointLocalOffset => firePointLocalOffset;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileDamage => projectileDamage;
    public float ProjectileLifetime => projectileLifetime;
    public int ProjectilesPerShot => projectilesPerShot;
    public float SpreadAngle => spreadAngle;

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

        cooldown = Mathf.Max(0f, cooldown);
        initialEngageDelay = Mathf.Max(0f, initialEngageDelay);
        telegraphTime = Mathf.Max(0f, telegraphTime);
        shotCount = Mathf.Max(1, shotCount);
        delayBetweenShots = Mathf.Max(0f, delayBetweenShots);
        shotWindupTime = Mathf.Max(0f, shotWindupTime);
        recoveryTime = Mathf.Max(0f, recoveryTime);

        projectileSpeed = Mathf.Max(0.01f, projectileSpeed);
        projectileDamage = Mathf.Max(0f, projectileDamage);
        projectileLifetime = Mathf.Max(0.05f, projectileLifetime);
        targetAimHeight = Mathf.Max(0f, targetAimHeight);
        projectilesPerShot = Mathf.Max(1, projectilesPerShot);
        spreadAngle = Mathf.Max(0f, spreadAngle);

        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
        stunDuration = Mathf.Max(0f, stunDuration);
    }
}
