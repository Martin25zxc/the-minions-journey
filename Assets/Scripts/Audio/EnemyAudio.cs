using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class EnemyAudio : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private AudioDataSO footstepData;

    [Tooltip("MoveY mínimo para reproducir paso. Evita sonido en transiciones de salida de animación.")]
    [SerializeField, Min(0f)] private float minMoveYThreshold = 0.05f;

    [Header("Ataques")]
    [SerializeField] private AudioDataSO meleeAttackData;
    [SerializeField] private AudioDataSO leapStartData;
    [SerializeField] private AudioDataSO leapLandData;
    [SerializeField] private AudioDataSO rangedAttackData;
    
    [Header("Hit")]
    [SerializeField] private AudioDataSO hitFront;
    [SerializeField] private AudioDataSO hitBack;

    private AudioSource _audioSource;
    private Animator _animator;
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _animator = GetComponentInChildren<Animator>();
    }

    // Animation Events
    public void OnFootstep()
    {
        if (_animator != null && _animator.GetFloat(MoveYHash) < minMoveYThreshold) return;
        PlaySound(footstepData);
    }

    public void OnMeleeAttack() => PlaySound(meleeAttackData);
    public void OnLeapStart()   => PlaySound(leapStartData);
    public void OnLeapLand()    => PlaySound(leapLandData);
    public void OnRangedAttack() => PlaySound(rangedAttackData);

     public void OnHitBack() => PlaySound(hitBack);
    public void OnHitFront()=>  PlaySound(hitFront);

    private void PlaySound(AudioDataSO data)
    {
        if (data == null || !data.HasClips) return;
        _audioSource.pitch = data.RandomPitch;
        _audioSource.PlayOneShot(data.GetClip(), data.Volume);
    }
}
