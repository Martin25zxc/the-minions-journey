using UnityEngine;

[DisallowMultipleComponent]
public sealed class SceneMusicStarter : MonoBehaviour
{
    [Header("Música inicial de escena")]
    [SerializeField]
    private AudioClip initialMusic;

    [SerializeField, Min(0f)]
    private float fadeDuration = 0f;

    [SerializeField]
    private bool playOnStart = true;

    [SerializeField, Tooltip("Si no hay clip inicial, detiene la música actual de la escena. Normalmente debería quedar apagado.")]
    private bool stopMusicWhenClipIsNull;

    [Header("Debug")]
    [SerializeField]
    private bool logWarnings = true;

    private void Start()
    {
        if (playOnStart)
        {
            PlaySceneMusic();
        }
    }

    public void PlaySceneMusic()
    {
        if (AudioManager.Instance == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning($"{nameof(SceneMusicStarter)} no encontró AudioManager en la escena.", this);
            }

            return;
        }

        if (initialMusic != null)
        {
            AudioManager.Instance.PlayMusic(initialMusic, fadeDuration);
            return;
        }

        if (stopMusicWhenClipIsNull)
        {
            AudioManager.Instance.StopMusic(fadeDuration);
        }
    }
}
