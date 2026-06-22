using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class DebugInteractable : MonoBehaviour, IPlayerInteractable, IPlayerInteractablePriority
{
    [Header("Debug Interactable")]
    [SerializeField]
    private string debugMessage = "Debug interactable used.";

    [SerializeField]
    private int interactionPriority;
    [SerializeField]
    private bool canInteractWhileThreatened = true;

    [SerializeField]
    private bool canInteract = false;

    [Header("Prompt")]
    [SerializeField]
    private InteractionPromptVerb promptVerb = InteractionPromptVerb.Interact;

    [Tooltip("Solo se usa si Prompt Verb = Custom.")]
    [SerializeField]
    private string customPromptLabel;

    [Header("Visual Test")]
    [Tooltip("Opcional: objeto a activar/desactivar para comprobar visualmente la interacción.")]
    [SerializeField]
    private GameObject objectToToggle;

    [SerializeField]
    private bool rotateOnInteract = true;

    public int InteractionPriority => interactionPriority;

    private void Reset()
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
    }

    public bool CanInteract(InteractionContext context)
    {
        if (!canInteract || !isActiveAndEnabled)
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

    public InteractionPromptData GetInteractionPrompt(InteractionContext context)
    {
        return promptVerb == InteractionPromptVerb.Custom
            ? InteractionPromptData.Custom(customPromptLabel)
            : new InteractionPromptData(promptVerb);
    }

    public void Interact(InteractionContext context)
    {
        Debug.Log($"{debugMessage} Interacted by {context.PlayerObject?.name}.", this);

        if (objectToToggle != null)
        {
            objectToToggle.SetActive(!objectToToggle.activeSelf);
        }

        if (rotateOnInteract)
        {
            transform.Rotate(0f, 45f, 0f, Space.World);
        }
    }
}
