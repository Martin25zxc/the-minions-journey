using UnityEngine;

/// <summary>
/// Contexto mínimo de una interacción iniciada por el jugador.
/// No conoce misiones, inventario ni UI: solo transporta quién interactúa y desde dónde.
/// </summary>
public readonly struct InteractionContext
{
    public InteractionContext(
        GameObject playerObject,
        Transform playerTransform,
        Transform interactionOrigin,
        PlayerInteractionController controller)
    {
        PlayerObject = playerObject;
        PlayerTransform = playerTransform;
        InteractionOrigin = interactionOrigin;
        Controller = controller;
    }

    public GameObject PlayerObject { get; }
    public Transform PlayerTransform { get; }
    public Transform InteractionOrigin { get; }
    public PlayerInteractionController Controller { get; }

    public Vector3 OriginPosition => InteractionOrigin != null
        ? InteractionOrigin.position
        : PlayerTransform != null
            ? PlayerTransform.position
            : Vector3.zero;
}
