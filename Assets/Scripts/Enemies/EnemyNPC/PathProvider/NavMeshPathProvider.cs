using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Path provider basado en NavMesh.CalculatePath.
///
/// Usa NavMesh solo para calcular una ruta y devolver corners.
/// No usa NavMeshAgent como dueno de movimiento y no modifica Rigidbody.
/// EnemyNavigator y EnemyMovement siguen ejecutando el desplazamiento real.
/// </summary>
[DisallowMultipleComponent]
public sealed class NavMeshPathProvider : MonoBehaviour, IEnemyPathProvider
{
    [Header("NavMesh")]
    [Tooltip("Radio para proyectar la posicion actual y el destino sobre el NavMesh. Aumentar levemente si el Rigidbody queda apenas fuera del NavMesh por fisica/rampas.")]
    [SerializeField, Min(0.01f)]
    private float sampleRadius = 1f;

    [Tooltip("Areas de NavMesh permitidas. -1 equivale a All Areas.")]
    [SerializeField]
    private int areaMask = NavMesh.AllAreas;

    [Tooltip("Si esta activo, acepta rutas parciales. Para aprendizaje conviene dejarlo apagado para detectar destinos mal configurados.")]
    [SerializeField]
    private bool allowPartialPath;

    [Header("Repath")]
    [Tooltip("Intervalo minimo entre recalculos de ruta. Valores bajos responden mejor a targets moviles, pero cuestan mas CPU.")]
    [SerializeField, Min(0.02f)]
    private float repathInterval = 0.2f;

    [Tooltip("Si el destino cambio mas que esta distancia, se recalcula la ruta aunque todavia no haya pasado el intervalo.")]
    [SerializeField, Min(0.01f)]
    private float destinationRepathDistance = 0.5f;

    [Header("Debug")]
    [SerializeField]
    private bool logPathFailures;

    [SerializeField]
    private bool drawDebugPath = true;

    private NavMeshPath path;
    private Vector3 cachedDestination;
    private Vector3 lastSampledDestination;
    private int currentCornerIndex;
    private float nextRepathTime;
    private bool hasPath;
    private EnemyPathStatus lastStatus = EnemyPathStatus.None;

    private void Awake()
    {
        path = new NavMeshPath();
    }

    public bool TryGetNextPoint(
        Vector3 currentPosition,
        Vector3 destination,
        float destinationArrivalDistance,
        float waypointArrivalDistance,
        out Vector3 nextPoint,
        out bool nextPointIsFinalDestination,
        out EnemyPathStatus status)
    {
        nextPoint = destination;
        nextPointIsFinalDestination = true;
        status = EnemyPathStatus.None;

        if (HasReachedHorizontal(currentPosition, destination, destinationArrivalDistance))
        {
            status = EnemyPathStatus.Complete;
            lastStatus = status;
            return false;
        }

        if (ShouldRecalculate(destination))
        {
            RecalculatePath(currentPosition, destination);
        }

        status = lastStatus;
        if (!hasPath)
        {
            nextPoint = destination;
            nextPointIsFinalDestination = true;
            return false;
        }

        Vector3[] corners = path.corners;
        if (corners == null || corners.Length == 0)
        {
            status = EnemyPathStatus.InvalidPath;
            lastStatus = status;
            hasPath = false;
            LogFailure(status, destination);
            return false;
        }

        while (currentCornerIndex < corners.Length - 1 && HasReachedHorizontal(currentPosition, corners[currentCornerIndex], waypointArrivalDistance))
        {
            currentCornerIndex++;
        }

        currentCornerIndex = Mathf.Clamp(currentCornerIndex, 0, corners.Length - 1);
        nextPoint = corners[currentCornerIndex];
        nextPointIsFinalDestination = currentCornerIndex >= corners.Length - 1;
        status = path.status == NavMeshPathStatus.PathPartial
            ? EnemyPathStatus.PartialPath
            : EnemyPathStatus.Valid;
        lastStatus = status;
        return true;
    }

    public void ClearPath()
    {
        hasPath = false;
        currentCornerIndex = 0;
        lastStatus = EnemyPathStatus.None;
        if (path != null)
        {
            path.ClearCorners();
        }
    }

    private void EnsurePath()
    {
        if (path == null)
        {
            path = new NavMeshPath();
        }
    }

    private bool ShouldRecalculate(Vector3 destination)
    {
        if (!hasPath)
        {
            return true;
        }

        if (Time.time >= nextRepathTime)
        {
            return true;
        }

        Vector3 delta = destination - cachedDestination;
        delta.y = 0f;
        return delta.sqrMagnitude >= destinationRepathDistance * destinationRepathDistance;
    }

    private void RecalculatePath(Vector3 currentPosition, Vector3 destination)
    {
        nextRepathTime = Time.time + repathInterval;
        cachedDestination = destination;
        hasPath = false;
        currentCornerIndex = 0;
        EnsurePath();
        path.ClearCorners();

        if (!NavMesh.SamplePosition(currentPosition, out NavMeshHit startHit, sampleRadius, areaMask))
        {
            lastStatus = EnemyPathStatus.InvalidStart;
            LogFailure(lastStatus, destination);
            return;
        }

        if (!NavMesh.SamplePosition(destination, out NavMeshHit destinationHit, sampleRadius, areaMask))
        {
            lastStatus = EnemyPathStatus.InvalidDestination;
            LogFailure(lastStatus, destination);
            return;
        }

        lastSampledDestination = destinationHit.position;

        bool calculated = NavMesh.CalculatePath(startHit.position, destinationHit.position, areaMask, path);
        if (!calculated || path.status == NavMeshPathStatus.PathInvalid)
        {
            lastStatus = EnemyPathStatus.InvalidPath;
            LogFailure(lastStatus, destination);
            return;
        }

        if (path.status == NavMeshPathStatus.PathPartial && !allowPartialPath)
        {
            lastStatus = EnemyPathStatus.PartialPath;
            LogFailure(lastStatus, destination);
            return;
        }

        Vector3[] corners = path.corners;
        if (corners == null || corners.Length == 0)
        {
            lastStatus = EnemyPathStatus.InvalidPath;
            LogFailure(lastStatus, destination);
            return;
        }

        hasPath = true;
        currentCornerIndex = corners.Length > 1 ? 1 : 0;
        lastStatus = path.status == NavMeshPathStatus.PathPartial
            ? EnemyPathStatus.PartialPath
            : EnemyPathStatus.Valid;
    }

    private static bool HasReachedHorizontal(Vector3 currentPosition, Vector3 destination, float distance)
    {
        currentPosition.y = 0f;
        destination.y = 0f;
        return (currentPosition - destination).sqrMagnitude <= distance * distance;
    }

    private void LogFailure(EnemyPathStatus status, Vector3 destination)
    {
        if (!logPathFailures)
        {
            return;
        }

        Debug.LogWarning($"[{nameof(NavMeshPathProvider)}] {name} path failed. Status: {status}. Destination: {destination}", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugPath || path == null || path.corners == null || path.corners.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Vector3[] corners = path.corners;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i + 1]);
            Gizmos.DrawWireSphere(corners[i], 0.12f);
        }

        Gizmos.DrawWireSphere(corners[corners.Length - 1], 0.16f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lastSampledDestination, 0.2f);
    }

    private void OnValidate()
    {
        sampleRadius = Mathf.Max(0.01f, sampleRadius);
        repathInterval = Mathf.Max(0.02f, repathInterval);
        destinationRepathDistance = Mathf.Max(0.01f, destinationRepathDistance);
    }
}
