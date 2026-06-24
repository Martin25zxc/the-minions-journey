/// <summary>
/// Acción de misión que un actor puede resolver cuando el jugador habla con él.
/// Es runtime: no es estado de misión ni estado visual del indicador.
/// </summary>
public enum MissionActorResolvedActionType
{
    None = 0,
    AcceptMission = 10,
    TurnInMission = 20,
    ShowPendingMission = 30,
    ShowCompletedMission = 40,
    Unavailable = 50
}
