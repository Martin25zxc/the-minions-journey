public readonly struct MissionProgressResult
{
    public MissionProgressResult(
        bool objectiveProgressed,
        bool objectiveCompleted,
        bool missionStateChanged,
        MissionState previousMissionState,
        MissionState currentMissionState,
        MissionObjectiveRuntimeState objectiveState,
        string message)
    {
        ObjectiveProgressed = objectiveProgressed;
        ObjectiveCompleted = objectiveCompleted;
        MissionStateChanged = missionStateChanged;
        PreviousMissionState = previousMissionState;
        CurrentMissionState = currentMissionState;
        ObjectiveState = objectiveState;
        Message = message ?? string.Empty;
    }

    public bool ObjectiveProgressed { get; }
    public bool ObjectiveCompleted { get; }
    public bool MissionStateChanged { get; }
    public MissionState PreviousMissionState { get; }
    public MissionState CurrentMissionState { get; }
    public MissionObjectiveRuntimeState ObjectiveState { get; }
    public string Message { get; }

    public bool HasAnyChange => ObjectiveProgressed || ObjectiveCompleted || MissionStateChanged;
    public bool MissionBecameReadyToTurnIn => MissionStateChanged && CurrentMissionState == MissionState.ReadyToTurnIn;
    public bool MissionCompleted => MissionStateChanged && CurrentMissionState == MissionState.Completed;

    public static MissionProgressResult NoChange(MissionState state, string message = "")
    {
        return new MissionProgressResult(
            objectiveProgressed: false,
            objectiveCompleted: false,
            missionStateChanged: false,
            previousMissionState: state,
            currentMissionState: state,
            objectiveState: null,
            message: message);
    }
}
