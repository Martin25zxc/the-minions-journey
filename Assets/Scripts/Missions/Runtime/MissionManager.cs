using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MissionManager : MonoBehaviour
{
    [Serializable]
    private sealed class MissionRuntimeDebugSnapshot
    {
        [SerializeField] private string missionId;
        [SerializeField] private MissionState state;
        [SerializeField] private bool isTracked;
        [SerializeField] private string progress;

        public void Set(MissionRuntimeState runtimeState)
        {
            if (runtimeState == null)
            {
                missionId = string.Empty;
                state = MissionState.Inactive;
                isTracked = false;
                progress = string.Empty;
                return;
            }

            missionId = runtimeState.MissionId;
            state = runtimeState.State;
            isTracked = runtimeState.IsTracked;
            progress = $"Required {runtimeState.GetCompletedRequiredObjectiveCount()}/{runtimeState.GetRequiredObjectiveCount()} | Bonus {runtimeState.GetCompletedBonusObjectiveCount()}/{runtimeState.GetBonusObjectiveCount()}";
        }
    }

    [Header("Referencias")]
    [SerializeField, Tooltip("Catálogo de misiones disponibles para esta scene o nivel. No guarda progreso.")]
    private MissionCatalog missionCatalog;

    [SerializeField, Tooltip("Opcional. Si se asigna, bloquea aceptar/entregar misiones cuando el estado de juego no lo permite, por ejemplo en combate.")]
    private GameplayActionGate actionGate;

    [Header("Inicialización")]
    [SerializeField, Tooltip("Si está activo, las misiones con Start Mode OnInteraction quedan Available al iniciar el manager.")]
    private bool makeInteractionMissionsAvailableOnAwake = true;

    [SerializeField, Tooltip("Si está activo, las misiones con Start Mode AutoOnLevelStart se aceptan automáticamente en Start.")]
    private bool acceptAutoStartMissionsOnStart = true;

    [SerializeField, Tooltip("Permite que TryAcceptMission registre una MissionDefinition que no estaba en el catálogo. Útil en prototipos; para contenido final conviene tener catálogo completo.")]
    private bool allowRuntimeRegistration = true;

    [Header("Debug")]
    [SerializeField, Tooltip("Muestra logs útiles mientras probamos el flujo.")]
    private bool logChanges;

    [SerializeField, Tooltip("Solo lectura aproximada para mirar estados en Inspector. La fuente real es el diccionario runtime.")]
    private List<MissionRuntimeDebugSnapshot> debugStates = new List<MissionRuntimeDebugSnapshot>();

    private readonly Dictionary<string, MissionRuntimeState> statesByMissionId = new Dictionary<string, MissionRuntimeState>(StringComparer.Ordinal);
    private readonly List<MissionRuntimeState> orderedStates = new List<MissionRuntimeState>();

    private MissionRuntimeState trackedMission;
    private bool initialized;
    private bool isInitializing;

    public event Action<MissionRuntimeState> MissionAvailable;
    public event Action<MissionRuntimeState> MissionAccepted;
    public event Action<MissionRuntimeState> MissionReadyToTurnIn;
    public event Action<MissionRuntimeState> MissionCompleted;
    public event Action<MissionRuntimeState, MissionObjectiveRuntimeState> ObjectiveUpdated;
    public event Action<MissionRuntimeState, MissionObjectiveRuntimeState> ObjectiveCompleted;
    public event Action<MissionRuntimeState> TrackedMissionChanged;

    public IReadOnlyList<MissionRuntimeState> Missions => orderedStates;

    private void Reset()
    {
        actionGate = FindFirstObjectByType<GameplayActionGate>();
    }

    private void Awake()
    {
        InitializeFromCatalog();
    }

    private void Start()
    {
        if (acceptAutoStartMissionsOnStart)
        {
            AcceptAutoStartMissions();
        }
    }

    public void InitializeFromCatalog()
    {
        if (isInitializing)
        {
            return;
        }

        isInitializing = true;

        statesByMissionId.Clear();
        orderedStates.Clear();
        trackedMission = null;
        initialized = false;

        if (missionCatalog == null)
        {
            Debug.LogWarning($"{nameof(MissionManager)} no tiene MissionCatalog asignado. Se podrán registrar misiones por runtime si Allow Runtime Registration está activo.", this);
            initialized = true;
            isInitializing = false;
            RefreshDebugStates();
            return;
        }

        IReadOnlyList<MissionDefinition> missionDefinitions = missionCatalog.Missions;

        for (int i = 0; i < missionDefinitions.Count; i++)
        {
            RegisterMissionDefinition(missionDefinitions[i], logIfDuplicate: true);
        }

        // Importante: marcamos initialized ANTES de emitir eventos iniciales.
        // Si un listener como MissionHUDTracker responde a MissionAvailable y consulta GetTrackedMission(),
        // EnsureInitialized() no debe volver a ejecutar InitializeFromCatalog().
        initialized = true;

        if (makeInteractionMissionsAvailableOnAwake)
        {
            MarkInitialInteractionMissionsAvailable();
        }

        RefreshDebugStates();
        isInitializing = false;
    }

    public bool TryMakeMissionAvailable(string missionId)
    {
        EnsureInitialized();

        if (!TryGetRuntimeState(missionId, out MissionRuntimeState runtimeState))
        {
            return false;
        }

        bool changed = runtimeState.MarkAvailable();

        if (!changed)
        {
            return false;
        }

        if (logChanges)
        {
            Debug.Log($"Misión disponible: {runtimeState.MissionId}", this);
        }

        RefreshDebugStates();
        MissionAvailable?.Invoke(runtimeState);
        return true;
    }

    public bool TryAcceptMission(MissionDefinition mission, string originalGiverId = null)
    {
        EnsureInitialized();

        if (mission == null)
        {
            Debug.LogWarning($"{nameof(MissionManager)} recibió una MissionDefinition null.", this);
            return false;
        }

        if (!TryEnsureMissionRegistered(mission, out MissionRuntimeState runtimeState))
        {
            return false;
        }

        return TryAcceptMission(runtimeState.MissionId, originalGiverId);
    }

    public bool TryAcceptMission(string missionId, string originalGiverId = null)
    {
        EnsureInitialized();

        GameplayActionBlockResult blockResult = GetActionBlockResult(GameplayActionType.AcceptMission);
        if (!blockResult.IsAllowed)
        {
            if (logChanges)
            {
                Debug.Log(blockResult.Reason, this);
            }

            return false;
        }

        if (!TryGetRuntimeState(missionId, out MissionRuntimeState runtimeState))
        {
            return false;
        }

        return AcceptMissionInternal(runtimeState, originalGiverId);
    }

    /// <summary>
    /// Acepta/inicia una misión desde sistemas internos, como cadenas de misión o AutoOnLevelStart.
    /// No consulta GameplayActionGate porque no representa una interacción manual del jugador.
    /// Usar TryAcceptMission para NPCs, prompts, UI o cualquier acción iniciada por input del jugador.
    /// </summary>
    public bool TryAcceptMissionFromSystem(MissionDefinition mission, string originalGiverId = null)
    {
        EnsureInitialized();

        if (mission == null)
        {
            Debug.LogWarning($"{nameof(MissionManager)} recibió una MissionDefinition null para aceptación de sistema.", this);
            return false;
        }

        if (!TryEnsureMissionRegistered(mission, out MissionRuntimeState runtimeState))
        {
            return false;
        }

        return AcceptMissionInternal(runtimeState, originalGiverId);
    }

    /// <summary>
    /// Acepta/inicia una misión desde sistemas internos, como cadenas de misión o AutoOnLevelStart.
    /// No consulta GameplayActionGate porque no representa una interacción manual del jugador.
    /// Usar TryAcceptMission para NPCs, prompts, UI o cualquier acción iniciada por input del jugador.
    /// </summary>
    public bool TryAcceptMissionFromSystem(string missionId, string originalGiverId = null)
    {
        EnsureInitialized();

        if (!TryGetRuntimeState(missionId, out MissionRuntimeState runtimeState))
        {
            return false;
        }

        return AcceptMissionInternal(runtimeState, originalGiverId);
    }

    public bool TryReportWorldEvent(GameWorldEvent worldEvent)
    {
        EnsureInitialized();

        if (!worldEvent.IsValid)
        {
            Debug.LogWarning($"{nameof(MissionManager)} recibió un GameWorldEvent inválido: {worldEvent}", this);
            return false;
        }

        bool anyChange = false;

        for (int missionIndex = 0; missionIndex < orderedStates.Count; missionIndex++)
        {
            MissionRuntimeState missionState = orderedStates[missionIndex];

            if (!missionState.IsActive)
            {
                continue;
            }

            IReadOnlyList<MissionObjectiveRuntimeState> objectives = missionState.Objectives;

            for (int objectiveIndex = 0; objectiveIndex < objectives.Count; objectiveIndex++)
            {
                MissionObjectiveRuntimeState objectiveState = objectives[objectiveIndex];

                if (objectiveState == null || objectiveState.IsCompleted)
                {
                    continue;
                }

                if (!MissionObjectiveEventMatcher.Matches(objectiveState.Definition, worldEvent))
                {
                    continue;
                }

                MissionProgressResult result = missionState.ApplyObjectiveProgress(objectiveState.ObjectiveId, worldEvent.Amount, Time.time);

                if (!result.HasAnyChange)
                {
                    continue;
                }

                anyChange = true;
                DispatchProgressEvents(missionState, result);

                if (!missionState.IsActive)
                {
                    break;
                }
            }
        }

        if (anyChange)
        {
            RefreshDebugStates();
        }
        else if (logChanges)
        {
            Debug.Log($"GameWorldEvent sin progreso de misión: {worldEvent}", this);
        }

        return anyChange;
    }

    public bool TryTurnInMission(string missionId, string turnInTargetId)
    {
        EnsureInitialized();

        GameplayActionBlockResult blockResult = GetActionBlockResult(GameplayActionType.TurnInMission);
        if (!blockResult.IsAllowed)
        {
            if (logChanges)
            {
                Debug.Log(blockResult.Reason, this);
            }

            return false;
        }

        if (!TryGetRuntimeState(missionId, out MissionRuntimeState runtimeState))
        {
            return false;
        }

        bool completed = runtimeState.TurnIn(turnInTargetId, Time.time);

        if (!completed)
        {
            if (logChanges)
            {
                Debug.Log($"No se pudo entregar '{missionId}' en target '{turnInTargetId}'.", this);
            }

            return false;
        }

        if (trackedMission == runtimeState)
        {
            runtimeState.SetTracked(false);
            trackedMission = null;
            TrackedMissionChanged?.Invoke(null);
        }

        if (logChanges)
        {
            Debug.Log($"Misión completada por entrega: {runtimeState.MissionId}", this);
        }

        RefreshDebugStates();
        MissionCompleted?.Invoke(runtimeState);
        return true;
    }

    public bool TrySetTrackedMission(string missionId)
    {
        EnsureInitialized();

        if (!TryGetRuntimeState(missionId, out MissionRuntimeState runtimeState))
        {
            return false;
        }

        if (!runtimeState.IsActive && !runtimeState.IsReadyToTurnIn)
        {
            return false;
        }

        return SetTrackedMissionInternal(runtimeState, emitEvent: true);
    }

    public bool TryClearTrackedMission()
    {
        EnsureInitialized();

        if (trackedMission == null)
        {
            return false;
        }

        trackedMission.SetTracked(false);
        trackedMission = null;
        RefreshDebugStates();
        TrackedMissionChanged?.Invoke(null);
        return true;
    }

    public bool IsMissionActive(string missionId)
    {
        return TryGetRuntimeState(missionId, out MissionRuntimeState runtimeState) && runtimeState.IsActive;
    }

    public bool IsMissionReadyToTurnIn(string missionId)
    {
        return TryGetRuntimeState(missionId, out MissionRuntimeState runtimeState) && runtimeState.IsReadyToTurnIn;
    }

    public bool IsMissionCompleted(string missionId)
    {
        return TryGetRuntimeState(missionId, out MissionRuntimeState runtimeState) && runtimeState.IsCompleted;
    }

    public MissionRuntimeState GetMissionState(string missionId)
    {
        TryGetRuntimeState(missionId, out MissionRuntimeState runtimeState);
        return runtimeState;
    }

    public bool TryGetRuntimeState(string missionId, out MissionRuntimeState runtimeState)
    {
        EnsureInitialized();

        string cleanedMissionId = CleanId(missionId);

        if (string.IsNullOrEmpty(cleanedMissionId))
        {
            runtimeState = null;
            return false;
        }

        return statesByMissionId.TryGetValue(cleanedMissionId, out runtimeState);
    }

    public MissionRuntimeState GetTrackedMission()
    {
        EnsureInitialized();
        return trackedMission;
    }

    private void AcceptAutoStartMissions()
    {
        EnsureInitialized();

        for (int i = 0; i < orderedStates.Count; i++)
        {
            MissionRuntimeState runtimeState = orderedStates[i];

            if (runtimeState.Definition.StartMode != MissionStartMode.AutoOnLevelStart)
            {
                continue;
            }

            TryAcceptMissionFromSystem(runtimeState.MissionId, originalGiverId: null);
        }
    }

    private void MarkInitialInteractionMissionsAvailable()
    {
        for (int i = 0; i < orderedStates.Count; i++)
        {
            MissionRuntimeState runtimeState = orderedStates[i];

            if (runtimeState.Definition.StartMode != MissionStartMode.OnInteraction)
            {
                continue;
            }

            if (runtimeState.MarkAvailable())
            {
                if (logChanges)
                {
                    Debug.Log($"Misión disponible inicial: {runtimeState.MissionId}", this);
                }

                MissionAvailable?.Invoke(runtimeState);
            }
        }
    }

    private bool TryEnsureMissionRegistered(MissionDefinition mission, out MissionRuntimeState runtimeState)
    {
        string missionId = mission != null ? CleanId(mission.MissionId) : string.Empty;

        if (string.IsNullOrEmpty(missionId))
        {
            runtimeState = null;
            return false;
        }

        if (statesByMissionId.TryGetValue(missionId, out runtimeState))
        {
            return true;
        }

        if (!allowRuntimeRegistration)
        {
            Debug.LogWarning($"La misión '{missionId}' no está registrada en el catálogo y Allow Runtime Registration está desactivado.", this);
            return false;
        }

        runtimeState = RegisterMissionDefinition(mission, logIfDuplicate: true);
        RefreshDebugStates();
        return runtimeState != null;
    }

    private MissionRuntimeState RegisterMissionDefinition(MissionDefinition mission, bool logIfDuplicate)
    {
        if (mission == null)
        {
            return null;
        }

        string missionId = CleanId(mission.MissionId);

        if (string.IsNullOrEmpty(missionId))
        {
            Debug.LogWarning($"{nameof(MissionManager)} ignoró una misión sin MissionId: '{mission.name}'.", mission);
            return null;
        }

        if (statesByMissionId.TryGetValue(missionId, out MissionRuntimeState existingState))
        {
            if (logIfDuplicate)
            {
                Debug.LogWarning($"{nameof(MissionManager)} detectó MissionId duplicado en runtime: '{missionId}'. Se conserva la primera instancia.", this);
            }

            return existingState;
        }

        MissionRuntimeState runtimeState = new MissionRuntimeState(mission);
        statesByMissionId.Add(missionId, runtimeState);
        orderedStates.Add(runtimeState);
        return runtimeState;
    }

    private bool AcceptMissionInternal(MissionRuntimeState runtimeState, string originalGiverId)
    {
        if (runtimeState == null)
        {
            return false;
        }

        string missionId = runtimeState.MissionId;
        bool accepted = runtimeState.Accept(originalGiverId, Time.time);

        if (!accepted)
        {
            if (logChanges)
            {
                Debug.Log($"No se pudo aceptar la misión '{missionId}'. Estado actual: {runtimeState.State}.", this);
            }

            return false;
        }

        bool trackedChanged = false;
        if (runtimeState.Definition.AutoTrackOnAccept)
        {
            trackedChanged = SetTrackedMissionInternal(runtimeState, emitEvent: false);
        }

        if (logChanges)
        {
            Debug.Log($"Misión aceptada: {runtimeState.MissionId}", this);
        }

        RefreshDebugStates();
        MissionAccepted?.Invoke(runtimeState);

        if (trackedChanged)
        {
            TrackedMissionChanged?.Invoke(trackedMission);
        }

        return true;
    }

    private void DispatchProgressEvents(MissionRuntimeState missionState, MissionProgressResult result)
    {
        if (result.ObjectiveProgressed && result.ObjectiveState != null)
        {
            ObjectiveUpdated?.Invoke(missionState, result.ObjectiveState);
        }

        if (result.ObjectiveCompleted && result.ObjectiveState != null)
        {
            ObjectiveCompleted?.Invoke(missionState, result.ObjectiveState);
        }

        if (result.MissionBecameReadyToTurnIn)
        {
            MissionReadyToTurnIn?.Invoke(missionState);
        }

        if (result.MissionCompleted)
        {
            if (trackedMission == missionState)
            {
                missionState.SetTracked(false);
                trackedMission = null;
                TrackedMissionChanged?.Invoke(null);
            }

            MissionCompleted?.Invoke(missionState);
        }

        if (logChanges)
        {
            Debug.Log(result.Message, this);
        }
    }

    private bool SetTrackedMissionInternal(MissionRuntimeState runtimeState, bool emitEvent)
    {
        if (runtimeState == null)
        {
            return false;
        }

        if (trackedMission == runtimeState && runtimeState.IsTracked)
        {
            return false;
        }

        if (trackedMission != null)
        {
            trackedMission.SetTracked(false);
        }

        trackedMission = runtimeState;
        trackedMission.SetTracked(true);
        RefreshDebugStates();

        if (emitEvent)
        {
            TrackedMissionChanged?.Invoke(trackedMission);
        }

        return true;
    }

    private GameplayActionBlockResult GetActionBlockResult(GameplayActionType actionType)
    {
        if (actionGate == null)
        {
            return GameplayActionBlockResult.Allowed();
        }

        return actionGate.GetBlockResult(actionType);
    }

    private void EnsureInitialized()
    {
        if (initialized || isInitializing)
        {
            return;
        }

        InitializeFromCatalog();
    }

    private void RefreshDebugStates()
    {
        debugStates.Clear();

        for (int i = 0; i < orderedStates.Count; i++)
        {
            MissionRuntimeDebugSnapshot snapshot = new MissionRuntimeDebugSnapshot();
            snapshot.Set(orderedStates[i]);
            debugStates.Add(snapshot);
        }
    }

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
