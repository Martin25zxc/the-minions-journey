using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Combat/Enemy Melee Attack Profile", fileName = "EnemyMeleeAttackProfile_NewAttack")]
public sealed class EnemyMeleeAttackProfile : ScriptableObject
{
    [Header("Damage")]
    [SerializeField, Min(0f)]
    private float damage = 8f;

    [Tooltip("Distancia maxima desde el root del enemigo para considerar que puede iniciar el ataque.")]
    [SerializeField, Min(0.05f)]
    private float attackRange = 1.6f;

    [Tooltip("Radio del golpe logico en el momento de impacto. No es collider permanente; se evalua con OverlapSphere.")]
    [SerializeField, Min(0.05f)]
    private float hitRadius = 0.65f;

    [Tooltip("Distancia hacia adelante desde el enemigo donde se centra el golpe.")]
    [SerializeField, Min(0f)]
    private float hitForwardOffset = 1.0f;

    [SerializeField]
    private Vector3 hitOffset = Vector3.zero;

    [Tooltip("Layers que pueden recibir daño. Normalmente Player.")]
    [SerializeField]
    private LayerMask targetLayers = ~0;

    [Header("Timing - Timers por ScriptableObject")]
    [Tooltip("Tiempo entre iniciar la animacion y aplicar el daño.")]
    [SerializeField, Min(0f)]
    private float startupTime = 0.25f;

    [Tooltip("Ventana/pausa luego del impacto. En esta version el daño se aplica una sola vez, pero este tiempo ayuda a sincronizar con la animacion.")]
    [SerializeField, Min(0f)]
    private float activeTime = 0.2f;

    [Tooltip("Tiempo de recuperacion antes de que el enemigo pueda volver a moverse/decidir.")]
    [SerializeField, Min(0f)]
    private float recoveryTime = 0.45f;

    [SerializeField, Min(0f)]
    private float cooldown = 1.1f;

    [Header("Impact opcional")]
    [SerializeField]
    private bool applyImpact;

    [SerializeField, Min(0f)]
    private float knockbackDistance;

    [SerializeField, Min(0f)]
    private float knockbackDuration;

    [SerializeField, Min(0f)]
    private float stunDuration;

    [Tooltip("Preparado para futuro. En esta etapa no necesitamos usarlo para cortar acciones complejas.")]
    [SerializeField]
    private bool interruptCurrentAction;

    public float Damage => damage;
    public float AttackRange => attackRange;
    public float HitRadius => hitRadius;
    public float HitForwardOffset => hitForwardOffset;
    public Vector3 HitOffset => hitOffset;
    public LayerMask TargetLayers => targetLayers;

    public float StartupTime => startupTime;
    public float ActiveTime => activeTime;
    public float RecoveryTime => recoveryTime;
    public float Cooldown => cooldown;

    public bool ApplyImpact => applyImpact;
    public float KnockbackDistance => knockbackDistance;
    public float KnockbackDuration => knockbackDuration;
    public float StunDuration => stunDuration;
    public bool InterruptCurrentAction => interruptCurrentAction;

    public Vector3 GetHitCenter(Transform attacker)
    {
        if (attacker == null)
        {
            return hitOffset;
        }

        return attacker.position
            + attacker.forward * hitForwardOffset
            + attacker.right * hitOffset.x
            + Vector3.up * hitOffset.y
            + attacker.forward * hitOffset.z;
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0f, damage);
        attackRange = Mathf.Max(0.05f, attackRange);
        hitRadius = Mathf.Max(0.05f, hitRadius);
        hitForwardOffset = Mathf.Max(0f, hitForwardOffset);

        startupTime = Mathf.Max(0f, startupTime);
        activeTime = Mathf.Max(0f, activeTime);
        recoveryTime = Mathf.Max(0f, recoveryTime);
        cooldown = Mathf.Max(0f, cooldown);

        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
        stunDuration = Mathf.Max(0f, stunDuration);
    }
}
