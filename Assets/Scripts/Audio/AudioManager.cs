using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-900)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("Música")]
    [SerializeField] private AudioSource musicSource;

    [Header("SFX Pool")]
    [Tooltip("Cantidad de AudioSources en el pool. Si se reproducen más sonidos simultáneos que este número, los más viejos se cortan.")]
    [SerializeField, Min(4)] private int sfxPoolSize = 12;
    [SerializeField] private AudioMixerGroup sfxGroup;

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
            _sfxPool[i].outputAudioMixerGroup = sfxGroup;
        }
    }

    public void PlaySFX(AudioDataSO data)
    {
        if (data == null || !data.HasClips) return;

        AudioSource src = _sfxPool[_poolIndex % sfxPoolSize];
        _poolIndex++;

        src.clip = data.GetClip();
        src.volume = data.Volume;
        src.pitch = data.RandomPitch;
        src.Play();
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();

    public void SetSFXVolume(float value)
    {
        mixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20);
    }

    public void SetMusicVolume(float value)
    {
        mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20);
    }
}
