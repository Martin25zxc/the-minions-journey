using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-900)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    public enum MusicState { None, Ambient, Combat }

    [Header("Música")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip ambientTrack;
    [SerializeField] private AudioClip combatTrack;

    private MusicState _currentState = MusicState.None;

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

    public void SetMusicState(MusicState state)
    {
        if (state == _currentState) return;
        _currentState = state;

        AudioClip clip = state switch
        {
            MusicState.Ambient => ambientTrack,
            MusicState.Combat  => combatTrack,
            _                  => null
        };

        if (clip != null) PlayMusic(clip);
        else StopMusic();
    }

    private float _lastMusicVolume = 1f;
    private float _lastSFXVolume = 1f;

    public void SetSFXVolume(float value)
    {
        _lastSFXVolume = value;
        mixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20);
    }

    public void SetMusicVolume(float value)
    {
        _lastMusicVolume = value;
        mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20);
    }

    public void ToggleMusic(bool enabled)
    {
        if (enabled) SetMusicVolume(_lastMusicVolume);
        else mixer.SetFloat("MusicVolume", -80f);
    }

    public void ToggleSFX(bool enabled)
    {
        if (enabled) SetSFXVolume(_lastSFXVolume);
        else mixer.SetFloat("SFXVolume", -80f);
    }
}
