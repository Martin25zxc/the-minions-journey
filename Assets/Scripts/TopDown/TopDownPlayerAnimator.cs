using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class TopDownPlayerAnimator : MonoBehaviour
{
    [SerializeField]
    Animator animator;

    [SerializeField, Min(0.01f)]
    float speedDampTime = 0.08f;

    [SerializeField, Min(0.01f)]
    float speedScale = 1f;

    Rigidbody body;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int LightAttackHash = Animator.StringToHash("LightAttack");
    static readonly int HeavyAttackHash = Animator.StringToHash("HeavyAttack");
    static readonly int ComboAttackHash = Animator.StringToHash("ComboAttack");
    static readonly int HitHash = Animator.StringToHash("Hit");
    static readonly int DieHash = Animator.StringToHash("Die");

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

        Vector3 velocity = body.linearVelocity;
        velocity.y = 0f;

        float speed = velocity.magnitude * speedScale;
        animator.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);
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
}