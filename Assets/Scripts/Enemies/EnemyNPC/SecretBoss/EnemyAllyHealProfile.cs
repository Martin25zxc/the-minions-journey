using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Boss/Enemy Ally Heal Profile", fileName = "EnemyAllyHealProfile_NewHeal")]
public sealed class EnemyAllyHealProfile : ScriptableObject
{
    [Header("Phase Gate")]
    [SerializeField, Min(1)] private int minPhase = 1;
    [SerializeField, Min(1)] private int maxPhase = 1;

    [Header("Range / Priority")]
    [SerializeField] private float priority = 72f;
    [SerializeField, Min(0f)] private float cooldown = 18f;
    [SerializeField, Min(0f)] private float maxHealRange = 9f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float telegraphTime = 0.85f;
    [SerializeField, Min(0f)] private float recoveryTime = 0.45f;

    [Header("Heal")]
    [SerializeField, Min(0f)] private float healAmount = 30f;
    [Tooltip("No castea heal si el mejor aliado herido perdió menos que este valor.")]
    [SerializeField, Min(0f)] private float minimumMissingHealthToCast = 8f;

    [Header("Special Gate")]
    [SerializeField] private bool useSpecialCooldownGate = true;
    [SerializeField, Min(0f)] private float globalCooldownAfterUse = 2.5f;

    public int MinPhase => minPhase;
    public int MaxPhase => maxPhase;
    public float Priority => priority;
    public float Cooldown => cooldown;
    public float MaxHealRange => maxHealRange;
    public float TelegraphTime => telegraphTime;
    public float RecoveryTime => recoveryTime;
    public float HealAmount => healAmount;
    public float MinimumMissingHealthToCast => minimumMissingHealthToCast;
    public bool UseSpecialCooldownGate => useSpecialCooldownGate;
    public float GlobalCooldownAfterUse => globalCooldownAfterUse;

    private void OnValidate()
    {
        minPhase = Mathf.Max(1, minPhase);
        maxPhase = Mathf.Max(minPhase, maxPhase);
        cooldown = Mathf.Max(0f, cooldown);
        maxHealRange = Mathf.Max(0f, maxHealRange);
        telegraphTime = Mathf.Max(0f, telegraphTime);
        recoveryTime = Mathf.Max(0f, recoveryTime);
        healAmount = Mathf.Max(0f, healAmount);
        minimumMissingHealthToCast = Mathf.Max(0f, minimumMissingHealthToCast);
        globalCooldownAfterUse = Mathf.Max(0f, globalCooldownAfterUse);
    }
}
