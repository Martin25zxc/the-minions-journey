/// <summary>
/// Resultado runtime de resolver qué debería hacer un actor de misión ante una interacción.
/// </summary>
public sealed class MissionActorResolvedAction
{
    private MissionActorResolvedAction(
        MissionActorResolvedActionType actionType,
        MissionRuntimeState missionState,
        MissionActorMissionEntry entry,
        string reason)
    {
        ActionType = actionType;
        MissionState = missionState;
        Entry = entry;
        Reason = reason;
    }

    public MissionActorResolvedActionType ActionType { get; }
    public MissionRuntimeState MissionState { get; }
    public MissionActorMissionEntry Entry { get; }
    public string Reason { get; }

    public bool HasMission => MissionState != null;
    public string MissionId => MissionState != null ? MissionState.MissionId : string.Empty;

    public static MissionActorResolvedAction Accept(MissionRuntimeState missionState, MissionActorMissionEntry entry)
    {
        return new MissionActorResolvedAction(MissionActorResolvedActionType.AcceptMission, missionState, entry, "Misión disponible para aceptar.");
    }

    public static MissionActorResolvedAction TurnIn(MissionRuntimeState missionState, MissionActorMissionEntry entry)
    {
        return new MissionActorResolvedAction(MissionActorResolvedActionType.TurnInMission, missionState, entry, "Misión lista para entregar en este actor.");
    }

    public static MissionActorResolvedAction Pending(MissionRuntimeState missionState, MissionActorMissionEntry entry)
    {
        return new MissionActorResolvedAction(MissionActorResolvedActionType.ShowPendingMission, missionState, entry, "Misión activa, pero todavía no lista para entregar.");
    }

    public static MissionActorResolvedAction Completed(MissionRuntimeState missionState, MissionActorMissionEntry entry)
    {
        return new MissionActorResolvedAction(MissionActorResolvedActionType.ShowCompletedMission, missionState, entry, "Misión ya completada.");
    }

    public static MissionActorResolvedAction Unavailable(string reason)
    {
        return new MissionActorResolvedAction(MissionActorResolvedActionType.Unavailable, null, null, reason);
    }

    public static MissionActorResolvedAction None(string reason)
    {
        return new MissionActorResolvedAction(MissionActorResolvedActionType.None, null, null, reason);
    }
}
