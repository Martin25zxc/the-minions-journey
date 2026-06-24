using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MissionAvailabilityResponder : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("MissionManager runtime de la scene. Es la fuente de verdad del estado de misiones.")]
    private MissionManager missionManager;

    [SerializeField, Tooltip("Assets de cadena que este responder debe ejecutar en esta scene o nivel.")]
    private MissionChainDefinition[] chainDefinitions;

    [Header("Sincronización")]
    [SerializeField, Tooltip("Si está activo, al iniciar revisa el estado actual y ejecuta reglas cuyo trigger ya se cumplió. Útil si el responder se activa tarde.")]
    private bool synchronizeFromCurrentStateOnStart = true;

    [Header("Debug")]
    [SerializeField, Tooltip("Muestra logs de reglas ejecutadas o ignoradas durante pruebas.")]
    private bool logDebug;

    private readonly HashSet<string> executedRuleKeys = new HashSet<string>();
    private bool subscribed;

    private enum ChainActionStatus
    {
        Failed,
        SucceededNoChange,
        SucceededChanged
    }

    private void Reset()
    {
        missionManager = FindFirstObjectByType<MissionManager>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        if (synchronizeFromCurrentStateOnStart)
        {
            SynchronizeFromCurrentState();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SynchronizeFromCurrentState()
    {
        if (missionManager == null)
        {
            Debug.LogWarning($"{nameof(MissionAvailabilityResponder)} no tiene MissionManager asignado.", this);
            return;
        }

        for (int chainIndex = 0; chainIndex < GetChainCount(); chainIndex++)
        {
            MissionChainDefinition chainDefinition = chainDefinitions[chainIndex];
            if (chainDefinition == null)
            {
                continue;
            }

            IReadOnlyList<MissionChainRule> rules = chainDefinition.Rules;

            for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
            {
                MissionChainRule rule = rules[ruleIndex];
                if (rule == null || !rule.IsValid)
                {
                    continue;
                }

                MissionRuntimeState sourceState = missionManager.GetMissionState(rule.SourceMissionId);
                if (!rule.HasSourceReachedTrigger(sourceState))
                {
                    continue;
                }

                TryExecuteRule(chainDefinition, rule, ruleIndex, fromSynchronization: true);
            }
        }
    }

    private void Subscribe()
    {
        if (subscribed || missionManager == null)
        {
            return;
        }

        missionManager.MissionAccepted += HandleMissionAccepted;
        missionManager.MissionReadyToTurnIn += HandleMissionReadyToTurnIn;
        missionManager.MissionCompleted += HandleMissionCompleted;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || missionManager == null)
        {
            return;
        }

        missionManager.MissionAccepted -= HandleMissionAccepted;
        missionManager.MissionReadyToTurnIn -= HandleMissionReadyToTurnIn;
        missionManager.MissionCompleted -= HandleMissionCompleted;
        subscribed = false;
    }

    private void HandleMissionAccepted(MissionRuntimeState missionState)
    {
        ProcessTrigger(missionState, MissionChainTrigger.OnAccepted);
    }

    private void HandleMissionReadyToTurnIn(MissionRuntimeState missionState)
    {
        ProcessTrigger(missionState, MissionChainTrigger.OnReadyToTurnIn);
    }

    private void HandleMissionCompleted(MissionRuntimeState missionState)
    {
        ProcessTrigger(missionState, MissionChainTrigger.OnCompleted);
    }

    private void ProcessTrigger(MissionRuntimeState sourceState, MissionChainTrigger trigger)
    {
        if (sourceState == null)
        {
            return;
        }

        for (int chainIndex = 0; chainIndex < GetChainCount(); chainIndex++)
        {
            MissionChainDefinition chainDefinition = chainDefinitions[chainIndex];
            if (chainDefinition == null)
            {
                continue;
            }

            IReadOnlyList<MissionChainRule> rules = chainDefinition.Rules;

            for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
            {
                MissionChainRule rule = rules[ruleIndex];
                if (rule == null || !rule.IsValid)
                {
                    continue;
                }

                if (!rule.Matches(sourceState, trigger))
                {
                    continue;
                }

                TryExecuteRule(chainDefinition, rule, ruleIndex, fromSynchronization: false);
            }
        }
    }

    private bool TryExecuteRule(MissionChainDefinition chainDefinition, MissionChainRule rule, int ruleIndex, bool fromSynchronization)
    {
        string executionKey = rule.BuildExecutionKey(chainDefinition.ChainId, ruleIndex);

        if (rule.ExecuteOnce && executedRuleKeys.Contains(executionKey))
        {
            if (logDebug)
            {
                Debug.Log($"Regla de cadena ya ejecutada. {rule.BuildDebugLabel(ruleIndex)}", this);
            }

            return false;
        }

        ChainActionStatus status = ExecuteAction(rule);
        bool succeeded = status != ChainActionStatus.Failed;
        bool changed = status == ChainActionStatus.SucceededChanged;

        if (succeeded && rule.ExecuteOnce)
        {
            executedRuleKeys.Add(executionKey);
        }

        if (logDebug)
        {
            string syncPrefix = fromSynchronization ? "[Sync] " : string.Empty;
            string result = status == ChainActionStatus.Failed ? "falló" : changed ? "ejecutada" : "sin cambios";
            Debug.Log($"{syncPrefix}Regla de cadena {result}. {rule.BuildDebugLabel(ruleIndex)}", this);
        }

        return changed;
    }

    private ChainActionStatus ExecuteAction(MissionChainRule rule)
    {
        switch (rule.Action)
        {
            case MissionChainAction.MakeAvailable:
                return ExecuteMakeAvailable(rule.TargetMissionId);

            case MissionChainAction.StartMission:
                return ExecuteStartMission(rule);

            default:
                Debug.LogWarning($"{nameof(MissionAvailabilityResponder)} recibió una acción de cadena no soportada: {rule.Action}.", this);
                return ChainActionStatus.Failed;
        }
    }

    private ChainActionStatus ExecuteMakeAvailable(string targetMissionId)
    {
        MissionRuntimeState targetState = missionManager.GetMissionState(targetMissionId);
        if (targetState == null)
        {
            Debug.LogWarning($"{nameof(MissionAvailabilityResponder)} no encontró la misión destino '{targetMissionId}'.", this);
            return ChainActionStatus.Failed;
        }

        if (targetState.IsInactive)
        {
            bool changed = missionManager.TryMakeMissionAvailable(targetMissionId);
            return changed ? ChainActionStatus.SucceededChanged : ChainActionStatus.Failed;
        }

        if (targetState.IsAvailable || targetState.IsActive || targetState.IsReadyToTurnIn || targetState.IsCompleted)
        {
            return ChainActionStatus.SucceededNoChange;
        }

        return ChainActionStatus.Failed;
    }

    private ChainActionStatus ExecuteStartMission(MissionChainRule rule)
    {
        MissionRuntimeState targetState = missionManager.GetMissionState(rule.TargetMissionId);
        if (targetState == null)
        {
            Debug.LogWarning($"{nameof(MissionAvailabilityResponder)} no encontró la misión destino '{rule.TargetMissionId}'.", this);
            return ChainActionStatus.Failed;
        }

        if (targetState.IsActive || targetState.IsReadyToTurnIn || targetState.IsCompleted)
        {
            return ChainActionStatus.SucceededNoChange;
        }

        if (targetState.Definition.CompletionMode == MissionCompletionMode.RequiresTurnIn &&
            targetState.Definition.TurnInTargetMode == MissionTurnInTargetMode.OriginalGiver)
        {
            Debug.LogWarning($"No se puede usar StartMission con '{rule.TargetMissionId}' porque requiere entrega al OriginalGiver. Usá MakeAvailable para que un actor real la acepte, o configurá un SpecificActor si corresponde.", this);
            return ChainActionStatus.Failed;
        }

        bool madeAvailable = false;
        if (targetState.IsInactive)
        {
            madeAvailable = missionManager.TryMakeMissionAvailable(rule.TargetMissionId);

            targetState = missionManager.GetMissionState(rule.TargetMissionId);
            if (targetState == null || !targetState.IsAvailable)
            {
                return ChainActionStatus.Failed;
            }
        }

        if (!targetState.IsAvailable)
        {
            return ChainActionStatus.Failed;
        }

        bool accepted = missionManager.TryAcceptMission(rule.TargetMissionId, originalGiverId: null);
        if (!accepted)
        {
            return ChainActionStatus.Failed;
        }

        return madeAvailable ? ChainActionStatus.SucceededChanged : ChainActionStatus.SucceededChanged;
    }

    private int GetChainCount()
    {
        return chainDefinitions != null ? chainDefinitions.Length : 0;
    }
}
