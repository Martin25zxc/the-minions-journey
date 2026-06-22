/// <summary>
/// Prioridad opcional para resolver empates cuando hay varios interactuables cerca.
/// Si un interactuable no implementa esta interfaz, se asume prioridad 0.
/// </summary>
public interface IPlayerInteractablePriority
{
    int InteractionPriority { get; }
}
