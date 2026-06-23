using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responder de escena para una misión concreta.
///
/// Escucha eventos del MissionManager y ejecuta consecuencias simples de escena:
/// - activar objetos;
/// - desactivar objetos;
/// - setear WorldFlags.
///
/// No acepta misiones, no completa objetivos, no entrega rewards y no modifica la UI.
/// </summary>
[DisallowMultipleComponent]
public sealed class MissionSceneResponder : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private MissionManager missionManager;

    [SerializeField]
    private WorldFlagRegistry worldFlagRegistry;

    [Header("Mission")]
    [SerializeField]
    private MissionDefinition mission;

    [Header("Response Blocks")]
    [SerializeField]
    private List<MissionSceneResponseBlock> responseBlocks = new List<MissionSceneResponseBlock>();

    [Header("Options")]
    [Tooltip("Si falta MissionManager o WorldFlagRegistry, intenta resolverlos una vez al iniciar. Preferir referencias por Inspector cuando sea posible.")]
    [SerializeField]
    private bool autoResolveMissingReferences = true;

    [SerializeField]
    private bool logDebug;

    private bool isSubscribed;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        EnsureReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    [ContextMenu("Reset Response Blocks Execution State")]
    public void ResetExecutionState()
    {
        if (responseBlocks == null)
        {
            return;
        }

        for (int i = 0; i < responseBlocks.Count; i++)
        {
            responseBlocks[i]?.ResetExecutionState();
        }
    }

    private void EnsureReferences()
    {
        if (!autoResolveMissingReferences)
        {
            return;
        }

        if (missionManager == null)
        {
            missionManager = FindFirstObjectByType<MissionManager>();
        }

        if (worldFlagRegistry == null)
        {
            worldFlagRegistry = FindFirstObjectByType<WorldFlagRegistry>();
        }
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        if (missionManager == null)
        {
            Debug.LogWarning($"{nameof(MissionSceneResponder)} '{name}' has no MissionManager assigned.", this);
            return;
        }

        missionManager.MissionAccepted += HandleMissionAccepted;
        missionManager.ObjectiveCompleted += HandleObjectiveCompleted;
        missionManager.MissionReadyToTurnIn += HandleMissionReadyToTurnIn;
        missionManager.MissionCompleted += HandleMissionCompleted;

        isSubscribed = true;

        if (logDebug)
        {
            Debug.Log($"{nameof(MissionSceneResponder)} '{name}' subscribed to MissionManager.", this);
        }
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || missionManager == null)
        {
            isSubscribed = false;
            return;
        }

        missionManager.MissionAccepted -= HandleMissionAccepted;
        missionManager.ObjectiveCompleted -= HandleObjectiveCompleted;
        missionManager.MissionReadyToTurnIn -= HandleMissionReadyToTurnIn;
        missionManager.MissionCompleted -= HandleMissionCompleted;

        isSubscribed = false;
    }

    private void HandleMissionAccepted(MissionRuntimeState runtimeState)
    {
        TryExecuteBlocks(MissionSceneTriggerMoment.OnAccepted, runtimeState, null);
    }

    private void HandleObjectiveCompleted(MissionRuntimeState runtimeState, MissionObjectiveRuntimeState objectiveState)
    {
        TryExecuteBlocks(MissionSceneTriggerMoment.OnObjectiveCompleted, runtimeState, objectiveState);
    }

    private void HandleMissionReadyToTurnIn(MissionRuntimeState runtimeState)
    {
        TryExecuteBlocks(MissionSceneTriggerMoment.OnReadyToTurnIn, runtimeState, null);
    }

    private void HandleMissionCompleted(MissionRuntimeState runtimeState)
    {
        TryExecuteBlocks(MissionSceneTriggerMoment.OnCompleted, runtimeState, null);
    }

    private void TryExecuteBlocks(
        MissionSceneTriggerMoment moment,
        MissionRuntimeState runtimeState,
        MissionObjectiveRuntimeState objectiveState)
    {
        if (!MatchesMission(runtimeState))
        {
            return;
        }

        if (responseBlocks == null || responseBlocks.Count == 0)
        {
            if (logDebug)
            {
                Debug.Log($"{nameof(MissionSceneResponder)} '{name}' matched {moment}, but has no response blocks.", this);
            }

            return;
        }

        for (int i = 0; i < responseBlocks.Count; i++)
        {
            MissionSceneResponseBlock block = responseBlocks[i];
            if (block == null)
            {
                continue;
            }

            if (!block.CanExecute(moment, objectiveState))
            {
                continue;
            }

            if (logDebug)
            {
                string objectiveText = objectiveState != null && objectiveState.Definition != null
                    ? $" objective '{objectiveState.Definition.ObjectiveId}'"
                    : string.Empty;

                Debug.Log($"{nameof(MissionSceneResponder)} '{name}' executing block '{block.BlockName}' on {moment}{objectiveText}.", this);
            }

            block.Execute(worldFlagRegistry, this, logDebug);
        }
    }

    private bool MatchesMission(MissionRuntimeState runtimeState)
    {
        if (runtimeState == null || runtimeState.Definition == null || mission == null)
        {
            return false;
        }

        if (ReferenceEquals(runtimeState.Definition, mission))
        {
            return true;
        }

        return string.Equals(
            runtimeState.Definition.MissionId,
            mission.MissionId,
            StringComparison.Ordinal
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (responseBlocks != null)
        {
            for (int i = 0; i < responseBlocks.Count; i++)
            {
                responseBlocks[i]?.OnValidate();
            }
        }

        if (mission == null)
        {
            return;
        }

        if (responseBlocks == null)
        {
            return;
        }

        for (int i = 0; i < responseBlocks.Count; i++)
        {
            MissionSceneResponseBlock block = responseBlocks[i];
            if (block == null)
            {
                continue;
            }

            if (block.FilterByObjectiveId && string.IsNullOrWhiteSpace(block.ObjectiveId))
            {
                Debug.LogWarning($"{nameof(MissionSceneResponder)} '{name}' has a block with Objective Filter enabled but an empty Objective Id.", this);
            }
        }
    }
#endif
}
