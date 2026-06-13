using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class TopDownPlayerAnimator : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [Header("Locomotion")]
    [SerializeField, Min(0.01f)]
    private float locomotionBlendSpeed = 5f;

    [SerializeField, Min(0.01f)]
    private float moveDampTime = 0.08f;

    [Header("Optional")]
    [SerializeField, Min(0.01f)]
    private float speedDampTime = 0.08f;

    private Rigidbody body;

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

    private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
    private static readonly int DodgeHash = Animator.StringToHash("Dodge");
    private static readonly int UseItemHash = Animator.StringToHash("UseItem");
    private static readonly int InteractHash = Animator.StringToHash("Interact");

    [Header("Hit Reaction")]
    [SerializeField]
    private bool ignoreHitAnimationWhileAttacking = true;

    [SerializeField, Range(-1f, 1f)]
    private float frontBackThreshold = 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (animator == null || body == null)
        {
            return;
        }

        Vector3 worldVelocity = body.linearVelocity;
        worldVelocity.y = 0f;

        float speed = worldVelocity.magnitude;
        animator.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);

        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        float moveX = Mathf.Clamp(localVelocity.x / locomotionBlendSpeed, -1f, 1f);
        float moveY = Mathf.Clamp(localVelocity.z / locomotionBlendSpeed, -1f, 1f);

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

        Vector3 directionToSource = damageInfo.SourcePosition - transform.position;
        directionToSource.y = 0f;

        if (directionToSource.sqrMagnitude < 0.0001f)
        {
            PlayHitFront();
            return;
        }

        directionToSource.Normalize();

        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            PlayHitFront();
            return;
        }

        forward.Normalize();

        float dot = Vector3.Dot(forward, directionToSource);

        if (dot >= frontBackThreshold)
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

        animator.ResetTrigger(HitBackHash);
        animator.SetTrigger(HitFrontHash);
    }

    public void PlayHitBack()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(HitFrontHash);
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

        if (IsCurrentOrNextStateTagged("Hit"))
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
        animator?.SetTrigger(DieHash);
    }

    public void SetBlocking(bool isBlocking)
    {
        animator?.SetBool(IsBlockingHash, isBlocking);
    }

    public void PlayDodge()
    {
        animator?.SetTrigger(DodgeHash);
    }

    public void PlayUseItem()
    {
        animator?.SetTrigger(UseItemHash);
    }

    public void PlayInteract()
    {
        animator?.SetTrigger(InteractHash);
    }
}
