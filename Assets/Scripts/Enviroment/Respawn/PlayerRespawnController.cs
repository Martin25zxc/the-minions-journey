using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum PlayerLifeLimitMode
{
    Infinite,
    Limited
}

public class PlayerRespawnController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Manager que conoce el punto de respawn actual. Si queda vacío, se busca uno en la escena.")]
    [SerializeField] RespawnPointManager respawnPointManager;

    [Tooltip("Health del jugador. Si queda vacío, se toma desde PlayerIdentity cuando el jugador esté listo.")]
    [SerializeField] TopDownHealth playerHealth;

    [Tooltip("Controlador de estado global. Se usa para entrar en GameOver cuando no quedan vidas.")]
    [SerializeField] GameStateController gameStateController;

    [Header("Respawn")]
    [Tooltip("Tiempo de espera entre la muerte del jugador y la reaparición.")]
    [SerializeField, Min(0f)] float respawnDelay = 2f;

    [Tooltip("Tiempo opcional después de revivir. Útil si se bloquea input hasta que termine Lie_StandUp. Para el clip actual puede ser 2.333.")]
    [SerializeField, Min(0f)] float completionDelayAfterRevive = 0f;

    [Tooltip("Si está apagado, el respawn solo restaura vida. No restaura shield.")]
    [SerializeField] bool restoreShieldOnRespawn;

    [Tooltip("Si está activo, una falta de RespawnPointManager o punto inicial lleva a derrota. Recomendado ON para evitar revivir donde murió.")]
    [SerializeField] bool defeatWhenRespawnPositionMissing = true;

    [Header("Lives")]
    [Tooltip("Infinite: no hay HUD de vidas ni derrota por contador. Limited: usa Max Lives.")]
    [SerializeField] PlayerLifeLimitMode lifeLimitMode = PlayerLifeLimitMode.Infinite;

    [Tooltip("Cantidad total de vidas cuando Life Limit Mode está en Limited. Ejemplo: 3 => muere a 2, muere a 1, muere a 0 y derrota.")]
    [SerializeField, Min(1)] int maxLives = 3;

    [Header("Notifications")]
    [Tooltip("Muestra notificaciones de respawn usando TMJNotifications. No afecta la lógica si no existe NotificationManager en escena.")]
    [SerializeField] bool showRespawnNotifications = true;

    [Tooltip("Mensaje mostrado cuando empieza el proceso de respawn.")]
    [SerializeField] string respawnStartedMessage = "Has caído. Reaparecerás en el último punto de respawn.";

    [Tooltip("Mensaje mostrado cuando el jugador reaparece.")]
    [SerializeField] string respawnCompletedMessage = "Has vuelto al último punto de respawn.";

    [Tooltip("Mensaje mostrado cuando el jugador queda en su última vida.")]
    [SerializeField] string lastLifeMessage = "Última vida.";

    [Tooltip("Mensaje mostrado cuando no quedan vidas.")]
    [SerializeField] string defeatedMessage = "No quedan vidas.";

    [Tooltip("Mensaje mostrado si el nivel no tiene punto de respawn válido.")]
    [SerializeField] string missingRespawnPointMessage = "No hay punto de respawn configurado.";

    [Header("Events")]
    [Tooltip("Se invoca cuando empieza el proceso de respawn, antes del delay.")]
    [SerializeField] UnityEvent onRespawnStarted;

    [Tooltip("Se invoca cuando el jugador ya fue movido y revivido. Conectar aquí TopDownPlayerAnimator.PlayRespawnStandUp().")]
    [SerializeField] UnityEvent onPlayerRevived;

    [Tooltip("Se invoca al final del flujo de respawn. Si Completion Delay After Revive es 2.333, sirve para desbloquear input después de Lie_StandUp.")]
    [SerializeField] UnityEvent onRespawnCompleted;

    [Tooltip("Se invoca cuando Life Limit Mode es Limited y no quedan vidas, o cuando falta punto de respawn y Defeat When Respawn Position Missing está activo.")]
    [SerializeField] UnityEvent onDefeated;

    Coroutine respawnCoroutine;
    int currentLives;
    bool hasTriggeredDefeat;

    public event Action<int, int> LivesChanged;
    public event Action<int> LifeLost;
    public event Action Defeated;

    public bool IsRespawning => respawnCoroutine != null;
    public bool UsesLimitedLives => lifeLimitMode == PlayerLifeLimitMode.Limited;
    public int CurrentLives => UsesLimitedLives ? currentLives : -1;
    public int MaxLives => maxLives;
    public PlayerLifeLimitMode LifeLimitMode => lifeLimitMode;

    void Awake()
    {
        currentLives = maxLives;

        if (respawnPointManager == null)
        {
            respawnPointManager = FindFirstObjectByType<RespawnPointManager>();
        }

        if (gameStateController == null)
        {
            gameStateController = FindFirstObjectByType<GameStateController>();
        }
    }

    void Start()
    {
        NotifyLivesChanged();
    }

    void OnEnable()
    {
        PlayerIdentity.OnPlayerHealthReady += HandlePlayerReady;

        if (playerHealth != null)
        {
            playerHealth.OnDied += HandlePlayerDied;
        }
        else if (PlayerIdentity.CurrentPlayerHealth != null)
        {
            HandlePlayerReady(PlayerIdentity.CurrentPlayerHealth);
        }
    }

    void OnDisable()
    {
        PlayerIdentity.OnPlayerHealthReady -= HandlePlayerReady;

        if (playerHealth != null)
        {
            playerHealth.OnDied -= HandlePlayerDied;
        }

        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }
    }

    void OnValidate()
    {
        maxLives = Mathf.Max(1, maxLives);
        respawnDelay = Mathf.Max(0f, respawnDelay);
        completionDelayAfterRevive = Mathf.Max(0f, completionDelayAfterRevive);
    }

    public void ResetLivesToMax()
    {
        currentLives = maxLives;
        hasTriggeredDefeat = false;
        NotifyLivesChanged();
    }

    void HandlePlayerReady(TopDownHealth health)
    {
        if (playerHealth == health) return;

        if (playerHealth != null)
        {
            playerHealth.OnDied -= HandlePlayerDied;
        }

        playerHealth = health;

        if (playerHealth != null)
        {
            playerHealth.OnDied += HandlePlayerDied;
        }
    }

    void HandlePlayerDied()
    {
        if (playerHealth == null || respawnCoroutine != null || hasTriggeredDefeat)
        {
            return;
        }

        if (UsesLimitedLives)
        {
            currentLives = Mathf.Max(0, currentLives - 1);
            LifeLost?.Invoke(currentLives);
            NotifyLivesChanged();

            if (currentLives == 1 && showRespawnNotifications)
            {
                TMJNotifications.ShowSystem(lastLifeMessage, NotificationPriority.High, "Respawn", "player_last_life", this);
            }

            if (currentLives <= 0)
            {
                TriggerDefeat(defeatedMessage);
                return;
            }
        }

        if (!TryGetRespawnPosition(out Vector3 spawnPos))
        {
            if (defeatWhenRespawnPositionMissing)
            {
                TriggerDefeat(missingRespawnPointMessage);
                return;
            }

            spawnPos = playerHealth.transform.position;
        }

        respawnCoroutine = StartCoroutine(RespawnPlayerDelayed(spawnPos));
    }

    IEnumerator RespawnPlayerDelayed(Vector3 spawnPos)
    {
        if (showRespawnNotifications)
        {
            TMJNotifications.ShowSystem(respawnStartedMessage, NotificationPriority.Normal, "Respawn", "player_respawn_started", this);
        }

        onRespawnStarted?.Invoke();

        if (respawnDelay > 0f)
        {
            yield return new WaitForSeconds(respawnDelay);
        }

        RespawnPlayer(spawnPos);
        onPlayerRevived?.Invoke();

        if (completionDelayAfterRevive > 0f)
        {
            yield return new WaitForSeconds(completionDelayAfterRevive);
        }

        respawnCoroutine = null;

        if (showRespawnNotifications)
        {
            TMJNotifications.ShowSystem(respawnCompletedMessage, NotificationPriority.Normal, "Respawn", "player_respawn_completed", this);
        }

        onRespawnCompleted?.Invoke();
    }

    bool TryGetRespawnPosition(out Vector3 spawnPos)
    {
        spawnPos = default;

        if (respawnPointManager == null)
        {
            return false;
        }

        return respawnPointManager.TryGetCurrentRespawnPosition(out spawnPos);
    }

    void RespawnPlayer(Vector3 spawnPos)
    {
        Rigidbody rb = playerHealth.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = spawnPos;
        }
        else
        {
            playerHealth.transform.position = spawnPos;
        }

        playerHealth.ReviveFull(restoreShieldOnRespawn);
    }

    void TriggerDefeat(string message)
    {
        if (hasTriggeredDefeat)
        {
            return;
        }

        hasTriggeredDefeat = true;
        respawnCoroutine = null;

        if (showRespawnNotifications && !string.IsNullOrWhiteSpace(message))
        {
            TMJNotifications.ShowSystem(message, NotificationPriority.Critical, "Derrota", "player_defeated", this);
        }

        if (gameStateController != null)
        {
            gameStateController.ForceSetState(GameState.GameOver);
        }

        Defeated?.Invoke();
        onDefeated?.Invoke();
    }

    void NotifyLivesChanged()
    {
        LivesChanged?.Invoke(CurrentLives, maxLives);
    }
}
