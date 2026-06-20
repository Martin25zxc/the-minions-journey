using UnityEngine;

public sealed class GameplayActionGateDebugTester : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Controlador del estado general del juego.")]
    [SerializeField] private GameStateController gameStateController;

    [Tooltip("Gate que valida si una acción se puede ejecutar o no.")]
    [SerializeField] private GameplayActionGate actionGate;

    private void Reset()
    {
        gameStateController = FindFirstObjectByType<GameStateController>();
        actionGate = FindFirstObjectByType<GameplayActionGate>();
    }

    private void Update()
    {
    }

    private void TryToggleJournal()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        GameplayActionBlockResult result = actionGate.GetBlockResult(GameplayActionType.OpenMissionJournal);

        if (!result.IsAllowed)
        {
            Debug.Log(result.Reason, this);
            return;
        }

        bool changed = gameStateController.TryToggleMissionJournal();

        if (changed)
        {
            Debug.Log($"Debug: Mission Journal toggleado. Estado actual: {gameStateController.CurrentState}", this);
        }
    }

    private void TryResolveEscape()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        GameplayActionBlockResult result = actionGate.GetBlockResult(GameplayActionType.OpenPauseMenu);

        if (!result.IsAllowed)
        {
            Debug.Log(result.Reason, this);
            return;
        }

        bool changed = gameStateController.TryResolveEscape();

        if (changed)
        {
            Debug.Log($"Debug: Escape resuelto. Estado actual: {gameStateController.CurrentState}", this);
        }
    }

    private void TryInteract()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        GameplayActionBlockResult result = actionGate.GetBlockResult(GameplayActionType.Interact);

        if (!result.IsAllowed)
        {
            Debug.Log(result.Reason, this);
            return;
        }

        Debug.Log("Debug: interacción permitida.", this);
    }

    private bool HasRequiredReferences()
    {
        if (gameStateController == null)
        {
            Debug.LogWarning($"Falta asignar {nameof(GameStateController)}.", this);
            return false;
        }

        if (actionGate == null)
        {
            Debug.LogWarning($"Falta asignar {nameof(GameplayActionGate)}.", this);
            return false;
        }

        return true;
    }
}
