using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TopDownHealth))]
public sealed class TopDownPlayerAnimator : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [Header("Health Events")]
    [Tooltip("Optional explicit reference. If empty, the component is searched on the player root. Used to bridge damage/death events into hit/death animations.")]
    [SerializeField]
    private TopDownHealth health;

    [Header("Locomotion")]
    [SerializeField, Min(0.01f)]
    private float locomotionBlendSpeed = 5f;

    [SerializeField, Min(0.01f)]
    private float moveDampTime = 0.08f;

    [Header("Optional")]
    [SerializeField, Min(0.01f)]
    private float speedDampTime = 0.08f;

    [SerializeField, Min(0f)]
    private float movementDeadZone = 0.08f;

    private Rigidbody body;
    private bool isSubscribedToHealth;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");

    private static readonly int LightAttackHash = Animator.StringToHash("LightAttack");
    private static readonly int HeavyAttackHash = Animator.StringToHash("HeavyAttack");
    private static readonly int LeapSlashAttackHash = Animator.StringToHash("LeapSlashAttack");
    private static readonly int SpinSlashAttackHash = Animator.StringToHash("SpinSlashAttack");

    private static readonly int HitFrontHash = Animator.StringToHash("HitFront");
    private static readonly int HitBackHash = Animator.StringToHash("HitBack");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int InteractHash = Animator.StringToHash("Interact");
    
    private static readonly int HookLaunchHash = Animator.StringToHash("HookLaunch");

    [Header("Hit Reaction")]
    [SerializeField]
    private bool ignoreHitAnimationWhileAttacking = true;

    [SerializeField, Range(-1f, 1f)]
    private float frontBackThreshold = 0f;

    [Tooltip("Tiempo minimo entre reacciones visuales de hit. Evita spam, pero permite repetir HitFront/HitBack si el jugador recibe varios golpes.")]
    [SerializeField, Min(0f)]
    private float hitAnimationMinInterval = 0.15f;

    [Header("Respawn Animation")]
    [Tooltip("Nombre exacto del estado base al que vuelve el Animator después de Lie_StandUp.")]
    [SerializeField]
    private string locomotionStateName = "Locomotion";

    [Tooltip("Nombre exacto del estado que usa el clip lie stand up.")]
    [SerializeField]
    private string standUpStateName = "Lie_StandUp";

    [Tooltip("Duración del clip Lie_StandUp. El clip encontrado dura 2.333 segundos.")]
    [SerializeField, Min(0f)]
    private float standUpDuration = 2.333f;

    [Tooltip("Transición corta hacia Lie_StandUp al revivir.")]
    [SerializeField, Min(0f)]
    private float standUpCrossFadeDuration = 0.05f;

    [Tooltip("Transición corta hacia Locomotion al terminar Lie_StandUp.")]
    [SerializeField, Min(0f)]
    private float locomotionCrossFadeDuration = 0.05f;

    [Tooltip("Layer del Animator donde están Locomotion, Death y Lie_StandUp.")]
    [SerializeField]
    private int baseLayerIndex = 0;

    [Header("Respawn Events")]
    [Tooltip("Se invoca cuando empieza la animación Lie_StandUp.")]
    [SerializeField]
    private UnityEvent onRespawnStandUpStarted;

    [Tooltip("Se invoca cuando termina Lie_StandUp y el Animator vuelve a Locomotion.")]
    [SerializeField]
    private UnityEvent onRespawnStandUpCompleted;

    private float lastHitAnimationTime = -999f;
    private Coroutine standUpCoroutine;

    public bool IsPlayingRespawnStandUp => standUpCoroutine != null;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (health == null)
        {
            health = GetComponent<TopDownHealth>();
        }
    }

    private void OnEnable()
    {
        SubscribeToHealthEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromHealthEvents();

        if (standUpCoroutine != null)
        {
            StopCoroutine(standUpCoroutine);
            standUpCoroutine = null;
        }
    }

    private void SubscribeToHealthEvents()
    {
        if (isSubscribedToHealth)
        {
            return;
        }

        if (health == null)
        {
            health = GetComponent<TopDownHealth>();
        }

        if (health == null)
        {
            Debug.LogWarning($"{name} has no TopDownHealth assigned to TopDownPlayerAnimator.", this);
            return;
        }

        health.OnDamaged += HandleDamaged;
        health.OnDied += HandleDied;
        isSubscribedToHealth = true;
    }

    private void UnsubscribeFromHealthEvents()
    {
        if (!isSubscribedToHealth || health == null)
        {
            return;
        }

        health.OnDamaged -= HandleDamaged;
        health.OnDied -= HandleDied;
        isSubscribedToHealth = false;
    }

    private void HandleDamaged(TMJ_DamageInfo damageInfo)
    {
        // TopDownHealth invokes OnDamaged before OnDied, but CurrentHealth has already been reduced.
        // This guard prevents a lethal hit from briefly playing a hit reaction before the death animation.
        if (health == null || !health.IsAlive)
        {
            return;
        }

        PlayHit(damageInfo);
    }

    private void HandleDied()
    {
        PlayDeath();
    }

    private void Update()
    {
        if (animator == null || body == null)
        {
            return;
        }

        if (standUpCoroutine != null)
        {
            animator.SetFloat(SpeedHash, 0f);
            animator.SetFloat(MoveXHash, 0f);
            animator.SetFloat(MoveYHash, 0f);
            return;
        }

        Vector3 worldVelocity = body.linearVelocity;
        worldVelocity.y = 0f;

        float deadZone = movementDeadZone;
        float deadZoneSqr = deadZone * deadZone;

        if (worldVelocity.sqrMagnitude < deadZoneSqr)
        {
            animator.SetFloat(SpeedHash, 0f);
            animator.SetFloat(MoveXHash, 0f);
            animator.SetFloat(MoveYHash, 0f);
            return;
        }

        float speed = worldVelocity.magnitude;
        animator.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);

        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        float moveX = Mathf.Clamp(localVelocity.x / locomotionBlendSpeed, -1f, 1f);
        float moveY = Mathf.Clamp(localVelocity.z / locomotionBlendSpeed, -1f, 1f);

        if (Mathf.Abs(moveX) < 0.001f)
        {
            moveX = 0f;
        }

        if (Mathf.Abs(moveY) < 0.001f)
        {
            moveY = 0f;
        }

        animator.SetFloat(MoveXHash, moveX, moveDampTime, Time.deltaTime);
        animator.SetFloat(MoveYHash, moveY, moveDampTime, Time.deltaTime);
    }

    public void PlayLightAttack()
    {
        animator?.SetTrigger(LightAttackHash);
    }

    public void PlayHeavyAttack()
    {
        animator?.SetTrigger(HeavyAttackHash);
    }

    public void PlayHookLaunch()
    {
        animator?.SetTrigger(HookLaunchHash);
    }
    
    public void PlayCombo(PlayerComboAnimationCue animationCue)
    {
        switch (animationCue)
        {
            case PlayerComboAnimationCue.LeapSlash:
                PlayLeapSlashAttack();
                break;

            case PlayerComboAnimationCue.SpinSlash:
                PlaySpinSlashAttack();
                break;

            case PlayerComboAnimationCue.None:
            default:
                break;
        }
    }

    public void PlayLeapSlashAttack()
    {
        animator?.SetTrigger(LeapSlashAttackHash);
    }

    public void PlaySpinSlashAttack()
    {
        animator?.SetTrigger(SpinSlashAttackHash);
    }

    public void PlayHit(TMJ_DamageInfo damageInfo)
    {
        if (animator == null)
        {
            return;
        }

        if (ShouldIgnoreHitAnimation())
        {
            return;
        }

        bool hitFromFront = TMJ_DamageReactionUtility.IsDamageSourceInFront(
            damageInfo,
            transform,
            frontBackThreshold);

        if (hitFromFront)
        {
            PlayHitFront();
        }
        else
        {
            PlayHitBack();
        }
    }

    public void PlayHitFront()
    {
        if (animator == null)
        {
            return;
        }

        lastHitAnimationTime = Time.time;
        animator.ResetTrigger(HitFrontHash);
        animator.ResetTrigger(HitBackHash);
        animator.SetTrigger(HitFrontHash);
    }

    public void PlayHitBack()
    {
        if (animator == null)
        {
            return;
        }

        lastHitAnimationTime = Time.time;
        animator.ResetTrigger(HitFrontHash);
        animator.ResetTrigger(HitBackHash);
        animator.SetTrigger(HitBackHash);
    }

    private bool ShouldIgnoreHitAnimation()
    {
        if (animator == null)
        {
            return true;
        }

        if (IsCurrentOrNextStateTagged("Death"))
        {
            return true;
        }

        if (Time.time - lastHitAnimationTime < hitAnimationMinInterval)
        {
            return true;
        }

        if (ignoreHitAnimationWhileAttacking && IsCurrentOrNextStateTagged("Attack"))
        {
            return true;
        }

        return false;
    }

    private bool IsCurrentOrNextStateTagged(string tag)
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (currentState.IsTag(tag))
        {
            return true;
        }

        if (!animator.IsInTransition(0))
        {
            return false;
        }

        AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
        return nextState.IsTag(tag);
    }

    public void PlayDeath()
    {
        if (animator == null)
        {
            return;
        }

        if (standUpCoroutine != null)
        {
            StopCoroutine(standUpCoroutine);
            standUpCoroutine = null;
        }

        animator.ResetTrigger(HitFrontHash);
        animator.ResetTrigger(HitBackHash);
        animator.SetTrigger(DieHash);
    }

    public void PlayInteract()
    {
        animator?.SetTrigger(InteractHash);
    }

    public void SetBlocking(bool isBlocking)
    {
        SetBoolIfExists("IsBlocking", isBlocking);
    }

    public void PlayDodge()
    {
        SetTriggerIfExists("Dodge");
    }

    public void PlayUseItem()
    {
        SetTriggerIfExists("UseItem");
    }

    public void PlayRespawnStandUp()
    {
        if (animator == null)
        {
            return;
        }

        if (standUpCoroutine != null)
        {
            StopCoroutine(standUpCoroutine);
        }

        standUpCoroutine = StartCoroutine(PlayRespawnStandUpRoutine());
    }

    private IEnumerator PlayRespawnStandUpRoutine()
    {
        ClearRespawnAnimatorParameters();

        CrossFadeStateIfExists(standUpStateName, standUpCrossFadeDuration);
        onRespawnStandUpStarted?.Invoke();

        if (standUpDuration > 0f)
        {
            yield return new WaitForSeconds(standUpDuration);
        }

        ClearRespawnAnimatorParameters();
        CrossFadeStateIfExists(locomotionStateName, locomotionCrossFadeDuration);

        standUpCoroutine = null;
        onRespawnStandUpCompleted?.Invoke();
    }

    private void ClearRespawnAnimatorParameters()
    {
        animator.speed = 1f;

        animator.ResetTrigger(LightAttackHash);
        animator.ResetTrigger(HeavyAttackHash);
        animator.ResetTrigger(LeapSlashAttackHash);
        animator.ResetTrigger(SpinSlashAttackHash);
        animator.ResetTrigger(HitFrontHash);
        animator.ResetTrigger(HitBackHash);
        animator.ResetTrigger(DieHash);
        animator.ResetTrigger(InteractHash);

        animator.SetFloat(SpeedHash, 0f);
        animator.SetFloat(MoveXHash, 0f);
        animator.SetFloat(MoveYHash, 0f);
    }

    private void CrossFadeStateIfExists(string stateName, float duration)
    {
        if (string.IsNullOrWhiteSpace(stateName) || animator == null)
        {
            return;
        }

        int stateHash = Animator.StringToHash(stateName);

        if (!animator.HasState(baseLayerIndex, stateHash))
        {
            Debug.LogWarning($"Animator state '{stateName}' does not exist on layer {baseLayerIndex}.", this);
            return;
        }

        animator.CrossFadeInFixedTime(stateHash, duration, baseLayerIndex);
        animator.Update(0f);
    }

    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (string.IsNullOrWhiteSpace(parameterName) || animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool &&
                parameter.name == parameterName)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }

    private void SetTriggerIfExists(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName) || animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger &&
                parameter.name == parameterName)
            {
                animator.SetTrigger(parameterName);
                return;
            }
        }
    }
}
