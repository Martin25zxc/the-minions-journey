using UnityEngine;

/// <summary>
/// Configuracion de la habilidad EnemyShieldThrowAbility.
///
/// Este profile define cuándo el enemigo puede lanzar el escudo,
/// cómo viaja el proyectil, cuánto daño hace en la ida/vuelta,
/// qué layers puede golpear, qué layers bloquean el recorrido
/// y cómo se comporta el impacto/knockback.
///
/// Importante:
/// - El visual real del escudo en la mano lo maneja EnemyShieldVisualController.
/// - La ejecucion concreta la maneja EnemyShieldThrowAbility.
/// - El movimiento/colisiones del proyectil lo maneja EnemyShieldProjectile.
/// </summary>
[CreateAssetMenu(menuName = "Enemies/Combat/Enemy Shield Throw Profile", fileName = "EnemyShieldThrowProfile_NewShieldThrow")]
public sealed class EnemyShieldThrowProfile : ScriptableObject
{
    [Header("Rango / Prioridad")]
    [Tooltip("Distancia minima al target para poder lanzar el escudo. Si el Player esta mas cerca que esto, esta habilidad no se usa.")]
    [SerializeField, Min(0f)]
    private float minAttackRange = 2.5f;

    [Tooltip("Distancia maxima al target para poder lanzar el escudo. Si el Player esta mas lejos que esto, esta habilidad no se usa.")]
    [SerializeField, Min(0.05f)]
    private float maxAttackRange = 8f;

    [Tooltip("Prioridad relativa frente a otras abilities. El EnemyBrain suele elegir la ability disponible con mayor prioridad.")]
    [SerializeField]
    private float priority = 65f;

    [Tooltip("Tiempo en segundos antes de que esta habilidad pueda volver a usarse despues de lanzar el escudo.")]
    [SerializeField, Min(0f)]
    private float cooldown = 5f;

    [Header("Timing")]
    [Tooltip("Tiempo antes de crear el proyectil. Sirve como windup/telegraph: el enemigo mira al Player y anticipa el lanzamiento.")]
    [SerializeField, Min(0f)]
    private float windupTime = 0.35f;

    [Tooltip("Tiempo posterior al lanzamiento antes de que la ability termine y el Brain pueda elegir otra accion. El escudo puede seguir viajando aunque la ability ya haya terminado.")]
    [SerializeField, Min(0f)]
    private float recoveryTime = 0.25f;

    [Header("Projectile")]
    [Tooltip("Prefab del proyectil de escudo. Debe tener el componente EnemyShieldProjectile.")]
    [SerializeField]
    private EnemyShieldProjectile projectilePrefab;

    [Tooltip("Offset local usado como punto de lanzamiento si la ability no tiene FirePoint asignado. Relativo al transform del enemigo.")]
    [SerializeField]
    private Vector3 firePointLocalOffset = new Vector3(0f, 0.9f, 0.7f);

    [Tooltip("Altura aproximada del punto al que apunta sobre el target. 0.7 suele apuntar al torso del Player.")]
    [SerializeField, Min(0f)]
    private float targetAimHeight = 0.7f;

    [Tooltip("Velocidad del escudo durante la fase de ida.")]
    [SerializeField, Min(0.01f)]
    private float outboundSpeed = 8f;

    [Tooltip("Velocidad del escudo durante la fase de vuelta.")]
    [SerializeField, Min(0.01f)]
    private float returnSpeed = 10f;

    [Tooltip("Distancia maxima que el escudo puede recorrer en la ida antes de empezar a volver.")]
    [SerializeField, Min(0.1f)]
    private float maxOutboundDistance = 8f;

    [Tooltip("Distancia al ReturnAnchor a partir de la cual el escudo se considera recuperado.")]
    [SerializeField, Min(0.05f)]
    private float returnArrivalDistance = 0.35f;

    [Tooltip("Radio usado por el SphereCast del proyectil para detectar impactos. Mas alto = mas facil acertar, pero puede sentirse injusto.")]
    [SerializeField, Min(0.05f)]
    private float hitRadius = 0.35f;

    [Tooltip("Tiempo maximo de vida del proyectil. Evita que el escudo quede perdido si algo falla.")]
    [SerializeField, Min(0.1f)]
    private float maxLifetime = 4f;

    [Header("Damage")]
    [Tooltip("Daño aplicado si el escudo golpea al target durante la ida.")]
    [SerializeField, Min(0f)]
    private float outboundDamage = 12f;

    [Tooltip("Daño aplicado si el escudo golpea al target durante la vuelta.")]
    [SerializeField, Min(0f)]
    private float returnDamage = 8f;

    [Tooltip("Si esta activo, el mismo target puede recibir daño una vez en la ida y otra vez en la vuelta. Si esta apagado, un target golpeado en la ida no recibe daño en la vuelta.")]
    [SerializeField]
    private bool canHitSameTargetOnReturn = true;

    [Tooltip("Tiempo minimo antes de que el mismo target pueda recibir daño en la vuelta despues de haber sido golpeado en la ida. Evita doble-hit instantaneo cuando el escudo cambia a Returning estando encima del Player.")]
    [SerializeField, Min(0f)]
    private float sameTargetReturnHitGraceTime = 0.2f;

    [Header("Layers")]
    [Tooltip("Layers que el escudo puede dañar. Para enemigos comunes deberia ser Player.")]
    [SerializeField]
    private LayerMask targetLayers;

