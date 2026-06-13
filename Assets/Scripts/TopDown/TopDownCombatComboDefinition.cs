using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class TopDownCombatComboDefinition
{
    private const string LegacyTripleSlashComboId = "TripleSlash";
    private const string LeapSlashComboId = "LeapSlash";
    private const string SpinSlashComboId = "SpinSlash";

    [Header("Identidad")]
    [Tooltip("Nombre interno del combo. Sirve para reconocerlo en debug o en el Inspector. Evitá usar este texto para decidir lógica importante.")]
    [SerializeField]
    private string comboId = LeapSlashComboId;

    [Header("Secuencia")]
    [Tooltip("Orden de inputs que hay que hacer para activar el combo. Ejemplo: Light, Light, Heavy.")]
    [SerializeField]
    private List<TopDownCombatInputAction> sequence = new List<TopDownCombatInputAction>();

    [Tooltip("Tiempo máximo permitido entre un input y el siguiente. Si tardás más que esto, se corta la lectura del combo.")]
    [SerializeField, Min(0.01f)]
    private float maxGapSeconds = 0.35f;

    [Tooltip("Tiempo máximo que puede durar toda la secuencia completa. Evita que inputs viejos activen combos mucho después.")]
    [SerializeField, Min(0f)]
    private float maxSequenceAgeSeconds = 1.25f;

    [Header("Resultado")]
    [Tooltip("Qué sistema ejecuta el combo. Por ahora normalmente usamos Weapon para combos de ataque melee.")]
    [SerializeField]
    private TopDownCombatComboTarget target = TopDownCombatComboTarget.Weapon;

    [Tooltip("Si está activo, cuando sale el combo no se ejecuta además el ataque base del último input. Ojo: por ahora NO limpia el historial, así dejamos abierta la idea de combos encadenados.")]
    [SerializeField]
    private bool consumeMatchedInput = true;

    [Tooltip("Qué estilo de arma usa el combo cuando NO tiene Override Attack Profile. Si está en Heavy, usa el arma asignada a HeavyAttack en el WeaponLoadout.")]
    [SerializeField]
    private TopDownCombatAttackStyle weaponAttackStyle = TopDownCombatAttackStyle.Heavy;

    [Tooltip("Perfil propio del combo. Usalo cuando el combo tenga timing, arco o visual distinto al Light/Heavy normal. Ej: LeapSlash espera el salto antes de pegar; SpinSlash puede ser 360. Si queda vacío, usa Light o Heavy según Weapon Attack Style.")]
    [SerializeField]
    private PlayerMeleeAttackProfile overrideAttackProfile;

    [Header("Animación")]
    [Tooltip("Qué animación o feedback debería dispararse cuando sale este combo. Es mejor que comparar strings como ComboId == LeapSlash.")]
    [SerializeField]
    private PlayerComboAnimationCue animationCue = PlayerComboAnimationCue.LeapSlash;

    [Header("Multiplicadores de gameplay")]
    [Tooltip("Multiplica el daño base del perfil usado. Si usás Override Attack Profile y ya pusiste el daño final ahí, dejá esto en 1.")]
    [SerializeField, Min(0.1f)]
    private float damageMultiplier = 1.65f;

    [Tooltip("Multiplica el alcance del perfil usado. Útil para que el combo llegue un poco más lejos sin crear otro perfil.")]
    [SerializeField, Min(0.1f)]
    private float rangeMultiplier = 1.15f;

    [Tooltip("Multiplica el arco del perfil usado. Si el resultado llega a 360, funciona como golpe circular.")]
    [SerializeField, Min(0.1f)]
    private float arcMultiplier = 1.1f;

    [Tooltip("Multiplica el cooldown del perfil usado. Mayor a 1 lo hace más comprometido; menor a 1 lo hace más permisivo.")]
    [SerializeField, Min(0.01f)]
    private float cooldownMultiplier = 1f;

    [Header("Multiplicadores visuales")]
    [Tooltip("Multiplica cuánto dura visible el slash del combo. Es solo feedback visual.")]
    [SerializeField, Min(0.01f)]
    private float visualDurationMultiplier = 1.15f;

    [Tooltip("Multiplica el grosor visual del slash. Sirve para que el combo se lea más fuerte.")]
    [SerializeField, Min(0.01f)]
    private float visualWidthMultiplier = 1.25f;

    [Tooltip("Si está activo, el combo usa un color propio en vez del color del perfil de ataque.")]
    [SerializeField]
    private bool useSlashColorOverride;

    [Tooltip("Color especial del slash cuando este combo se ejecuta y Use Slash Color Override está activo.")]
    [SerializeField]
    private Color slashColorOverride = new Color(1f, 0.8f, 0.35f, 1f);

    [Header("Prioridad")]
    [Tooltip("Si dos combos matchean al mismo tiempo, gana el más largo. Si empatan en largo, gana el de mayor prioridad.")]
    [SerializeField, Min(0)]
    private int priority;

    public string ComboId => comboId;
    public IReadOnlyList<TopDownCombatInputAction> Sequence => sequence;
    public int SequenceCount => sequence != null ? sequence.Count : 0;
    public float MaxGapSeconds => maxGapSeconds;
    public float MaxSequenceAgeSeconds => maxSequenceAgeSeconds;
    public TopDownCombatComboTarget Target => target;
    public bool ConsumeMatchedInput => consumeMatchedInput;
    public TopDownCombatAttackStyle WeaponAttackStyle => weaponAttackStyle;
    public PlayerMeleeAttackProfile OverrideAttackProfile => overrideAttackProfile;
    public TMJ_WeaponUseSlot EffectiveWeaponUseSlot => overrideAttackProfile != null ? overrideAttackProfile.WeaponUseSlot : ToWeaponUseSlot(weaponAttackStyle);
    public PlayerComboAnimationCue AnimationCue => animationCue;
    public float DamageMultiplier => damageMultiplier;
    public float RangeMultiplier => rangeMultiplier;
    public float ArcMultiplier => arcMultiplier;
    public float CooldownMultiplier => cooldownMultiplier;
    public float VisualDurationMultiplier => visualDurationMultiplier;
    public float VisualWidthMultiplier => visualWidthMultiplier;
    public bool UseSlashColorOverride => useSlashColorOverride;
    public Color SlashColorOverride => slashColorOverride;
    public int Priority => priority;

    public void NormalizeForInspector()
    {
        if (sequence == null)
        {
            sequence = new List<TopDownCombatInputAction>();
        }

        bool migratedLegacyTripleSlash = comboId == LegacyTripleSlashComboId;
        if (migratedLegacyTripleSlash)
        {
            comboId = LeapSlashComboId;
        }

        if (comboId == LeapSlashComboId && (animationCue == PlayerComboAnimationCue.None || migratedLegacyTripleSlash))
        {
            animationCue = PlayerComboAnimationCue.LeapSlash;
        }
        else if (comboId == SpinSlashComboId && (animationCue == PlayerComboAnimationCue.None || animationCue == PlayerComboAnimationCue.LeapSlash))
        {
            // Cuando el campo AnimationCue aparece por primera vez en combos ya serializados,
            // Unity puede dejar el valor default LeapSlash. Si el combo se llama SpinSlash,
            // lo corregimos para que dispare la animación correcta.
            animationCue = PlayerComboAnimationCue.SpinSlash;
        }

        maxGapSeconds = Mathf.Max(0.01f, maxGapSeconds);
        maxSequenceAgeSeconds = Mathf.Max(0f, maxSequenceAgeSeconds);
        damageMultiplier = Mathf.Max(0.1f, damageMultiplier);
        rangeMultiplier = Mathf.Max(0.1f, rangeMultiplier);
        arcMultiplier = Mathf.Max(0.1f, arcMultiplier);
        cooldownMultiplier = Mathf.Max(0.01f, cooldownMultiplier);
        visualDurationMultiplier = Mathf.Max(0.01f, visualDurationMultiplier);
        visualWidthMultiplier = Mathf.Max(0.01f, visualWidthMultiplier);
        priority = Mathf.Max(0, priority);
    }

    public static IReadOnlyList<TopDownCombatComboDefinition> CreateDefaultWeaponCombos()
    {
        return new[]
        {
            CreateDefaultLeapSlash(),
            CreateDefaultSpinSlash()
        };
    }

    public static TopDownCombatComboDefinition CreateDefaultLeapSlash()
    {
        return new TopDownCombatComboDefinition
        {
            comboId = LeapSlashComboId,
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
            overrideAttackProfile = null,
            animationCue = PlayerComboAnimationCue.LeapSlash,
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

    public static TopDownCombatComboDefinition CreateDefaultSpinSlash()
    {
        return new TopDownCombatComboDefinition
        {
            comboId = SpinSlashComboId,
            sequence = new List<TopDownCombatInputAction>
            {
                TopDownCombatInputAction.HeavyAttack,
                TopDownCombatInputAction.LightAttack,
                TopDownCombatInputAction.HeavyAttack,
                TopDownCombatInputAction.HeavyAttack
            },
            maxGapSeconds = 0.45f,
            maxSequenceAgeSeconds = 1.8f,
            target = TopDownCombatComboTarget.Weapon,
            consumeMatchedInput = true,
            weaponAttackStyle = TopDownCombatAttackStyle.Heavy,
            overrideAttackProfile = null,
            animationCue = PlayerComboAnimationCue.SpinSlash,
            damageMultiplier = 1.45f,
            rangeMultiplier = 1.05f,
            arcMultiplier = 3f,
            cooldownMultiplier = 1.25f,
            visualDurationMultiplier = 1.35f,
            visualWidthMultiplier = 1.6f,
            useSlashColorOverride = true,
            slashColorOverride = new Color(0.55f, 0.9f, 1f, 1f),
            priority = 8
        };
    }

    private static TMJ_WeaponUseSlot ToWeaponUseSlot(TopDownCombatAttackStyle attackStyle)
    {
        return attackStyle == TopDownCombatAttackStyle.Light
            ? TMJ_WeaponUseSlot.LightAttack
            : TMJ_WeaponUseSlot.HeavyAttack;
    }
}
