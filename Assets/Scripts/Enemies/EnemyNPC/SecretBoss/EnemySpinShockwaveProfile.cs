using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Boss/Enemy Spin Shockwave Profile", fileName = "EnemySpinShockwaveProfile_NewSpin")]
public sealed class EnemySpinShockwaveProfile : ScriptableObject
{
    [Header("Phase Gate")]
    [SerializeField, Min(1)] private int minPhase = 2;
    [SerializeField, Min(1)] private int maxPhase = 99;

    [Header("Range / Priority")]
    [SerializeField, Min(0f)] private float minAttackRange = 0f;
    [SerializeField, Min(0.05f)] private float maxAttackRange = 3.2f;
    [SerializeField] private float priority = 90f;
    [SerializeField, Min(0f)] private float cooldown = 14f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float telegraphTime = 0.6f;

    [Tooltip("Cantidad de ejecuciones del clip de spin dentro de esta habilidad.")]
    [SerializeField, Min(1)] private int spinCount = 2;

    [Tooltip("Si está activo, el estado/clip BossSpinSlash se reinicia en cada giro.")]
    [SerializeField] private bool restartAnimationEachSpin = true;

    [Tooltip("Tiempo entre iniciar/reiniciar el clip de spin y aplicar daño/lanzar shockwaves.")]
    [SerializeField, Min(0f)] private float spinHitDelay = 0.2f;

    [Tooltip("Espera posterior al hit de cada giro antes de iniciar el siguiente giro.")]
    [SerializeField, Min(0f)] private float spinInterval = 0.25f;

    [SerializeField, Min(0f)] private float recoveryTime = 0.7f;

    [Header("Radial Hit")]
    [SerializeField] private bool applyRadialHit = true;
    [SerializeField, Min(0.05f)] private float radialHitRadius = 2.15f;
    [SerializeField, Min(0f)] private float radialDamage = 10f;
    [Tooltip("Si está apagado, un mismo target solo recibe el daño radial una vez por SpinShockwave completo.")]
    [SerializeField] private bool radialCanHitSameTargetEachSpin = false;
    [SerializeField] private LayerMask targetLayers;

    [Header("Radial Impact")]
    [SerializeField] private bool applyRadialImpact = true;
    [SerializeField, Min(0f)] private float radialKnockbackDistance = 1.6f;
    [SerializeField, Min(0f)] private float radialKnockbackDuration = 0.16f;
    [SerializeField, Min(0f)] private float radialStunDuration = 0.05f;
    [SerializeField] private bool radialInterruptCurrentAction;

    [Header("Shockwaves")]
    [SerializeField] private EnemyProjectile shockwavePrefab;
    [SerializeField, Min(0)] private int shockwavesPerSpin = 4;
    [SerializeField] private float baseAngleOffset = 0f;
    [SerializeField] private float angleOffsetPerSpin = 45f;
    [SerializeField] private Vector3 firePointLocalOffset = new Vector3(0f, 0.55f, 0.8f);
    [SerializeField, Min(0.01f)] private float shockwaveSpeed = 8f;
    [SerializeField, Min(0f)] private float shockwaveDamage = 8f;
    [SerializeField, Min(0.05f)] private float shockwaveLifetime = 1.8f;
    [SerializeField] private LayerMask blockingLayers;

    [Header("Shockwave Impact")]
    [SerializeField] private bool applyShockwaveImpact;
    [SerializeField, Min(0f)] private float shockwaveKnockbackDistance;
    [SerializeField, Min(0f)] private float shockwaveKnockbackDuration;
    [SerializeField, Min(0f)] private float shockwaveStunDuration;
    [SerializeField] private bool shockwaveInterruptCurrentAction;

    [Header("Special Gate")]
    [SerializeField] private bool useSpecialCooldownGate = true;
    [SerializeField, Min(0f)] private float globalCooldownAfterUse = 3.5f;

    public int MinPhase => minPhase;
    public int MaxPhase => maxPhase;
    public float MinAttackRange => minAttackRange;
    public float MaxAttackRange => maxAttackRange;
    public float Priority => priority;
    public float Cooldown => cooldown;
    public float TelegraphTime => telegraphTime;
    public int SpinCount => spinCount;
    public bool RestartAnimationEachSpin => restartAnimationEachSpin;
    public float SpinHitDelay => spinHitDelay;
    public float SpinInterval => spinInterval;
    public float RecoveryTime => recoveryTime;
    public bool ApplyRadialHit => applyRadialHit;
    public float RadialHitRadius => radialHitRadius;
    public float RadialDamage => radialDamage;
    public bool RadialCanHitSameTargetEachSpin => radialCanHitSameTargetEachSpin;
    public LayerMask TargetLayers => targetLayers;
    public bool ApplyRadialImpact => applyRadialImpact;
    public float RadialKnockbackDistance => radialKnockbackDistance;
    public float RadialKnockbackDuration => radialKnockbackDuration;
    public float RadialStunDuration => radialStunDuration;
    public bool RadialInterruptCurrentAction => radialInterruptCurrentAction;
    public EnemyProjectile ShockwavePrefab => shockwavePrefab;
    public int ShockwavesPerSpin => shockwavesPerSpin;
    public float BaseAngleOffset => baseAngleOffset;
    public float AngleOffsetPerSpin => angleOffsetPerSpin;
    public Vector3 FirePointLocalOffset => firePointLocalOffset;
    public float ShockwaveSpeed => shockwaveSpeed;
    public float ShockwaveDamage => shockwaveDamage;
    public float ShockwaveLifetime => shockwaveLifetime;
    public LayerMask BlockingLayers => blockingLayers;
    public bool ApplyShockwaveImpact => applyShockwaveImpact;
    public float ShockwaveKnockbackDistance => shockwaveKnockbackDistance;
    public float ShockwaveKnockbackDuration => shockwaveKnockbackDuration;
    public float ShockwaveStunDuration => shockwaveStunDuration;
    public bool ShockwaveInterruptCurrentAction => shockwaveInterruptCurrentAction;
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
        spinCount = Mathf.Max(1, spinCount);
        spinHitDelay = Mathf.Max(0f, spinHitDelay);
        spinInterval = Mathf.Max(0f, spinInterval);
        recoveryTime = Mathf.Max(0f, recoveryTime);
        radialHitRadius = Mathf.Max(0.05f, radialHitRadius);
        radialDamage = Mathf.Max(0f, radialDamage);
        radialKnockbackDistance = Mathf.Max(0f, radialKnockbackDistance);
        radialKnockbackDuration = Mathf.Max(0f, radialKnockbackDuration);
        radialStunDuration = Mathf.Max(0f, radialStunDuration);
        shockwavesPerSpin = Mathf.Max(0, shockwavesPerSpin);
        shockwaveSpeed = Mathf.Max(0.01f, shockwaveSpeed);
        shockwaveDamage = Mathf.Max(0f, shockwaveDamage);
        shockwaveLifetime = Mathf.Max(0.05f, shockwaveLifetime);
        shockwaveKnockbackDistance = Mathf.Max(0f, shockwaveKnockbackDistance);
        shockwaveKnockbackDuration = Mathf.Max(0f, shockwaveKnockbackDuration);
        shockwaveStunDuration = Mathf.Max(0f, shockwaveStunDuration);
        globalCooldownAfterUse = Mathf.Max(0f, globalCooldownAfterUse);
    }
}
