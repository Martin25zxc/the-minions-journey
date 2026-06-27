using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerInteractionFocusController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerInteractionScanner scanner;

    [SerializeField, Tooltip("Estado global del juego. Si está asignado, el foco de interacción solo existe durante Gameplay.")]
    private GameStateController gameStateController;

    [Tooltip("Si está activo, cuando falta GameStateController se preserva el comportamiento anterior y se permite mostrar foco.")]
    [SerializeField]
    private bool allowFocusWhenGameStateMissing = true;

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

    [SerializeField]
    private bool logConfigurationWarnings = true;

    private float nextRefreshTime;
    private IPlayerInteractable currentInteractable;
    private InteractionContext currentContext;
    private bool warnedMissingGameStateController;

    public event Action<IPlayerInteractable, IPlayerInteractable> FocusedInteractableChanged;

    public IPlayerInteractable CurrentInteractable => currentInteractable;
    public InteractionContext CurrentContext => currentContext;
    public bool HasFocus => currentInteractable != null;

    private void Reset()
    {
        scanner = GetComponent<PlayerInteractionScanner>();
        gameStateController = FindFirstObjectByType<GameStateController>();
    }

    private void Awake()
    {
        if (scanner == null)
        {
            scanner = GetComponent<PlayerInteractionScanner>();
        }

        if (gameStateController == null)
        {
            gameStateController = FindFirstObjectByType<GameStateController>();
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
        // Si hay Journal/Pause/Dialog/etc. abierto, el prompt de F no debe quedar vivo.
        // Usamos GameStateController en vez de GameplayActionGate para no cambiar reglas de combate existentes.
        if (!CanShowFocusNow())
        {
            nextRefreshTime = Time.time;
            SetFocus(null, default);
            return;
        }

        if (Time.time < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.time + refreshInterval;
        RefreshFocus();
    }

    public void ForceRefreshFocus()
    {
        if (!CanShowFocusNow())
        {
            nextRefreshTime = Time.time;
            SetFocus(null, default);
            return;
        }

        nextRefreshTime = Time.time + refreshInterval;
        RefreshFocus();
    }

    public bool TryGetFocusedInteractable(out IPlayerInteractable interactable, out InteractionContext context)
    {
        interactable = currentInteractable;
        context = currentContext;

        if (!CanShowFocusNow())
        {
            interactable = null;
            context = default;
            return false;
        }

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

    private bool CanShowFocusNow()
    {
        if (gameStateController == null)
        {
            WarnMissingGameStateControllerOnce();
            return allowFocusWhenGameStateMissing;
        }

        return gameStateController.CurrentState == GameState.Gameplay;
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

    private void WarnMissingGameStateControllerOnce()
    {
        if (!logConfigurationWarnings || warnedMissingGameStateController)
        {
            return;
        }

        warnedMissingGameStateController = true;
        Debug.LogWarning(
            $"{name} has no {nameof(GameStateController)} assigned. " +
            $"Interaction focus will {(allowFocusWhenGameStateMissing ? "preserve previous behaviour" : "stay hidden")}.",
            this);
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
