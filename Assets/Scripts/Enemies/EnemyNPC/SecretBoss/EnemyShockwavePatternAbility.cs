using System.Collections;
using UnityEngine;

/// <summary>
/// Ability genérica para shockwaves de boss.
/// Puede cubrir:
/// - Slash Shockwave simple: yawAngles [0], repeatCount 1.
/// - Triple Slash Shockwave: yawAngles [-20, 0, 20], repeatCount 1.
/// - Charged Slash Volley: yawAngles [0], repeatCount 3, delayBetweenRepeats > 0.
/// </summary>
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyShockwavePatternAbility : MonoBehaviour, IEnemyAbility
{
    [Header("References")]
    [SerializeField]
    private EnemyActor actor;

    [SerializeField]
    private EnemyMovement movement;

    [SerializeField]
    private EnemyBossPhaseState phaseState;

    [SerializeField]
    private EnemyBossAnimatorBridge bossAnimatorBridge;

    [SerializeField]
    private EnemyAnimator enemyAnimatorFallback;

    [SerializeField]
    private EnemyBossSpecialCooldownGate specialCooldownGate;

    [SerializeField]
    private Transform firePoint;

    [Header("Profile")]
    [SerializeField]
    private EnemyShockwavePatternProfile profile;

    [Header("Debug")]
    [SerializeField]
    private bool drawDebugGizmos = true;

    private bool isRunning;
    private bool cancelRequested;
    private float nextAvailableTime;
    private Vector3 lastOrigin;
    private Vector3 lastDirection;
    private bool hasLastShot;

    public bool IsRunning => isRunning;
    public bool IsOnCooldown => Time.time < nextAvailableTime;

    private void Awake()
    {
        if (actor == null) actor = GetComponent<EnemyActor>();
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (phaseState == null) phaseState = GetComponent<EnemyBossPhaseState>();
        if (bossAnimatorBridge == null) bossAnimatorBridge = GetComponent<EnemyBossAnimatorBridge>();
        if (enemyAnimatorFallback == null) enemyAnimatorFallback = GetComponent<EnemyAnimator>();
        if (specialCooldownGate == null) specialCooldownGate = GetComponent<EnemyBossSpecialCooldownGate>();
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

        if (isRunning || IsOnCooldown || profile.ProjectilePrefab == null)
        {
            return false;
        }

        if (phaseState != null && !phaseState.IsInsideRange(profile.MinPhase, profile.MaxPhase))
        {
            return false;
        }

        if (profile.UseSpecialCooldownGate && specialCooldownGate != null && !specialCooldownGate.CanUseSpecial)
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

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        if (bossAnimatorBridge != null)
        {
            bossAnimatorBridge.PlaySlashSpecial();
        }
        else
        {
            enemyAnimatorFallback?.PlayRangedAttack();
        }

        Vector3 baseDirection = GetDirectionToTarget(target, GetFireOrigin());

        yield return WaitAiming(profile.TelegraphTime, target);
        if (!CanContinue(target))
        {
            Finish(false);
            yield break;
        }

        for (int repeat = 0; repeat < profile.RepeatCount; repeat++)
        {
            if (!CanContinue(target))
            {
                Finish(false);
                yield break;
            }

            Vector3 origin = GetFireOrigin();
            if (profile.ReAimEachRepeat || repeat == 0)
            {
                baseDirection = GetDirectionToTarget(target, origin);
                movement?.FaceTarget(target.position);
            }

            FirePattern(origin, baseDirection);

            bool isLastRepeat = repeat >= profile.RepeatCount - 1;
            if (!isLastRepeat && profile.DelayBetweenRepeats > 0f)
            {
                yield return WaitAiming(profile.DelayBetweenRepeats, target);
            }
        }

        yield return WaitWhileUsable(profile.RecoveryTime);
        Finish(true);
    }

    public void Cancel()
    {
        cancelRequested = true;
        isRunning = false;
    }

    private void FirePattern(Vector3 origin, Vector3 baseDirection)
    {
        float[] angles = profile.YawAngles;
        if (angles == null || angles.Length == 0)
        {
            angles = new float[] { 0f };
        }

        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, angles[i], 0f) * baseDirection;
            LaunchShockwave(origin, direction.normalized);
        }
    }

    private void LaunchShockwave(Vector3 origin, Vector3 direction)
    {
        EnemyProjectile projectile = Instantiate(
            profile.ProjectilePrefab,
            origin,
            Quaternion.LookRotation(direction, Vector3.up),
            null);

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
        lastOrigin = origin;
        lastDirection = direction;
        hasLastShot = true;
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

    private void Finish(bool completedNormally)
    {
        if (completedNormally && profile != null)
        {
            nextAvailableTime = Time.time + profile.Cooldown;
            if (profile.UseSpecialCooldownGate)
            {
                specialCooldownGate?.NotifySpecialUsed(profile.GlobalCooldownAfterUse);
            }
        }

        isRunning = false;
        cancelRequested = false;
    }

    private Vector3 GetFireOrigin()
    {
        if (firePoint != null)
        {
            return firePoint.position;
        }

        if (profile == null)
        {
            return transform.position + transform.forward * 0.8f + Vector3.up * 0.6f;
        }

        return transform.TransformPoint(profile.FirePointLocalOffset);
    }

    private Vector3 GetDirectionToTarget(Transform target, Vector3 origin)
    {
        if (target == null || profile == null)
        {
            return transform.forward;
        }

        Vector3 aimPoint = target.position + Vector3.up * profile.TargetAimHeight;
        Vector3 direction = aimPoint - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
        }

        return direction.normalized;
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
            Gizmos.color = Color.red;
            Gizmos.DrawLine(lastOrigin, lastOrigin + lastDirection * 3f);
        }
    }
}
