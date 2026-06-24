using System;
using UnityEngine;

[Serializable]
public sealed class MissionChainRule
{
    [Header("Condición")]
    [SerializeField, Tooltip("Misión fuente que debe alcanzar el estado indicado por Trigger.")]
    private MissionDefinition sourceMission;

    [SerializeField, Tooltip("Momento de la misión fuente que dispara esta regla.")]
    private MissionChainTrigger trigger = MissionChainTrigger.OnCompleted;

    [Header("Resultado")]
    [SerializeField, Tooltip("Misión destino sobre la que se ejecuta la acción.")]
    private MissionDefinition targetMission;

    [SerializeField, Tooltip("Acción que se ejecuta sobre la misión destino.")]
    private MissionChainAction action = MissionChainAction.MakeAvailable;

    [SerializeField, Tooltip("Si está activo, la regla se ejecuta una sola vez por sesión runtime.")]
    private bool executeOnce = true;

    [SerializeField, Tooltip("Solo se usa con Action = Accept. ActorId que quedará como OriginalGiver de la misión destino si esa misión requiere entrega al giver original.")]
    private string acceptOriginalGiverId;

    [SerializeField, Tooltip("Si está activo y la acción deja la misión destino Active o ReadyToTurnIn, intenta trackearla en el HUD.")]
    private bool trackTargetAfterAction;

    [SerializeField, TextArea(1, 3), Tooltip("Nota de autoría para recordar intención narrativa o de gameplay. No afecta runtime.")]
    private string developerNote;

    public MissionDefinition SourceMission => sourceMission;
    public MissionChainTrigger Trigger => trigger;
    public MissionDefinition TargetMission => targetMission;
    public MissionChainAction Action => action;
    public bool ExecuteOnce => executeOnce;
    public string AcceptOriginalGiverId => CleanId(acceptOriginalGiverId);
    public bool TrackTargetAfterAction => trackTargetAfterAction;
    public string DeveloperNote => developerNote;

    public string SourceMissionId => sourceMission != null ? CleanId(sourceMission.MissionId) : string.Empty;
    public string TargetMissionId => targetMission != null ? CleanId(targetMission.MissionId) : string.Empty;

    public bool IsValid => sourceMission != null && targetMission != null && !string.IsNullOrEmpty(SourceMissionId) && !string.IsNullOrEmpty(TargetMissionId);

    public bool Matches(MissionRuntimeState sourceRuntimeState, MissionChainTrigger receivedTrigger)
    {
        if (sourceRuntimeState == null || receivedTrigger != trigger)
        {
            return false;
        }

        return string.Equals(SourceMissionId, CleanId(sourceRuntimeState.MissionId), StringComparison.Ordinal);
    }

    public bool HasSourceReachedTrigger(MissionRuntimeState sourceRuntimeState)
    {
        if (sourceRuntimeState == null)
        {
            return false;
        }

        switch (trigger)
        {
            case MissionChainTrigger.OnAccepted:
                return sourceRuntimeState.IsActive || sourceRuntimeState.IsReadyToTurnIn || sourceRuntimeState.IsCompleted;

            case MissionChainTrigger.OnReadyToTurnIn:
                return sourceRuntimeState.IsReadyToTurnIn || sourceRuntimeState.IsCompleted;

            case MissionChainTrigger.OnCompleted:
                return sourceRuntimeState.IsCompleted;

            default:
                return false;
        }
    }

    public string BuildDebugLabel(int ruleIndex)
    {
        return $"Rule {ruleIndex}: {SourceMissionId} {trigger} -> {action} {TargetMissionId}";
    }

    public string BuildExecutionKey(string chainId, int ruleIndex)
    {
        return $"{CleanId(chainId)}|{ruleIndex}|{SourceMissionId}|{trigger}|{action}|{TargetMissionId}";
    }

    public void Validate(UnityEngine.Object context, string chainName, int index)
    {
        if (sourceMission == null)
        {
            Debug.LogWarning($"{chainName}: Rule #{index} no tiene Source Mission.", context);
        }
        else if (string.IsNullOrEmpty(SourceMissionId))
        {
            Debug.LogWarning($"{chainName}: Rule #{index} tiene Source Mission sin MissionId.", sourceMission);
        }

        if (targetMission == null)
        {
            Debug.LogWarning($"{chainName}: Rule #{index} no tiene Target Mission.", context);
        }
        else if (string.IsNullOrEmpty(TargetMissionId))
        {
            Debug.LogWarning($"{chainName}: Rule #{index} tiene Target Mission sin MissionId.", targetMission);
        }

        if (!string.IsNullOrEmpty(SourceMissionId) && string.Equals(SourceMissionId, TargetMissionId, StringComparison.Ordinal))
        {
            Debug.LogWarning($"{chainName}: Rule #{index} usa la misma misión como Source y Target: '{SourceMissionId}'. Esto suele indicar un ciclo accidental.", context);
        }

        if (action == MissionChainAction.Accept && targetMission != null &&
            targetMission.CompletionMode == MissionCompletionMode.RequiresTurnIn &&
            targetMission.TurnInTargetMode == MissionTurnInTargetMode.OriginalGiver &&
            string.IsNullOrEmpty(AcceptOriginalGiverId))
        {
            Debug.LogWarning($"{chainName}: Rule #{index} acepta automáticamente '{TargetMissionId}', pero la misión requiere OriginalGiver y Accept Original Giver Id está vacío. Esa aceptación puede fallar.", context);
        }
    }

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
