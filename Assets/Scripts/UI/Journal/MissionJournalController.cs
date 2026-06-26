using UnityEngine;

/// <summary>
/// Orquesta el Journal de misiones.
/// 
/// Responsabilidad:
/// - Conecta MissionManager con MissionJournalView.
/// - Escucha cambios de misiones.
/// - Ejecuta acciones simples como trackear/cerrar.
/// 
/// No responsabilidad:
/// - No decide progreso de misiones.
/// - No acepta/entrega misiones.
/// - No aplica recompensas.
/// - No maneja todavía TAB/ESC/input global.
/// </summary>
[DisallowMultipleComponent]
public sealed class MissionJournalController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Fuente de verdad del runtime de misiones.")]
    private MissionManager missionManager;

    [SerializeField, Tooltip("Vista visual del Journal. Renderiza lista, detalle, objetivos y recompensas.")]
    private MissionJournalView journalView;

    [SerializeField, Tooltip("Dimmer de pantalla usado como fondo/bloqueador visual del Journal.")]
    private GameObject screenDimmer;

    [Header("Debug")]
    [SerializeField, Tooltip("Abre el Journal automáticamente al iniciar para validar layout y render.")]
    private bool openOnStartForDebug;

    [SerializeField, Tooltip("Muestra logs de diagnóstico del Journal.")]
    private bool logDebug;

    private void Reset()
    {
        missionManager = FindFirstObjectByType<MissionManager>();
        journalView = FindFirstObjectByType<MissionJournalView>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        SubscribeMissionManager();
        SubscribeView();
    }

    private void Start()
    {
        HideInstant();

        if (openOnStartForDebug)
        {
            Open();
        }
    }

    private void OnDisable()
    {
        UnsubscribeMissionManager();
        UnsubscribeView();
    }

    public void Open()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        if (screenDimmer != null)
        {
            screenDimmer.SetActive(true);
        }

        journalView.Show();
        Refresh();

        if (logDebug)
        {
            Debug.Log("MissionJournalController: Journal abierto.", this);
        }
    }

    public void Close()
    {
        HideInstant();

        if (logDebug)
        {
            Debug.Log("MissionJournalController: Journal cerrado.", this);
        }
    }

    public void HideInstant()
    {
        if (screenDimmer != null)
        {
            screenDimmer.SetActive(false);
        }

        if (journalView != null)
        {
            journalView.HideInstant();
        }
    }

    public void Refresh()
    {
        if (!HasRequiredReferences())
        {
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

        bool changed;

        if (missionState.IsTracked)
        {
            changed = missionManager.TryClearTrackedMission();
        }
        else
        {
            changed = missionManager.TrySetTrackedMission(missionState.MissionId);
        }

        if (logDebug)
        {
            Debug.Log($"MissionJournalController: Track toggle '{missionState.MissionId}' -> {changed}", this);
        }

        Refresh();
    }

    private void HandleCloseRequested()
    {
        Close();
    }

    private void HandleMissionChanged(MissionRuntimeState missionState)
    {
        Refresh();
    }

    private void HandleObjectiveChanged(MissionRuntimeState missionState, MissionObjectiveRuntimeState objectiveState)
    {
        Refresh();
    }

    private bool HasRequiredReferences()
    {
        bool hasReferences = missionManager != null && journalView != null;

        if (!hasReferences && logDebug)
        {
            Debug.LogWarning("MissionJournalController necesita MissionManager y MissionJournalView.", this);
        }

        return hasReferences;
    }

    private void SubscribeMissionManager()
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
    }

    private void UnsubscribeMissionManager()
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
    }

    private void SubscribeView()
    {
        if (journalView == null)
        {
            return;
        }

        journalView.TrackRequested -= HandleTrackRequested;
        journalView.CloseRequested -= HandleCloseRequested;

        journalView.TrackRequested += HandleTrackRequested;
        journalView.CloseRequested += HandleCloseRequested;
    }

    private void UnsubscribeView()
    {
        if (journalView == null)
        {
            return;
        }

        journalView.TrackRequested -= HandleTrackRequested;
        journalView.CloseRequested -= HandleCloseRequested;
    }
}