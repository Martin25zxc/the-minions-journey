using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpinSlash de boss: daño radial + patrón de shockwaves por giro.
///
/// v2: la animación de spin se inicia/reinicia por cada giro, no una sola vez por habilidad.
/// Flujo por giro:
/// - reinicia clip BossSpinSlash;
/// - espera SpinHitDelay;
/// - aplica daño radial y lanza shockwaves;
/// - espera SpinInterval antes del siguiente giro.
/// </summary>
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemySpinShockwaveAbility : MonoBehaviour, IEnemyAbility
{
    [Header("References")]
    [SerializeField] private EnemyActor actor;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyBossPhaseState phaseState;
    [SerializeField] private EnemyBossAnimatorBridge bossAnimatorBridge;
    [SerializeField] private EnemyAnimator enemyAnimatorFallback;
    [SerializeField] private EnemyBossSpecialCooldownGate specialCooldownGate;
    [SerializeField] private Transform firePoint;

    [Header("Profile")]
    [SerializeField] private EnemySpinShockwaveProfile profile;

    [Header("Query")]
    [SerializeField, Min(1)] private int maxHitResults = 16;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;

    private readonly HashSet<ITopDownDamageable> radialProcessedTargets = new HashSet<ITopDownDamageable>();
    private Collider[] hitResults;
    private bool isRunning;
    private bool cancelRequested;
    private float nextAvailableTime;

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
        hitResults = new Collider[Mathf.Max(1, maxHitResults)];
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
        radialProcessedTargets.Clear();
        movement?.Stop();

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        yield return WaitAiming(profile.TelegraphTime, target);
        if (!CanContinue())
        {
            Finish(false);
            yield break;
        }

        for (int spinIndex = 0; spinIndex < profile.SpinCount; spinIndex++)
        {
            if (!CanContinue())
            {
                Finish(false);
                yield break;
            }

            if (target != null)
            {
                movement?.FaceTarget(target.position);
            }

            PlaySpinAnimation();

            yield return WaitWhileUsable(profile.SpinHitDelay);
            if (!CanContinue())
            {
                Finish(false);
                yield break;
            }

            PerformRadialHit();
            FireShockwavesForSpin(spinIndex);

            bool isLast = spinIndex >= profile.SpinCount - 1;
            if (!isLast && profile.SpinInterval > 0f)
            {
                yield return WaitWhileUsable(profile.SpinInterval);
            }
        }

        yield return WaitWhileUsable(profile.RecoveryTime);
        Finish(true);
    }

    public void Cancel()
    {
        cancelRequested = true;
        isRunning = false;
        radialProcessedTargets.Clear();
    }

    private void PlaySpinAnimation()
    {
        if (bossAnimatorBridge != null)
        {
            bossAnimatorBridge.PlaySpinSlash(profile != null && profile.RestartAnimationEachSpin);
            return;
        }

        enemyAnimatorFallback?.PlayMeleeAttack();
    }

    private void PerformRadialHit()
    {
        if (profile == null || !profile.ApplyRadialHit || profile.RadialDamage <= 0f)
        {
            return;
        }

        if (profile.RadialCanHitSameTargetEachSpin)
        {
            radialProcessedTargets.Clear();
        }

        Vector3 center = transform.position;
        int hitCount = Physics.OverlapSphereNonAlloc(
            center,
            profile.RadialHitRadius,
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
            Vector3 targetPosition = TMJ_DamageUtility.GetTargetReferencePosition(targetCollider);
            Vector3 directionFromSourceToTarget = targetPosition - center;
            directionFromSourceToTarget.y = 0f;

            TMJ_DamageInfo damageInfo = new TMJ_DamageInfo(
                profile.RadialDamage,
                center,
                gameObject,
                gameObject,
                TMJ_DamageUtility.GetSafeClosestPoint(targetCollider, center),
                directionFromSourceToTarget);

            bool damaged = TMJ_DamageUtility.TryDamageCollider(
                targetCollider,
                damageInfo,
                profile.TargetLayers,
                gameObject,
                radialProcessedTargets,
                out ITopDownDamageable damageable);

            if (!damaged)
            {
                continue;
            }

            bool targetStillAlive = targetHealth == null || targetHealth.IsAlive;
            if (targetStillAlive && profile.ApplyRadialImpact)
            {
                ApplyRadialImpact(targetCollider, center);
            }
        }
    }

    private void FireShockwavesForSpin(int spinIndex)
    {
        if (profile == null || profile.ShockwavePrefab == null || profile.ShockwavesPerSpin <= 0)
        {
            return;
        }

        float step = 360f / profile.ShockwavesPerSpin;
        float offset = profile.BaseAngleOffset + profile.AngleOffsetPerSpin * spinIndex;
        Vector3 origin = GetFireOrigin();

        for (int i = 0; i < profile.ShockwavesPerSpin; i++)
        {
            float angle = offset + step * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            LaunchShockwave(origin, direction.normalized);
        }
    }

    private void LaunchShockwave(Vector3 origin, Vector3 direction)
    {
        EnemyProjectile projectile = Instantiate(
            profile.ShockwavePrefab,
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
            profile.ShockwaveSpeed,
            profile.ShockwaveDamage,
            profile.ShockwaveLifetime,
            profile.TargetLayers,
            profile.BlockingLayers,
            profile.ApplyShockwaveImpact,
            profile.ShockwaveKnockbackDistance,
            profile.ShockwaveKnockbackDuration,
            profile.ShockwaveStunDuration,
            profile.ShockwaveInterruptCurrentAction);

        projectile.Launch(launchData);
    }

    private void ApplyRadialImpact(Collider targetCollider, Vector3 sourcePosition)
    {
        IImpactReceiver receiver = FindImpactReceiver(targetCollider);
        if (receiver == null)
        {
            return;
        }

        Vector3 targetPosition = TMJ_DamageUtility.GetTargetReferencePosition(targetCollider);
        Vector3 direction = targetPosition - sourcePosition;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
        }

        ImpactInfo impactInfo = new ImpactInfo(
            gameObject,
            sourcePosition,
            direction.normalized,
            profile.RadialKnockbackDistance,
            profile.RadialKnockbackDuration,
            profile.RadialStunDuration,
            profile.RadialInterruptCurrentAction);

        receiver.ReceiveImpact(impactInfo);
    }

    private IEnumerator WaitAiming(float duration, Transform target)
    {
        float endTime = Time.time + Mathf.Max(0f, duration);
        while (Time.time < endTime)
        {
            if (!CanContinue())
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
        radialProcessedTargets.Clear();
    }

    private Vector3 GetFireOrigin()
    {
        if (firePoint != null)
        {
            return firePoint.position;
        }

        if (profile == null)
        {
            return transform.position + Vector3.up * 0.55f;
        }

        return transform.TransformPoint(profile.FirePointLocalOffset);
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

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, profile.MaxAttackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, profile.RadialHitRadius);
    }
}
