using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base común para interactuables reales del juego.
/// Centraliza reglas compartidas: ID estable, prioridad, amenaza, prompt y feedback opcional.
/// No conoce misiones, inventario ni diálogo final.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class PlayerInteractableBase : MonoBehaviour, IPlayerInteractable, IPlayerInteractablePriority
{
    [Header("Identidad")]
    [Tooltip("ID estable de contenido. Usar snake_case. Ejemplos: npc_dying_hopplo, npc_nature_ent, object_old_gate.")]
    [SerializeField]
    private string interactableId;

    [Tooltip("Nombre legible para debug, título de notificación o futura UI. Si queda vacío se usa el nombre del GameObject.")]
    [SerializeField]
    private string displayName;

    [Header("Reglas de interacción")]
    [Tooltip("Prioridad usada cuando hay varios interactuables en rango. NPCs importantes deberían tener prioridad mayor que objetos de ambiente.")]
    [SerializeField]
    private int interactionPriority;

    [Tooltip("Permite desactivar temporalmente este interactuable sin desactivar el GameObject completo.")]
    [SerializeField]
    private bool canInteract = true;

    [Tooltip("Si está apagado, no se puede interactuar cuando PlayerThreatTracker indica que el jugador está amenazado/en combate.")]
    [SerializeField]
    private bool canInteractWhileThreatened = true;

    [Header("Prompt")]
    [Tooltip("Verbo semántico del prompt. No incluye la tecla. La UI combina input + verbo.")]
    [SerializeField]
    private InteractionPromptVerb promptVerb = InteractionPromptVerb.Interact;

    [Tooltip("Solo se usa si Prompt Verb = Custom. No incluir la tecla; escribir solo el verbo o acción, por ejemplo: Escuchar.")]
    [SerializeField]
    private string customPromptLabel;

    [Header("Feedback opcional")]
    [Tooltip("Si está activo, al interactuar intenta mostrar una notificación con el mensaje configurado. Si no hay NotificationManager, usa Debug.Log como fallback.")]
    [SerializeField]
    private bool showNotificationOnInteract = true;

    [Tooltip("Opcional. Si está vacío, Reset intenta encontrar un NotificationManager en la escena.")]
    [SerializeField]
    private NotificationManager notificationManager;

    [Tooltip("Canal usado para la notificación de feedback. Para NPCs de historia suele servir Lore; para quest givers puede cambiarse a Mission.")]
    [SerializeField]
    private NotificationChannel notificationChannel = NotificationChannel.Lore;

    [Tooltip("Prioridad de la notificación. Mantener Normal salvo que sea un feedback importante.")]
    [SerializeField]
    private NotificationPriority notificationPriority = NotificationPriority.Normal;

    [Tooltip("Duración de la notificación. Usar -1 para respetar la duración por defecto del NotificationManager.")]
    [SerializeField]
    private float notificationDuration = -1f;

    [Tooltip("Mensaje mostrado al interactuar. Puede quedar vacío si otro sistema se conecta por evento.")]
    [SerializeField, TextArea(2, 5)]
    private string interactionMessage;

    [Tooltip("Muestra un log de debug cuando se ejecuta la interacción. Útil durante la primera integración.")]
    [SerializeField]
    private bool logInteraction = true;

    [Header("Eventos")]
    [Tooltip("Evento simple de Inspector para enganchar prototipos sin acoplar esta clase a misiones o diálogo.")]
    [SerializeField]
    private UnityEvent onInteracted;

    public int InteractionPriority => interactionPriority;
    public string InteractableId => interactableId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public bool CanInteractWhileThreatened => canInteractWhileThreatened;

    /// <summary>
    /// Evento C# para sistemas futuros. No reemplaza el UnityEvent del Inspector.
    /// </summary>
    public event Action<PlayerInteractableBase, InteractionContext> Interacted;

    protected virtual void Reset()
    {
        Collider ownCollider = GetComponent<Collider>();
        if (ownCollider != null)
        {
            ownCollider.isTrigger = true;
        }

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer >= 0)
        {
            gameObject.layer = interactableLayer;
        }

        notificationManager = FindFirstObjectByType<NotificationManager>();
    }

    protected virtual void OnValidate()
    {
        if (notificationDuration < -1f)
        {
            notificationDuration = -1f;
        }
    }

    public bool CanInteract(InteractionContext context)
    {
        if (!isActiveAndEnabled || !canInteract)
        {
            return false;
        }

        if (!canInteractWhileThreatened &&
            context.ThreatTracker != null &&
            context.ThreatTracker.IsInCombat)
        {
            return false;
        }

        return CanInteractCore(context);
    }

    public InteractionPromptData GetInteractionPrompt(InteractionContext context)
    {
        if (promptVerb == InteractionPromptVerb.Custom)
        {
            return InteractionPromptData.Custom(customPromptLabel);
        }

        return new InteractionPromptData(promptVerb);
    }

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context))
        {
            return;
        }

        if (logInteraction)
        {
            Debug.Log($"{DisplayName} interacted by {context.PlayerObject?.name ?? "Unknown Player"}.", this);
        }

        ExecuteInteraction(context);

        onInteracted?.Invoke();
        Interacted?.Invoke(this, context);
    }

    /// <summary>
    /// Regla extra para clases hijas. La base ya valida activo, flag manual y amenaza.
    /// </summary>
    protected virtual bool CanInteractCore(InteractionContext context)
    {
        return true;
    }

    /// <summary>
    /// Acción concreta del interactuable. Las clases hijas deciden qué significa interactuar.
    /// </summary>
    protected abstract void ExecuteInteraction(InteractionContext context);

    /// <summary>
    /// Muestra el mensaje configurado o un override puntual. No hace nada si no hay mensaje.
    /// </summary>
    protected void ShowInteractionFeedback(string overrideMessage = null)
    {
        string message = string.IsNullOrWhiteSpace(overrideMessage) ? interactionMessage : overrideMessage;
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (showNotificationOnInteract && notificationManager != null)
        {
            string title = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName;
            string groupKey = BuildNotificationGroupKey(message);
            notificationManager.Show(NotificationData.Create(
                message,
                notificationChannel,
                notificationPriority,
                notificationDuration,
                title,
                groupKey));
            return;
        }

        Debug.Log($"[{DisplayName}] {message}", this);
    }

    protected void SetDefaultPromptVerb(InteractionPromptVerb value)
    {
        promptVerb = value;
    }

    protected void SetDefaultInteractionPriority(int value)
    {
        interactionPriority = value;
    }

    protected void SetDefaultCanInteractWhileThreatened(bool value)
    {
        canInteractWhileThreatened = value;
    }

    protected void SetDefaultNotificationChannel(NotificationChannel value)
    {
        notificationChannel = value;
    }

    protected void SetDefaultInteractionMessage(string value)
    {
        if (string.IsNullOrWhiteSpace(interactionMessage))
        {
            interactionMessage = value;
        }
    }

    private string BuildNotificationGroupKey(string message)
    {
        string idPart = string.IsNullOrWhiteSpace(interactableId) ? name : interactableId.Trim();
        return $"interact:{idPart}:{message.Trim().ToLowerInvariant()}";
    }
}
