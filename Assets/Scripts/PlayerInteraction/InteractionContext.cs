using UnityEngine;

/// <summary>
/// Contexto mínimo de una interacción iniciada por el jugador.
/// Transporta datos propios del player para que los interactuables no tengan que buscar referencias comunes.
/// No conoce misiones, inventario, UI ni mundo.
/// </summary>
public readonly struct InteractionContext
{
    public InteractionContext(
        GameObject playerObject,
        Transform playerTransform,
        Transform interactionOrigin,
        PlayerInteractionController controller,
        PlayerThreatTracker threatTracker)
    {
        PlayerObject = playerObject;
        PlayerTransform = playerTransform;
        InteractionOrigin = interactionOrigin;
        Controller = controller;
        ThreatTracker = threatTracker;
    }

    public GameObject PlayerObject { get; }
    public Transform PlayerTransform { get; }
    public Transform InteractionOrigin { get; }
    public PlayerInteractionController Controller { get; }
    public PlayerThreatTracker ThreatTracker { get; }

    public Vector3 OriginPosition => InteractionOrigin != null
        ? InteractionOrigin.position
        : PlayerTransform != null
            ? PlayerTransform.position
            : Vector3.zero;
}
