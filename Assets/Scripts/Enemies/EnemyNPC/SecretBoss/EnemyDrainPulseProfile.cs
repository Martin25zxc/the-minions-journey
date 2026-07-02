using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Boss/Enemy Drain Pulse Profile", fileName = "EnemyDrainPulseProfile_NewDrainPulse")]
public sealed class EnemyDrainPulseProfile : ScriptableObject
{
    [Header("Phase Gate")]
    [SerializeField, Min(1)] private int minPhase = 2;
    [SerializeField, Min(1)] private int maxPhase = 99;

    [Header("Range / Priority")]
    [SerializeField, Min(0f)] private float minAttackRange = 0f;
    [SerializeField, Min(0.05f)] private float maxAttackRange = 3.4f;
    [SerializeField] private float priority = 82f;
    [SerializeField, Min(0f)] private float cooldown = 22f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float telegraphTime = 0.85f;
    [SerializeField, Min(0f)] private float recoveryTime = 0.55f;

    [Header("Pulse")]
    [SerializeField, Min(0.05f)] private float radius = 3f;
    [SerializeField, Min(0f)] private float damage = 12f;
    [SerializeField, Min(0f)] private float healPerDamagedTarget = 16f;
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
    public float Radius => radius;
    public float Damage => damage;
    public float HealPerDamagedTarget => healPerDamagedTarget;
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
        radius = Mathf.Max(0.05f, radius);
        damage = Mathf.Max(0f, damage);
        healPerDamagedTarget = Mathf.Max(0f, healPerDamagedTarget);
        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
        stunDuration = Mathf.Max(0f, stunDuration);
        globalCooldownAfterUse = Mathf.Max(0f, globalCooldownAfterUse);
    }
}
