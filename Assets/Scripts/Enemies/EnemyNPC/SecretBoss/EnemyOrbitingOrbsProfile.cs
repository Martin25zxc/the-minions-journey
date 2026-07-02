using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Boss/Enemy Orbiting Orbs Profile", fileName = "EnemyOrbitingOrbsProfile_NewOrbs")]
public sealed class EnemyOrbitingOrbsProfile : ScriptableObject
{
    [Header("Phase Gate")]
    [SerializeField, Min(1)] private int minPhase = 2;
    [SerializeField, Min(1)] private int maxPhase = 99;

    [Header("Range / Priority")]
    [SerializeField, Min(0f)] private float minAttackRange = 0f;
    [SerializeField, Min(0.05f)] private float maxAttackRange = 5f;
    [SerializeField] private float priority = 85f;
    [SerializeField, Min(0f)] private float cooldown = 18f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float telegraphTime = 0.7f;
    [SerializeField, Min(0f)] private float recoveryTime = 0.35f;

    [Header("Orbs")]
    [SerializeField] private EnemyOrbitingOrbHazard orbPrefab;
    [SerializeField, Min(1)] private int orbCount = 3;
    [SerializeField, Min(0.1f)] private float orbitRadius = 2f;
    [SerializeField] private float orbitDegreesPerSecond = 140f;
    [SerializeField, Min(0.05f)] private float duration = 5.5f;
    [SerializeField, Min(0f)] private float heightOffset = 0.75f;
    [SerializeField] private bool preventCastWhileOrbsActive = true;

    [Header("Damage")]
    [SerializeField, Min(0.05f)] private float hitRadius = 0.35f;
    [SerializeField, Min(0f)] private float damage = 6f;
    [SerializeField, Min(0f)] private float damageCooldownPerTarget = 0.65f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Impact")]
    [SerializeField] private bool applyImpact;
    [SerializeField, Min(0f)] private float knockbackDistance;
    [SerializeField, Min(0f)] private float knockbackDuration;
    [SerializeField, Min(0f)] private float stunDuration;
    [SerializeField] private bool interruptCurrentAction;

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
    public float RecoveryTime => recoveryTime;
    public EnemyOrbitingOrbHazard OrbPrefab => orbPrefab;
    public int OrbCount => orbCount;
    public float OrbitRadius => orbitRadius;
    public float OrbitDegreesPerSecond => orbitDegreesPerSecond;
    public float Duration => duration;
    public float HeightOffset => heightOffset;
    public bool PreventCastWhileOrbsActive => preventCastWhileOrbsActive;
    public float HitRadius => hitRadius;
    public float Damage => damage;
    public float DamageCooldownPerTarget => damageCooldownPerTarget;
    public LayerMask TargetLayers => targetLayers;
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
        orbCount = Mathf.Max(1, orbCount);
        orbitRadius = Mathf.Max(0.1f, orbitRadius);
        duration = Mathf.Max(0.05f, duration);
        heightOffset = Mathf.Max(0f, heightOffset);
        hitRadius = Mathf.Max(0.05f, hitRadius);
        damage = Mathf.Max(0f, damage);
        damageCooldownPerTarget = Mathf.Max(0f, damageCooldownPerTarget);
        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
        stunDuration = Mathf.Max(0f, stunDuration);
        globalCooldownAfterUse = Mathf.Max(0f, globalCooldownAfterUse);
    }
}
