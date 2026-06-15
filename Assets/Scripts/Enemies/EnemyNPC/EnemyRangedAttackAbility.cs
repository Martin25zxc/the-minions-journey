using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyRangedAttackAbility : MonoBehaviour, IEnemyAbility
{
    [Header("References")]
    [SerializeField]
    private EnemyActor actor;

    [SerializeField]
    private EnemyMovement movement;

    [SerializeField]
    private EnemyAnimator enemyAnimator;

    [Tooltip("Punto desde donde sale el proyectil. Si queda vacio, se usa el offset del profile desde el root.")]
    [SerializeField]
    private Transform firePoint;

    [Header("Profile")]
    [SerializeField]
    private EnemyRangedAttackProfile profile;

    [Header("Debug")]
    [SerializeField]
    private bool drawDebugGizmos = true;

    [SerializeField]
    private bool logShots;

    private bool isRunning;
    private bool cancelRequested;
    private Coroutine selfRoutine;
    private float nextAvailableTime;
    private Vector3 lastFireOrigin;
    private Vector3 lastFireDirection;
    private bool hasLastShot;

    public bool IsRunning => isRunning;
    public bool IsOnCooldown => Time.time < nextAvailableTime;
    public EnemyRangedAttackProfile Profile => profile;

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
            Debug.LogWarning($"[{nameof(EnemyRangedAttackAbility)}] {name} has no EnemyRangedAttackProfile assigned. Ranged attack is disabled.", this);
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

        enemyAnimator?.PlayRangedAttack();

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        yield return WaitWhileUsable(profile.AimTime);
        if (!CanContinue(target))
        {
            Finish(false);
            yield break;
        }

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        yield return WaitWhileUsable(profile.FireDelay);
        if (!CanContinue(target))
        {
            Finish(false);
            yield break;
        }

        Fire(target);

        yield return WaitWhileUsable(profile.RecoveryTime);
        Finish(true);
    }

    /// <summary>
    /// Metodo de compatibilidad si se quiere probar la ability sin el EnemyBrain nuevo.
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
            if (cancelRequested || actor == null || !actor.IsAlive)
            {
                yield break;
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

    private void Fire(Transform target)
    {
        if (profile == null || target == null)
        {
            return;
        }

        Vector3 origin = GetFireOrigin();
        Vector3 aimPoint = target.position + Vector3.up * profile.TargetAimHeight;
        Vector3 direction = aimPoint - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
        }

        direction.Normalize();
        lastFireOrigin = origin;
        lastFireDirection = direction;
        hasLastShot = true;

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

        if (logShots)
        {
            Debug.Log($"[{nameof(EnemyRangedAttackAbility)}] {name} fired projectile toward {target.name}.", this);
        }
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

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, profile.MinAttackRange);
        Gizmos.DrawWireSphere(transform.position, profile.MaxAttackRange);

        if (hasLastShot)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(lastFireOrigin, lastFireOrigin + lastFireDirection * 3f);
        }
    }
}
