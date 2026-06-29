using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-900)]
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer opcional")]
    [SerializeField, Tooltip("Opcional. Si está asignado, se intentan aplicar mute de Música/SFX por parámetros expuestos. Si no está o falla, se usa mute directo en AudioSource.")]
    private AudioMixer mixer;

    [SerializeField, Tooltip("Parámetro expuesto del AudioMixer para música. Se usa solo para mute runtime, no para persistencia.")]
    private string musicVolumeParameter = "MusicVolume";

    [SerializeField, Tooltip("Parámetro expuesto del AudioMixer para SFX. Se usa solo para mute runtime, no para persistencia.")]
    private string sfxVolumeParameter = "SFXVolume";

    [Header("Música")]
    [SerializeField, Tooltip("AudioSource dedicado para música. Si queda vacío, el manager intenta usar uno existente o crear uno automáticamente.")]
    private AudioSource musicSource;

    [SerializeField, Tooltip("Grupo de mixer opcional para la música.")]
    private AudioMixerGroup musicGroup;

    [SerializeField, Range(0f, 1f), Tooltip("Volumen runtime de música. No se persiste entre escenas ni partidas.")]
    private float musicVolume = 1f;

    [SerializeField, Tooltip("Estado inicial de mute para la escena actual. No se persiste.")]
    private bool startMusicMuted;

    [Header("SFX Pool")]
    [Tooltip("Cantidad de AudioSources en el pool. Si se reproducen más sonidos simultáneos que este número, los más viejos se reutilizan.")]
    [SerializeField, Min(4)]
    private int sfxPoolSize = 12;

    [SerializeField, Tooltip("Grupo de mixer opcional para SFX.")]
    private AudioMixerGroup sfxGroup;

    [SerializeField, Range(0f, 1f), Tooltip("Multiplicador runtime para todos los SFX. No se persiste.")]
    private float sfxMasterVolume = 1f;

    [SerializeField, Tooltip("Estado inicial de mute para SFX en la escena actual. No se persiste.")]
    private bool startSFXMuted;

    [Header("Debug")]
    [SerializeField]
    private bool logWarnings = true;

    private AudioSource[] sfxPool;
    private int poolIndex;
    private Coroutine musicFadeRoutine;

    public event Action<bool> MusicMutedChanged;
    public event Action<bool> SFXMutedChanged;
    public event Action<AudioClip> MusicChanged;

    public bool IsMusicMuted { get; private set; }
    public bool IsSFXMuted { get; private set; }
    public AudioClip CurrentMusicClip => musicSource != null ? musicSource.clip : null;
    public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Importante para TMJ MVP: AudioManager es local de escena.
        // No usar DontDestroyOnLoad; cada escena/partida debe arrancar limpia.

        EnsureMusicSource();
        BuildPool();

        IsMusicMuted = startMusicMuted;
        IsSFXMuted = startSFXMuted;

        ApplyMusicMuteState();
        ApplySFXMuteState();
    }

    private void OnDestroy()
    {
        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxMasterVolume = Mathf.Clamp01(sfxMasterVolume);
        sfxPoolSize = Mathf.Max(4, sfxPoolSize);
    }

    private void EnsureMusicSource()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        if (musicGroup != null)
        {
            musicSource.outputAudioMixerGroup = musicGroup;
        }
    }

    private void BuildPool()
    {
        sfxPool = new AudioSource[sfxPoolSize];

        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.volume = sfxMasterVolume;
            source.mute = IsSFXMuted;

            if (sfxGroup != null)
            {
                source.outputAudioMixerGroup = sfxGroup;
            }

            sfxPool[i] = source;
        }
    }

    public void PlaySFX(AudioDataSO data)
    {
        if (IsSFXMuted)
        {
            return;
        }

        if (data == null || !data.HasClips)
        {
            return;
        }

        if (sfxPool == null || sfxPool.Length == 0)
        {
            BuildPool();
        }

        AudioClip clip = data.GetClip();
        if (clip == null)
        {
            return;
        }

        AudioSource source = sfxPool[poolIndex % sfxPool.Length];
        poolIndex++;

        source.clip = clip;
        source.volume = data.Volume * sfxMasterVolume;
        source.pitch = data.RandomPitch;
        source.mute = IsSFXMuted;
        source.Play();
    }

    public void PlayMusic(AudioClip clip)
    {
        PlayMusic(clip, 0f);
    }

    public void PlayMusic(AudioClip clip, float fadeDuration)
    {
        if (clip == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning("AudioManager.PlayMusic recibió un AudioClip null.", this);
            }

            return;
        }

        EnsureMusicSource();

        if (musicSource.clip == clip)
        {
            if (!musicSource.isPlaying)
            {
                musicSource.loop = true;
                musicSource.Play();
            }

            ApplyMusicMuteState();
            return;
        }

        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }

        if (fadeDuration <= 0f || !musicSource.isPlaying || musicSource.clip == null)
        {
            SetMusicClipImmediate(clip);
            return;
        }

        musicFadeRoutine = StartCoroutine(FadeToMusicClip(clip, fadeDuration));
    }

    public void StopMusic()
    {
        StopMusic(0f);
    }

    public void StopMusic(float fadeDuration)
    {
        if (musicSource == null)
        {
            return;
        }

        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }

        if (fadeDuration <= 0f || !musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = null;
            MusicChanged?.Invoke(null);
            return;
        }

        musicFadeRoutine = StartCoroutine(FadeOutAndStop(fadeDuration));
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);

        if (musicSource != null && musicFadeRoutine == null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float value)
    {
        sfxMasterVolume = Mathf.Clamp01(value);

        if (sfxPool == null)
        {
            return;
        }

        for (int i = 0; i < sfxPool.Length; i++)
        {
            if (sfxPool[i] != null)
            {
                sfxPool[i].volume = sfxMasterVolume;
            }
        }
    }

    public void ToggleMusic()
    {
        SetMusicMuted(!IsMusicMuted);
    }

    // Compatibilidad con llamadas existentes: enabled true significa música encendida.
    public void ToggleMusic(bool enabled)
    {
        SetMusicMuted(!enabled);
    }

    public void ToggleSFX()
    {
        SetSFXMuted(!IsSFXMuted);
    }

    // Compatibilidad con llamadas existentes: enabled true significa SFX encendidos.
    public void ToggleSFX(bool enabled)
    {
        SetSFXMuted(!enabled);
    }

    public void SetMusicMuted(bool muted)
    {
        if (IsMusicMuted == muted)
        {
            ApplyMusicMuteState();
            return;
        }

        IsMusicMuted = muted;
        ApplyMusicMuteState();
        MusicMutedChanged?.Invoke(IsMusicMuted);
    }

    public void SetSFXMuted(bool muted)
    {
        if (IsSFXMuted == muted)
        {
            ApplySFXMuteState();
            return;
        }

        IsSFXMuted = muted;
        ApplySFXMuteState();
        SFXMutedChanged?.Invoke(IsSFXMuted);
    }

    private void SetMusicClipImmediate(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();
        ApplyMusicMuteState();
        MusicChanged?.Invoke(clip);
    }

    private IEnumerator FadeToMusicClip(AudioClip nextClip, float fadeDuration)
    {
        float halfDuration = Mathf.Max(0.01f, fadeDuration * 0.5f);
        float startVolume = musicSource.volume;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfDuration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.clip = nextClip;
        musicSource.loop = true;
        musicSource.Play();
        ApplyMusicMuteState();
        MusicChanged?.Invoke(nextClip);

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / halfDuration);
            yield return null;
        }

        musicSource.volume = musicVolume;
        musicFadeRoutine = null;
    }

    private IEnumerator FadeOutAndStop(float fadeDuration)
    {
        float duration = Mathf.Max(0.01f, fadeDuration);
        float startVolume = musicSource.volume;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = musicVolume;
        MusicChanged?.Invoke(null);
        musicFadeRoutine = null;
    }

    private void ApplyMusicMuteState()
    {
        if (musicSource != null)
        {
            musicSource.mute = IsMusicMuted;
        }

        TrySetMixerMute(musicVolumeParameter, IsMusicMuted);
    }

    private void ApplySFXMuteState()
    {
        if (sfxPool != null)
        {
            for (int i = 0; i < sfxPool.Length; i++)
            {
                if (sfxPool[i] != null)
                {
                    sfxPool[i].mute = IsSFXMuted;
                }
            }
        }

        TrySetMixerMute(sfxVolumeParameter, IsSFXMuted);
    }

    private bool TrySetMixerMute(string parameterName, bool muted)
    {
        if (mixer == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        // 0 dB = sin atenuación adicional. -80 dB = silencio práctico.
        bool applied = mixer.SetFloat(parameterName, muted ? -80f : 0f);

        if (!applied && logWarnings)
        {
            Debug.LogWarning($"AudioManager: no se pudo modificar el parámetro de mixer '{parameterName}'. Se usa fallback por AudioSource si corresponde.", this);
        }

        return applied;
    }
}
