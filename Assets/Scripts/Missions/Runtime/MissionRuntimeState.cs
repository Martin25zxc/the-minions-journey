using System;
using System.Collections.Generic;

public sealed class MissionRuntimeState
{
    private readonly List<MissionObjectiveRuntimeState> objectives = new List<MissionObjectiveRuntimeState>();
    private readonly Dictionary<string, MissionObjectiveRuntimeState> objectiveLookup = new Dictionary<string, MissionObjectiveRuntimeState>(StringComparer.Ordinal);

    public MissionRuntimeState(MissionDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        State = MissionState.Inactive;
        AcceptedAtTime = -1f;
        ReadyToTurnInAtTime = -1f;
        CompletedAtTime = -1f;

        BuildObjectiveStates(definition);
    }

    public MissionDefinition Definition { get; }
    public MissionState State { get; private set; }
    public IReadOnlyList<MissionObjectiveRuntimeState> Objectives => objectives;
    public bool IsTracked { get; private set; }
    public string OriginalGiverId { get; private set; } = string.Empty;
    public float AcceptedAtTime { get; private set; }
    public float ReadyToTurnInAtTime { get; private set; }
    public float CompletedAtTime { get; private set; }

    public string MissionId => Definition.MissionId;
    public bool IsInactive => State == MissionState.Inactive;
    public bool IsAvailable => State == MissionState.Available;
    public bool IsActive => State == MissionState.Active;
    public bool IsReadyToTurnIn => State == MissionState.ReadyToTurnIn;
    public bool IsCompleted => State == MissionState.Completed;

    public bool MarkAvailable()
    {
        if (State != MissionState.Inactive)
        {
            return false;
        }

        State = MissionState.Available;
        return true;
    }

    public bool Accept(string originalGiverId, float currentTime)
    {
        if (State != MissionState.Inactive && State != MissionState.Available)
        {
            return false;
        }

        string cleanedGiverId = CleanId(originalGiverId);

        if (Definition.CompletionMode == MissionCompletionMode.RequiresTurnIn &&
            Definition.TurnInTargetMode == MissionTurnInTargetMode.OriginalGiver &&
            string.IsNullOrEmpty(cleanedGiverId))
        {
            return false;
        }

        OriginalGiverId = cleanedGiverId;
        AcceptedAtTime = currentTime;
        State = MissionState.Active;
        return true;
    }

    public MissionProgressResult ApplyObjectiveProgress(string objectiveId, int amount, float currentTime)
    {
        if (State != MissionState.Active)
        {
            return MissionProgressResult.NoChange(State, "La misión no está activa; se ignora el progreso.");
        }

        if (amount <= 0)
        {
            return MissionProgressResult.NoChange(State, "El progreso debe ser mayor a 0.");
        }

        if (!TryGetObjectiveState(objectiveId, out MissionObjectiveRuntimeState objectiveState))
        {
            return MissionProgressResult.NoChange(State, $"No se encontró el objetivo '{objectiveId}'.");
        }

        MissionState previousMissionState = State;
        bool objectiveProgressed = objectiveState.AddProgress(amount, out bool completedNow);

        if (!objectiveProgressed)
        {
            return new MissionProgressResult(
                objectiveProgressed: false,
                objectiveCompleted: false,
                missionStateChanged: false,
                previousMissionState: previousMissionState,
                currentMissionState: State,
                objectiveState: objectiveState,
                message: "El objetivo no cambió. Puede estar completo o el progreso no era válido.");
        }

        bool objectiveCompleted = completedNow && objectiveState.TryConsumeCompletionEvent();
        EvaluateCompletion(currentTime);

        bool missionStateChanged = previousMissionState != State;

        return new MissionProgressResult(
            objectiveProgressed: true,
            objectiveCompleted: objectiveCompleted,
            missionStateChanged: missionStateChanged,
            previousMissionState: previousMissionState,
            currentMissionState: State,
            objectiveState: objectiveState,
            message: BuildProgressMessage(objectiveState, objectiveCompleted, missionStateChanged));
    }

    public bool MarkReadyToTurnIn(float currentTime)
    {
        if (State != MissionState.Active)
        {
            return false;
        }

        if (Definition.CompletionMode != MissionCompletionMode.RequiresTurnIn)
        {
            return false;
        }

        if (!AreRequiredObjectivesCompleted())
        {
            return false;
        }

        ReadyToTurnInAtTime = currentTime;
        State = MissionState.ReadyToTurnIn;
        return true;
    }

    public bool Complete(float currentTime)
    {
        if (State == MissionState.Completed)
        {
            return false;
        }

        if (State == MissionState.ReadyToTurnIn)
        {
            SetCompleted(currentTime);
            return true;
        }

        if (State == MissionState.Active &&
            Definition.CompletionMode == MissionCompletionMode.AutoComplete &&
            AreRequiredObjectivesCompleted())
        {
            SetCompleted(currentTime);
            return true;
        }

        return false;
    }

