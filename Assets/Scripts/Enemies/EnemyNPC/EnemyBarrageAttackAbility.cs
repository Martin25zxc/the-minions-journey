using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyBarrageAttackAbility : MonoBehaviour, IEnemyAbility
{
    [Header("References")]
    [SerializeField]
    private EnemyActor actor;

    [SerializeField]
    private EnemyMovement movement;

    [SerializeField]
    private EnemyAnimator enemyAnimator;

    [Tooltip("Punto desde donde salen los proyectiles. Si queda vacio, se usa el offset del profile desde el root.")]
    [SerializeField]
    private Transform firePoint;

    [Header("Profile")]
    [SerializeField]
    private EnemyBarrageAttackProfile profile;

    [Header("Debug")]
    [SerializeField]
    private bool drawDebugGizmos = true;

    [SerializeField]
    private bool logShots;

    private bool isRunning;
    private bool cancelRequested;
    private Coroutine selfRoutine;
    private float nextAvailableTime;
    private Transform engageDelayTarget;
    private float engageDelayEndsAt;
    private bool initialDelayConsumed;
    private Vector3 lastFireOrigin;
    private Vector3 lastFireDirection;
    private bool hasLastShot;

    public bool IsRunning => isRunning;
    public bool IsOnCooldown => Time.time < nextAvailableTime;
    public EnemyBarrageAttackProfile Profile => profile;

    private void Awake()
    {
        if (actor == null)
        {
            actor = GetComponent<EnemyActor>();
        }

        if (movement == null)
        {
            movement = GetComponent<EnemyMovement>();
        }

        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<EnemyAnimator>();
        }
    }

    private void Start()
    {
        if (profile == null)
        {
            Debug.LogWarning($"[{nameof(EnemyBarrageAttackAbility)}] {name} has no EnemyBarrageAttackProfile assigned. Barrage is disabled.", this);
            return;
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

        if (!PassesInitialEngageDelay(target))
        {
            return false;
        }

        float distance = HorizontalDistance(transform.position, target.position);
        return profile.IsDistanceInRange(distance);
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

        // Barrage es una ability especial, pero visualmente es una serie de disparos normales.
        // No usamos un estado largo BarrageAttack: visualmente es una serie de disparos normales.
        Vector3 lockedDirection = GetAimDirection(target);

        yield return WaitAiming(profile.TelegraphTime, target, lockedDirection);
        if (!CanContinue(target))
        {
            Finish(false);
            yield break;
        }

        for (int shotIndex = 0; shotIndex < profile.ShotCount; shotIndex++)
        {
            Vector3 desiredDirection = profile.AimMode == EnemyBarrageAimMode.ReaimEachShot
                ? GetAimDirection(target)
                : lockedDirection;

            FaceDirection(desiredDirection);
            enemyAnimator?.PlayRangedAttack();

            yield return WaitAiming(profile.ShotWindupTime, target, desiredDirection);
            if (!CanContinue(target))
            {
                Finish(false);
                yield break;
            }

            Vector3 visualDirection = GetCurrentFireDirection(desiredDirection);
            FireShot(visualDirection, shotIndex);

            if (shotIndex < profile.ShotCount - 1)
            {
                yield return WaitAiming(profile.DelayBetweenShots, target, desiredDirection);
                if (!CanContinue(target))
                {
                    Finish(false);
                    yield break;
                }
            }
        }

        yield return WaitAiming(profile.RecoveryTime, target, lockedDirection);
        Finish(true);
    }

    /// <summary>
    /// Metodo de compatibilidad para probar la ability sin EnemyBrain.
    /// </summary>
    public bool TryStartAttack(Transform target)
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

    private bool PassesInitialEngageDelay(Transform target)
    {
        if (profile == null || profile.InitialEngageDelay <= 0f)
        {
            return true;
        }

        bool shouldStartNewDelay = engageDelayTarget != target;
        if (profile.ApplyInitialDelayOnlyOnce && initialDelayConsumed)
        {
            shouldStartNewDelay = false;
        }

        if (shouldStartNewDelay)
        {
            engageDelayTarget = target;
            engageDelayEndsAt = Time.time + profile.InitialEngageDelay;
        }

        if (Time.time < engageDelayEndsAt)
        {
            return false;
        }

        initialDelayConsumed = true;
        return true;
    }

    private IEnumerator SelfExecuteRoutine(Transform target)
    {
        yield return Execute(target);
        selfRoutine = null;
    }

    private IEnumerator WaitAiming(float duration, Transform target, Vector3 fallbackDirection)
    {
        float endTime = Time.time + Mathf.Max(0f, duration);
        while (Time.time < endTime)
        {
            if (cancelRequested || actor == null || !actor.IsAlive)
            {
                yield break;
            }

            Vector3 direction = profile != null && profile.AimMode == EnemyBarrageAimMode.ReaimEachShot && target != null
                ? GetAimDirection(target)
                : fallbackDirection;

            FaceDirection(direction);
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

    private Vector3 GetAimDirection(Transform target)
    {
        Vector3 origin = GetFireOrigin();
        Vector3 direction = transform.forward;

        if (target != null && profile != null)
        {
            Vector3 aimPoint = target.position + Vector3.up * profile.TargetAimHeight;
            direction = aimPoint - origin;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector3.forward;
        }

        return direction.normalized;
    }

    private Vector3 GetCurrentFireDirection(Vector3 fallbackDirection)
    {
        Transform originTransform = firePoint != null ? firePoint : transform;
        Vector3 direction = originTransform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = fallbackDirection;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector3.forward;
        }

        return direction.normalized;
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        movement?.FaceTarget(transform.position + direction.normalized);
    }

    private void FireShot(Vector3 baseDirection, int shotIndex)
    {
        if (profile == null)
        {
            return;
        }

        Vector3 origin = GetFireOrigin();
        int projectileCount = Mathf.Max(1, profile.ProjectilesPerShot);
        float spread = projectileCount > 1 ? profile.SpreadAngle : 0f;
        float halfSpread = spread * 0.5f;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = 0f;
            if (projectileCount > 1)
            {
                float t = projectileCount == 1 ? 0.5f : (float)i / (projectileCount - 1);
                angle = Mathf.Lerp(-halfSpread, halfSpread, t);
            }

            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * baseDirection;
            FireProjectile(origin, direction.normalized);
        }

        lastFireOrigin = origin;
        lastFireDirection = baseDirection;
        hasLastShot = true;

        if (logShots)
        {
            Debug.Log($"[{nameof(EnemyBarrageAttackAbility)}] {name} fired barrage shot {shotIndex + 1}/{profile.ShotCount}.", this);
        }
    }

    private void FireProjectile(Vector3 origin, Vector3 direction)
    {
        EnemyProjectile projectile = GetProjectileInstance(origin, direction);
        if (projectile == null)
        {
            return;
        }

        EnemyProjectileLaunchData launchData = new EnemyProjectileLaunchData(
            gameObject,
            origin,
            direction,
            profile.ProjectileSpeed,
            profile.ProjectileDamage,
            profile.ProjectileLifetime,
            profile.TargetLayers,
            profile.BlockingLayers,
            profile.ApplyImpact,
            profile.KnockbackDistance,
            profile.KnockbackDuration,
            profile.StunDuration,
            profile.InterruptCurrentAction);

        projectile.Launch(launchData);
    }

    private EnemyProjectile GetProjectileInstance(Vector3 origin, Vector3 direction)
    {
        if (profile == null || profile.ProjectilePrefab == null)
        {
            return null;
        }

        // Instanciar sin parent: el proyectil activo no debe heredar movimiento/rotacion del enemigo.
        return Instantiate(profile.ProjectilePrefab, origin, Quaternion.LookRotation(direction, Vector3.up), null);
    }

    private Vector3 GetFireOrigin()
    {
        if (firePoint != null)
        {
            return firePoint.position;
        }

        return transform.TransformPoint(profile.FirePointLocalOffset);
    }

    private void Finish(bool completedNormally)
    {
        if (completedNormally && profile != null)
        {
            nextAvailableTime = Time.time + profile.Cooldown;
        }

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

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, profile.MinAttackRange);
        Gizmos.DrawWireSphere(transform.position, profile.MaxAttackRange);

        if (hasLastShot)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(lastFireOrigin, lastFireOrigin + lastFireDirection * 4f);
        }
    }
}
