using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerInteractionController : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Tecla usada para la intención de interactuar. Por ahora seguimos el estilo actual del player usando Input System directo.")]
    [SerializeField]
    private Key interactKey = Key.F;

    [Tooltip("Texto que la UI muestra para la acción de interactuar. Mantener centralizado acá evita que cada interactuable escriba 'F - ...'.")]
    [SerializeField]
    private string interactInputDisplayText = "F";

    [Header("Modal State")]
    [Tooltip("Estado global del juego. Si está asignado, F solo se procesa durante Gameplay para no interactuar debajo de Journal/PauseMenu/etc.")]
    [SerializeField]
    private GameStateController gameStateController;

    [Tooltip("Si está activo, cuando falta GameStateController se preserva el comportamiento anterior y se permite interactuar.")]
    [SerializeField]
    private bool allowInteractionWhenGameStateMissing = true;

    [Header("Player Reaction")]
    [Tooltip("Reproduce la animación Interact del jugador al presionar la tecla, aunque no haya un objeto cerca.")]
    [SerializeField]
    private bool playReactionOnInput = true;

    [Tooltip("Guarda las armas en la espalda al presionar interact. No bloquea ataques ni movimiento; solo cambia la pose visual de armas.")]
    [SerializeField]
    private bool sheatheWeaponsOnInput = true;

    [Tooltip("Si está activo, no guarda armas mientras PlayerThreatTracker indique que el player está amenazado/en combate.")]
    [SerializeField]
    private bool sheatheWeaponsOnlyWhenNotThreatened = true;

    [Tooltip("Si está apagado y no hay PlayerThreatTracker asignado, no guarda armas. Es más seguro para no esconder armas por una referencia mal configurada.")]
    [SerializeField]
    private bool allowSheatheWhenThreatTrackerMissing;

    [SerializeField]
    private TopDownPlayerAnimator playerAnimator;

    [SerializeField]
    private TopDownEquipmentVisualManager equipmentVisuals;

    [SerializeField]
    private PlayerThreatTracker threatTracker;

    [Header("Contextual Interaction")]
    [Tooltip("Si está activo, además de la reacción visual intenta interactuar con un IPlayerInteractable cercano.")]
    [SerializeField]
    private bool enableContextualInteraction = true;

    [Tooltip("Foco actual usado por el prompt. Si está asignado, F intenta interactuar primero con el mismo objeto que la UI está mostrando.")]
    [SerializeField]
    private PlayerInteractionFocusController focusController;

    [Tooltip("Scanner usado como fallback cuando no hay foco actual o el foco dejó de ser válido.")]
    [SerializeField]
    private PlayerInteractionScanner scanner;

    [Tooltip("Si está activo, usa el foco actual antes de hacer un scan directo. Recomendado para que el prompt y la acción coincidan.")]
    [SerializeField]
    private bool useFocusedInteractableWhenAvailable = true;

    [Tooltip("Si no hay foco válido, intenta hacer un scan directo al presionar F. Recomendado como respaldo.")]
    [SerializeField]
    private bool scanWhenNoValidFocus = true;

    [Header("Debug")]
    [SerializeField]
    private bool logInteractions;

    [SerializeField]
    private bool logConfigurationWarnings = true;

    private bool warnedMissingThreatTracker;
    private bool warnedMissingInteractionReferences;
    private bool warnedMissingGameStateController;

    public Key InteractKey => interactKey;

    public string InteractInputDisplayText => string.IsNullOrWhiteSpace(interactInputDisplayText)
        ? interactKey.ToString()
        : interactInputDisplayText;

    public PlayerThreatTracker ThreatTracker => threatTracker;

    private void Reset()
    {
        playerAnimator = GetComponent<TopDownPlayerAnimator>();
        equipmentVisuals = GetComponent<TopDownEquipmentVisualManager>();
        threatTracker = GetComponent<PlayerThreatTracker>();
        focusController = GetComponent<PlayerInteractionFocusController>();
        scanner = GetComponent<PlayerInteractionScanner>();
        gameStateController = FindFirstObjectByType<GameStateController>();
    }

    private void Awake()
    {
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<TopDownPlayerAnimator>();
        }

        if (equipmentVisuals == null)
        {
            equipmentVisuals = GetComponent<TopDownEquipmentVisualManager>();
        }

        if (threatTracker == null)
        {
            threatTracker = GetComponent<PlayerThreatTracker>();
        }

        if (focusController == null)
        {
            focusController = GetComponent<PlayerInteractionFocusController>();
        }

        if (scanner == null)
        {
            scanner = GetComponent<PlayerInteractionScanner>();
        }

        if (gameStateController == null)
        {
            gameStateController = FindFirstObjectByType<GameStateController>();
        }

        ValidateConfiguration();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || interactKey == Key.None)
        {
            return;
        }

        if (keyboard[interactKey].wasPressedThisFrame)
        {
            HandleInteractInput();
        }
    }

    public void HandleInteractInput()
    {
        // Importante: si hay una pantalla modal abierta, F no pertenece al contexto actual.
        // No reproducimos animación, no guardamos armas y no mostramos notificación.
        if (!CanProcessInteractionInputNow())
        {
            if (logInteractions)
            {
                Debug.Log($"{name} ignored interact input because current state is not Gameplay.", this);
            }

            return;
        }

        PlayPlayerReaction();

        if (!enableContextualInteraction)
        {
            return;
        }

        if (TryGetInteractableForInput(out IPlayerInteractable interactable, out InteractionContext context))
        {
            if (logInteractions)
            {
                Debug.Log($"{name} interacts with {GetDebugName(interactable)}.", this);
            }

            interactable.Interact(context);
            return;
        }

        if (logInteractions)
        {
            Debug.Log($"{name} pressed interact, but no valid interactable was found.", this);
        }
    }

    public void PlayPlayerReaction()
    {
        if (sheatheWeaponsOnInput && CanSheatheWeaponsNow())
        {
            equipmentVisuals?.SheatheWeapons();
        }

        if (playReactionOnInput)
        {
            playerAnimator?.PlayInteract();
        }
    }

    public bool TryGetInteractableForInput(out IPlayerInteractable interactable, out InteractionContext context)
    {
        interactable = null;
        context = default;

        if (!CanProcessInteractionInputNow())
        {
            return false;
        }

        if (useFocusedInteractableWhenAvailable && focusController != null)
        {
            if (focusController.TryGetFocusedInteractable(out interactable, out context))
            {
                return true;
            }
        }

        if (scanWhenNoValidFocus && scanner != null)
        {
            return scanner.TryFindBestInteractable(out interactable, out context);
        }

        WarnMissingInteractionReferencesOnce();
        return false;
    }

    private bool CanProcessInteractionInputNow()
    {
        if (gameStateController == null)
        {
            WarnMissingGameStateControllerOnce();
            return allowInteractionWhenGameStateMissing;
        }

        return gameStateController.CurrentState == GameState.Gameplay;
    }

    private bool CanSheatheWeaponsNow()
    {
        if (equipmentVisuals == null)
        {
            return false;
        }

        if (!sheatheWeaponsOnlyWhenNotThreatened)
        {
            return true;
        }

        if (threatTracker == null)
        {
            WarnMissingThreatTrackerOnce();
            return allowSheatheWhenThreatTrackerMissing;
        }

        // PlayerThreatTracker se usa como agregador real de amenaza hacia el player.
        // Si en tu versión la propiedad se renombró a IsThreatened, actualizar esta línea y mantener el resto igual.
        return !threatTracker.IsInCombat;
    }

    private void ValidateConfiguration()
    {
        if (!logConfigurationWarnings)
        {
            return;
        }

        if (playReactionOnInput && playerAnimator == null)
        {
            Debug.LogWarning($"{name} has Play Reaction enabled but no {nameof(TopDownPlayerAnimator)} assigned.", this);
        }

        if (sheatheWeaponsOnInput && equipmentVisuals == null)
        {
            Debug.LogWarning($"{name} has Sheathe Weapons enabled but no {nameof(TopDownEquipmentVisualManager)} assigned.", this);
        }

        if (sheatheWeaponsOnInput && sheatheWeaponsOnlyWhenNotThreatened && threatTracker == null && !allowSheatheWhenThreatTrackerMissing)
        {
            Debug.LogWarning(
                $"{name} is configured to sheathe only when not threatened, but no {nameof(PlayerThreatTracker)} is assigned. " +
                "Weapons will not be sheathed until the reference is assigned or Allow Sheathe When Threat Tracker Missing is enabled.",
                this);
        }

        if (enableContextualInteraction && focusController == null && scanner == null)
        {
            Debug.LogWarning(
                $"{name} has contextual interaction enabled but no {nameof(PlayerInteractionFocusController)} or {nameof(PlayerInteractionScanner)} is assigned. " +
                "Assign at least the scanner, or disable contextual interaction.",
                this);
        }

        if (gameStateController == null && !allowInteractionWhenGameStateMissing)
        {
            Debug.LogWarning(
                $"{name} has no {nameof(GameStateController)} assigned and {nameof(allowInteractionWhenGameStateMissing)} is disabled. " +
                "Interaction input will be ignored until the reference is assigned.",
                this);
        }
    }

    private void WarnMissingThreatTrackerOnce()
    {
        if (!logConfigurationWarnings || warnedMissingThreatTracker)
        {
            return;
        }

        warnedMissingThreatTracker = true;
        Debug.LogWarning(
            $"{name} cannot know whether the player is threatened because {nameof(PlayerThreatTracker)} is missing. " +
            "Current configuration will not sheathe weapons.",
            this);
    }

    private void WarnMissingInteractionReferencesOnce()
    {
        if (!logConfigurationWarnings || warnedMissingInteractionReferences)
        {
            return;
        }

        warnedMissingInteractionReferences = true;
        Debug.LogWarning(
            $"{name} tried contextual interaction, but no valid focus or scanner is available.",
            this);
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
            $"Interaction input will {(allowInteractionWhenGameStateMissing ? "preserve previous behaviour" : "be ignored")}.",
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
