using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameStateController : MonoBehaviour
{
    [Header("Estado inicial")]
    [Tooltip("Estado con el que arranca la scene. Para gameplay normal debería ser Gameplay.")]
    [SerializeField] private GameState initialState = GameState.Gameplay;

    [Header("Pausa")]
    [Tooltip("Escala de tiempo cuando el juego está en gameplay o soft pause. Normalmente se deja en 1.")]
    [SerializeField, Min(0f)] private float gameplayTimeScale = 1f;

    [Header("Debug")]
    [Tooltip("Activalo solo mientras probamos el flujo. Después conviene apagarlo para no ensuciar la consola.")]
    [SerializeField] private bool logStateChanges;

    private GameState currentState;
    private PauseMode currentPauseMode;
    private bool hasAppliedPauseMode;

    public GameState CurrentState => currentState;
    public PauseMode CurrentPauseMode => currentPauseMode;
    public bool IsGameplay => currentState == GameState.Gameplay;
    public bool IsPaused => currentPauseMode != PauseMode.None;

    public event Action<GameState, GameState> GameStateChanged;
    public event Action<PauseMode> PauseModeChanged;

    private void Awake()
    {
        currentState = initialState;
        ApplyPauseMode(GetPauseModeForState(currentState), force: true);
    }

    public bool TrySetState(GameState nextState)
    {
        if (!CanTransitionTo(nextState))
        {
            return false;
        }

        SetState(nextState);
        return true;
    }

    public void ForceSetState(GameState nextState)
    {
        SetState(nextState);
    }

    public bool TryReturnToGameplay()
    {
        if (currentState == GameState.Cutscene || currentState == GameState.GameOver)
        {
            return false;
        }

        SetState(GameState.Gameplay);
        return true;
    }

    public bool TryTogglePauseMenu()
    {
        if (currentState == GameState.PauseMenu)
        {
            return TryReturnToGameplay();
        }

        if (currentState == GameState.Cutscene || currentState == GameState.GameOver)
        {
            return false;
        }

        SetState(GameState.PauseMenu);
        return true;
    }

    public bool TryToggleMissionJournal()
    {
        if (currentState == GameState.MissionJournal)
        {
            return TryReturnToGameplay();
        }

        if (currentState != GameState.Gameplay)
        {
            return false;
        }

        SetState(GameState.MissionJournal);
        return true;
    }

    public bool TryResolveEscape()
    {
        switch (currentState)
        {
            case GameState.Gameplay:
                SetState(GameState.PauseMenu);
                return true;

            case GameState.PauseMenu:
            case GameState.MissionJournal:
            case GameState.Dialogue:
                SetState(GameState.Gameplay);
                return true;

            case GameState.Cutscene:
            case GameState.GameOver:
            default:
                return false;
        }
    }

    private bool CanTransitionTo(GameState nextState)
    {
        if (currentState == nextState)
        {
            return true;
        }

        if (currentState == GameState.GameOver)
        {
            return false;
        }

        if (currentState == GameState.Cutscene && nextState != GameState.Gameplay)
        {
            return false;
        }

        return true;
    }

    private void SetState(GameState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }

        GameState previousState = currentState;
        currentState = nextState;

        ApplyPauseMode(GetPauseModeForState(currentState));

        if (logStateChanges)
        {
            Debug.Log($"GameState cambió: {previousState} -> {currentState}", this);
        }

        GameStateChanged?.Invoke(previousState, currentState);
    }

    private PauseMode GetPauseModeForState(GameState state)
    {
        switch (state)
        {
            case GameState.MissionJournal:
            case GameState.Dialogue:
                return PauseMode.SoftPause;

            case GameState.PauseMenu:
            case GameState.GameOver:
                return PauseMode.HardPause;

            case GameState.Gameplay:
            case GameState.Cutscene:
            default:
                return PauseMode.None;
        }
    }

    private void ApplyPauseMode(PauseMode pauseMode, bool force = false)
    {
        bool changed = !hasAppliedPauseMode || currentPauseMode != pauseMode;

        currentPauseMode = pauseMode;
        hasAppliedPauseMode = true;

        Time.timeScale = currentPauseMode == PauseMode.HardPause ? 0f : gameplayTimeScale;

        if (force || changed)
        {
            PauseModeChanged?.Invoke(currentPauseMode);
        }
    }

    private void OnDestroy()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
}
