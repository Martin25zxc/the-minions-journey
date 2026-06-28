using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyDisengageAbility : MonoBehaviour, IEnemyAbility
{
    [Header("References")]
    [SerializeField]
    private EnemyActor actor;

    [SerializeField]
    private EnemyMovement movement;

    [SerializeField]
    private EnemyAnimator enemyAnimator;

    [Tooltip("Hijo visual que hace el arco vertical. Si queda vacio, intenta usar el Transform del Animator hijo.")]
    [SerializeField]
    private Transform visualRoot;

    [Header("Profile")]
    [SerializeField]
    private EnemyDisengageProfile profile;

    [Header("Debug")]
    [SerializeField]
    private bool drawDebugGizmos = true;

    private Coroutine selfRoutine;
    private bool isRunning;
    private bool cancelRequested;
    private float nextAvailableTime;

    private Vector3 visualBaseLocalPosition;
    private bool hasVisualBaseLocalPosition;

    private Vector3 lastStartPosition;
    private Vector3 lastLandingPosition;
    private bool hasLastRetreat;

    public bool IsRunning => isRunning;
    public bool IsOnCooldown => Time.time < nextAvailableTime;
    public EnemyDisengageProfile Profile => profile;

    private void Awake()
    {
        if (actor == null) actor = GetComponent<EnemyActor>();
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (enemyAnimator == null) enemyAnimator = GetComponent<EnemyAnimator>();

        if (visualRoot == null)
        {
            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                visualRoot = animator.transform;
            }
        }

        if (visualRoot != null)
        {
            visualBaseLocalPosition = visualRoot.localPosition;
            hasVisualBaseLocalPosition = true;
        }
    }

    private void Start()
    {
        if (profile == null)
        {
            Debug.LogWarning($"[{nameof(EnemyDisengageAbility)}] {name} has no EnemyDisengageProfile assigned. Disengage is disabled.", this);
        }
    }

    private void OnDisable()
    {
        Cancel();
    }

    public bool CanUse(Transform target)
    {
        if (actor == null || !actor.IsAlive || profile == null || target == null)
        {
            return false;
        }

        if (isRunning || IsOnCooldown)
        {
            return false;
        }

        float distance = HorizontalDistance(transform.position, target.position);
        if (!profile.IsDistanceInRange(distance))
        {
            return false;
        }

        return TryFindLandingPosition(target.position, out _);
    }

    public float GetPriority(Transform target)
    {
        return profile != null ? profile.Priority : 0f;
    }

    public IEnumerator Execute(Transform target)
    {
        if (!CanUse(target))
        {
            yield break;
        }

        isRunning = true;
        cancelRequested = false;

        // El cooldown empieza al iniciar para evitar spam si la habilidad se cancela por impacto.
        nextAvailableTime = Time.time + profile.Cooldown;

        movement?.Stop();

        Vector3 threatPosition = target != null ? target.position : transform.position - transform.forward;

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        enemyAnimator?.PlayDisengage();

        yield return WaitWhileUsable(profile.StartupTime);
        if (!CanContinue())
        {
            Finish(false);
            yield break;
        }

        if (target != null)
        {
            threatPosition = target.position;
        }

        if (!TryFindLandingPosition(threatPosition, out Vector3 landingPosition))
        {
            Finish(false);
            yield break;
        }

        Vector3 startPosition = movement != null ? movement.CurrentPosition : transform.position;
        float rootY = startPosition.y;
        landingPosition.y = rootY;

        lastStartPosition = startPosition;
        lastLandingPosition = landingPosition;
        hasLastRetreat = true;

        float duration = Mathf.Max(0.01f, profile.JumpDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!CanContinue())
            {
                Finish(false);
                yield break;
            }

            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 rootPosition = Vector3.Lerp(startPosition, landingPosition, t);
            rootPosition.y = rootY;

            movement?.MoveControlledTo(rootPosition, preserveCurrentY: true);

            if (target != null)
            {
                movement?.FaceTarget(target.position);
            }

            ApplyVisualArc(t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        movement?.MoveControlledTo(landingPosition, preserveCurrentY: true);

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        RestoreVisualRoot();

        yield return WaitWhileUsable(profile.RecoveryTime);
        Finish(true);
    }

    public bool TryStartDisengage(Transform target)
    {
        if (!CanUse(target))
        {
            return false;
        }

        selfRoutine = StartCoroutine(SelfExecuteRoutine(target));
        return true;
    }

    public void Cancel()
    {
        cancelRequested = true;

        if (selfRoutine != null)
        {
            StopCoroutine(selfRoutine);
            selfRoutine = null;
        }

        RestoreVisualRoot();
        isRunning = false;
    }

    private IEnumerator SelfExecuteRoutine(Transform target)
    {
        yield return Execute(target);
        selfRoutine = null;
    }

    private IEnumerator WaitWhileUsable(float duration)
    {
        float endTime = Time.time + Mathf.Max(0f, duration);

        while (Time.time < endTime)
        {
            if (!CanContinue())
            {
                yield break;
            }

            yield return null;
        }
    }

    private bool CanContinue()
    {
        return !cancelRequested
            && actor != null
            && actor.IsAlive
            && profile != null
            && isActiveAndEnabled
            && gameObject.activeInHierarchy;
    }

    private void Finish(bool completedNormally)
    {
        RestoreVisualRoot();
        movement?.Stop();

        isRunning = false;
        cancelRequested = false;
    }

    private bool TryFindLandingPosition(Vector3 threatPosition, out Vector3 landingPosition)
    {
        landingPosition = transform.position;

        if (profile == null)
        {
            return false;
        }

        Vector3 startPosition = movement != null ? movement.CurrentPosition : transform.position;
        Vector3 awayDirection = startPosition - threatPosition;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude <= 0.0001f)
        {
            awayDirection = -transform.forward;
            awayDirection.y = 0f;
        }

        if (awayDirection.sqrMagnitude <= 0.0001f)
        {
            awayDirection = Vector3.back;
        }

        awayDirection.Normalize();

        int steps = Mathf.Max(1, profile.DistanceSearchSteps);

        for (int i = 0; i < steps; i++)
        {
            float t = steps == 1 ? 0f : (float)i / (steps - 1);
            float distance = Mathf.Lerp(profile.RetreatDistance, profile.MinimumRetreatDistance, t);
            Vector3 candidate = startPosition + awayDirection * distance;
            candidate.y = startPosition.y;

            if (IsRetreatPathSafe(startPosition, candidate))
            {
                landingPosition = candidate;
                return true;
            }
        }

        return false;
    }

    private bool IsRetreatPathSafe(Vector3 startPosition, Vector3 candidate)
    {
        if (profile == null)
        {
            return false;
        }

        float radius = GetBodyRadius();

        Vector3 pathStart = startPosition + Vector3.up * profile.PathCheckHeight;
        Vector3 pathEnd = candidate + Vector3.up * profile.PathCheckHeight;

        Vector3 direction = pathEnd - pathStart;
        float distance = direction.magnitude;

        if (distance <= 0.0001f)
        {
            return false;
        }

        direction /= distance;

        bool pathBlocked = Physics.SphereCast(
            pathStart,
            radius,
            direction,
            out _,
            distance,
            profile.BlockingLayers,
            QueryTriggerInteraction.Ignore);

        if (pathBlocked)
        {
            return false;
        }

        Vector3 landingCheckCenter = candidate + Vector3.up * profile.LandingCheckHeight;

        bool landingBlocked = Physics.CheckSphere(
            landingCheckCenter,
            radius,
            profile.BlockingLayers,
            QueryTriggerInteraction.Ignore);

        return !landingBlocked;
    }

    private float GetBodyRadius()
    {
        if (profile != null && profile.BodyRadiusOverride > 0f)
        {
            return profile.BodyRadiusOverride;
        }

        if (actor != null && actor.Definition != null)
        {
            return actor.Definition.BodyRadius;
        }

        return 0.45f;
    }

    private void ApplyVisualArc(float t)
    {
        if (visualRoot == null || profile == null)
        {
            return;
        }

        if (!hasVisualBaseLocalPosition)
        {
            visualBaseLocalPosition = visualRoot.localPosition;
            hasVisualBaseLocalPosition = true;
        }

        float arc = profile.JumpHeight * 4f * t * (1f - t);
        visualRoot.localPosition = visualBaseLocalPosition + Vector3.up * arc;
    }

    private void RestoreVisualRoot()
    {
        if (visualRoot == null || !hasVisualBaseLocalPosition)
        {
            return;
        }

        visualRoot.localPosition = visualBaseLocalPosition;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos || profile == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, profile.TriggerDistance);

        if (profile.MinTargetDistance > 0f)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, profile.MinTargetDistance);
        }

        if (hasLastRetreat)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(lastStartPosition + Vector3.up * 0.1f, lastLandingPosition + Vector3.up * 0.1f);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lastLandingPosition + Vector3.up * profile.LandingCheckHeight, GetBodyRadius());
        }
    }
}