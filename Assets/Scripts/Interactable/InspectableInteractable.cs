using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Interactuable real para objetos examinables: ruinas, carteles, restos, fogatas o elementos de lore.
/// Se incluye en 5A como base futura, aunque la primera validación del paso se enfoque en NPC quest givers.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class InspectableInteractable : PlayerInteractableBase
{
    [Header("Inspectable")]
    [Tooltip("Evento específico para enganchar respuestas de prototipo cuando el objeto es examinado.")]
    [SerializeField]
    private UnityEvent onInspected;

    public event Action<InspectableInteractable, InteractionContext> Inspected;

    protected override void Reset()
    {
        base.Reset();

        SetDefaultPromptVerb(InteractionPromptVerb.Examine);
        SetDefaultInteractionPriority(0);
        SetDefaultCanInteractWhileThreatened(false);
        SetDefaultNotificationChannel(NotificationChannel.Lore);
        SetDefaultInteractionMessage("No hay descripción configurada para este objeto.");
    }

    protected override void ExecuteInteraction(InteractionContext context)
    {
        ShowInteractionFeedback();

        onInspected?.Invoke();
        Inspected?.Invoke(this, context);
    }
}
