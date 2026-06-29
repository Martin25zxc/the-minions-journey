using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Input global mínimo para resolver pantallas modales.
///
/// ESC no pertenece al Journal ni al PauseMenu de forma individual:
/// - Si hay ConfirmDialog abierto, lo cierra.
/// - Si hay un modal de GameState abierto, lo resuelve.
/// - Si estamos en Gameplay, puede abrir PauseMenu cuando el menú visual esté listo.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameModalInputController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField]
    private GameStateController gameStateController;

    [SerializeField]
    private GameplayActionGate actionGate;

    [SerializeField, Tooltip("Submodal de confirmación. Si está abierto, ESC lo cierra antes de resolver PauseMenu.")]
    private ConfirmDialogController confirmDialogController;

    [Header("Input")]
    [SerializeField]
    private Key escapeKey = Key.Escape;

    [Header("Pause Menu")]
    [Tooltip("Si está activo, ESC desde Gameplay abre PauseMenu. Activarlo cuando PauseMenuRoot tenga controller visual funcionando.")]
    [SerializeField]
    private bool allowEscapeToOpenPauseMenu = true;

    [Header("Debug")]
    [SerializeField]
    private bool logDebug;

    private void Reset()
    {
        gameStateController = FindFirstObjectByType<GameStateController>();
        actionGate = FindFirstObjectByType<GameplayActionGate>();
        confirmDialogController = FindFirstObjectByType<ConfirmDialogController>(FindObjectsInactive.Include);
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

        if (confirmDialogController == null)
        {
            confirmDialogController = FindFirstObjectByType<ConfirmDialogController>(FindObjectsInactive.Include);
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
        if (confirmDialogController != null && confirmDialogController.IsOpen)
        {
            confirmDialogController.Cancel();

            if (logDebug)
            {
                Debug.Log("ESC cerró ConfirmDialog.", this);
            }

            return;
        }

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
