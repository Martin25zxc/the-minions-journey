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

    [Tooltip("Mueve el punto desde donde nace el golpe. X = derecha/izquierda local del jugador, Y = adelante/atrás según hacia dónde mira. Si queda en 0,0 no cambia nada; por ahora lo usaremos solo en el profile de LeapSlash por configuración, no por una excepción en código.")]
    [SerializeField]
    private Vector2 impactOriginOffset = Vector2.zero;

    [Header("Timing")]
    [Tooltip("Cuánto espera desde que se acepta el ataque hasta que aparece el slash y se aplica el daño. Sirve para que el golpe coincida mejor con la animación. Ej: LeapSlash puede esperar más porque primero salta.")]
    [SerializeField, Min(0f)]
    private float impactDelaySeconds;

    [Tooltip("Tiempo mínimo antes de poder volver a usar este ataque. Importante: debería ser mayor o igual al momento de impacto si querés evitar ataques que se pisen demasiado.")]
    [SerializeField, Min(0.05f)]
    private float cooldown = 0.25f;

    [Header("Impacto")]
    [Tooltip("Si está activo, este ataque además de hacer daño intenta empujar o aturdir. Dejalo apagado en ataques rápidos si no querés que sean demasiado seguros.")]
    [SerializeField]
    private bool applyImpact;

    [Tooltip("Cuánto empuja al enemigo. Usar valores chicos: esto es feedback/control de combate, no física realista.")]
    [SerializeField, Min(0f)]
    private float knockbackDistance;

    [Tooltip("Durante cuánto tiempo se reparte el empuje. Más bajo = golpe seco; más alto = empuje más flotado.")]
    [SerializeField, Min(0f)]
    private float knockbackDuration = 0.1f;

    [Tooltip("Cuánto tiempo el enemigo queda sin actuar. Empezá bajo porque el stun puede romper la dificultad muy rápido.")]
    [SerializeField, Min(0f)]
    private float stunDuration;

    [Tooltip("Reservado para enemigos futuros con casteos, channeling o habilidades cargadas. En esta etapa NO se usa para cortar ataques melee comunes.")]
    [SerializeField]
    private bool interruptCurrentAction;

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
    public bool ApplyImpact => applyImpact;
    public float KnockbackDistance => knockbackDistance;
    public float KnockbackDuration => knockbackDuration;
    public float StunDuration => stunDuration;
    public bool InterruptCurrentAction => interruptCurrentAction;
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
        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
        stunDuration = Mathf.Max(0f, stunDuration);
        visualDuration = Mathf.Max(0.01f, visualDuration);
        visualWidth = Mathf.Clamp(visualWidth, 0.05f, 0.5f);
    }
}