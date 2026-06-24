using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class MissionAreaReachedReporter : MonoBehaviour
{
    [Header("Evento")]
    [SerializeField, Tooltip("ID estable del área que debe coincidir con el TargetId del objetivo ReachArea. Ejemplo: valley_heights.")]
    private string areaId = "valley_heights";

    [SerializeField, Tooltip("ID opcional de la fuente que reporta el evento. Si queda vacío se usa el nombre del GameObject.")]
    private string sourceId;

    [Header("Referencias")]
    [SerializeField, Tooltip("MissionManager que recibirá el GameWorldEvent.AreaReached.")]
    private MissionManager missionManager;

    [Header("Filtro del jugador")]
    [SerializeField, Tooltip("Si está activo, solo reporta cuando entra un collider con el Tag indicado.")]
    private bool usePlayerTagFilter = true;

    [SerializeField, Tooltip("Tag esperado para el jugador. El proyecto suele usar Player.")]
    private string playerTag = "Player";

    [SerializeField, Tooltip("Filtro opcional por LayerMask. Útil si querés limitar el trigger a Player u otra capa específica.")]
    private bool useLayerMaskFilter;

    [SerializeField, Tooltip("Capas válidas si Use Layer Mask Filter está activo.")]
    private LayerMask validLayers = ~0;

    [Header("Comportamiento")]
    [SerializeField, Tooltip("Si está activo, solo se marca como reportado cuando MissionManager acepta/procesa el evento.")]
    private bool requireSuccessfulMissionReport = true;

    [SerializeField, Tooltip("Si está activo, el área reporta una sola vez luego de un reporte exitoso.")]
    private bool reportOnce = true;

    [SerializeField, Tooltip("Si está activo, reintenta mientras el jugador permanece dentro. Sirve si el jugador entró antes de que la misión estuviera activa.")]
    private bool retryWhileInside = true;

    [SerializeField, Min(0.05f), Tooltip("Intervalo mínimo entre reintentos mientras el jugador permanece dentro del área.")]
    private float retryInterval = 0.25f;

    [Header("Setup")]
    [SerializeField, Tooltip("Si está activo, el componente fuerza el Collider local como Trigger en Reset/OnValidate.")]
    private bool autoSetColliderAsTrigger = true;

    [Header("Debug")]
    [SerializeField, Tooltip("Muestra logs útiles mientras se valida el área.")]
    private bool logDebug;

    [SerializeField, TextArea(2, 4), Tooltip("Solo lectura aproximada para mirar el último reporte en Inspector.")]
    private string lastDebug = "Todavía no se reportó AreaReached.";

    private Collider cachedCollider;
    private bool hasReported;
    private float nextRetryTime;

    private void Reset()
    {
        cachedCollider = GetComponent<Collider>();
        missionManager = FindFirstObjectByType<MissionManager>();
        EnsureTriggerCollider();
    }

    private void Awake()
    {
        if (missionManager == null)
        {
            missionManager = FindFirstObjectByType<MissionManager>();
        }

        cachedCollider = GetComponent<Collider>();
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        if (retryInterval < 0.05f)
        {
            retryInterval = 0.05f;
        }

        cachedCollider = GetComponent<Collider>();
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryReportFromCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!retryWhileInside || Time.time < nextRetryTime)
        {
            return;
        }

        TryReportFromCollider(other);
    }

    private void TryReportFromCollider(Collider other)
    {
        if (other == null)
        {
            return;
        }

        if (reportOnce && hasReported)
        {
            return;
        }

        if (!IsAllowedCollider(other))
        {
            return;
        }

        nextRetryTime = Time.time + retryInterval;
        TryReportAreaReached(other.gameObject);
    }

    public bool TryReportAreaReached(GameObject instigator = null)
    {
        if (reportOnce && hasReported)
        {
            return false;
        }

        string cleanedAreaId = CleanId(areaId);
        if (string.IsNullOrEmpty(cleanedAreaId))
        {
            Debug.LogWarning($"{nameof(MissionAreaReachedReporter)} en '{name}' no tiene Area Id configurado.", this);
            return false;
        }

        if (missionManager == null)
        {
            Debug.LogWarning($"{nameof(MissionAreaReachedReporter)} en '{name}' no tiene MissionManager asignado.", this);
            return false;
        }

        string eventSourceId = ResolveSourceId(instigator);
        GameWorldEvent worldEvent = GameWorldEvent.AreaReached(cleanedAreaId, eventSourceId);
        bool processed = missionManager.TryReportWorldEvent(worldEvent);

        if (processed || !requireSuccessfulMissionReport)
        {
            hasReported = true;
        }

        lastDebug = $"AreaReached '{cleanedAreaId}' | processed: {processed} | source: {eventSourceId}";

        if (logDebug)
        {
            Debug.Log($"{nameof(MissionAreaReachedReporter)} reportó {worldEvent}. Procesado por misión: {processed}.", this);
        }

        return processed;
    }

    public void ResetReportedState()
    {
        hasReported = false;
        nextRetryTime = 0f;
        lastDebug = "Estado de reporte reiniciado.";
    }

    private bool IsAllowedCollider(Collider other)
    {
        if (usePlayerTagFilter)
        {
            if (string.IsNullOrWhiteSpace(playerTag))
            {
                return false;
            }

            bool matchesSelf = other.CompareTag(playerTag);
            bool matchesRoot = other.transform.root != null && other.transform.root.CompareTag(playerTag);

            if (!matchesSelf && !matchesRoot)
            {
                return false;
            }
        }

        if (useLayerMaskFilter)
        {
            int layerBit = 1 << other.gameObject.layer;
            if ((validLayers.value & layerBit) == 0)
            {
                return false;
            }
        }

        return true;
    }

    private string ResolveSourceId(GameObject instigator)
    {
        string cleanedSourceId = CleanId(sourceId);
        if (!string.IsNullOrEmpty(cleanedSourceId))
        {
            return cleanedSourceId;
        }

        if (instigator != null)
        {
            return instigator.name;
        }

        return name;
    }

    private void EnsureTriggerCollider()
    {
        if (!autoSetColliderAsTrigger || cachedCollider == null)
        {
            return;
        }

        cachedCollider.isTrigger = true;
    }

    private void OnDrawGizmosSelected()
    {
        Collider localCollider = cachedCollider != null ? cachedCollider : GetComponent<Collider>();
        if (localCollider == null)
        {
            return;
        }

        Gizmos.matrix = transform.localToWorldMatrix;

        if (localCollider is BoxCollider boxCollider)
        {
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else if (localCollider is SphereCollider sphereCollider)
        {
            Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
        }
        else if (localCollider is CapsuleCollider capsuleCollider)
        {
            Gizmos.DrawWireSphere(capsuleCollider.center, capsuleCollider.radius);
        }
    }

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
