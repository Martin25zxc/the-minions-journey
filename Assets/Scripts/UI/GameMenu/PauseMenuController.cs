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
/// - Botón Salir: abre ConfirmDialogController.
/// - Si ConfirmDialog está abierto, oculta visualmente el PauseMenu sin cambiar GameState.
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

    [SerializeField, Tooltip("Modal de confirmación usado por el botón Salir.")]
    private ConfirmDialogController confirmDialogController;

    [Header("UI Root")]
    [SerializeField, Tooltip("CanvasGroup del PauseMenuRoot.")]
    private CanvasGroup rootCanvasGroup;

    [SerializeField, Tooltip("Si está activo, además del CanvasGroup se activa/desactiva el GameObject del root. Recomendado: false si este script vive en PauseMenuRoot.")]
    private bool setRootGameObjectActive;

    [Header("Confirm Dialog")]
    [SerializeField, Tooltip("Si está activo, el PauseMenu se oculta visualmente mientras el ConfirmDialog está abierto.")]
    private bool hidePauseMenuBehindConfirmDialog = true;

    [Header("Botones")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button journalButton;
    [SerializeField] private Button exitButton;

    [Header("Selección UI")]
    [SerializeField, Tooltip("Limpia el selected del EventSystem al cerrar o cambiar pantalla para que los botones no queden marcados.")]
    private bool clearSelectionOnClose = true;

    [Header("Debug")]
    [SerializeField] private bool logDebug;

    private bool pauseMenuHiddenByConfirmDialog;

    private void Reset()
    {
        gameStateController = FindFirstObjectByType<GameStateController>();
        actionGate = FindFirstObjectByType<GameplayActionGate>();
        missionJournalController = FindFirstObjectByType<MissionJournalController>();
        confirmDialogController = FindFirstObjectByType<ConfirmDialogController>(FindObjectsInactive.Include);
        rootCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        ResolveMissingReferences();
        WireButtons();
        SubscribeConfirmDialog();

        // Defensa contra prefabs/scenes que quedaron guardados visibles por accidente.
        ApplyRootVisibility(false);
        SyncVisibilityWithGameState();
    }

    private void OnEnable()
    {
        ResolveMissingReferences();
        WireButtons();
        SubscribeConfirmDialog();

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
        UnsubscribeConfirmDialog();

        if (gameStateController != null)
        {
            gameStateController.GameStateChanged -= HandleGameStateChanged;
        }
    }

    public void RequestResume()
    {
        ClearSelectionIfNeeded();
        CloseConfirmDialogIfOpen();

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
        CloseConfirmDialogIfOpen();

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

    public void RequestExitConfirmation()
    {
        ClearSelectionIfNeeded();

        if (confirmDialogController == null)
        {
            TMJNotifications.ShowSystem(
                "No hay diálogo de confirmación configurado.",
                NotificationPriority.Normal,
                title: "Salir",
                groupKey: "pause_menu_missing_confirm_dialog",
                context: this);
            return;
        }

        if (hidePauseMenuBehindConfirmDialog)
        {
            pauseMenuHiddenByConfirmDialog = true;
            SyncVisibilityWithGameState();
        }

        confirmDialogController.ShowExitConfirmation();
    }

    public void HideInstant()
    {
        pauseMenuHiddenByConfirmDialog = false;
        ApplyRootVisibility(false);
        CloseConfirmDialogIfOpen();
        ClearSelectionIfNeeded();
    }

    private void ResolveMissingReferences()
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

        if (confirmDialogController == null)
        {
            confirmDialogController = FindFirstObjectByType<ConfirmDialogController>(FindObjectsInactive.Include);
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
        bool isPauseState = gameStateController != null && gameStateController.CurrentState == GameState.PauseMenu;

        if (!isPauseState)
        {
            pauseMenuHiddenByConfirmDialog = false;
            CloseConfirmDialogIfOpen();
        }

        bool shouldShow = isPauseState && !pauseMenuHiddenByConfirmDialog;
        ApplyRootVisibility(shouldShow);
    }

    private void ApplyRootVisibility(bool visible)
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }

        // Para este controller, desactivar el GameObject del root es peligroso:
        // si el script vive en PauseMenuRoot y el root se apaga, deja de escuchar cambios de GameState
        // y puede no restaurarse al cerrar ConfirmDialog. Por eso esta versión NO desactiva el GameObject;
        // solo usa CanvasGroup. Dejar Set Root GameObject Active apagado en Inspector.
        if (setRootGameObjectActive && logDebug)
        {
            Debug.LogWarning("PauseMenuController: Set Root GameObject Active está activo, pero esta versión lo ignora para evitar desincronización. Usar CanvasGroup.", this);
        }

        if (!visible)
        {
            ClearSelectionIfNeeded();
        }

        if (logDebug)
        {
            Debug.Log($"PauseMenu visible: {visible}. HiddenByConfirm: {pauseMenuHiddenByConfirmDialog}", this);
        }
    }

    private void SubscribeConfirmDialog()
    {
        if (confirmDialogController == null)
        {
            return;
        }

        confirmDialogController.DialogShown -= HandleConfirmDialogShown;
        confirmDialogController.DialogHidden -= HandleConfirmDialogHidden;

        confirmDialogController.DialogShown += HandleConfirmDialogShown;
        confirmDialogController.DialogHidden += HandleConfirmDialogHidden;
    }

    private void UnsubscribeConfirmDialog()
    {
        if (confirmDialogController == null)
        {
            return;
        }

        confirmDialogController.DialogShown -= HandleConfirmDialogShown;
        confirmDialogController.DialogHidden -= HandleConfirmDialogHidden;
    }

    private void HandleConfirmDialogShown(ConfirmDialogController dialog)
    {
        if (!hidePauseMenuBehindConfirmDialog)
        {
            return;
        }

        pauseMenuHiddenByConfirmDialog = true;
        SyncVisibilityWithGameState();
    }

    private void HandleConfirmDialogHidden(ConfirmDialogController dialog)
    {
        pauseMenuHiddenByConfirmDialog = false;
        SyncVisibilityWithGameState();
    }

    private void CloseConfirmDialogIfOpen()
    {
        if (confirmDialogController != null && confirmDialogController.IsOpen)
        {
            confirmDialogController.HideInstant();
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
            exitButton.onClick.RemoveListener(RequestExitConfirmation);
            exitButton.onClick.AddListener(RequestExitConfirmation);
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
            exitButton.onClick.RemoveListener(RequestExitConfirmation);
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
