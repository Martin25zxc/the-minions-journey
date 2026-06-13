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

    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
    private static readonly int DodgeHash = Animator.StringToHash("Dodge");
    private static readonly int UseItemHash = Animator.StringToHash("UseItem");
    private static readonly int InteractHash = Animator.StringToHash("Interact");

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

    public void PlayHit()
    {
        animator?.SetTrigger(HitHash);
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
