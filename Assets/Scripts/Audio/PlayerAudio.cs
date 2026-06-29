using UnityEngine;

public sealed class PlayerAudio : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private AudioDataSO footstepData;

    [Tooltip("Velocidad mínima del Rigidbody para reproducir paso. Evita sonido durante transiciones de salida de animación.")]
    [SerializeField, Min(0f)] private float minSpeedThreshold = 0.5f;

    [Header("Ataques")]
    [SerializeField] private AudioDataSO lightAttackData;
    [SerializeField] private AudioDataSO heavyAttackData;
    [SerializeField] private AudioDataSO leapSlashData;
    [SerializeField] private AudioDataSO spinSlashData;

    [Header("Hit")]
    [SerializeField] private AudioDataSO hitFront;
    [SerializeField] private AudioDataSO hitBack;

    private Animator animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Animation Events
    public void OnFootstep()
    {
        if (animator != null && animator.GetFloat(SpeedHash) < minSpeedThreshold)
        {
            return;
        }

        PlaySound(footstepData);
    }

    public void OnMeleeAttack() => PlaySound(lightAttackData);
    public void OnHeavyAttack() => PlaySound(heavyAttackData);
    public void OnLeapSlash() => PlaySound(leapSlashData);
    public void OnSpinSlash() => PlaySound(spinSlashData);
    public void OnHitBack() => PlaySound(hitBack);
    public void OnHitFront() => PlaySound(hitFront);

    private void PlaySound(AudioDataSO data)
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySFX(data);
    }
}
