using UnityEngine;

[CreateAssetMenu(menuName = "Game/Combat/Player Melee Attack Profile")]
public sealed class PlayerMeleeAttackProfile : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string attackName = "Melee Attack";

    [Tooltip("Cual slot de arma usa")]
    [SerializeField]
    private TMJ_WeaponUseSlot weaponUseSlot = TMJ_WeaponUseSlot.LightAttack;

    [Header("Damage")]
    [SerializeField, Min(0f)]
    private float baseDamage = 1f;

    [Header("Timing")]
    [SerializeField, Min(0.05f)]
    private float cooldown = 0.25f;

    [Header("Hit Area")]
    [SerializeField, Min(0.1f)]
    private float attackRange = 1.8f;

    [SerializeField, Range(20f, 180f)]
    private float attackArc = 95f;

    [Header("Visual")]
    [SerializeField, Min(0.01f)]
    private float visualDuration = 0.12f;

    [SerializeField, Range(0.05f, 0.4f)]
    private float visualWidth = 0.18f;

    [SerializeField]
    private Color slashColor = new Color(1f, 0.9f, 0.6f, 1f);

    public string AttackName => attackName;

    public TMJ_WeaponUseSlot WeaponUseSlot => weaponUseSlot;

    public float BaseDamage => baseDamage;

    public float Cooldown => cooldown;

    public float AttackRange => attackRange;

    public float AttackArc => attackArc;

    public float VisualDuration => visualDuration;

    public float VisualWidth => visualWidth;

    public Color SlashColor => slashColor;

    private void OnValidate()
    {
        baseDamage = Mathf.Max(0f, baseDamage);
        cooldown = Mathf.Max(0.05f, cooldown);
        attackRange = Mathf.Max(0.1f, attackRange);
        attackArc = Mathf.Clamp(attackArc, 20f, 180f);
        visualDuration = Mathf.Max(0.01f, visualDuration);
        visualWidth = Mathf.Clamp(visualWidth, 0.05f, 0.4f);
    }
}
