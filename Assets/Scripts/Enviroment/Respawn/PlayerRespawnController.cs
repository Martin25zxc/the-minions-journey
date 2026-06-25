using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum PlayerRespawnAttemptMode
{
    Infinite,
    Limited
}

public class PlayerRespawnController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Manager que conoce el último punto de respawn activado. Si queda vacío, se busca uno en la escena.")]
    [SerializeField] RespawnPointManager respawnPointManager;

    [Tooltip("Health del jugador. Si queda vacío, se toma desde PlayerIdentity cuando el jugador esté listo.")]
    [SerializeField] TopDownHealth playerHealth;

    [Header("Respawn")]
    [Tooltip("Tiempo de espera entre la muerte del jugador y la reaparición.")]
    [SerializeField, Min(0f)] float respawnDelay = 2f;

    [Tooltip("Tiempo opcional después de revivir. Útil si se bloquea input hasta que termine Lie_StandUp. Para el clip actual puede ser 2.333.")]
    [SerializeField, Min(0f)] float completionDelayAfterRevive = 0f;

    [Tooltip("Si está apagado, el respawn solo restaura vida. No restaura shield.")]
    [SerializeField] bool restoreShieldOnRespawn;

    [Header("Attempts")]
    [Tooltip("Infinite: el jugador puede reaparecer sin límite. Limited: usa Max Respawn Attempts.")]
    [SerializeField] PlayerRespawnAttemptMode attemptMode = PlayerRespawnAttemptMode.Infinite;

    [Tooltip("Cantidad de reapariciones disponibles cuando Attempt Mode está en Limited.")]
    [SerializeField, Min(1)] int maxRespawnAttempts = 3;

    [Header("Notifications")]
    [Tooltip("Muestra notificaciones de respawn usando TMJNotifications. No afecta la lógica si no existe NotificationManager en escena.")]
    [SerializeField] bool showRespawnNotifications = true;

    [Tooltip("Mensaje mostrado cuando empieza el proceso de respawn.")]
    [SerializeField] string respawnStartedMessage = "Has caído. Reaparecerás en el último punto de respawn.";

    [Tooltip("Mensaje mostrado cuando el jugador reaparece.")]
    [SerializeField] string respawnCompletedMessage = "Has vuelto al último punto de respawn.";

    [Tooltip("Mensaje mostrado cuando no quedan reapariciones disponibles.")]
    [SerializeField] string attemptsDepletedMessage = "No quedan intentos de respawn.";

    [Header("Events")]
    [Tooltip("Se invoca cuando empieza el proceso de respawn, antes del delay.")]
    [SerializeField] UnityEvent onRespawnStarted;

    [Tooltip("Se invoca cuando el jugador ya fue movido y revivido. Conectar aquí TopDownPlayerAnimator.PlayRespawnStandUp().")]
    [SerializeField] UnityEvent onPlayerRevived;

    [Tooltip("Se invoca al final del flujo de respawn. Si Completion Delay After Revive es 2.333, sirve para desbloquear input después de Lie_StandUp.")]
    [SerializeField] UnityEvent onRespawnCompleted;

    [Tooltip("Se invoca cuando Attempt Mode es Limited y no quedan reapariciones.")]
    [SerializeField] UnityEvent onRespawnAttemptsDepleted;

    Coroutine respawnCoroutine;
    int remainingRespawnAttempts;

    public bool IsRespawning => respawnCoroutine != null;
    public bool HasInfiniteAttempts => attemptMode == PlayerRespawnAttemptMode.Infinite;
    public int RemainingRespawnAttempts => HasInfiniteAttempts ? -1 : remainingRespawnAttempts;

    void Awake()
    {
        remainingRespawnAttempts = maxRespawnAttempts;

        if (respawnPointManager == null)
        {
            respawnPointManager = FindFirstObjectByType<RespawnPointManager>();
        }
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

    public void ResetRespawnAttempts()
    {
        remainingRespawnAttempts = maxRespawnAttempts;
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
        if (playerHealth == null || respawnCoroutine != null)
        {
            return;
        }

        if (!CanRespawn())
        {
            if (showRespawnNotifications)
            {
                TMJNotifications.ShowSystem(attemptsDepletedMessage, NotificationPriority.High, "Respawn", "respawn_attempts_depleted", this);
            }

            onRespawnAttemptsDepleted?.Invoke();
            return;
        }

        ConsumeAttemptIfNeeded();
        respawnCoroutine = StartCoroutine(RespawnPlayerDelayed());
    }

    IEnumerator RespawnPlayerDelayed()
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

        RespawnPlayer();
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

    void RespawnPlayer()
    {
        Vector3 spawnPos = playerHealth.transform.position;

        if (respawnPointManager != null)
        {
            spawnPos = respawnPointManager.GetCurrentRespawnPosition(spawnPos);
        }

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

    bool CanRespawn()
    {
        return HasInfiniteAttempts || remainingRespawnAttempts > 0;
    }

    void ConsumeAttemptIfNeeded()
    {
        if (!HasInfiniteAttempts)
        {
            remainingRespawnAttempts = Mathf.Max(0, remainingRespawnAttempts - 1);
        }
    }
}
