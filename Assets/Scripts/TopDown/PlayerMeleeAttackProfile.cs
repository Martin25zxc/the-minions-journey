using UnityEngine;

[CreateAssetMenu(menuName = "Game/Combat/Player Melee Attack Profile")]
public sealed class PlayerMeleeAttackProfile : ScriptableObject
{
    [Header("Identidad")]
    [Tooltip("Nombre para reconocer este ataque en el Inspector. No afecta la lógica, es para que vos sepas qué estás editando.")]
    [SerializeField]
    private string displayName = "Melee Attack";

    [Tooltip("Qué arma usa este ataque. No es una ranura física: es si usa el arma asignada a LightAttack o a HeavyAttack.")]
    [SerializeField]
    private TMJ_WeaponUseSlot weaponUseSlot = TMJ_WeaponUseSlot.LightAttack;

    [Header("Daño")]
    [Tooltip("Daño base del ataque antes de sumar el bonus del arma. El arma modifica este daño, no lo reemplaza.")]
    [SerializeField, Min(0f)]
    private float baseDamage = 1f;

    [Header("Hitbox lógica")]
    [Tooltip("Distancia máxima del golpe. Pensalo como el alcance del ataque, no necesariamente como el largo exacto del modelo del arma.")]
    [SerializeField, Min(0.1f)]
    private float attackRange = 1.8f;

    [Tooltip("Apertura del golpe en grados. 90 pega al frente, 180 es medio círculo, 360 pega alrededor del jugador.")]
    [SerializeField, Range(20f, 360f)]
    private float attackArc = 95f;

    [Tooltip("Mueve el punto desde donde nace el golpe, pero por ahora el código solo lo usa para LeapSlash. X = derecha/izquierda local del jugador, Y = adelante/atrás. Útil para que el impacto salga más desde la mano que desde el centro del cuerpo.")]
    [SerializeField]
    private Vector2 impactOriginOffset = Vector2.zero;

    [Header("Timing")]
    [Tooltip("Cuánto espera desde que se acepta el ataque hasta que aparece el slash y se aplica el daño. Sirve para que el golpe coincida mejor con la animación. Ej: LeapSlash puede esperar más porque primero salta.")]
    [SerializeField, Min(0f)]
    private float impactDelaySeconds;

    [Tooltip("Tiempo mínimo antes de poder volver a usar este ataque. Importante: debería ser mayor o igual al momento de impacto si querés evitar ataques que se pisen demasiado.")]
    [SerializeField, Min(0.05f)]
    private float cooldown = 0.25f;

    [Header("Visual")]
    [Tooltip("Cuánto dura visible el slash. Es feedback visual, no decide cuánto dura la hitbox.")]
    [SerializeField, Min(0.01f)]
    private float visualDuration = 0.12f;

    [Tooltip("Grosor visual del slash. No cambia el área real de daño, solo cómo se ve.")]
    [SerializeField, Range(0.05f, 0.5f)]
    private float visualWidth = 0.18f;

    [Tooltip("Color del slash cuando este perfil se usa sin override de combo.")]
    [SerializeField]
    private Color slashColor = new Color(1f, 0.9f, 0.6f, 1f);

    public string DisplayName => displayName;
    public TMJ_WeaponUseSlot WeaponUseSlot => weaponUseSlot;
    public float BaseDamage => baseDamage;
    public float AttackRange => attackRange;
    public float AttackArc => attackArc;
    public Vector2 ImpactOriginOffset => impactOriginOffset;
    public float ImpactDelaySeconds => impactDelaySeconds;
    public float Cooldown => cooldown;
    public float VisualDuration => visualDuration;
    public float VisualWidth => visualWidth;
    public Color SlashColor => slashColor;

    private void OnValidate()
    {
        baseDamage = Mathf.Max(0f, baseDamage);
        attackRange = Mathf.Max(0.1f, attackRange);
        attackArc = Mathf.Clamp(attackArc, 20f, 360f);
        impactDelaySeconds = Mathf.Max(0f, impactDelaySeconds);
        cooldown = Mathf.Max(0.05f, cooldown);
        visualDuration = Mathf.Max(0.01f, visualDuration);
        visualWidth = Mathf.Clamp(visualWidth, 0.05f, 0.5f);
    }
}
