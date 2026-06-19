using UnityEngine;

/// <summary>
/// Ruta de patrulla definida en escena.
///
/// Responsabilidad:
/// - Contener los puntos de patrol.
/// - Definir el modo de avance de la ruta.
/// - Dar datos simples a EnemyDutyController.
///
/// No mueve enemigos, no conoce EnemyBrain y no calcula pathfinding.
/// En la macroetapa de navegacion, estos puntos seguiran siendo destinos;
/// la ruta/path real la resolvera EnemyNavigator / PathProvider.
/// </summary>
public sealed class EnemyPatrolRoute : MonoBehaviour
{
    [Header("Route")]
    [SerializeField]
    private EnemyPatrolRouteMode routeMode = EnemyPatrolRouteMode.Loop;

    [SerializeField]
    private Transform[] points;

    [Header("Debug")]
    [SerializeField]
    private bool drawGizmos = true;

    [SerializeField, Min(0.05f)]
    private float gizmoPointRadius = 0.25f;

    public EnemyPatrolRouteMode RouteMode => routeMode;
    public int Count => points != null ? points.Length : 0;
    public bool IsValid => Count > 0;

    public Transform GetPointTransform(int index)
    {
        if (!IsIndexValid(index))
        {
            return null;
        }

        return points[index];
    }

    public Vector3 GetPointPosition(int index)
    {
        Transform point = GetPointTransform(index);
        return point != null ? point.position : transform.position;
    }

    public float GetPointWaitTime(int index, float fallbackWaitTime)
    {
        Transform point = GetPointTransform(index);
        if (point == null)
        {
            return Mathf.Max(0f, fallbackWaitTime);
        }

        EnemyPatrolPoint patrolPoint = point.GetComponent<EnemyPatrolPoint>();
        return patrolPoint != null ? patrolPoint.WaitTime : Mathf.Max(0f, fallbackWaitTime);
    }

    public int GetClosestPointIndex(Vector3 worldPosition)
    {
        if (!IsValid)
        {
            return -1;
        }

        int bestIndex = -1;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < points.Length; i++)
        {
            Transform point = points[i];
            if (point == null)
            {
                continue;
            }

            Vector3 delta = point.position - worldPosition;
            delta.y = 0f;
            float distance = delta.sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    public int GetNextIndex(int currentIndex, ref int direction)
    {
        if (!IsValid)
        {
            return -1;
        }

        if (Count == 1)
        {
            return 0;
        }

        if (!IsIndexValid(currentIndex))
        {
            return 0;
        }

        switch (routeMode)
        {
            case EnemyPatrolRouteMode.Random:
                return GetRandomDifferentIndex(currentIndex);

            case EnemyPatrolRouteMode.PingPong:
                return GetPingPongNextIndex(currentIndex, ref direction);

            case EnemyPatrolRouteMode.Loop:
            default:
                return (currentIndex + 1) % Count;
        }
    }

    public bool IsIndexValid(int index)
    {
        return points != null && index >= 0 && index < points.Length && points[index] != null;
    }

    private int GetRandomDifferentIndex(int currentIndex)
    {
        if (Count <= 1)
        {
            return currentIndex;
        }

        int nextIndex = currentIndex;
        const int maxAttempts = 8;

        for (int i = 0; i < maxAttempts && nextIndex == currentIndex; i++)
        {
            nextIndex = Random.Range(0, Count);
        }

        if (nextIndex == currentIndex)
        {
            nextIndex = (currentIndex + 1) % Count;
        }

        return nextIndex;
    }

    private int GetPingPongNextIndex(int currentIndex, ref int direction)
    {
        if (direction == 0)
        {
            direction = 1;
        }

        int nextIndex = currentIndex + direction;

        if (nextIndex >= Count)
        {
            direction = -1;
            nextIndex = Count - 2;
        }
        else if (nextIndex < 0)
        {
            direction = 1;
            nextIndex = 1;
        }

        return Mathf.Clamp(nextIndex, 0, Count - 1);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || points == null || points.Length == 0)
        {
            return;
        }

        Gizmos.matrix = Matrix4x4.identity;

        for (int i = 0; i < points.Length; i++)
        {
            Transform point = points[i];
            if (point == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(point.position, gizmoPointRadius);

            Transform next = GetNextPointForGizmos(i);
            if (next != null)
            {
                Gizmos.DrawLine(point.position, next.position);
            }
        }
    }

    private Transform GetNextPointForGizmos(int index)
    {
        if (points == null || points.Length <= 1 || !IsIndexValid(index))
        {
            return null;
        }

        switch (routeMode)
        {
            case EnemyPatrolRouteMode.PingPong:
                if (index + 1 < points.Length)
                {
                    return points[index + 1];
                }
                return null;

            case EnemyPatrolRouteMode.Random:
                return null;

            case EnemyPatrolRouteMode.Loop:
            default:
                return points[(index + 1) % points.Length];
        }
    }

    private void OnValidate()
    {
        gizmoPointRadius = Mathf.Max(0.05f, gizmoPointRadius);
    }
}
