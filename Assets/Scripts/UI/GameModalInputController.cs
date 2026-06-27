using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Input global mínimo para resolver pantallas modales.
///
/// ESC no pertenece al Journal ni al PauseMenu de forma individual:
/// - Si hay un modal abierto, lo resuelve.
/// - Si estamos en Gameplay, puede abrir PauseMenu cuando el menú visual esté listo.
///
/// Para evitar abrir un PauseMenu invisible durante esta etapa,
/// allowEscapeToOpenPauseMenu arranca apagado.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameModalInputController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField]
    private GameStateController gameStateController;

    [SerializeField]
    private GameplayActionGate actionGate;

    [Header("Input")]
    [SerializeField]
    private Key escapeKey = Key.Escape;

    [Header("Pause Menu")]
    [Tooltip("Si está activo, ESC desde Gameplay abre PauseMenu. Activarlo cuando PauseMenuRoot tenga controller visual funcionando.")]
    [SerializeField]
    private bool allowEscapeToOpenPauseMenu;

    [Header("Debug")]
    [SerializeField]
    private bool logDebug;

    private void Reset()
    {
        gameStateController = FindFirstObjectByType<GameStateController>();
        actionGate = FindFirstObjectByType<GameplayActionGate>();
    }

    private void Awake()
    {
        if (gameStateController == null)
        {
            gameStateController = FindFirstObjectByType<GameStateController>();
        }

        if (actionGate == null)
        {
            actionGate = FindFirstObjectByType<GameplayActionGate>();
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || escapeKey == Key.None)
        {
            return;
        }

        if (keyboard[escapeKey].wasPressedThisFrame)
        {
            ResolveEscape();
        }
    }

    private void ResolveEscape()
    {
        if (gameStateController == null)
        {
            Debug.LogWarning($"{nameof(GameModalInputController)} necesita {nameof(GameStateController)}.", this);
            return;
        }

        if (gameStateController.CurrentState == GameState.Gameplay)
        {
            if (!allowEscapeToOpenPauseMenu)
            {
                if (logDebug)
                {
                    Debug.Log("ESC ignorado en Gameplay porque Allow Escape To Open Pause Menu está apagado.", this);
                }

                return;
            }

            if (actionGate != null)
            {
                GameplayActionBlockResult result = actionGate.GetBlockResult(GameplayActionType.OpenPauseMenu);
                if (!result.IsAllowed)
                {
                    if (logDebug)
                    {
                        Debug.Log(result.Reason, this);
                    }

                    return;
                }
            }
        }

        bool resolved = gameStateController.TryResolveEscape();

        if (logDebug)
        {
            Debug.Log($"ESC resuelto: {resolved}. Estado actual: {gameStateController.CurrentState}", this);
        }
    }
}
