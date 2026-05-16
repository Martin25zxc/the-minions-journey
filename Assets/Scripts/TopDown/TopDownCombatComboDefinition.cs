using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class TopDownCombatComboDefinition
{
    [SerializeField]
    string comboId = "TripleSlash";

    [SerializeField]
    List<TopDownCombatInputAction> sequence = new List<TopDownCombatInputAction>();

    [SerializeField, Min(0.01f)]
    float maxGapSeconds = 0.35f;

    [SerializeField, Min(0f)]
    float maxSequenceAgeSeconds = 1.25f;

    [SerializeField]
    TopDownCombatComboTarget target = TopDownCombatComboTarget.Weapon;

    [SerializeField]
    bool consumeMatchedInput = true;

    [SerializeField]
    TopDownCombatAttackStyle weaponAttackStyle = TopDownCombatAttackStyle.Heavy;

    [SerializeField, Min(0.1f)]
    float damageMultiplier = 1.65f;

    [SerializeField, Min(0.1f)]
    float rangeMultiplier = 1.15f;

    [SerializeField, Min(0.1f)]
    float arcMultiplier = 1.1f;

    [SerializeField, Min(0.01f)]
    float cooldownMultiplier = 1f;

    [SerializeField, Min(0.01f)]
    float visualDurationMultiplier = 1.15f;

    [SerializeField, Min(0.01f)]
    float visualWidthMultiplier = 1.25f;

    [SerializeField]
    bool useSlashColorOverride;

    [SerializeField]
    Color slashColorOverride = new Color(1f, 0.8f, 0.35f, 1f);

    [SerializeField, Min(0)]
    int priority;

    public string ComboId => comboId;

    public IReadOnlyList<TopDownCombatInputAction> Sequence => sequence;

    public int SequenceCount => sequence != null ? sequence.Count : 0;

    public float MaxGapSeconds => maxGapSeconds;

    public float MaxSequenceAgeSeconds => maxSequenceAgeSeconds;

    public TopDownCombatComboTarget Target => target;

    public bool ConsumeMatchedInput => consumeMatchedInput;

    public TopDownCombatAttackStyle WeaponAttackStyle => weaponAttackStyle;

    public float DamageMultiplier => damageMultiplier;

    public float RangeMultiplier => rangeMultiplier;

    public float ArcMultiplier => arcMultiplier;

    public float CooldownMultiplier => cooldownMultiplier;

    public float VisualDurationMultiplier => visualDurationMultiplier;

    public float VisualWidthMultiplier => visualWidthMultiplier;

    public bool UseSlashColorOverride => useSlashColorOverride;

    public Color SlashColorOverride => slashColorOverride;

    public int Priority => priority;

    public static TopDownCombatComboDefinition CreateDefaultTripleSlash()
    {
        return new TopDownCombatComboDefinition
        {
            comboId = "TripleSlash",
            sequence = new List<TopDownCombatInputAction>
            {
                TopDownCombatInputAction.LightAttack,
                TopDownCombatInputAction.LightAttack,
                TopDownCombatInputAction.HeavyAttack
            },
            maxGapSeconds = 0.4f,
            maxSequenceAgeSeconds = 1.4f,
            target = TopDownCombatComboTarget.Weapon,
            consumeMatchedInput = true,
            weaponAttackStyle = TopDownCombatAttackStyle.Heavy,
            damageMultiplier = 1.75f,
            rangeMultiplier = 1.2f,
            arcMultiplier = 1.15f,
            cooldownMultiplier = 1.2f,
            visualDurationMultiplier = 1.2f,
            visualWidthMultiplier = 1.4f,
            useSlashColorOverride = true,
            slashColorOverride = new Color(1f, 0.65f, 0.25f, 1f),
            priority = 10
        };
    }
}