using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyDeathReporter : MonoBehaviour
{
    [Header("Evento")]
    [SerializeField, Tooltip("ID estable del enemigo o grupo enemigo que debe coincidir con el TargetId del objetivo DefeatEnemies. Ejemplo: valley_leader.")]
    private string enemyId = "valley_leader";

    [SerializeField, Tooltip("ID opcional de la fuente que reporta el evento. Si queda vacío se usa el nombre del GameObject.")]
    private string sourceId;

    [Header("Referencias")]
    [SerializeField, Tooltip("MissionManager que recibirá el GameWorldEvent.EnemyDefeated.")]
    private MissionManager missionManager;

    [SerializeField, Tooltip("Health del enemigo. Si está asignado, el reporter se suscribe a OnDied.")]
    private TopDownHealth health;

    [Header("Comportamiento")]
    [SerializeField, Tooltip("Si está activo, intenta obtener TopDownHealth automáticamente en el mismo GameObject o en sus hijos.")]
    private bool autoFindHealth = true;

    [SerializeField, Tooltip("Si está activo, solo se marca como reportado cuando MissionManager acepta/procesa el evento.")]
    private bool requireSuccessfulMissionReport = true;

    [SerializeField, Tooltip("Si está activo, reporta una sola vez luego de un reporte exitoso.")]
    private bool reportOnce = true;

    [Header("Debug")]
    [SerializeField, Tooltip("Muestra logs útiles mientras se valida el evento de muerte.")]
    private bool logDebug;

    [SerializeField, TextArea(2, 4), Tooltip("Solo lectura aproximada para mirar el último reporte en Inspector.")]
    private string lastDebug = "Todavía no se reportó EnemyDefeated.";

    private bool hasReported;
    private bool subscribed;

    private void Reset()
    {
        missionManager = FindFirstObjectByType<MissionManager>();
        TryAutoFindHealth();
    }

    private void Awake()
    {
        if (missionManager == null)
        {
            missionManager = FindFirstObjectByType<MissionManager>();
        }

        TryAutoFindHealth();
    }

    private void OnEnable()
    {
        SubscribeToHealth();
    }

    private void OnDisable()
    {
        UnsubscribeFromHealth();
    }

    private void OnValidate()
    {
        if (autoFindHealth && health == null)
        {
            TryAutoFindHealth();
        }
    }

    private void TryAutoFindHealth()
    {
        if (!autoFindHealth || health != null)
        {
            return;
        }

        health = GetComponent<TopDownHealth>();
        if (health == null)
        {
            health = GetComponentInChildren<TopDownHealth>();
        }
    }

    private void SubscribeToHealth()
    {
        if (subscribed || health == null)
        {
            return;
        }

        health.OnDied += HandleDied;
        subscribed = true;
    }

    private void UnsubscribeFromHealth()
    {
        if (!subscribed || health == null)
        {
            return;
        }

        health.OnDied -= HandleDied;
        subscribed = false;
    }

    private void HandleDied()
    {
        TryReportEnemyDefeated();
    }

    public bool TryReportEnemyDefeated()
    {
        if (reportOnce && hasReported)
        {
            return false;
        }

        string cleanedEnemyId = CleanId(enemyId);
        if (string.IsNullOrEmpty(cleanedEnemyId))
        {
            Debug.LogWarning($"{nameof(EnemyDeathReporter)} en '{name}' no tiene Enemy Id configurado.", this);
            return false;
        }

        if (missionManager == null)
        {
            Debug.LogWarning($"{nameof(EnemyDeathReporter)} en '{name}' no tiene MissionManager asignado.", this);
            return false;
        }

        string eventSourceId = ResolveSourceId();
        GameWorldEvent worldEvent = GameWorldEvent.EnemyDefeated(cleanedEnemyId, eventSourceId);
        bool processed = missionManager.TryReportWorldEvent(worldEvent);

        if (processed || !requireSuccessfulMissionReport)
        {
            hasReported = true;
        }

        lastDebug = $"EnemyDefeated '{cleanedEnemyId}' | processed: {processed} | source: {eventSourceId}";

        if (logDebug)
        {
            Debug.Log($"{nameof(EnemyDeathReporter)} reportó {worldEvent}. Procesado por misión: {processed}.", this);
        }

        return processed;
    }

    public void ResetReportedState()
    {
        hasReported = false;
        lastDebug = "Estado de reporte reiniciado.";
    }


    public void Configure(
        string newEnemyId,
        MissionManager newMissionManager,
        string newSourceId = null,
        TopDownHealth healthOverride = null,
        bool requireSuccessfulReport = true,
        bool reportOnlyOnce = true,
        bool enableDebugLogs = false)
    {
        UnsubscribeFromHealth();

        enemyId = CleanId(newEnemyId);
        sourceId = CleanId(newSourceId);
        missionManager = newMissionManager;
        requireSuccessfulMissionReport = requireSuccessfulReport;
        reportOnce = reportOnlyOnce;
        logDebug = enableDebugLogs;
        hasReported = false;

        if (healthOverride != null)
        {
            health = healthOverride;
        }
        else if (autoFindHealth)
        {
            health = null;
            TryAutoFindHealth();
        }

        SubscribeToHealth();

        lastDebug = $"Configurado EnemyDefeated '{enemyId}' | source: {ResolveSourceId()}";
    }

    private string ResolveSourceId()
    {
        string cleanedSourceId = CleanId(sourceId);
        return string.IsNullOrEmpty(cleanedSourceId) ? name : cleanedSourceId;
    }

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
