using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pulso necrótico: daño radial y curación del boss por cada target dañado.
/// Stretch goal: útil si se quiere reforzar identidad de no muerto.
/// </summary>
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyDrainPulseAbility : MonoBehaviour, IEnemyAbility
{
    [Header("References")]
    [SerializeField] private EnemyActor actor;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyBossPhaseState phaseState;
    [SerializeField] private EnemyBossAnimatorBridge bossAnimatorBridge;
    [SerializeField] private EnemyAnimator enemyAnimatorFallback;
    [SerializeField] private EnemyBossSpecialCooldownGate specialCooldownGate;

    [Header("Profile")]
    [SerializeField] private EnemyDrainPulseProfile profile;

    [Header("Query")]
    [SerializeField, Min(1)] private int maxHitResults = 16;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;

    private readonly HashSet<ITopDownDamageable> processedTargets = new HashSet<ITopDownDamageable>();
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
        movement?.Stop();

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        if (bossAnimatorBridge != null)
        {
            bossAnimatorBridge.PlayCast();
        }
        else
        {
            enemyAnimatorFallback?.PlayRangedAttack();
        }

        yield return WaitAiming(profile.TelegraphTime, target);
        if (!CanContinue(target))
        {
            Finish(false);
            yield break;
        }

        int damagedTargets = PerformPulse();
        if (damagedTargets > 0 && actor.Health != null)
        {
            actor.Health.Heal(profile.HealPerDamagedTarget * damagedTargets);
        }

        yield return WaitWhileUsable(profile.RecoveryTime);
        Finish(true);
    }

    public void Cancel()
    {
        cancelRequested = true;
        isRunning = false;
        processedTargets.Clear();
    }

    private int PerformPulse()
    {
        processedTargets.Clear();
        int damagedCount = 0;
        Vector3 center = transform.position;

        int hitCount = Physics.OverlapSphereNonAlloc(
            center,
            profile.Radius,
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
                profile.Damage,
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
                processedTargets,
                out ITopDownDamageable damageable);

            if (!damaged)
            {
                continue;
            }

            damagedCount++;

            bool targetStillAlive = targetHealth == null || targetHealth.IsAlive;
            if (targetStillAlive && profile.ApplyImpact)
            {
                ApplyImpact(targetCollider, center);
            }
        }

        processedTargets.Clear();
        return damagedCount;
    }

    private void ApplyImpact(Collider targetCollider, Vector3 sourcePosition)
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
            profile.KnockbackDistance,
            profile.KnockbackDuration,
            profile.StunDuration,
            profile.InterruptCurrentAction);

        receiver.ReceiveImpact(impactInfo);
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
            if (!CanContinue(null))
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
        processedTargets.Clear();
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
        Gizmos.DrawWireSphere(transform.position, profile.Radius);
    }
}
