using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Interactuable real para NPCs. Es apropiado para prototipos de quest giver sin acoplarse todavía al sistema de misiones.
/// Su responsabilidad es responder a F como "Hablar" y exponer eventos para conectar diálogo/misiones luego.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class NpcInteractable : PlayerInteractableBase
{
    [Header("NPC")]
    [Tooltip("Evento específico de NPC. Usarlo para prototipos de diálogo, quest giver o feedback temporal.")]
    [SerializeField]
    private UnityEvent onTalkedTo;

    public event Action<NpcInteractable, InteractionContext> TalkedTo;

    protected override void Reset()
    {
        base.Reset();

        SetDefaultPromptVerb(InteractionPromptVerb.Talk);
        SetDefaultInteractionPriority(50);
        SetDefaultCanInteractWhileThreatened(false);
        SetDefaultNotificationChannel(NotificationChannel.Lore);
        SetDefaultInteractionMessage("Todavía no hay diálogo configurado para este NPC.");
    }

    protected override void ExecuteInteraction(InteractionContext context)
    {
        ShowInteractionFeedback();

        onTalkedTo?.Invoke();
        TalkedTo?.Invoke(this, context);
    }
}