    public bool CanTurnInAtTarget(string turnInTargetId)
    {
        if (State != MissionState.ReadyToTurnIn)
        {
            return false;
        }

        if (Definition.CompletionMode != MissionCompletionMode.RequiresTurnIn)
        {
            return false;
        }

        string cleanedTargetId = CleanId(turnInTargetId);

        switch (Definition.TurnInTargetMode)
        {
            case MissionTurnInTargetMode.OriginalGiver:
                return !string.IsNullOrEmpty(OriginalGiverId) && string.Equals(cleanedTargetId, OriginalGiverId, StringComparison.Ordinal);

            case MissionTurnInTargetMode.SpecificActor:
            case MissionTurnInTargetMode.SpecificWorldObject:
                return !string.IsNullOrEmpty(Definition.TurnInTargetId) && string.Equals(cleanedTargetId, Definition.TurnInTargetId, StringComparison.Ordinal);

            case MissionTurnInTargetMode.None:
            default:
                return false;
        }
    }

    public bool TurnIn(string turnInTargetId, float currentTime)
    {
        if (!CanTurnInAtTarget(turnInTargetId))
        {
            return false;
        }

        return Complete(currentTime);
    }

    public bool SetTracked(bool value)
    {
        if (IsTracked == value)
        {
            return false;
        }

        IsTracked = value;
        return true;
    }

    public MissionObjectiveRuntimeState GetObjectiveState(string objectiveId)
    {
        TryGetObjectiveState(objectiveId, out MissionObjectiveRuntimeState objectiveState);
        return objectiveState;
    }

    public bool TryGetObjectiveState(string objectiveId, out MissionObjectiveRuntimeState objectiveState)
    {
        string cleanedObjectiveId = CleanId(objectiveId);

        if (string.IsNullOrEmpty(cleanedObjectiveId))
        {
            objectiveState = null;
            return false;
        }

        return objectiveLookup.TryGetValue(cleanedObjectiveId, out objectiveState);
    }

    public bool AreRequiredObjectivesCompleted()
    {
        bool hasRequiredObjective = false;

        for (int i = 0; i < objectives.Count; i++)
        {
            MissionObjectiveRuntimeState objective = objectives[i];

            if (!objective.IsRequired)
            {
                continue;
            }

            hasRequiredObjective = true;

            if (!objective.IsCompleted)
            {
                return false;
            }
        }

        return hasRequiredObjective;
    }

    public bool AreAllObjectivesCompleted()
    {
        if (objectives.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < objectives.Count; i++)
        {
            if (!objectives[i].IsCompleted)
            {
                return false;
            }
        }

        return true;
    }

    public int GetCompletedRequiredObjectiveCount()
    {
        return CountObjectives(ObjectiveImportance.Required, completedOnly: true);
    }

    public int GetRequiredObjectiveCount()
    {
        return CountObjectives(ObjectiveImportance.Required, completedOnly: false);
    }

    public int GetCompletedBonusObjectiveCount()
    {
        return CountObjectives(ObjectiveImportance.Bonus, completedOnly: true);
    }

    public int GetBonusObjectiveCount()
    {
        return CountObjectives(ObjectiveImportance.Bonus, completedOnly: false);
    }

    private void BuildObjectiveStates(MissionDefinition definition)
    {
        IReadOnlyList<MissionObjectiveDefinition> objectiveDefinitions = definition.Objectives;

        for (int i = 0; i < objectiveDefinitions.Count; i++)
        {
            MissionObjectiveDefinition objectiveDefinition = objectiveDefinitions[i];

            if (objectiveDefinition == null)
            {
                continue;
            }

            MissionObjectiveRuntimeState objectiveState = new MissionObjectiveRuntimeState(objectiveDefinition);
            objectives.Add(objectiveState);

            string objectiveId = CleanId(objectiveDefinition.ObjectiveId);

            if (string.IsNullOrEmpty(objectiveId))
            {
                continue;
            }

            if (objectiveLookup.ContainsKey(objectiveId))
            {
                throw new InvalidOperationException($"La misión '{definition.MissionId}' tiene ObjectiveId duplicado en runtime: '{objectiveId}'. Revisar el asset de autoría.");
            }

            objectiveLookup.Add(objectiveId, objectiveState);
        }
    }

    private void EvaluateCompletion(float currentTime)
    {
        if (State != MissionState.Active)
        {
            return;
        }

        if (!AreRequiredObjectivesCompleted())
        {
            return;
        }

        if (Definition.CompletionMode == MissionCompletionMode.RequiresTurnIn)
        {
            MarkReadyToTurnIn(currentTime);
            return;
        }

        if (Definition.CompletionMode == MissionCompletionMode.AutoComplete)
        {
            Complete(currentTime);
        }
    }

    private void SetCompleted(float currentTime)
    {
        CompletedAtTime = currentTime;
        State = MissionState.Completed;
    }

    private int CountObjectives(ObjectiveImportance importance, bool completedOnly)
    {
        int count = 0;

        for (int i = 0; i < objectives.Count; i++)
        {
            MissionObjectiveRuntimeState objective = objectives[i];

            if (objective.Definition.Importance != importance)
            {
                continue;
            }

            if (completedOnly && !objective.IsCompleted)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private static string BuildProgressMessage(MissionObjectiveRuntimeState objectiveState, bool objectiveCompleted, bool missionStateChanged)
    {
        if (missionStateChanged)
        {
            return $"Progreso aplicado. Objetivo: {objectiveState.ObjectiveId} {objectiveState.GetProgressText()}. La misión cambió de estado.";
        }

        if (objectiveCompleted)
        {
            return $"Objetivo completado: {objectiveState.ObjectiveId}.";
        }

        return $"Objetivo actualizado: {objectiveState.ObjectiveId} {objectiveState.GetProgressText()}.";
    }

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
