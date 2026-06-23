/// <summary>
/// Estados visuales principales para NPCs humanoides de escena.
/// Representan poses o estados de puesta en escena, no lógica de misión ni vida real.
/// </summary>
public enum NpcHumanoidSceneState
{
    /// <summary>
    /// NPC vivo o neutral, usando idle normal.
    /// </summary>
    Idle = 0,

    /// <summary>
    /// NPC debilitado, herido, sentado o agotado.
    /// </summary>
    Weakened = 10,

    /// <summary>
    /// Pose de muerto variante A.
    /// </summary>
    DeadA = 20,

    /// <summary>
    /// Pose de muerto variante B.
    /// </summary>
    DeadB = 30
}