    [Tooltip("Layers que bloquean el escudo. Recomendado: Obstacle, Boundary, Rock, LimitWall.")]
    [SerializeField]
    private LayerMask blockingLayers;

    [Header("Path")]
    [Tooltip("Si esta activo, la ability solo se usa si hay linea directa sin obstaculos entre el enemigo y el target al momento de lanzar.")]
    [SerializeField]
    private bool requireClearPathToTarget = true;

    [Tooltip("Si esta activo, cuando el escudo golpea un obstaculo desaparece y vuelve inmediatamente a la mano. Si esta apagado, empieza la fase de retorno desde el punto de impacto.")]
    [SerializeField]
    private bool snapBackOnBlockingHit = true;

    [Header("Impact / Knockback opcional")]
    [Tooltip("Si esta activo, ademas de daño, el escudo intenta aplicar ImpactReceiver al target.")]
    [SerializeField]
    private bool applyImpact;

    [Tooltip("Si esta activo, cada target solo puede recibir knockback/stun una vez por lanzamiento completo. El daño de ida y vuelta puede seguir aplicandose segun Damage.")]
    [SerializeField]
    private bool applyImpactOnlyOncePerTargetPerThrow = true;

    [Tooltip("Distancia de knockback aplicada si Apply Impact esta activo.")]
    [SerializeField, Min(0f)]
    private float knockbackDistance;

    [Tooltip("Duracion del desplazamiento de knockback si Apply Impact esta activo.")]
    [SerializeField, Min(0f)]
    private float knockbackDuration;

    [Tooltip("Duracion del stun si Apply Impact esta activo y el receptor lo soporta.")]
    [SerializeField, Min(0f)]
    private float stunDuration;

    [Tooltip("Si esta activo, el impacto puede interrumpir la accion actual del target, dependiendo de como este implementado su ImpactReceiver.")]
    [SerializeField]
    private bool interruptCurrentAction;

    public float MinAttackRange => minAttackRange;
    public float MaxAttackRange => maxAttackRange;
    public float Priority => priority;
    public float Cooldown => cooldown;

    public float WindupTime => windupTime;
    public float RecoveryTime => recoveryTime;

    public EnemyShieldProjectile ProjectilePrefab => projectilePrefab;
    public Vector3 FirePointLocalOffset => firePointLocalOffset;
    public float TargetAimHeight => targetAimHeight;
    public float OutboundSpeed => outboundSpeed;
    public float ReturnSpeed => returnSpeed;
    public float MaxOutboundDistance => maxOutboundDistance;
    public float ReturnArrivalDistance => returnArrivalDistance;
    public float HitRadius => hitRadius;
    public float MaxLifetime => maxLifetime;

    public float OutboundDamage => outboundDamage;
    public float ReturnDamage => returnDamage;
    public bool CanHitSameTargetOnReturn => canHitSameTargetOnReturn;
    public float SameTargetReturnHitGraceTime => sameTargetReturnHitGraceTime;

    public LayerMask TargetLayers => targetLayers;
    public LayerMask BlockingLayers => blockingLayers;

    public bool RequireClearPathToTarget => requireClearPathToTarget;
    public bool SnapBackOnBlockingHit => snapBackOnBlockingHit;

    public bool ApplyImpact => applyImpact;
    public bool ApplyImpactOnlyOncePerTargetPerThrow => applyImpactOnlyOncePerTargetPerThrow;
    public float KnockbackDistance => knockbackDistance;
    public float KnockbackDuration => knockbackDuration;
    public float StunDuration => stunDuration;
    public bool InterruptCurrentAction => interruptCurrentAction;

    /// <summary>
    /// Evalua si la distancia horizontal al target permite usar esta habilidad.
    /// No revisa cooldown, linea de vision ni disponibilidad del escudo visual.
    /// </summary>
    public bool IsDistanceInRange(float horizontalDistance)
    {
        return horizontalDistance >= minAttackRange && horizontalDistance <= maxAttackRange;
    }

    private void OnValidate()
    {
        minAttackRange = Mathf.Max(0f, minAttackRange);
        maxAttackRange = Mathf.Max(0.05f, maxAttackRange);

        if (minAttackRange > maxAttackRange)
        {
            minAttackRange = maxAttackRange;
        }

        cooldown = Mathf.Max(0f, cooldown);
        windupTime = Mathf.Max(0f, windupTime);
        recoveryTime = Mathf.Max(0f, recoveryTime);

        targetAimHeight = Mathf.Max(0f, targetAimHeight);
        outboundSpeed = Mathf.Max(0.01f, outboundSpeed);
        returnSpeed = Mathf.Max(0.01f, returnSpeed);
        maxOutboundDistance = Mathf.Max(0.1f, maxOutboundDistance);
        returnArrivalDistance = Mathf.Max(0.05f, returnArrivalDistance);
        hitRadius = Mathf.Max(0.05f, hitRadius);
        maxLifetime = Mathf.Max(0.1f, maxLifetime);

        outboundDamage = Mathf.Max(0f, outboundDamage);
        returnDamage = Mathf.Max(0f, returnDamage);
        sameTargetReturnHitGraceTime = Mathf.Max(0f, sameTargetReturnHitGraceTime);

        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
        stunDuration = Mathf.Max(0f, stunDuration);
    }
}
