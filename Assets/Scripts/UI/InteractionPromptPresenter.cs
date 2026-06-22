using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractionPromptPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerInteractionFocusController focusController;

    [Tooltip("Opcional. Si está asignado, el prompt toma de acá el texto visual del input actual para evitar duplicar 'F' en varios lugares.")]
    [SerializeField]
    private PlayerInteractionController interactionController;

    [SerializeField]
    private InteractionPromptView promptView;

    [Header("Input Display")]
    [Tooltip("Si está activo y hay controller asignado, usa PlayerInteractionController.InteractInputDisplayText.")]
    [SerializeField]
    private bool useControllerInputDisplayText = true;

    [Tooltip("Texto visual de respaldo si no hay controller asignado. No escribir esto dentro de cada interactuable.")]
    [SerializeField]
    private string fallbackInteractInputDisplayText = "F";

    [Tooltip("Formato del prompt. {0} = input, {1} = acción.")]
    [SerializeField]
    private string promptFormat = "{0} - {1}";

    [Header("Labels")]
    [SerializeField]
    private InteractionPromptVerbTextSet verbTexts = new();

    [Header("Behaviour")]
    [Tooltip("Si está activo, oculta el prompt cuando el interactuable actual ya no puede interactuarse.")]
    [SerializeField]
    private bool hideWhenCannotInteract = true;

    [Tooltip("Cada cuánto se refresca la vista mientras el foco no cambia. Útil si CanInteract cambia por amenaza, estado del mundo o UI.")]
    [SerializeField, Min(0.02f)]
    private float refreshInterval = 0.1f;

    private float nextRefreshTime;

    private void Reset()
    {
        promptView = GetComponent<InteractionPromptView>();
        focusController = FindFirstObjectByType<PlayerInteractionFocusController>();
        interactionController = FindFirstObjectByType<PlayerInteractionController>();
    }

    private void Awake()
    {
        if (promptView == null)
        {
            promptView = GetComponent<InteractionPromptView>();
        }
    }

    private void OnEnable()
    {
        if (focusController != null)
        {
            focusController.FocusedInteractableChanged += HandleFocusedInteractableChanged;
        }

        ForceRefreshPrompt();
    }

    private void OnDisable()
    {
        if (focusController != null)
        {
            focusController.FocusedInteractableChanged -= HandleFocusedInteractableChanged;
        }

        promptView?.Hide();
    }

    private void Update()
    {
        if (Time.time < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.time + refreshInterval;
        RefreshPrompt();
    }

    public void SetFallbackInputDisplayText(string value)
    {
        fallbackInteractInputDisplayText = value;
        ForceRefreshPrompt();
    }

    public void ForceRefreshPrompt()
    {
        nextRefreshTime = Time.time + refreshInterval;
        RefreshPrompt();
    }

    private void HandleFocusedInteractableChanged(IPlayerInteractable previous, IPlayerInteractable current)
    {
        ForceRefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (promptView == null || focusController == null)
        {
            return;
        }

        if (!focusController.TryGetFocusedInteractable(out IPlayerInteractable interactable, out InteractionContext context))
        {
            promptView.Hide();
            return;
        }

        if (hideWhenCannotInteract && !interactable.CanInteract(context))
        {
            promptView.Hide();
            return;
        }

        InteractionPromptData promptData = interactable.GetInteractionPrompt(context);
        string actionLabel = verbTexts.GetLabel(promptData);
        string inputLabel = GetInputDisplayText();
        string promptText = string.Format(promptFormat, inputLabel, actionLabel);

        promptView.Show(promptText);
    }

    private string GetInputDisplayText()
    {
        if (useControllerInputDisplayText && interactionController != null)
        {
            return interactionController.InteractInputDisplayText;
        }

        return string.IsNullOrWhiteSpace(fallbackInteractInputDisplayText)
            ? "F"
            : fallbackInteractInputDisplayText;
    }
}
