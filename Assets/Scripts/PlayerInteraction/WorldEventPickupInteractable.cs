using UnityEngine;

/// <summary>
/// Adapter entre el sistema de interacción contextual del jugador y WorldEventPickup.
/// 
/// No reporta eventos por sí mismo.
/// No conoce MissionManager.
/// No consume visuales directamente.
/// Solo expone un WorldEventPickup como IPlayerInteractable cuando su CollectMode es Interact.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(WorldEventPickup))]
public sealed class WorldEventPickupInteractable : MonoBehaviour, IPlayerInteractable, IPlayerInteractablePriority
{
    [Header("References")]
    [SerializeField]
    private WorldEventPickup pickup;

    [Header("Interaction")]
    [SerializeField]
    private bool requireInteractCollectMode = true;

    [SerializeField]
    private bool canInteractWhileThreatened;

    [SerializeField]
    private int interactionPriority = 50;

    [Tooltip("Texto usado si la Definition no tiene DefaultPromptVerb.")]
    [SerializeField]
    private string fallbackPromptLabel = "Recoger";

    [Header("Debug")]
    [SerializeField]
    private bool logDebug;

    public int InteractionPriority => interactionPriority;

    private void Reset()
    {
        pickup = GetComponent<WorldEventPickup>();
    }

    private void Awake()
    {
        if (pickup == null)
        {
            pickup = GetComponent<WorldEventPickup>();
        }
    }

    public bool CanInteract(InteractionContext context)
    {
        if (pickup == null)
        {
            return false;
        }

        if (!gameObject.activeInHierarchy)
        {
            return false;
        }

        if (pickup.HasReported)
        {
            return false;
        }

        if (pickup.Definition == null)
        {
            return false;
        }

        if (requireInteractCollectMode &&
            pickup.ActiveCollectMode != WorldEventPickupCollectMode.Interact)
        {
            return false;
        }

        if (!canInteractWhileThreatened &&
            context.ThreatTracker != null &&
            context.ThreatTracker.IsInCombat)
        {
            return false;
        }

        return true;
    }

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context))
        {
            return;
        }

        GameObject collector = context.PlayerObject;

        bool collected = pickup.TryCollect(collector);

        if (logDebug)
        {
            Debug.Log(
                collected
                    ? $"{name} was collected through interaction."
                    : $"{name} interaction was attempted, but collection failed.",
                this);
        }
    }

    public InteractionPromptData GetInteractionPrompt(InteractionContext context)
    {
        string prompt = fallbackPromptLabel;

        if (pickup != null &&
            pickup.Definition != null &&
            !string.IsNullOrWhiteSpace(pickup.Definition.DefaultPromptVerb))
        {
            prompt = pickup.Definition.DefaultPromptVerb;
        }

        return InteractionPromptData.Custom(prompt);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (pickup == null)
        {
            pickup = GetComponent<WorldEventPickup>();
        }

        if (interactionPriority < -1000)
        {
            interactionPriority = -1000;
        }

        if (interactionPriority > 1000)
        {
            interactionPriority = 1000;
        }

        if (string.IsNullOrWhiteSpace(fallbackPromptLabel))
        {
            fallbackPromptLabel = "Recoger";
        }
    }
#endif
}