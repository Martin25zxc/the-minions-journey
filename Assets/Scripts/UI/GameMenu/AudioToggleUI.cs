using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI simple para prender/apagar música y SFX en la escena actual.
/// Uso esperado con Buttons:
/// PauseMenuRoot/Panel
/// ├── MusicButton
/// │   └── Text (TMP)
/// └── SFXButton
///     └── Text (TMP)
/// </summary>
[DisallowMultipleComponent]
public sealed class AudioToggleUI : MonoBehaviour
{
    [Header("Referencias - Buttons")]
    [SerializeField, Tooltip("Botón que alterna música ON/OFF.")]
    private Button musicButton;

    [SerializeField, Tooltip("Botón que alterna sonido/SFX ON/OFF.")]
    private Button sfxButton;

    [SerializeField, Tooltip("Texto del botón de música. Ejemplo: Música: ON.")]
    private TMP_Text musicLabel;

    [SerializeField, Tooltip("Texto del botón de sonido/SFX. Ejemplo: Sonido: ON.")]
    private TMP_Text sfxLabel;

    [SerializeField, Tooltip("CanvasGroup opcional del bloque de audio. Sirve para atenuar si no hay AudioManager.")]
    private CanvasGroup controlsCanvasGroup;

    [Header("Textos")]
    [SerializeField] private string musicOnText = "Música: ON";
    [SerializeField] private string musicOffText = "Música: OFF";
    [SerializeField] private string sfxOnText = "Sonido: ON";
    [SerializeField] private string sfxOffText = "Sonido: OFF";

    [Header("Sin AudioManager")]
    [SerializeField, Tooltip("Si está activo, deshabilita los botones cuando no existe AudioManager en la escena.")]
    private bool disableControlsWhenAudioManagerMissing = true;

    [SerializeField, Range(0f, 1f), Tooltip("Alpha usado para el bloque de audio cuando no hay AudioManager.")]
    private float missingAudioManagerAlpha = 0.45f;

    [Header("UX")]
    [SerializeField, Tooltip("Limpia el botón seleccionado luego de click para que no quede visualmente selected.")]
    private bool clearSelectionOnClick = true;

    [Header("Debug")]
    [SerializeField, Tooltip("Muestra warnings si no hay AudioManager. Dejar apagado si hay escenas sin audio intencionalmente.")]
    private bool logMissingAudioManager;

    private AudioManager subscribedAudioManager;
    private bool listenersWired;
    private bool warnedMissingAudioManager;

