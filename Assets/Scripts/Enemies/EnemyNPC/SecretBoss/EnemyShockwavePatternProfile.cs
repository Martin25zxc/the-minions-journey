using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Boss/Enemy Shockwave Pattern Profile", fileName = "EnemyShockwavePatternProfile_NewPattern")]
public sealed class EnemyShockwavePatternProfile : ScriptableObject
{
    [Header("Phase Gate")]
    [SerializeField, Min(1)]
    private int minPhase = 1;

    [SerializeField, Min(1)]
    private int maxPhase = 99;

    [Header("Range / Priority")]
    [SerializeField, Min(0f)]
    private float minAttackRange = 2.5f;

    [SerializeField, Min(0.05f)]
    private float maxAttackRange = 9f;

    [SerializeField]
    private float priority = 70f;

    [SerializeField, Min(0f)]
    private float cooldown = 7f;

    [Header("Timing")]
    [Tooltip("Anticipación visual antes de lanzar el patrón.")]
    [SerializeField, Min(0f)]
    private float telegraphTime = 0.65f;

    [Tooltip("Tiempo posterior al último disparo antes de devolver control al EnemyBrain.")]
    [SerializeField, Min(0f)]
    private float recoveryTime = 0.45f;

    [Header("Pattern")]
    [Tooltip("Ángulos Y relativos al forward del boss. Triple Slash típico: -20, 0, 20. Shockwave simple: 0.")]
    [SerializeField]
    private float[] yawAngles = new float[] { 0f };

    [Tooltip("Cantidad de repeticiones del patrón. Charged Volley: 3 repeticiones con un único ángulo 0.")]
    [SerializeField, Min(1)]
    private int repeatCount = 1;

    [SerializeField, Min(0f)]
    private float delayBetweenRepeats = 0.25f;

    [Tooltip("Si está activo, recalcula dirección hacia el target en cada repetición.")]
    [SerializeField]
    private bool reAimEachRepeat = true;

    [Header("Projectile")]
    [SerializeField]
    private EnemyProjectile projectilePrefab;

    [Tooltip("Si no hay firePoint asignado en la ability, se usa este offset local.")]
    [SerializeField]
    private Vector3 firePointLocalOffset = new Vector3(0f, 0.55f, 0.9f);

    [SerializeField, Min(0f)]
    private float targetAimHeight = 0.6f;

    [SerializeField, Min(0.01f)]
    private float projectileSpeed = 8f;

    [SerializeField, Min(0f)]
    private float projectileDamage = 12f;

    [SerializeField, Min(0.05f)]
    private float projectileLifetime = 2f;

    [SerializeField]
    private LayerMask targetLayers;

    [SerializeField]
    private LayerMask blockingLayers;

    [Header("Impact")]
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

    [Header("Special Gate")]
    [Tooltip("Si está activo, consulta EnemyBossSpecialCooldownGate antes de usarse y lo notifica al completar.")]
    [SerializeField]
    private bool useSpecialCooldownGate = true;

    [SerializeField, Min(0f)]
    private float globalCooldownAfterUse = 3f;

    public int MinPhase => minPhase;
    public int MaxPhase => maxPhase;
    public float MinAttackRange => minAttackRange;
    public float MaxAttackRange => maxAttackRange;
    public float Priority => priority;
    public float Cooldown => cooldown;
    public float TelegraphTime => telegraphTime;
    public float RecoveryTime => recoveryTime;
    public float[] YawAngles => yawAngles;
    public int RepeatCount => repeatCount;
    public float DelayBetweenRepeats => delayBetweenRepeats;
    public bool ReAimEachRepeat => reAimEachRepeat;
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
    public bool UseSpecialCooldownGate => useSpecialCooldownGate;
    public float GlobalCooldownAfterUse => globalCooldownAfterUse;

    public bool IsDistanceInRange(float horizontalDistance)
    {
        return horizontalDistance >= minAttackRange && horizontalDistance <= maxAttackRange;
    }

    private void OnValidate()
    {
        minPhase = Mathf.Max(1, minPhase);
        maxPhase = Mathf.Max(minPhase, maxPhase);
        minAttackRange = Mathf.Max(0f, minAttackRange);
        maxAttackRange = Mathf.Max(0.05f, maxAttackRange);
        if (maxAttackRange < minAttackRange) maxAttackRange = minAttackRange;
        cooldown = Mathf.Max(0f, cooldown);
        telegraphTime = Mathf.Max(0f, telegraphTime);
        recoveryTime = Mathf.Max(0f, recoveryTime);
        repeatCount = Mathf.Max(1, repeatCount);
        delayBetweenRepeats = Mathf.Max(0f, delayBetweenRepeats);
        if (yawAngles == null || yawAngles.Length == 0) yawAngles = new float[] { 0f };
        targetAimHeight = Mathf.Max(0f, targetAimHeight);
        projectileSpeed = Mathf.Max(0.01f, projectileSpeed);
        projectileDamage = Mathf.Max(0f, projectileDamage);
        projectileLifetime = Mathf.Max(0.05f, projectileLifetime);
        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
        stunDuration = Mathf.Max(0f, stunDuration);
        globalCooldownAfterUse = Mathf.Max(0f, globalCooldownAfterUse);
    }
}
