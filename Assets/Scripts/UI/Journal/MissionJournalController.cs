using UnityEngine;

/// <summary>
/// Controla la apertura/cierre del Mission Journal desde el estado del juego.
/// 
/// Regla principal:
/// - GameStateController es la fuente de verdad de la pantalla actual.
/// - Este controller solo muestra/oculta la vista cuando el estado pasa a MissionJournal.
/// - Input y botones piden abrir/cerrar, pero no activan el panel directamente.
/// </summary>
[DisallowMultipleComponent]
public sealed class MissionJournalController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Controlador central de estados del juego.")]
    private GameStateController gameStateController;

    [SerializeField, Tooltip("Gate de acciones. Bloquea abrir el Journal en combate u otros estados inválidos.")]
    private GameplayActionGate actionGate;

    [SerializeField, Tooltip("Fuente de verdad del runtime de misiones.")]
    private MissionManager missionManager;

    [SerializeField, Tooltip("Vista visual del Journal.")]
    private MissionJournalView journalView;

    [SerializeField, Tooltip("Dimmer de pantalla usado como fondo/bloqueador del Journal.")]
    private GameObject screenDimmer;

    [Header("Debug")]
    [SerializeField, Tooltip("Muestra logs de diagnóstico del Journal.")]
    private bool logDebug;

    private GameState returnStateAfterJournal = GameState.Gameplay;

    private void Reset()
    {
        gameStateController = FindFirstObjectByType<GameStateController>();
        actionGate = FindFirstObjectByType<GameplayActionGate>();
        missionManager = FindFirstObjectByType<MissionManager>();
        journalView = FindFirstObjectByType<MissionJournalView>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        SyncVisibilityWithGameState();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    /// <summary>
    /// Usar para la tecla J.
    /// Si el Journal está abierto, vuelve al estado anterior válido.
    /// Si estamos en Gameplay, intenta abrir respetando GameplayActionGate.
    /// En PauseMenu no hace nada: para eso usar RequestOpenFromPauseMenuButton().
    /// </summary>
    public void RequestToggleFromGameplayKey()
    {
        if (gameStateController == null)
        {
            Debug.LogWarning($"{nameof(MissionJournalController)} necesita GameStateController.", this);
            return;
        }

        if (gameStateController.CurrentState == GameState.MissionJournal)
        {
            RequestCloseToPreviousState();
            return;
        }

        if (gameStateController.CurrentState != GameState.Gameplay)
        {
            return;
        }

        RequestOpenJournal();
    }

    /// <summary>
    /// Usar en el botón futuro del menú de pausa.
    /// Permite ir desde PauseMenu a MissionJournal sin duplicar lógica.
    /// </summary>
    public void RequestOpenFromPauseMenuButton()
    {
        RequestOpenJournal();
    }

    /// <summary>
    /// Usar en botones internos del Journal, como Header CloseButton o Footer CloseButton.
    /// Si el Journal fue abierto desde PauseMenu, vuelve a PauseMenu; si fue abierto desde Gameplay, vuelve a Gameplay.
    /// </summary>
    public void RequestCloseToPreviousState()
    {
        if (gameStateController == null)
        {
            return;
        }

        if (returnStateAfterJournal == GameState.PauseMenu)
        {
            gameStateController.TrySetState(GameState.PauseMenu);
            return;
        }

        gameStateController.TryReturnToGameplay();
    }

    private void RequestOpenJournal()
    {
        if (gameStateController == null)
        {
            Debug.LogWarning($"{nameof(MissionJournalController)} necesita GameStateController.", this);
            return;
        }

        GameplayActionBlockResult blockResult = GetJournalOpenBlockResult();

        if (!blockResult.IsAllowed)
        {
            ShowBlockedFeedback(blockResult.Reason);

            if (logDebug)
            {
                Debug.Log($"MissionJournalController: apertura bloqueada. {blockResult.Reason}", this);
            }

            return;
        }

        CacheReturnState();
        bool changed = gameStateController.TryOpenMissionJournal();

        if (logDebug)
        {
            Debug.Log($"MissionJournalController: TryOpenMissionJournal -> {changed}", this);
        }
    }

    private void CacheReturnState()
    {
        if (gameStateController == null)
        {
            returnStateAfterJournal = GameState.Gameplay;
            return;
        }

        returnStateAfterJournal = gameStateController.CurrentState == GameState.PauseMenu
            ? GameState.PauseMenu
            : GameState.Gameplay;
    }

    private GameplayActionBlockResult GetJournalOpenBlockResult()
    {
        if (actionGate == null)
        {
            return GameplayActionBlockResult.Allowed();
        }

        return actionGate.GetBlockResult(GameplayActionType.OpenMissionJournal);
    }

    private void ShowBlockedFeedback(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        TMJNotifications.ShowSystem(
            reason,
            NotificationPriority.Normal,
            title: "Diario",
            groupKey: "journal_blocked",
            context: this);
    }

    private void HandleGameStateChanged(GameState previousState, GameState currentState)
    {
        SyncVisibilityWithGameState();
    }

    private void SyncVisibilityWithGameState()
    {
        bool shouldShow = gameStateController != null &&
                          gameStateController.CurrentState == GameState.MissionJournal;

        if (screenDimmer != null)
        {
            screenDimmer.SetActive(shouldShow);
        }

        if (journalView == null)
        {
            return;
        }

        if (shouldShow)
        {
            journalView.Show();
            Refresh();
        }
        else
        {
            journalView.HideInstant();
        }
    }

    public void Refresh()
    {
        if (missionManager == null || journalView == null)
        {
            if (logDebug)
            {
                Debug.LogWarning($"{nameof(MissionJournalController)} necesita MissionManager y MissionJournalView.", this);
            }

            return;
        }

        journalView.Render(missionManager.Missions, missionManager.GetTrackedMission());
    }

    private void HandleTrackRequested(MissionRuntimeState missionState)
    {
        if (missionState == null || missionManager == null)
        {
            return;
        }

        bool changed = missionState.IsTracked
            ? missionManager.TryClearTrackedMission()
            : missionManager.TrySetTrackedMission(missionState.MissionId);

        if (logDebug)
        {
            Debug.Log($"MissionJournalController: toggle track '{missionState.MissionId}' -> {changed}", this);
        }

        Refresh();
    }

    private void HandleCloseRequested()
    {
        RequestCloseToPreviousState();
    }

    private void HandleMissionChanged(MissionRuntimeState missionState)
    {
        RefreshIfOpen();
    }

    private void HandleObjectiveChanged(MissionRuntimeState missionState, MissionObjectiveRuntimeState objectiveState)
    {
        RefreshIfOpen();
    }

    private void RefreshIfOpen()
    {
        if (gameStateController != null && gameStateController.CurrentState == GameState.MissionJournal)
        {
            Refresh();
        }
    }

    private void Subscribe()
    {
        if (gameStateController != null)
        {
            gameStateController.GameStateChanged -= HandleGameStateChanged;
            gameStateController.GameStateChanged += HandleGameStateChanged;
        }

        if (journalView != null)
        {
            journalView.TrackRequested -= HandleTrackRequested;
            journalView.CloseRequested -= HandleCloseRequested;

            journalView.TrackRequested += HandleTrackRequested;
            journalView.CloseRequested += HandleCloseRequested;
        }

        if (missionManager != null)
        {
            missionManager.MissionAvailable -= HandleMissionChanged;
            missionManager.MissionAccepted -= HandleMissionChanged;
            missionManager.MissionReadyToTurnIn -= HandleMissionChanged;
            missionManager.MissionCompleted -= HandleMissionChanged;
            missionManager.ObjectiveUpdated -= HandleObjectiveChanged;
            missionManager.ObjectiveCompleted -= HandleObjectiveChanged;
            missionManager.TrackedMissionChanged -= HandleMissionChanged;

            missionManager.MissionAvailable += HandleMissionChanged;
            missionManager.MissionAccepted += HandleMissionChanged;
            missionManager.MissionReadyToTurnIn += HandleMissionChanged;
            missionManager.MissionCompleted += HandleMissionChanged;
            missionManager.ObjectiveUpdated += HandleObjectiveChanged;
            missionManager.ObjectiveCompleted += HandleObjectiveChanged;
            missionManager.TrackedMissionChanged += HandleMissionChanged;
        }
    }

    private void Unsubscribe()
    {
        if (gameStateController != null)
        {
            gameStateController.GameStateChanged -= HandleGameStateChanged;
        }

        if (journalView != null)
        {
            journalView.TrackRequested -= HandleTrackRequested;
            journalView.CloseRequested -= HandleCloseRequested;
        }

        if (missionManager != null)
        {
            missionManager.MissionAvailable -= HandleMissionChanged;
            missionManager.MissionAccepted -= HandleMissionChanged;
            missionManager.MissionReadyToTurnIn -= HandleMissionChanged;
            missionManager.MissionCompleted -= HandleMissionChanged;
            missionManager.ObjectiveUpdated -= HandleObjectiveChanged;
            missionManager.ObjectiveCompleted -= HandleObjectiveChanged;
            missionManager.TrackedMissionChanged -= HandleMissionChanged;
        }
    }
}