    private void Reset()
    {
        controlsCanvasGroup = GetComponent<CanvasGroup>();

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string buttonName = buttons[i].name;

            if (musicButton == null && buttonName.Contains("Music"))
            {
                musicButton = buttons[i];
            }
            else if (sfxButton == null && (buttonName.Contains("SFX") || buttonName.Contains("Sound") || buttonName.Contains("Sonido")))
            {
                sfxButton = buttons[i];
            }
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            string textName = texts[i].name;

            if (musicLabel == null && textName.Contains("Music"))
            {
                musicLabel = texts[i];
            }
            else if (sfxLabel == null && (textName.Contains("SFX") || textName.Contains("Sound") || textName.Contains("Sonido")))
            {
                sfxLabel = texts[i];
            }
        }
    }

    private void OnEnable()
    {
        WireButtonListeners();
        RefreshAudioManagerSubscription();
        RefreshVisuals();
    }

    private void OnDisable()
    {
        UnwireButtonListeners();
        UnsubscribeFromAudioManager();
    }

    private void Update()
    {
        // Defensa liviana: si la UI se habilitó antes que el AudioManager,
        // o si por tests cambia la instancia local, nos resincronizamos.
        if (subscribedAudioManager != AudioManager.Instance)
        {
            RefreshAudioManagerSubscription();
            RefreshVisuals();
        }
    }

    public void RefreshVisuals()
    {
        AudioManager audioManager = AudioManager.Instance;
        bool hasAudioManager = audioManager != null;

        if (!hasAudioManager)
        {
            WarnMissingAudioManagerOnce();
            SetControlsAvailable(false);
            SetText(musicLabel, musicOffText);
            SetText(sfxLabel, sfxOffText);
            return;
        }

        SetControlsAvailable(true);

        bool musicOn = !audioManager.IsMusicMuted;
        bool sfxOn = !audioManager.IsSFXMuted;

        SetText(musicLabel, musicOn ? musicOnText : musicOffText);
        SetText(sfxLabel, sfxOn ? sfxOnText : sfxOffText);
    }

    private void HandleMusicClicked()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            RefreshVisuals();
            ClearSelectionIfNeeded();
            return;
        }

        audioManager.ToggleMusic();
        RefreshVisuals();
        ClearSelectionIfNeeded();
    }

    private void HandleSFXClicked()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            RefreshVisuals();
            ClearSelectionIfNeeded();
            return;
        }

        audioManager.ToggleSFX();
        RefreshVisuals();
        ClearSelectionIfNeeded();
    }

    private void HandleMusicMutedChanged(bool muted)
    {
        RefreshVisuals();
    }

    private void HandleSFXMutedChanged(bool muted)
    {
        RefreshVisuals();
    }

    private void RefreshAudioManagerSubscription()
    {
        AudioManager current = AudioManager.Instance;
        if (subscribedAudioManager == current)
        {
            return;
        }

        UnsubscribeFromAudioManager();

        subscribedAudioManager = current;
        if (subscribedAudioManager == null)
        {
            return;
        }

        subscribedAudioManager.MusicMutedChanged += HandleMusicMutedChanged;
        subscribedAudioManager.SFXMutedChanged += HandleSFXMutedChanged;
        warnedMissingAudioManager = false;
    }

    private void UnsubscribeFromAudioManager()
    {
        if (subscribedAudioManager == null)
        {
            return;
        }

        subscribedAudioManager.MusicMutedChanged -= HandleMusicMutedChanged;
        subscribedAudioManager.SFXMutedChanged -= HandleSFXMutedChanged;
        subscribedAudioManager = null;
    }

    private void WireButtonListeners()
    {
        if (listenersWired)
        {
            return;
        }

        if (musicButton != null)
        {
            musicButton.onClick.RemoveListener(HandleMusicClicked);
            musicButton.onClick.AddListener(HandleMusicClicked);
        }

        if (sfxButton != null)
        {
            sfxButton.onClick.RemoveListener(HandleSFXClicked);
            sfxButton.onClick.AddListener(HandleSFXClicked);
        }

        listenersWired = true;
    }

    private void UnwireButtonListeners()
    {
        if (!listenersWired)
        {
            return;
        }

        if (musicButton != null)
        {
            musicButton.onClick.RemoveListener(HandleMusicClicked);
        }

        if (sfxButton != null)
        {
            sfxButton.onClick.RemoveListener(HandleSFXClicked);
        }

        listenersWired = false;
    }

    private void SetControlsAvailable(bool hasAudioManager)
    {
        bool interactable = hasAudioManager || !disableControlsWhenAudioManagerMissing;

        if (musicButton != null)
        {
            musicButton.interactable = interactable;
        }

        if (sfxButton != null)
        {
            sfxButton.interactable = interactable;
        }

        if (controlsCanvasGroup != null)
        {
            controlsCanvasGroup.alpha = hasAudioManager ? 1f : missingAudioManagerAlpha;
            controlsCanvasGroup.interactable = interactable;
            controlsCanvasGroup.blocksRaycasts = interactable;
        }
    }

    private void ClearSelectionIfNeeded()
    {
        if (!clearSelectionOnClick || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    private void WarnMissingAudioManagerOnce()
    {
        if (!logMissingAudioManager || warnedMissingAudioManager)
        {
            return;
        }

        warnedMissingAudioManager = true;
        Debug.LogWarning($"{nameof(AudioToggleUI)} no encontró AudioManager.Instance. Los controles de audio quedan deshabilitados.", this);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
