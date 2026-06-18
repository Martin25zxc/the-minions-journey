using UnityEngine;

/// <summary>
/// Capa de navegacion del enemigo.
///
/// Responsabilidad:
/// - Recibir destinos de Brain, Duty o Positioning.
/// - Consultar un IEnemyPathProvider.
/// - Enviar el proximo punto a EnemyMovement.
///
/// No decide estados, no detecta al jugador, no ataca, no calcula damage
/// y no modifica directamente Rigidbody. EnemyMovement sigue siendo el dueno fisico.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMovement))]
public sealed class EnemyNavigator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Componente que ejecuta el movimiento fisico con Rigidbody. Si queda vacio, se busca automaticamente en este GameObject.")]
    [SerializeField]
    private EnemyMovement movement;

    [Tooltip("Componente que implementa IEnemyPathProvider. Usar DirectPathProvider para escenas sin NavMesh o NavMeshPathProvider para escenas bakeadas.")]
    [SerializeField]
    private MonoBehaviour pathProviderComponent;

    [Tooltip("Si Path Provider queda vacio, intenta encontrar un unico IEnemyPathProvider en este GameObject.")]
    [SerializeField]
    private bool autoFindPathProvider = true;

    [Header("Navigation")]
    [Tooltip("Distancia horizontal para considerar que se llego a un waypoint intermedio del path. No reemplaza la distancia final del destino.")]
    [SerializeField, Min(0.05f)]
    private float waypointArrivalDistance = 0.25f;

    [Tooltip("Si esta activo, al llegar al destino mira hacia el destino final. Normalmente se puede dejar activo.")]
    [SerializeField]
    private bool faceDestinationOnArrival = true;

    [Header("Debug")]
    [SerializeField]
    private bool logConfigurationWarnings = true;

    [SerializeField]
    private bool logPathFailures;

    [SerializeField]
    private bool drawDebugGizmos = true;

    [Header("Runtime Debug - Solo lectura conceptual")]
    [SerializeField]
    private string debugPathProvider = "None";

    [SerializeField]
    private EnemyPathStatus debugLastStatus = EnemyPathStatus.None;

    [SerializeField]
    private Vector3 debugDestination;

    [SerializeField]
    private Vector3 debugNextPoint;

    [SerializeField]
    private bool debugHasActiveDestination;

    [SerializeField]
    private bool debugReachedDestination;

    private IEnemyPathProvider pathProvider;
    private Vector3 currentDestination;
    private Vector3 currentNextPoint;
    private bool hasActiveDestination;
    private bool reachedDestination;
    private EnemyPathStatus lastStatus = EnemyPathStatus.None;

    public bool HasActiveDestination => hasActiveDestination;
    public bool ReachedDestination => reachedDestination;
    public EnemyPathStatus LastStatus => lastStatus;
    public string PathProviderName => pathProvider != null ? pathProvider.GetType().Name : "None";

    private void Awake()
    {
        ResolveReferences();
        ResolvePathProvider();
        RefreshDebugSnapshot();
    }

    public void Initialize(EnemyMovement newMovement)
    {
        if (newMovement != null)
        {
            movement = newMovement;
        }

        ResolveReferences();
        ResolvePathProvider();
        RefreshDebugSnapshot();
    }

    /// <summary>
    /// Pide navegar hacia un destino. Devuelve true si el destino final ya fue alcanzado.
    /// </summary>
    public bool MoveTo(Vector3 destination, float arrivalDistance, float speedMultiplier = 1f)
    {
        arrivalDistance = Mathf.Max(0.01f, arrivalDistance);
        currentDestination = destination;
        hasActiveDestination = true;
        reachedDestination = false;

        if (movement == null)
        {
            lastStatus = EnemyPathStatus.InvalidPath;
            RefreshDebugSnapshot();
            return false;
        }

        if (HasReachedHorizontal(movement.CurrentPosition, destination, arrivalDistance))
        {
            reachedDestination = true;
            lastStatus = EnemyPathStatus.Complete;
            currentNextPoint = destination;
            movement.Stop();

            if (faceDestinationOnArrival)
            {
                movement.FaceTarget(destination);
            }

            RefreshDebugSnapshot();
            return true;
        }

        if (pathProvider == null)
        {
            ResolvePathProvider();
        }

        if (pathProvider == null)
        {
            lastStatus = EnemyPathStatus.NoProvider;
            movement.Stop();
            LogFailureOnce(lastStatus, destination);
            RefreshDebugSnapshot();
            return false;
        }

        bool hasNextPoint = pathProvider.TryGetNextPoint(
            movement.CurrentPosition,
            destination,
            arrivalDistance,
            waypointArrivalDistance,
            out Vector3 nextPoint,
            out bool nextPointIsFinalDestination,
            out EnemyPathStatus status);

        lastStatus = status;
        currentNextPoint = nextPoint;

        if (!hasNextPoint)
        {
            movement.Stop();
            LogFailureOnce(status, destination);
            RefreshDebugSnapshot();
            return false;
        }

        float effectiveStopDistance = nextPointIsFinalDestination
            ? arrivalDistance
            : waypointArrivalDistance;

        movement.MoveTowards(nextPoint, effectiveStopDistance, speedMultiplier);
        RefreshDebugSnapshot();
        return false;
    }

    public void StopNavigation()
    {
        hasActiveDestination = false;
        reachedDestination = false;
        currentDestination = Vector3.zero;
        currentNextPoint = Vector3.zero;
        lastStatus = EnemyPathStatus.None;
        pathProvider?.ClearPath();
        movement?.Stop();
        RefreshDebugSnapshot();
    }

    public void ResolvePathProvider()
    {
        pathProvider = null;

        if (pathProviderComponent != null)
        {
            if (pathProviderComponent is IEnemyPathProvider explicitProvider)
            {
                pathProvider = explicitProvider;
                RefreshDebugSnapshot();
                return;
            }

            if (logConfigurationWarnings)
            {
                Debug.LogWarning($"[{nameof(EnemyNavigator)}] {name} Path Provider Component does not implement IEnemyPathProvider: {pathProviderComponent.GetType().Name}.", this);
            }
        }

        if (!autoFindPathProvider)
        {
            RefreshDebugSnapshot();
            return;
        }

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IEnemyPathProvider foundProvider)
            {
                if (pathProvider != null)
                {
                    if (logConfigurationWarnings)
                    {
                        Debug.LogWarning($"[{nameof(EnemyNavigator)}] {name} has multiple IEnemyPathProvider components. Assign Path Provider explicitly to avoid ambiguity.", this);
                    }

                    pathProvider = null;
                    RefreshDebugSnapshot();
                    return;
                }

                pathProvider = foundProvider;
            }
        }

        RefreshDebugSnapshot();
    }

    private void ResolveReferences()
    {
        if (movement == null)
        {
            movement = GetComponent<EnemyMovement>();
        }
    }

    private void LogFailureOnce(EnemyPathStatus status, Vector3 destination)
    {
        if (!logPathFailures || status == EnemyPathStatus.Valid || status == EnemyPathStatus.Complete)
        {
            return;
        }

        Debug.LogWarning($"[{nameof(EnemyNavigator)}] {name} cannot navigate. Status: {status}. Destination: {destination}. Provider: {PathProviderName}", this);
    }

    private static bool HasReachedHorizontal(Vector3 currentPosition, Vector3 destination, float distance)
    {
        currentPosition.y = 0f;
        destination.y = 0f;
        return (currentPosition - destination).sqrMagnitude <= distance * distance;
    }

    private void RefreshDebugSnapshot()
    {
        debugPathProvider = PathProviderName;
        debugLastStatus = lastStatus;
        debugDestination = currentDestination;
        debugNextPoint = currentNextPoint;
        debugHasActiveDestination = hasActiveDestination;
        debugReachedDestination = reachedDestination;
    }

    private void OnValidate()
    {
        waypointArrivalDistance = Mathf.Max(0.05f, waypointArrivalDistance);
        RefreshDebugSnapshot();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos || !hasActiveDestination)
        {
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(currentDestination, 0.25f);
        Gizmos.DrawLine(transform.position, currentDestination);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(currentNextPoint, 0.18f);
        Gizmos.DrawLine(transform.position, currentNextPoint);
    }
}
