using UnityEngine;

[DisallowMultipleComponent]
public sealed class MissionHUDTracker : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Fuente de verdad de las misiones. El HUD solo lee su estado y escucha eventos.")]
    private MissionManager missionManager;

    [SerializeField, Tooltip("Vista visual del HUD. No contiene lógica de misión.")]
    private MissionHUDView hudView;

    [SerializeField, Tooltip("Opcional. Si se asigna, permite ocultar el HUD durante pausa, cutscene o game over.")]
    private GameStateController gameStateController;

    [Header("Modo visual")]
    [SerializeField, Tooltip("Si está activo, el HUD se muestra compacto. Útil hasta conectar PlayerThreatTracker o una regla de combate real.")]
    private bool forceCompactMode;

    [SerializeField, Tooltip("Si está activo, oculta el HUD cuando el GameState no es Gameplay. Requiere GameStateController asignado.")]
    private bool hideWhenNotInGameplay = true;

    [Header("Debug")]
    [SerializeField, Tooltip("Muestra logs cuando faltan referencias o cuando refresca el HUD.")]
    private bool logDebug;

    private bool started;

    private void Reset()
    {
        missionManager = FindFirstObjectByType<MissionManager>();
        hudView = GetComponent<MissionHUDView>();
        gameStateController = FindFirstObjectByType<GameStateController>();
    }

    private void OnEnable()
    {
        Subscribe();

        if (started)
        {
            Refresh();
        }
    }

    private void Start()
    {
        started = true;
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetForceCompactMode(bool value)
    {
        if (forceCompactMode == value)
        {
            return;
        }

        forceCompactMode = value;
        Refresh();
    }

    public void Refresh()
    {
        if (missionManager == null || hudView == null)
        {
            if (logDebug)
            {
                Debug.LogWarning($"{nameof(MissionHUDTracker)} necesita MissionManager y MissionHUDView.", this);
            }

            return;
        }

        MissionRuntimeState trackedMission = missionManager.GetTrackedMission();
        MissionHUDDisplayMode displayMode = ResolveDisplayMode(trackedMission);

        hudView.Render(trackedMission, displayMode);

        if (logDebug)
        {
            string missionId = trackedMission == null ? "null" : trackedMission.MissionId;
            Debug.Log($"Mission HUD refresh -> Mission: {missionId}, Mode: {displayMode}", this);
        }
    }

    private MissionHUDDisplayMode ResolveDisplayMode(MissionRuntimeState trackedMission)
    {
        if (trackedMission == null || trackedMission.IsCompleted || !trackedMission.Definition.ShowInHUD)
        {
            return MissionHUDDisplayMode.Hidden;
        }

        if (hideWhenNotInGameplay && gameStateController != null && !gameStateController.IsGameplay)
        {
            return MissionHUDDisplayMode.Hidden;
        }

        return forceCompactMode ? MissionHUDDisplayMode.Compact : MissionHUDDisplayMode.Expanded;
    }

    private void Subscribe()
    {
        if (missionManager == null)
        {
            return;
        }

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

        if (gameStateController != null)
        {
            gameStateController.GameStateChanged -= HandleGameStateChanged;
            gameStateController.GameStateChanged += HandleGameStateChanged;
        }
    }

    private void Unsubscribe()
    {
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

        if (gameStateController != null)
        {
            gameStateController.GameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleMissionChanged(MissionRuntimeState missionState)
    {
        Refresh();
    }

    private void HandleObjectiveChanged(MissionRuntimeState missionState, MissionObjectiveRuntimeState objectiveState)
    {
        Refresh();
    }

    private void HandleGameStateChanged(GameState previousState, GameState currentState)
    {
        Refresh();
    }
}
