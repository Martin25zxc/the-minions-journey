/// <summary>
/// Acción que una regla de cadena puede ejecutar sobre la misión destino.
/// MakeAvailable se usa cuando el jugador debe aceptar la misión desde un actor.
/// StartMission se usa para continuidad inmediata de la cadena, sin volver a un NPC.
/// </summary>
public enum MissionChainAction
{
    MakeAvailable = 0,
    StartMission = 1
}
