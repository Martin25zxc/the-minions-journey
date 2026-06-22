/// <summary>
/// Contrato base para cualquier objeto que pueda recibir una interacción contextual del jugador.
/// No lee input y no decide cómo se busca: solo responde cuando el player intenta interactuar.
/// </summary>
public interface IPlayerInteractable
{
    bool CanInteract(InteractionContext context);
    void Interact(InteractionContext context);
}
