using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class TopDownPlayerAnimator : MonoBehaviour
{
    [SerializeField]
    Animator animator;

    [Header("Locomotion")]
    [SerializeField, Min(0.01f)]
    float locomotionBlendSpeed = 5f;

    [SerializeField, Min(0.01f)]
    float moveDampTime = 0.08f;

    [Header("Optional")]
    [SerializeField, Min(0.01f)]
    float speedDampTime = 0.08f;

    Rigidbody body;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int MoveXHash = Animator.StringToHash("MoveX");
    static readonly int MoveYHash = Animator.StringToHash("MoveY");

    static readonly int LightAttackHash = Animator.StringToHash("LightAttack");
    static readonly int HeavyAttackHash = Animator.StringToHash("HeavyAttack");
    static readonly int ComboAttackHash = Animator.StringToHash("ComboAttack");

    static readonly int HitHash = Animator.StringToHash("Hit");
    static readonly int DieHash = Animator.StringToHash("Die");

    static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
    static readonly int DodgeHash = Animator.StringToHash("Dodge");
    static readonly int UseItemHash = Animator.StringToHash("UseItem");
    static readonly int InteractHash = Animator.StringToHash("Interact");

    void Awake()
    {
        body = GetComponent<Rigidbody>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
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

    public void PlayComboAttack()
    {
        animator?.SetTrigger(ComboAttackHash);
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