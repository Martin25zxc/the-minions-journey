using UnityEngine;

/// <summary>
/// Singleton de audio. Reproduce SFX mediante un pool de AudioSources para
/// evitar crear/destruir componentes en runtime. La música corre en su propio
/// AudioSource separado para poder controlar volumen independientemente.
/// </summary>
[DefaultExecutionOrder(-900)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Música")]
    [SerializeField] private AudioSource musicSource;

    [Header("SFX Pool")]
    [Tooltip("Cantidad de AudioSources en el pool. Si se reproducen más sonidos simultáneos que este número, los más viejos se cortan.")]
    [SerializeField, Min(4)] private int sfxPoolSize = 12;

    [Header("Volumen global")]
    [SerializeField, Range(0f, 1f)] private float masterSfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float masterMusicVolume = 0.7f;

    private AudioSource[] _sfxPool;
    private int _poolIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildPool();
    }

    private void BuildPool()
    {
        _sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            _sfxPool[i] = gameObject.AddComponent<AudioSource>();
            _sfxPool[i].playOnAwake = false;
        }
    }

    public void PlaySFX(AudioDataSO data)
    {
        if (data == null || !data.HasClips) return;

        AudioSource src = _sfxPool[_poolIndex % sfxPoolSize];
        _poolIndex++;

        src.clip = data.GetClip();
        src.volume = data.Volume * masterSfxVolume;
        src.pitch = data.RandomPitch;
        src.Play();
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = masterMusicVolume;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();

    public void SetSFXVolume(float value)
    {
        masterSfxVolume = Mathf.Clamp01(value);
    }

    public void SetMusicVolume(float value)
    {
        masterMusicVolume = Mathf.Clamp01(value);
        ApplyMusicVolume();
    }

    private void ApplyMusicVolume()
    {
        if (musicSource != null) musicSource.volume = masterMusicVolume;
    }
}
