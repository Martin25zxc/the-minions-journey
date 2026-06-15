using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyLeapAttackAbility : MonoBehaviour, IEnemyAbility
{
    [Header("References")]
    [SerializeField]
    private EnemyActor actor;

    [SerializeField]
    private EnemyMovement movement;

    [SerializeField]
    private EnemyAnimator enemyAnimator;

    [Tooltip("Hijo visual que hace el arco vertical. Si queda vacio, se intenta usar el Transform del Animator hijo.")]
    [SerializeField]
    private Transform visualRoot;

    [Header("Profile")]
    [SerializeField]
    private EnemyLeapAttackProfile profile;

    [Header("Queries")]
    [SerializeField, Min(1)]
    private int maxHitResults = 16;

    [Header("Debug")]
    [SerializeField]
    private bool drawDebugGizmos = true;

    private readonly HashSet<ITopDownDamageable> processedTargets = new HashSet<ITopDownDamageable>();
    private Collider[] hitResults;
    private Coroutine selfRoutine;
    private bool isRunning;
    private bool cancelRequested;
    private float nextAvailableTime;
    private Vector3 visualBaseLocalPosition;
    private bool hasVisualBaseLocalPosition;
    private Vector3 lastLandingPosition;
    private Vector3 lastLandingDamageCenter;
    private bool hasLastLandingPosition;

    public bool IsRunning => isRunning;
    public bool IsBusy => isRunning;
    public bool IsOnCooldown => Time.time < nextAvailableTime;
    public EnemyLeapAttackProfile Profile => profile;

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

        hitResults = new Collider[Mathf.Max(1, maxHitResults)];
    }

    private void Start()
    {
        if (profile == null)
        {
            Debug.LogWarning($"[{nameof(EnemyLeapAttackAbility)}] {name} has no EnemyLeapAttackProfile assigned. Leap attack is disabled.", this);
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

        if (!IsTargetInLeapRange(target))
        {
            return false;
        }

        return TryFindLandingPosition(target, out _);
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
        movement?.Stop();

        // El cooldown empieza al iniciar la habilidad: si se cancela, no se reintenta inmediatamente.
        nextAvailableTime = Time.time + profile.Cooldown;

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        enemyAnimator?.PlayLeapStart();

        yield return WaitWhileUsable(profile.TelegraphTime);
        if (!CanContinueLeap())
        {
            Finish(false);
            yield break;
        }

        if (!TryFindLandingPosition(target, out Vector3 landingPosition))
        {
            Finish(false);
            yield break;
        }

        lastLandingPosition = landingPosition;
        hasLastLandingPosition = true;

        enemyAnimator?.SetOnAir(true);

        Vector3 startPosition = movement != null ? movement.CurrentPosition : transform.position;
        float rootY = startPosition.y;
        landingPosition.y = rootY;

        float duration = Mathf.Max(0.01f, profile.JumpDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!CanContinueLeap())
            {
                Finish(false);
                yield break;
            }

            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 rootPosition = Vector3.Lerp(startPosition, landingPosition, t);
            rootPosition.y = rootY;

            movement?.MoveControlledTo(rootPosition, preserveCurrentY: true);
            ApplyVisualArc(t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        movement?.MoveControlledTo(landingPosition, preserveCurrentY: true);
        RestoreVisualRoot();

        enemyAnimator?.PlayLeapLand();
        PerformLandingDamage(landingPosition);

        yield return WaitWhileUsable(profile.RecoveryTime);
        Finish(true);
    }

    public bool TryStartLeap(Transform target)
    {
        if (!CanUse(target))
        {
            return false;
        }

        selfRoutine = StartCoroutine(SelfExecuteRoutine(target));
        return true;
    }

    public void CancelCurrentLeap()
    {
        Cancel();
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
        enemyAnimator?.SetOnAir(false);
        processedTargets.Clear();
        isRunning = false;
    }

    public bool IsTargetInLeapRange(Transform target)
    {
        if (profile == null || target == null)
        {
            return false;
        }

        float distance = HorizontalDistance(transform.position, target.position);
        return profile.IsDistanceInRange(distance);
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
            if (!CanContinueLeap())
            {
                yield break;
            }

            yield return null;
        }
    }

    private bool CanContinueLeap()
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
        enemyAnimator?.SetOnAir(false);
        processedTargets.Clear();
        isRunning = false;
        cancelRequested = false;
    }

    private void ApplyVisualArc(float t)
    {
        if (visualRoot == null)
        {
            return;
        }

        if (!hasVisualBaseLocalPosition)
        {
            visualBaseLocalPosition = visualRoot.localPosition;
            hasVisualBaseLocalPosition = true;
        }

        float arc = profile != null ? profile.JumpHeight * 4f * t * (1f - t) : 0f;
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

    private bool TryFindLandingPosition(Transform target, out Vector3 landingPosition)
    {
        landingPosition = transform.position;

        if (profile == null || target == null)
        {
            return false;
        }

        Vector3 enemyToTarget = target.position - transform.position;
        enemyToTarget.y = 0f;

        if (enemyToTarget.sqrMagnitude <= 0.0001f)
        {
            enemyToTarget = transform.forward;
            enemyToTarget.y = 0f;
        }

        if (enemyToTarget.sqrMagnitude <= 0.0001f)
        {
            enemyToTarget = Vector3.forward;
        }

        enemyToTarget.Normalize();
        Vector3 targetPosition = target.position;
        targetPosition.y = transform.position.y;

        Vector3 ideal = targetPosition - enemyToTarget * profile.PreferredLandingDistanceFromTarget;
        if (IsLandingPositionSafe(ideal))
        {
            landingPosition = ideal;
            return true;
        }

        int steps = Mathf.Max(1, profile.LandingSearchSteps);
        Vector3 baseDirectionFromTarget = -enemyToTarget;
        for (int i = 0; i < steps; i++)
        {
            float angle = (360f / steps) * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * baseDirectionFromTarget;
            Vector3 candidate = targetPosition + direction.normalized * profile.PreferredLandingDistanceFromTarget;

            if (IsLandingPositionSafe(candidate))
            {
                landingPosition = candidate;
                return true;
            }
        }

        float extraRadius = profile.LandingSearchRadius;
        if (extraRadius <= 0f)
        {
            return false;
        }

        for (int i = 0; i < steps; i++)
        {
            float angle = (360f / steps) * i;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * extraRadius;
            Vector3 candidate = ideal + offset;

            if (IsLandingPositionSafe(candidate))
            {
                landingPosition = candidate;
                return true;
            }
        }

        return false;
    }

    private bool IsLandingPositionSafe(Vector3 candidate)
    {
        if (profile == null)
        {
            return false;
        }

        float radius = GetLandingBodyRadius();
        Vector3 checkCenter = candidate + Vector3.up * profile.LandingCheckHeight;

        bool blocked = Physics.CheckSphere(
            checkCenter,
            radius,
            profile.BlockingLayers,
            QueryTriggerInteraction.Ignore);

        return !blocked;
    }

    private float GetLandingBodyRadius()
    {
        if (profile != null && profile.LandingBodyRadiusOverride > 0f)
        {
            return profile.LandingBodyRadiusOverride;
        }

        if (actor != null && actor.Definition != null)
        {
            return actor.Definition.BodyRadius;
        }

        return 0.45f;
    }

    private void PerformLandingDamage(Vector3 landingPosition)
    {
        if (profile == null)
        {
            return;
        }

        processedTargets.Clear();

        Vector3 damageCenter = landingPosition
            + transform.right * profile.LandingDamageOffset.x
            + Vector3.up * profile.LandingDamageOffset.y
            + transform.forward * profile.LandingDamageOffset.z;

        lastLandingDamageCenter = damageCenter;

        int hitCount = Physics.OverlapSphereNonAlloc(
            damageCenter,
            profile.LandingDamageRadius,
            hitResults,
            profile.TargetLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider targetCollider = hitResults[i];
            if (targetCollider == null)
            {
                continue;
            }

            TopDownHealth targetHealth = targetCollider.GetComponentInParent<TopDownHealth>();

            bool damaged = TMJ_DamageUtility.TryDamageCollider(
                targetCollider,
                profile.LandingDamage,
                damageCenter,
                gameObject,
                profile.TargetLayers,
                gameObject,
                processedTargets,
                out ITopDownDamageable damageable);

            if (!damaged)
            {
                continue;
            }

            bool targetStillAlive = targetHealth == null || targetHealth.IsAlive;
            if (targetStillAlive && profile.ApplyImpactOnLanding)
            {
                ApplyImpact(targetCollider, damageCenter);
            }
        }
    }

    private void ApplyImpact(Collider targetCollider, Vector3 sourcePosition)
    {
        IImpactReceiver impactReceiver = FindImpactReceiver(targetCollider);
        if (impactReceiver == null)
        {
            return;
        }

        Vector3 direction = GetImpactDirection(targetCollider, sourcePosition);
        ImpactInfo impactInfo = new ImpactInfo(
            gameObject,
            sourcePosition,
            direction,
            profile.LandingKnockbackDistance,
            profile.LandingKnockbackDuration,
            profile.LandingStunDuration,
            profile.InterruptCurrentAction);

        impactReceiver.ReceiveImpact(impactInfo);
    }

    private Vector3 GetImpactDirection(Collider targetCollider, Vector3 sourcePosition)
    {
        Vector3 targetPosition = targetCollider.attachedRigidbody != null
            ? targetCollider.attachedRigidbody.position
            : targetCollider.transform.position;

        Vector3 direction = targetPosition - sourcePosition;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
        }

        return direction.normalized;
    }

    private static IImpactReceiver FindImpactReceiver(Collider targetCollider)
    {
        if (targetCollider == null)
        {
            return null;
        }

        MonoBehaviour[] behaviours = targetCollider.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IImpactReceiver receiver)
            {
                return receiver;
            }
        }

        return null;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnValidate()
    {
        maxHitResults = Mathf.Max(1, maxHitResults);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos || profile == null)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, profile.MinRange);
        Gizmos.DrawWireSphere(transform.position, profile.MaxRange);

        if (hasLastLandingPosition)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lastLandingPosition + Vector3.up * profile.LandingCheckHeight, GetLandingBodyRadius());

            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(lastLandingDamageCenter, profile.LandingDamageRadius);
        }
    }
}
