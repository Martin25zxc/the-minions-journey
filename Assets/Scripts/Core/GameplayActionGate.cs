using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameplayActionGate : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Controlador central del estado del juego. Normalmente vive en _GameSystems.")]
    [SerializeField] private GameStateController gameStateController;

    [Tooltip("Estado de combate del jugador. Si queda vacío, las reglas funcionan pero nunca detectan combate.")]
    [SerializeField] private PlayerThreatTracker playerThreatTracker;

    [Header("Mensajes de bloqueo")]
    [Tooltip("Mensaje cuando el jugador intenta abrir el diario mientras está en combate.")]
    [SerializeField] private string combatJournalBlockedMessage = "No puedes abrir el diario en combate.";

    [Tooltip("Mensaje cuando el jugador intenta aceptar o entregar misiones en combate.")]
    [SerializeField] private string combatMissionBlockedMessage = "No puedes gestionar misiones en combate.";

    [Tooltip("Mensaje cuando el jugador intenta arrancar diálogo largo en combate.")]
    [SerializeField] private string combatDialogueBlockedMessage = "Hablar ahora parece poco saludable.";

    [Tooltip("Mensaje genérico cuando el estado actual no permite la acción.")]
    [SerializeField] private string notGameplayBlockedMessage = "No puedes hacer eso ahora.";

    private void Reset()
    {
        gameStateController = FindFirstObjectByType<GameStateController>();
        playerThreatTracker = FindFirstObjectByType<PlayerThreatTracker>();
    }

    public bool CanExecute(GameplayActionType actionType)
    {
        return GetBlockResult(actionType).IsAllowed;
    }

    public GameplayActionBlockResult GetBlockResult(GameplayActionType actionType)
    {
        if (gameStateController == null)
        {
            return GameplayActionBlockResult.Blocked($"Falta asignar {nameof(GameStateController)} en {nameof(GameplayActionGate)}.");
        }

        bool isInCombat = playerThreatTracker != null && playerThreatTracker.IsInCombat;

        switch (actionType)
        {
            case GameplayActionType.OpenPauseMenu:
                return CanOpenPauseMenu();

            case GameplayActionType.OpenMissionJournal:
                return CanOpenMissionJournal(isInCombat);

            case GameplayActionType.AcceptMission:
            case GameplayActionType.TurnInMission:
                return CanUseMissionInteraction(isInCombat);

            case GameplayActionType.StartDialogue:
                return CanStartDialogue(isInCombat);

            case GameplayActionType.Interact:
                return CanInteract(isInCombat);

            case GameplayActionType.UseAbility:
            case GameplayActionType.Attack:
                return CanUseCombatAction();

            default:
                return GameplayActionBlockResult.Allowed();
        }
    }

    private GameplayActionBlockResult CanOpenPauseMenu()
    {
        if (gameStateController.CurrentState == GameState.Cutscene ||
            gameStateController.CurrentState == GameState.GameOver)
        {
            return GameplayActionBlockResult.Blocked(notGameplayBlockedMessage);
        }

        return GameplayActionBlockResult.Allowed();
    }

    private GameplayActionBlockResult CanOpenMissionJournal(bool isInCombat)
    {
        if (isInCombat)
        {
            return GameplayActionBlockResult.Blocked(combatJournalBlockedMessage);
        }

        if (gameStateController.CurrentState != GameState.Gameplay &&
            gameStateController.CurrentState != GameState.MissionJournal)
        {
            return GameplayActionBlockResult.Blocked(notGameplayBlockedMessage);
        }

        return GameplayActionBlockResult.Allowed();
    }

    private GameplayActionBlockResult CanUseMissionInteraction(bool isInCombat)
    {
        if (isInCombat)
        {
            return GameplayActionBlockResult.Blocked(combatMissionBlockedMessage);
        }

        if (gameStateController.CurrentState != GameState.Gameplay)
        {
            return GameplayActionBlockResult.Blocked(notGameplayBlockedMessage);
        }

        return GameplayActionBlockResult.Allowed();
    }

    private GameplayActionBlockResult CanStartDialogue(bool isInCombat)
    {
        if (isInCombat)
        {
            return GameplayActionBlockResult.Blocked(combatDialogueBlockedMessage);
        }

        if (gameStateController.CurrentState != GameState.Gameplay)
        {
            return GameplayActionBlockResult.Blocked(notGameplayBlockedMessage);
        }

        return GameplayActionBlockResult.Allowed();
    }

    private GameplayActionBlockResult CanInteract(bool isInCombat)
    {
        if (isInCombat)
        {
            return GameplayActionBlockResult.Blocked(combatMissionBlockedMessage);
        }

        if (gameStateController.CurrentState != GameState.Gameplay)
        {
            return GameplayActionBlockResult.Blocked(notGameplayBlockedMessage);
        }

        return GameplayActionBlockResult.Allowed();
    }

    private GameplayActionBlockResult CanUseCombatAction()
    {
        if (gameStateController.CurrentState != GameState.Gameplay)
        {
            return GameplayActionBlockResult.Blocked(notGameplayBlockedMessage);
        }

        return GameplayActionBlockResult.Allowed();
    }
}
