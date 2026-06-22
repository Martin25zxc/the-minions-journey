using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerInteractionFocusController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerInteractionScanner scanner;

    [Header("Focus")]
    [Tooltip("Cada cuánto se recalcula el foco para el prompt. No hace falta hacerlo cada frame.")]
    [SerializeField, Min(0.02f)]
    private float refreshInterval = 0.15f;

    [Tooltip("Mantiene el foco actual si sigue válido y no aparece otro candidato con más prioridad. Reduce parpadeos entre objetos cercanos.")]
    [SerializeField]
    private bool keepCurrentFocusWhenStillValid = true;

    [Header("Debug")]
    [SerializeField]
    private bool logFocusChanges;

    private float nextRefreshTime;
    private IPlayerInteractable currentInteractable;
    private InteractionContext currentContext;

    public event Action<IPlayerInteractable, IPlayerInteractable> FocusedInteractableChanged;

    public IPlayerInteractable CurrentInteractable => currentInteractable;
    public InteractionContext CurrentContext => currentContext;
    public bool HasFocus => currentInteractable != null;

    private void Reset()
    {
        scanner = GetComponent<PlayerInteractionScanner>();
    }

    private void Awake()
    {
        if (scanner == null)
        {
            scanner = GetComponent<PlayerInteractionScanner>();
        }
    }

    private void OnEnable()
    {
        ForceRefreshFocus();
    }

    private void OnDisable()
    {
        SetFocus(null, default);
    }

    private void Update()
    {
        if (Time.time < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.time + refreshInterval;
        RefreshFocus();
    }

    public void ForceRefreshFocus()
    {
        nextRefreshTime = Time.time + refreshInterval;
        RefreshFocus();
    }

    public bool TryGetFocusedInteractable(out IPlayerInteractable interactable, out InteractionContext context)
    {
        interactable = currentInteractable;
        context = currentContext;

        if (scanner == null || interactable == null)
        {
            return false;
        }

        context = scanner.BuildContext();
        if (!scanner.IsFocusable(interactable, context))
        {
            return false;
        }

        return true;
    }

    private void RefreshFocus()
    {
        if (scanner == null)
        {
            SetFocus(null, default);
            return;
        }

        InteractionContext context = scanner.BuildContext();

        bool hasBest = scanner.TryFindBestInteractable(context, out IPlayerInteractable bestInteractable);
        if (!hasBest)
        {
            SetFocus(null, context);
            return;
        }

        if (keepCurrentFocusWhenStillValid && currentInteractable != null)
        {
            bool currentStillValid = scanner.IsFocusable(currentInteractable, context);
            if (currentStillValid)
            {
                int currentPriority = scanner.GetPriority(currentInteractable);
                int bestPriority = scanner.GetPriority(bestInteractable);

                if (bestPriority <= currentPriority)
                {
                    SetFocus(currentInteractable, context);
                    return;
                }
            }
        }

        SetFocus(bestInteractable, context);
    }

    private void SetFocus(IPlayerInteractable newInteractable, InteractionContext context)
    {
        if (ReferenceEquals(currentInteractable, newInteractable))
        {
            currentContext = context;
            return;
        }

        IPlayerInteractable previous = currentInteractable;
        currentInteractable = newInteractable;
        currentContext = context;

        if (logFocusChanges)
        {
            Debug.Log($"Interaction focus changed: {GetDebugName(previous)} -> {GetDebugName(newInteractable)}", this);
        }

        FocusedInteractableChanged?.Invoke(previous, newInteractable);
    }

    private static string GetDebugName(IPlayerInteractable interactable)
    {
        return interactable is Component component && component != null
            ? component.name
            : interactable != null
                ? interactable.ToString()
                : "None";
    }
}
