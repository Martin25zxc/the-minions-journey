using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerLivesHUD : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Controller de respawn/vidas del jugador. Si queda vacío, se busca uno en la escena.")]
    [SerializeField] PlayerRespawnController respawnController;

    [Tooltip("Controlador de estado global. Se usa para ocultar este HUD fuera de Gameplay.")]
    [SerializeField] GameStateController gameStateController;

    [Tooltip("Canvas propio del widget. Se desactiva con Canvas.enabled para ocultarlo sin apagar este script.")]
    [SerializeField] Canvas livesCanvas;

    [Tooltip("Opcional. Útil si luego se quiere hacer fade. Interactable y Blocks Raycasts deberían estar apagados.")]
    [SerializeField] CanvasGroup canvasGroup;

    [Tooltip("Texto TMP que muestra el contador. Ejemplo: x 3.")]
    [SerializeField] TMP_Text countText;

    [Tooltip("RectTransform usado para el pequeño punch visual cuando cambia el contador. Si queda vacío, se usa este RectTransform.")]
    [SerializeField] RectTransform pulseTarget;

    [Header("Display")]
    [Tooltip("Formato del texto. {0} se reemplaza por las vidas actuales.")]
    [SerializeField] string countFormat = "x {0}";

    [Tooltip("Si está activo, el HUD se oculta cuando el modo de vidas es Infinite.")]
    [SerializeField] bool hideWhenInfiniteLives = true;

    [Tooltip("Si está activo, el HUD solo se muestra en GameState.Gameplay.")]
    [SerializeField] bool showOnlyInGameplay = true;

    [Header("Pulse")]
    [Tooltip("Activa una animación simple de escala al perder una vida.")]
    [SerializeField] bool pulseOnLifeLost = true;

    [Tooltip("Escala máxima del punch visual.")]
    [SerializeField, Min(1f)] float pulseScale = 1.12f;

    [Tooltip("Duración total del punch visual.")]
    [SerializeField, Min(0.01f)] float pulseDuration = 0.16f;

    Coroutine pulseCoroutine;
    Vector3 originalPulseScale = Vector3.one;

    void Awake()
    {
        if (respawnController == null)
        {
            respawnController = FindFirstObjectByType<PlayerRespawnController>();
        }

        if (gameStateController == null)
        {
            gameStateController = FindFirstObjectByType<GameStateController>();
        }

        if (livesCanvas == null)
        {
            livesCanvas = GetComponent<Canvas>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (pulseTarget == null)
        {
            pulseTarget = transform as RectTransform;
        }

        if (pulseTarget != null)
        {
            originalPulseScale = pulseTarget.localScale;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    void OnEnable()
    {
        if (respawnController != null)
        {
            respawnController.LivesChanged += HandleLivesChanged;
            respawnController.LifeLost += HandleLifeLost;
            respawnController.Defeated += HandleDefeated;
        }

        if (gameStateController != null)
        {
            gameStateController.GameStateChanged += HandleGameStateChanged;
        }

        Refresh();
    }

    void OnDisable()
    {
        if (respawnController != null)
        {
            respawnController.LivesChanged -= HandleLivesChanged;
            respawnController.LifeLost -= HandleLifeLost;
            respawnController.Defeated -= HandleDefeated;
        }

        if (gameStateController != null)
        {
            gameStateController.GameStateChanged -= HandleGameStateChanged;
        }

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (pulseTarget != null)
        {
            pulseTarget.localScale = originalPulseScale;
        }
    }

    public void Refresh()
    {
        UpdateCountText();
        UpdateVisibility();
    }

    void HandleLivesChanged(int currentLives, int maxLives)
    {
        UpdateCountText(currentLives);
        UpdateVisibility();
    }

    void HandleLifeLost(int currentLives)
    {
        if (!pulseOnLifeLost || pulseTarget == null)
        {
            return;
        }

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }

        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    void HandleDefeated()
    {
        SetVisible(false);
    }

    void HandleGameStateChanged(GameState previousState, GameState nextState)
    {
        UpdateVisibility();
    }

    void UpdateCountText()
    {
        if (respawnController == null)
        {
            return;
        }

        UpdateCountText(respawnController.CurrentLives);
    }

    void UpdateCountText(int currentLives)
    {
        if (countText == null)
        {
            return;
        }

        int visibleLives = Mathf.Max(0, currentLives);
        countText.text = string.Format(countFormat, visibleLives);
    }

    void UpdateVisibility()
    {
        bool visible = ShouldBeVisible();
        SetVisible(visible);
    }

    bool ShouldBeVisible()
    {
        if (respawnController == null)
        {
            return false;
        }

        if (hideWhenInfiniteLives && !respawnController.UsesLimitedLives)
        {
            return false;
        }

        if (showOnlyInGameplay && gameStateController != null && gameStateController.CurrentState != GameState.Gameplay)
        {
            return false;
        }

        return respawnController.UsesLimitedLives && respawnController.CurrentLives > 0;
    }

    void SetVisible(bool visible)
    {
        if (livesCanvas != null)
        {
            livesCanvas.enabled = visible;
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
    }

    IEnumerator PulseRoutine()
    {
        float halfDuration = pulseDuration * 0.5f;
        float elapsed = 0f;
        Vector3 targetScale = originalPulseScale * pulseScale;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            pulseTarget.localScale = Vector3.Lerp(originalPulseScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            pulseTarget.localScale = Vector3.Lerp(targetScale, originalPulseScale, t);
            yield return null;
        }

        pulseTarget.localScale = originalPulseScale;
        pulseCoroutine = null;
    }
}
