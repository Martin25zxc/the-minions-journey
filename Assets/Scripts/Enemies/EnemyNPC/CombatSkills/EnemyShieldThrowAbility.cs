using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyShieldThrowAbility : MonoBehaviour, IEnemyAbility
{
    [Header("References")]
    [SerializeField]
    private EnemyActor actor;

    [SerializeField]
    private EnemyMovement movement;

    [SerializeField]
    private EnemyAnimator enemyAnimator;

    [Tooltip("Controla el visual del escudo equipado en la mano. Evita lanzar otro escudo mientras el actual esta viajando.")]
    [SerializeField]
    private EnemyShieldVisualController shieldVisualController;

    [Tooltip("Punto de lanzamiento del escudo. Puede ser el shield_badge o un empty en la mano. Si queda vacio, se usa Fire Point Local Offset del profile.")]
    [SerializeField]
    private Transform firePoint;

    [Tooltip("Punto al que vuelve el escudo. Si queda vacio, usa FirePoint. Si FirePoint queda vacio, usa el transform del enemigo.")]
    [SerializeField]
    private Transform returnAnchor;

    [Header("Profile")]
    [SerializeField]
    private EnemyShieldThrowProfile profile;

    [Header("Debug")]
    [SerializeField]
    private bool drawDebugGizmos = true;

    private bool isRunning;
    private bool cancelRequested;
    private Coroutine selfRoutine;
    private float nextAvailableTime;

    private Vector3 lastThrowOrigin;
    private Vector3 lastThrowDirection;
    private bool hasLastThrow;

    public bool IsRunning => isRunning;
    public bool IsOnCooldown => Time.time < nextAvailableTime;

    private void Awake()
    {
        if (actor == null) actor = GetComponent<EnemyActor>();
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (enemyAnimator == null) enemyAnimator = GetComponent<EnemyAnimator>();
        if (shieldVisualController == null) shieldVisualController = GetComponent<EnemyShieldVisualController>();
    }

    private void Start()
    {
        if (profile == null)
        {
            Debug.LogWarning($"[{nameof(EnemyShieldThrowAbility)}] {name} has no EnemyShieldThrowProfile assigned. Shield Throw is disabled.", this);
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

        if (profile.ProjectilePrefab == null)
        {
            return false;
        }

        if (shieldVisualController != null && !shieldVisualController.IsShieldAvailable)
        {
            return false;
        }

        float distance = HorizontalDistance(transform.position, target.position);
        if (!profile.IsDistanceInRange(distance))
        {
            return false;
        }

        if (profile.RequireClearPathToTarget && !HasClearPathToTarget(target))
        {
            return false;
        }

        return true;
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

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        enemyAnimator?.PlayShieldThrow();

        yield return WaitAiming(profile.WindupTime, target);
        if (!CanContinue(target))
        {
            Finish(false);
            yield break;
        }

        Vector3 origin = GetFireOrigin();
        Vector3 direction = GetThrowDirection(origin, target);

        if (shieldVisualController != null && !shieldVisualController.TryMarkShieldThrown())
        {
            Finish(false);
            yield break;
        }

        LaunchProjectile(origin, direction);

        nextAvailableTime = Time.time + profile.Cooldown;

        yield return WaitAiming(profile.RecoveryTime, target);
        Finish(true);
    }

    public bool TryStartShieldThrow(Transform target)
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

        isRunning = false;
    }

    private IEnumerator SelfExecuteRoutine(Transform target)
    {
        yield return Execute(target);
        selfRoutine = null;
    }

    private IEnumerator WaitAiming(float duration, Transform target)
    {
        float endTime = Time.time + Mathf.Max(0f, duration);

        while (Time.time < endTime)
        {
            if (!CanContinue(target))
            {
                yield break;
            }

            if (target != null)
            {
                movement?.FaceTarget(target.position);
            }

            yield return null;
        }
    }

    private bool CanContinue(Transform target)
    {
        return !cancelRequested
            && actor != null
            && actor.IsAlive
            && profile != null
            && target != null
            && isActiveAndEnabled
            && gameObject.activeInHierarchy;
    }

    private void LaunchProjectile(Vector3 origin, Vector3 direction)
    {
        EnemyShieldProjectile projectile = Instantiate(
            profile.ProjectilePrefab,
            origin,
            Quaternion.LookRotation(direction, Vector3.up),
            null);

        Transform resolvedReturnAnchor = returnAnchor != null
            ? returnAnchor
            : firePoint != null
                ? firePoint
                : transform;

        EnemyShieldProjectileLaunchData launchData = new EnemyShieldProjectileLaunchData(
            gameObject,
            gameObject,
            resolvedReturnAnchor,
            origin,
            direction,
            profile.OutboundSpeed,
            profile.ReturnSpeed,
            profile.MaxOutboundDistance,
            profile.ReturnArrivalDistance,
            profile.HitRadius,
            profile.MaxLifetime,
            profile.OutboundDamage,
            profile.ReturnDamage,
            profile.CanHitSameTargetOnReturn,
            profile.SameTargetReturnHitGraceTime,
            profile.TargetLayers,
            profile.BlockingLayers,
            profile.SnapBackOnBlockingHit,
            profile.ApplyImpact,
            profile.ApplyImpactOnlyOncePerTargetPerThrow,
            profile.KnockbackDistance,
            profile.KnockbackDuration,
            profile.StunDuration,
            profile.InterruptCurrentAction);

        projectile.Launch(launchData, HandleShieldProjectileFinished);

        lastThrowOrigin = origin;
        lastThrowDirection = direction;
        hasLastThrow = true;
    }

    private void HandleShieldProjectileFinished(EnemyShieldProjectile projectile)
    {
        if (shieldVisualController != null)
        {
            shieldVisualController.MarkShieldReturned();
        }
    }

    private Vector3 GetFireOrigin()
    {
        if (firePoint != null)
        {
            return firePoint.position;
        }

        if (profile == null)
        {
            return transform.position + Vector3.up * 0.8f;
        }

        return transform.TransformPoint(profile.FirePointLocalOffset);
    }

    private Vector3 GetThrowDirection(Vector3 origin, Transform target)
    {
        Vector3 aimPoint = target.position + Vector3.up * profile.TargetAimHeight;
        Vector3 direction = aimPoint - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
        }

        return direction.normalized;
    }

    private bool HasClearPathToTarget(Transform target)
    {
        if (target == null || profile == null)
        {
            return false;
        }

        Vector3 origin = GetFireOrigin();
        Vector3 aimPoint = target.position + Vector3.up * profile.TargetAimHeight;
        Vector3 direction = aimPoint - origin;
        direction.y = 0f;

        float distance = direction.magnitude;
        if (distance <= 0.0001f)
        {
            return true;
        }

        direction /= distance;

        return !Physics.SphereCast(
            origin,
            profile.HitRadius,
            direction,
            out _,
            distance,
            profile.BlockingLayers,
            QueryTriggerInteraction.Ignore);
    }

    private void Finish(bool completedNormally)
    {
        isRunning = false;
        cancelRequested = false;
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

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, profile.MinAttackRange);
        Gizmos.DrawWireSphere(transform.position, profile.MaxAttackRange);

        if (hasLastThrow)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(lastThrowOrigin, lastThrowOrigin + lastThrowDirection * profile.MaxOutboundDistance);
        }
    }
}
