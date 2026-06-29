using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controller del botón MENÚ del HUD.
///
/// Responsabilidad:
/// - Mostrar el botón solo durante Gameplay.
/// - Abrir PauseMenu al hacer click.
///
/// No controla el panel de pausa. Eso lo hace PauseMenuController.
/// </summary>
[DisallowMultipleComponent]
public sealed class HUDMenuButtonController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameStateController gameStateController;
    [SerializeField] private GameplayActionGate actionGate;

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button menuButton;

    [Header("Reglas")]
    [SerializeField, Tooltip("Si está activo, el botón solo se muestra durante Gameplay.")]
    private bool hideWhenNotInGameplay = true;

    [Header("Selección UI")]
    [SerializeField, Tooltip("Limpia selección al hacer click para que el botón no quede selected.")]
    private bool clearSelectionOnClick = true;

    [Header("Debug")]
    [SerializeField] private bool logDebug;

    private void Reset()
    {
        gameStateController = FindFirstObjectByType<GameStateController>();
        actionGate = FindFirstObjectByType<GameplayActionGate>();
        canvasGroup = GetComponent<CanvasGroup>();
        menuButton = GetComponent<Button>();
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

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (menuButton == null)
        {
            menuButton = GetComponent<Button>();
        }

        WireButton();
        SyncVisibilityWithGameState();
    }

    private void OnEnable()
    {
        WireButton();

        if (gameStateController != null)
        {
            gameStateController.GameStateChanged -= HandleGameStateChanged;
            gameStateController.GameStateChanged += HandleGameStateChanged;
        }

        SyncVisibilityWithGameState();
    }

    private void OnDisable()
    {
        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(RequestOpenPauseMenu);
        }

        if (gameStateController != null)
        {
            gameStateController.GameStateChanged -= HandleGameStateChanged;
        }
    }

    public void RequestOpenPauseMenu()
    {
        ClearSelectionIfNeeded();

        if (gameStateController == null)
        {
            Debug.LogWarning($"{nameof(HUDMenuButtonController)} necesita {nameof(GameStateController)}.", this);
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

        gameStateController.TryTogglePauseMenu();
    }

    private void HandleGameStateChanged(GameState previousState, GameState currentState)
    {
        SyncVisibilityWithGameState();
    }

    private void SyncVisibilityWithGameState()
    {
        bool visible = !hideWhenNotInGameplay ||
                       gameStateController == null ||
                       gameStateController.CurrentState == GameState.Gameplay;

        SetVisible(visible);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (menuButton != null)
        {
            menuButton.interactable = visible;
        }
    }

    private void WireButton()
    {
        if (menuButton == null)
        {
            return;
        }

        menuButton.onClick.RemoveListener(RequestOpenPauseMenu);
        menuButton.onClick.AddListener(RequestOpenPauseMenu);
    }

    private void ClearSelectionIfNeeded()
    {
        if (!clearSelectionOnClick || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
    }
}
