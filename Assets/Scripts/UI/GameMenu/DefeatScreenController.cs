using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum DefeatRetryMode
{
    ReloadCurrentScene,
    LoadConfiguredScene
}

/// <summary>
/// Controller visual de la pantalla de derrota.
///
/// Responsabilidad:
/// - Mostrar/ocultar DefeatScreenRoot según GameState.GameOver.
/// - Botón Reintentar: recarga la escena actual o carga una escena configurada.
/// - Botón Menú principal: carga la escena de menú configurada.
///
/// No decide cuándo el jugador pierde. Eso corresponde a PlayerRespawnController.
/// No lee ESC. GameState.GameOver es HardPause y no debería resolverse con ESC.
/// </summary>
[DisallowMultipleComponent]
public sealed class DefeatScreenController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Controlador central del estado del juego. Debe entrar en GameOver cuando el jugador pierde.")]
    private GameStateController gameStateController;

    [Header("UI Root")]
    [SerializeField, Tooltip("CanvasGroup del DefeatScreenRoot. Mantener el GameObject activo para que el script siga escuchando GameState.")]
    private CanvasGroup rootCanvasGroup;

    [Header("Botones")]
    [SerializeField, Tooltip("Botón principal. Reintenta el nivel o carga la escena configurada.")]
    private Button retryButton;

    [SerializeField, Tooltip("Botón secundario. Vuelve al menú principal configurado.")]
    private Button mainMenuButton;

    [Header("Reintentar")]
    [SerializeField, Tooltip("ReloadCurrentScene: recarga la escena actual. LoadConfiguredScene: carga Retry Scene Name.")]
    private DefeatRetryMode retryMode = DefeatRetryMode.ReloadCurrentScene;

    [SerializeField, Tooltip("Escena a cargar si Retry Mode está en LoadConfiguredScene. Útil si el flujo debe volver a una intro del nivel.")]
    private string retrySceneName;

    [Header("Menú Principal")]
    [SerializeField, Tooltip("Nombre exacto de la escena del menú principal.")]
    private string mainMenuSceneName = "MainMenu";

    [Header("Selección UI")]
    [SerializeField, Tooltip("Limpia el selected del EventSystem al mostrar, ocultar o pulsar botones.")]
    private bool clearSelectionOnChange = true;

    [SerializeField, Tooltip("Selecciona automáticamente el botón Reintentar al mostrar la derrota.")]
    private bool selectRetryButtonOnShow = true;

    [Header("Debug")]
    [SerializeField] private bool logDebug;

    private bool isVisible;

    private void Reset()
    {
        gameStateController = FindFirstObjectByType<GameStateController>();
        rootCanvasGroup = GetComponent<CanvasGroup>();
        retryButton = transform.Find("Panel/RetryButton")?.GetComponent<Button>();
        mainMenuButton = transform.Find("Panel/MainMenuButton")?.GetComponent<Button>();
    }

    private void Awake()
    {
        ResolveMissingReferences();
        WireButtons();

        // Defensa contra scenes/prefabs guardados visibles por accidente.
        ApplyRootVisibility(false);
        SyncVisibilityWithGameState();
    }

    private void OnEnable()
    {
        ResolveMissingReferences();
        WireButtons();

        if (gameStateController != null)
        {
            gameStateController.GameStateChanged -= HandleGameStateChanged;
            gameStateController.GameStateChanged += HandleGameStateChanged;
        }

        SyncVisibilityWithGameState();
    }

    private void OnDisable()
    {
        UnwireButtons();

        if (gameStateController != null)
        {
            gameStateController.GameStateChanged -= HandleGameStateChanged;
        }
    }

    public void RequestRetry()
    {
        ClearSelectionIfNeeded();
        Time.timeScale = 1f;

        string sceneToLoad = GetRetrySceneName();

        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            TMJNotifications.ShowSystem(
                "No hay escena de reintento configurada.",
                NotificationPriority.High,
                title: "Derrota",
                groupKey: "defeat_retry_missing_scene",
                context: this);
            return;
        }

        SceneManager.LoadScene(sceneToLoad);
    }

    public void RequestMainMenu()
    {
        ClearSelectionIfNeeded();
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            TMJNotifications.ShowSystem(
                "No hay escena de menú configurada.",
                NotificationPriority.High,
                title: "Derrota",
                groupKey: "defeat_menu_missing_scene",
                context: this);
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void HideInstant()
    {
        ApplyRootVisibility(false);
        ClearSelectionIfNeeded();
    }

    private void ResolveMissingReferences()
    {
        if (gameStateController == null)
        {
            gameStateController = FindFirstObjectByType<GameStateController>();
        }

        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void HandleGameStateChanged(GameState previousState, GameState currentState)
    {
        SyncVisibilityWithGameState();
    }

    private void SyncVisibilityWithGameState()
    {
        bool shouldShow = gameStateController != null && gameStateController.CurrentState == GameState.GameOver;
        ApplyRootVisibility(shouldShow);
    }

    private void ApplyRootVisibility(bool visible)
    {
        isVisible = visible;

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }

        if (visible)
        {
            SelectRetryButtonIfNeeded();
        }
        else
        {
            ClearSelectionIfNeeded();
        }

        if (logDebug)
        {
            Debug.Log($"DefeatScreen visible: {visible}", this);
        }
    }

    private string GetRetrySceneName()
    {
        if (retryMode == DefeatRetryMode.LoadConfiguredScene)
        {
            return retrySceneName;
        }

        return SceneManager.GetActiveScene().name;
    }

    private void WireButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RequestRetry);
            retryButton.onClick.AddListener(RequestRetry);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(RequestMainMenu);
            mainMenuButton.onClick.AddListener(RequestMainMenu);
        }
    }

    private void UnwireButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RequestRetry);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(RequestMainMenu);
        }
    }

    private void SelectRetryButtonIfNeeded()
    {
        if (!selectRetryButtonOnShow || retryButton == null || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
    }

    private void ClearSelectionIfNeeded()
    {
        if (!clearSelectionOnChange || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
    }
}
