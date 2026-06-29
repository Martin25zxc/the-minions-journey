using UnityEngine;

/// <summary>
/// Puente entre la IA del enemigo y el Animator Controller.
///
/// Contrato fijo para AC_Enemy_Humanoid_Base:
/// - MoveX      float   (locomotion estilo player / blend tree 2D)
/// - MoveY      float   (locomotion estilo player / blend tree 2D)
/// - MeleeAttack trigger
/// - LeapStart   trigger
/// - OnAir       bool
/// - LeapLand    trigger
/// - RangedAttack trigger
/// - HitFront   trigger
/// - HitBack    trigger
/// - Disengage   trigger
/// - ShieldThrow trigger
/// - IsDead      bool
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyAnimator : MonoBehaviour
{
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int MeleeAttackHash = Animator.StringToHash("MeleeAttack");
    private static readonly int LeapStartHash = Animator.StringToHash("LeapStart");
    private static readonly int OnAirHash = Animator.StringToHash("OnAir");
    private static readonly int LeapLandHash = Animator.StringToHash("LeapLand");
    private static readonly int RangedAttackHash = Animator.StringToHash("RangedAttack");
    private static readonly int HitFrontHash = Animator.StringToHash("HitFront");
    private static readonly int HitBackHash = Animator.StringToHash("HitBack");
    private static readonly int DisengageHash = Animator.StringToHash("Disengage");
    private static readonly int ShieldThrowHash = Animator.StringToHash("ShieldThrow");

    [Header("References")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private EnemyActor actor;

    [SerializeField]
    private EnemyMovement movement;

    [Header("Locomotion - Contrato tipo Player")]
    [Tooltip("Si esta activo, MoveY se envia en rango 0..1 usando EnemyDefinition.MoveSpeed. Si esta apagado, se envia velocidad real.")]
    [SerializeField]
    private bool sendNormalizedMoveY = true;

    [Tooltip("Para Enemy_MeleeLeap, normalmente MoveX queda en 0 porque el enemigo gira hacia el target y avanza hacia adelante.")]
    [SerializeField]
    private float fixedMoveX = 0f;

    [SerializeField, Min(0.01f)]
    private float locomotionDampTime = 0.08f;

    [Header("Hit Direction")]
    [Tooltip("Si la fuente del daño esta delante del enemigo, se dispara HitFront. Si esta detras, HitBack.")]
    [SerializeField]
    private bool useDirectionalHit = true;

    [Header("Debug")]
    [SerializeField]
    private bool warnMissingParameters = true;

    private bool hasShieldThrow;

    private bool hasDisengage;
    private bool hasMoveX;
    private bool hasMoveY;
    private bool hasMeleeAttack;
    private bool hasLeapStart;
    private bool hasOnAir;
    private bool hasLeapLand;
    private bool hasRangedAttack;
    private bool hasHitFront;
    private bool hasHitBack;
    private bool hasIsDead;
    private bool isDeadVisual;

    private void Awake()
    {
        ResolveReferences();
        CacheParameterAvailability();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (actor != null)
        {
            actor.Damaged += HandleDamaged;
            actor.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (actor != null)
        {
            actor.Damaged -= HandleDamaged;
            actor.Died -= HandleDied;
        }
    }

    private void Update()
    {
        UpdateLocomotion();
    }

    public void PlayMeleeAttack()
    {
        if (animator == null || !hasMeleeAttack)
        {
            return;
        }

        animator.ResetTrigger(MeleeAttackHash);
        animator.SetTrigger(MeleeAttackHash);
    }

    public void PlayLeapStart()
    {
        if (animator == null || !hasLeapStart)
        {
            return;
        }

        animator.ResetTrigger(LeapStartHash);
        animator.SetTrigger(LeapStartHash);
    }

    public void SetOnAir(bool value)
    {
        if (animator == null || !hasOnAir)
        {
            return;
        }

        animator.SetBool(OnAirHash, value);
    }

    public void PlayLeapLand()
    {
        if (animator == null)
        {
            return;
        }

        if (hasOnAir)
        {
            animator.SetBool(OnAirHash, false);
        }

        if (!hasLeapLand)
        {
            return;
        }

        animator.ResetTrigger(LeapLandHash);
        animator.SetTrigger(LeapLandHash);
    }


    public void PlayRangedAttack()
    {
        if (animator == null || !hasRangedAttack)
        {
            return;
        }

        animator.ResetTrigger(RangedAttackHash);
        animator.SetTrigger(RangedAttackHash);
    }

    public void PlayHitFromDamage(TMJ_DamageInfo damageInfo)
    {
        if (animator == null)
        {
            return;
        }

        // Si el golpe ya dejo al enemigo sin vida, Death debe tener prioridad.
        if (actor != null && (actor.HasDied || actor.Health == null || actor.Health.CurrentHealth <= 0f))
        {
            return;
        }

        bool hitFromFront = !useDirectionalHit || IsDamageSourceInFront(damageInfo);

        if (hitFromFront)
        {
            if (hasHitFront)
            {
                animator.ResetTrigger(HitFrontHash);
                animator.SetTrigger(HitFrontHash);
            }

            return;
        }

        if (hasHitBack)
        {
            animator.ResetTrigger(HitBackHash);
            animator.SetTrigger(HitBackHash);
        }
    }

    public void PlayDeath()
    {
        if (animator == null)
        {
            return;
        }

        isDeadVisual = true;

        ResetActionTriggersForDeath();
        SetLocomotionValuesImmediate(0f, 0f);
        SetOnAir(false);

        if (hasIsDead)
        {
            animator.SetBool(IsDeadHash, true);
            return;
        }
    }

    public void PlayDisengage()
    {
        if (animator == null || !hasDisengage)
        {
            return;
        }

        animator.ResetTrigger(DisengageHash);
        animator.SetTrigger(DisengageHash);
    }

    public void PlayShieldThrow()
    {
        if (animator == null || !hasShieldThrow)
        {
            return;
        }

        animator.ResetTrigger(ShieldThrowHash);
        animator.SetTrigger(ShieldThrowHash);
    }
    
    private void UpdateLocomotion()
    {
        if (animator == null)
        {
            return;
        }

        if (actor != null && !actor.IsAlive)
        {
            SetLocomotionValues(0f, 0f);
            return;
        }

        float moveY = 0f;
        if (movement != null)
        {
            moveY = movement.DesiredVelocity.magnitude;
        }

        if (sendNormalizedMoveY)
        {
            float maxSpeed = actor != null && actor.Definition != null
                ? actor.Definition.MoveSpeed
                : Mathf.Max(moveY, 0.0001f);

            moveY = maxSpeed > 0f ? Mathf.Clamp01(moveY / maxSpeed) : 0f;
        }

        // Contrato tipo player: el enemigo melee no usa strafe por ahora.
        // Gira hacia el target desde EnemyMovement y alimenta el blend tree como avance frontal.
        SetLocomotionValues(fixedMoveX, moveY);
    }

    private void SetLocomotionValues(float moveX, float moveY)
    {
        if (hasMoveX)
        {
            animator.SetFloat(MoveXHash, moveX, locomotionDampTime, Time.deltaTime);
        }

        if (hasMoveY)
        {
            animator.SetFloat(MoveYHash, moveY, locomotionDampTime, Time.deltaTime);
        }
    }

    private bool IsDamageSourceInFront(TMJ_DamageInfo damageInfo)
    {
        return TMJ_DamageReactionUtility.IsDamageSourceInFront(damageInfo, transform, 0f);
    }

    private void HandleDamaged(EnemyActor damagedActor, TMJ_DamageInfo damageInfo)
    {
        PlayHitFromDamage(damageInfo);
    }

    private void HandleDied(EnemyActor deadActor)
    {
        PlayDeath();
    }

    private void ResolveReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (actor == null)
        {
            actor = GetComponent<EnemyActor>();
        }

        if (movement == null)
        {
            movement = GetComponent<EnemyMovement>();
        }
    }

    private void CacheParameterAvailability()
    {
        if (animator == null)
        {
            Debug.LogWarning($"[{nameof(EnemyAnimator)}] {name} has no Animator assigned/found in children.", this);
            return;
        }

        hasMoveX = HasParameter(MoveXHash, AnimatorControllerParameterType.Float, "MoveX");
        hasMoveY = HasParameter(MoveYHash, AnimatorControllerParameterType.Float, "MoveY");
        hasMeleeAttack = HasParameter(MeleeAttackHash, AnimatorControllerParameterType.Trigger, "MeleeAttack");
        hasLeapStart = HasParameter(LeapStartHash, AnimatorControllerParameterType.Trigger, "LeapStart");
        hasOnAir = HasParameter(OnAirHash, AnimatorControllerParameterType.Bool, "OnAir");
        hasLeapLand = HasParameter(LeapLandHash, AnimatorControllerParameterType.Trigger, "LeapLand");
        hasRangedAttack = HasParameter(RangedAttackHash, AnimatorControllerParameterType.Trigger, "RangedAttack");
        hasHitFront = HasParameter(HitFrontHash, AnimatorControllerParameterType.Trigger, "HitFront");
        hasHitBack = HasParameter(HitBackHash, AnimatorControllerParameterType.Trigger, "HitBack");
        hasDisengage = HasParameter(DisengageHash, AnimatorControllerParameterType.Trigger, "Disengage");
        hasShieldThrow = HasParameter(ShieldThrowHash, AnimatorControllerParameterType.Trigger, "ShieldThrow");
        hasIsDead = HasParameter(IsDeadHash, AnimatorControllerParameterType.Bool, "IsDead");
    }

    private bool HasParameter(int hash, AnimatorControllerParameterType expectedType, string parameterName)
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.nameHash != hash)
            {
                continue;
            }

            if (parameter.type == expectedType)
            {
                return true;
            }

            if (warnMissingParameters)
            {
                Debug.LogWarning(
                    $"[{nameof(EnemyAnimator)}] Parameter '{parameterName}' exists in {animator.runtimeAnimatorController?.name}, " +
                    $"but type is {parameter.type} instead of {expectedType}.",
                    this);
            }

            return false;
        }

        if (warnMissingParameters)
        {
            Debug.LogWarning(
                $"[{nameof(EnemyAnimator)}] Missing Animator parameter '{parameterName}' ({expectedType}) in " +
                $"{animator.runtimeAnimatorController?.name}. AC_Enemy_Humanoid_Base must follow the fixed enemy contract.",
                this);
        }

        return false;
    }

    private void ResetActionTriggersForDeath()
    {
        if (animator == null)
        {
            return;
        }

        if (hasMeleeAttack)
        {
            animator.ResetTrigger(MeleeAttackHash);
        }

        if (hasLeapStart)
        {
            animator.ResetTrigger(LeapStartHash);
        }

        if (hasLeapLand)
        {
            animator.ResetTrigger(LeapLandHash);
        }

        if (hasRangedAttack)
        {
            animator.ResetTrigger(RangedAttackHash);
        }

        if (hasHitFront)
        {
            animator.ResetTrigger(HitFrontHash);
        }

        if (hasHitBack)
        {
            animator.ResetTrigger(HitBackHash);
        }

        if (hasDisengage)
        {
            animator.ResetTrigger(DisengageHash);
        }

        if (hasShieldThrow)
        {
            animator.ResetTrigger(ShieldThrowHash);
        }
    }

    private void SetLocomotionValuesImmediate(float moveX, float moveY)
    {
        if (animator == null)
        {
            return;
        }

        if (hasMoveX)
        {
            animator.SetFloat(MoveXHash, moveX);
        }

        if (hasMoveY)
        {
            animator.SetFloat(MoveYHash, moveY);
        }
    }

    private bool IsDeathLocked()
    {
        return isDeadVisual || actor != null && !actor.IsAlive;
    }
    private void OnValidate()
    {
        locomotionDampTime = Mathf.Max(0.01f, locomotionDampTime);
        fixedMoveX = Mathf.Clamp(fixedMoveX, -1f, 1f);
    }
}
