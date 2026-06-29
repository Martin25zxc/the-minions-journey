using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controller visual del PauseMenu.
///
/// Responsabilidad:
/// - Mostrar/ocultar PauseMenuRoot según GameState.PauseMenu.
/// - Botón Continuar: vuelve a Gameplay.
/// - Botón Diario: abre el Mission Journal usando el flujo existente.
/// - Botón Salir: placeholder para Etapa 5C.
///
/// No lee ESC. ESC lo maneja GameModalInputController.
/// No activa/desactiva el Journal directamente.
/// </summary>
[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Controlador central del estado del juego.")]
    private GameStateController gameStateController;

    [SerializeField, Tooltip("Gate opcional. Valida abrir Journal desde PauseMenu si corresponde.")]
    private GameplayActionGate actionGate;

    [SerializeField, Tooltip("Controller existente del Journal. Debe vivir fuera de MissionJournalRoot.")]
    private MissionJournalController missionJournalController;

    [Header("UI Root")]
    [SerializeField, Tooltip("CanvasGroup del PauseMenuRoot.")]
    private CanvasGroup rootCanvasGroup;

    [SerializeField, Tooltip("Si está activo, además del CanvasGroup se activa/desactiva el GameObject del root. Para UI modal suele ser seguro dejarlo apagado.")]
    private bool setRootGameObjectActive;

    [Header("Botones")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button journalButton;
    [SerializeField] private Button exitButton;

    [Header("Salida placeholder")]
    [SerializeField, Tooltip("Mensaje temporal hasta implementar ConfirmDialog en Etapa 5C.")]
    private string exitNotImplementedMessage = "La salida con confirmación se implementará en la Etapa 5C.";

    [Header("Selección UI")]
    [SerializeField, Tooltip("Limpia el selected del EventSystem al cerrar o cambiar pantalla para que los botones no queden marcados.")]
    private bool clearSelectionOnClose = true;

    [Header("Debug")]
    [SerializeField] private bool logDebug;

    private void Reset()
    {
        gameStateController = FindFirstObjectByType<GameStateController>();
        actionGate = FindFirstObjectByType<GameplayActionGate>();
        missionJournalController = FindFirstObjectByType<MissionJournalController>();
        rootCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (gameStateController == null)
        {
            gameStateController = FindFirstObjectByType<GameStateController>();
        }

        if (actionGate == null)
        {
            actionGate = FindFirstObjectByType<GameplayActionGate>();
        }

        if (missionJournalController == null)
        {
            missionJournalController = FindFirstObjectByType<MissionJournalController>();
        }

        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = GetComponent<CanvasGroup>();
        }

        WireButtons();
        SyncVisibilityWithGameState();
    }

    private void OnEnable()
    {
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

    public void RequestResume()
    {
        ClearSelectionIfNeeded();

        if (gameStateController == null)
        {
            Debug.LogWarning($"{nameof(PauseMenuController)} necesita {nameof(GameStateController)}.", this);
            return;
        }

        gameStateController.TryReturnToGameplay();
    }

    public void RequestOpenJournal()
    {
        ClearSelectionIfNeeded();

        if (actionGate != null)
        {
            GameplayActionBlockResult result = actionGate.GetBlockResult(GameplayActionType.OpenMissionJournal);
            if (!result.IsAllowed)
            {
                TMJNotifications.ShowSystem(
                    result.Reason,
                    NotificationPriority.Normal,
                    title: "Diario",
                    groupKey: "pause_menu_journal_blocked",
                    context: this);
                return;
            }
        }

        if (missionJournalController != null)
        {
            missionJournalController.RequestOpenFromPauseMenuButton();
            return;
        }

        // Fallback defensivo. Lo ideal es usar MissionJournalController para conservar una sola entrada oficial.
        if (gameStateController != null)
        {
            gameStateController.TrySetState(GameState.MissionJournal);
        }
    }

    public void RequestExitPlaceholder()
    {
        ClearSelectionIfNeeded();

        if (!string.IsNullOrWhiteSpace(exitNotImplementedMessage))
        {
            TMJNotifications.ShowSystem(
                exitNotImplementedMessage,
                NotificationPriority.Normal,
                title: "Salir",
                groupKey: "pause_menu_exit_placeholder",
                context: this);
        }
    }

    private void HandleGameStateChanged(GameState previousState, GameState currentState)
    {
        SyncVisibilityWithGameState();
    }

    private void SyncVisibilityWithGameState()
    {
        bool shouldShow = gameStateController != null && gameStateController.CurrentState == GameState.PauseMenu;
        SetVisible(shouldShow);
    }

    private void SetVisible(bool visible)
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }

        if (setRootGameObjectActive)
        {
            gameObject.SetActive(visible);
        }

        if (!visible)
        {
            ClearSelectionIfNeeded();
        }

        if (logDebug)
        {
            Debug.Log($"PauseMenu visible: {visible}", this);
        }
    }

    private void WireButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(RequestResume);
            continueButton.onClick.AddListener(RequestResume);
        }

        if (journalButton != null)
        {
            journalButton.onClick.RemoveListener(RequestOpenJournal);
            journalButton.onClick.AddListener(RequestOpenJournal);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(RequestExitPlaceholder);
            exitButton.onClick.AddListener(RequestExitPlaceholder);
        }
    }

    private void UnwireButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(RequestResume);
        }

        if (journalButton != null)
        {
            journalButton.onClick.RemoveListener(RequestOpenJournal);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(RequestExitPlaceholder);
        }
    }

    private void ClearSelectionIfNeeded()
    {
        if (!clearSelectionOnClose || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
    }
}
