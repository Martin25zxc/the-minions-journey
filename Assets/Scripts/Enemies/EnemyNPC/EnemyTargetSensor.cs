using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyTargetSensor : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField]
    private EnemyDefinition definition;

    [Header("Target")]
    [SerializeField]
    private string targetTag = "Player";

    [Tooltip("Sirve para testear en escena arrastrando un target manual. Si esta asignado, se usa antes que buscar por tag.")]
    [SerializeField]
    private Transform targetOverride;

    [Header("Line of Sight")]
    [Tooltip("Si esta activo, la vision frontal requiere Line of Sight contra obstaculos.")]
    [SerializeField]
    private bool requireLineOfSight = true;

    [Tooltip("Si esta activo, incluso la mini deteccion circular cercana requiere Line of Sight. Para melee suele convenir dejarlo apagado al principio.")]
    [SerializeField]
    private bool requireLineOfSightForProximity;

    [Tooltip("Solo obstaculos del escenario. No incluir Player, Enemy, Hitbox ni Projectile para evitar falsos bloqueos.")]
    [SerializeField]
    private LayerMask obstacleLayers;

    [SerializeField, Min(0f)]
    private float eyeHeight = 0.6f;

    [SerializeField, Min(0f)]
    private float targetEyeHeight = 0.6f;

    [Header("Search")]
    [SerializeField, Min(0.02f)]
    private float searchInterval = 0.2f;

    private Transform currentTarget;
    private Vector3 lastKnownTargetPosition;
    private float nextSearchTime;
    private float lastConfirmedTargetTime;
    private bool hasLastKnownTargetPosition;

    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;
    public Vector3 LastKnownTargetPosition => lastKnownTargetPosition;
    public bool HasLastKnownTargetPosition => hasLastKnownTargetPosition;

    public bool HasVisibleTarget => currentTarget != null && CanAcquireTarget(currentTarget);

    public float DistanceToTarget
    {
        get
        {
            if (currentTarget == null)
            {
                return Mathf.Infinity;
            }

            return HorizontalDistance(transform.position, currentTarget.position);
        }
    }

    public void Initialize(EnemyDefinition newDefinition)
    {
        if (newDefinition != null)
        {
            definition = newDefinition;
        }
    }

    public void Tick()
    {
        if (definition == null)
        {
            return;
        }

        if (targetOverride != null)
        {
            UpdateCurrentTarget(targetOverride);
            return;
        }

        if (currentTarget != null)
        {
            UpdateCurrentTarget(currentTarget);
            return;
        }

        if (Time.time < nextSearchTime)
        {
            return;
        }

        nextSearchTime = Time.time + searchInterval;
        TryAcquireTargetByTag();
    }

    public void ClearTarget()
    {
        currentTarget = null;
        hasLastKnownTargetPosition = false;
    }

    /// <summary>
    /// Mantiene el nombre del metodo anterior para no romper llamadas existentes.
    /// Conceptualmente significa: puede adquirir el target ahora mismo.
    /// </summary>
    public bool CanDetect(Transform candidate)
    {
        return CanAcquireTarget(candidate);
    }

    public bool CanAcquireTarget(Transform candidate)
    {
        if (definition == null || candidate == null)
        {
            return false;
        }

        // Primero se prueba la mini deteccion circular.
        // Esto evita que un enemigo melee ignore al jugador si se le acerca por atras.
        if (IsInsideProximityRange(candidate))
        {
            return !requireLineOfSightForProximity || HasLineOfSight(candidate);
        }

        // Si no esta en proximidad, debe entrar por vision frontal.
        return IsInsideFrontalVision(candidate);
    }

    private void UpdateCurrentTarget(Transform candidate)
    {
        if (candidate == null || IsBeyondLoseAggroRange(candidate))
        {
            ClearTarget();
            return;
        }

        if (CanAcquireTarget(candidate))
        {
            ConfirmTarget(candidate);
            return;
        }

        float memoryDuration = definition.TargetMemoryDuration;
        bool memoryExpired = Time.time - lastConfirmedTargetTime > memoryDuration;
        if (memoryExpired)
        {
            ClearTarget();
        }
    }

    private void TryAcquireTargetByTag()
    {
        if (string.IsNullOrWhiteSpace(targetTag))
        {
            return;
        }

        GameObject[] targetObjects = GameObject.FindGameObjectsWithTag(targetTag);
        Transform bestTarget = null;
        float bestDistanceSquared = Mathf.Infinity;

        for (int i = 0; i < targetObjects.Length; i++)
        {
            GameObject targetObject = targetObjects[i];
            if (targetObject == null)
            {
                continue;
            }

            Transform candidate = targetObject.transform;
            if (!CanAcquireTarget(candidate))
            {
                continue;
            }

            Vector3 toCandidate = candidate.position - transform.position;
            toCandidate.y = 0f;
            float distanceSquared = toCandidate.sqrMagnitude;

            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                bestTarget = candidate;
            }
        }

        if (bestTarget != null)
        {
            ConfirmTarget(bestTarget);
        }
    }

    private void ConfirmTarget(Transform target)
    {
        currentTarget = target;
        lastKnownTargetPosition = target.position;
        lastConfirmedTargetTime = Time.time;
        hasLastKnownTargetPosition = true;
    }

    private bool IsInsideProximityRange(Transform candidate)
    {
        float proximityRange = definition.ProximityDetectionRange;
        if (proximityRange <= 0f)
        {
            return false;
        }

        float distanceSquared = HorizontalSqrDistance(transform.position, candidate.position);
        return distanceSquared <= proximityRange * proximityRange;
    }

    private bool IsInsideFrontalVision(Transform candidate)
    {
        Vector3 toTarget = candidate.position - transform.position;
        toTarget.y = 0f;

        float distanceSquared = toTarget.sqrMagnitude;
        float detectionRange = definition.DetectionRange;

        if (detectionRange <= 0f || distanceSquared > detectionRange * detectionRange)
        {
            return false;
        }

        if (distanceSquared > 0.0001f && definition.DetectionAngle < 359.9f)
        {
            float angle = Vector3.Angle(transform.forward, toTarget.normalized);
            if (angle > definition.DetectionHalfAngle)
            {
                return false;
            }
        }

        if (requireLineOfSight && !HasLineOfSight(candidate))
        {
            return false;
        }

        return true;
    }

    private bool HasLineOfSight(Transform candidate)
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 target = candidate.position + Vector3.up * targetEyeHeight;

        return !Physics.Linecast(
            origin,
            target,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);
    }

    private bool IsBeyondLoseAggroRange(Transform target)
    {
        if (definition == null || target == null)
        {
            return true;
        }

        float distanceSquared = HorizontalSqrDistance(transform.position, target.position);
        return distanceSquared > definition.LoseAggroRange * definition.LoseAggroRange;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(HorizontalSqrDistance(a, b));
    }

    private static float HorizontalSqrDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return (a - b).sqrMagnitude;
    }

    private void OnDrawGizmosSelected()
    {
        EnemyDefinition gizmoDefinition = definition;
        if (gizmoDefinition == null)
        {
            return;
        }

        // Amarillo: vision frontal principal.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, gizmoDefinition.DetectionRange);

        float halfAngle = gizmoDefinition.DetectionHalfAngle;
        Vector3 forward = transform.forward;
        Vector3 left = Quaternion.Euler(0f, -halfAngle, 0f) * forward;
        Vector3 right = Quaternion.Euler(0f, halfAngle, 0f) * forward;
        Gizmos.DrawRay(transform.position, left * gizmoDefinition.DetectionRange);
        Gizmos.DrawRay(transform.position, right * gizmoDefinition.DetectionRange);

        // Cyan: mini deteccion circular cercana.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, gizmoDefinition.ProximityDetectionRange);

        // Gris: distancia de perdida de aggro.
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, gizmoDefinition.LoseAggroRange);

        if (currentTarget != null)
        {
            Gizmos.color = HasVisibleTarget ? Color.green : Color.magenta;
            Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight, currentTarget.position + Vector3.up * targetEyeHeight);
        }
    }
}
