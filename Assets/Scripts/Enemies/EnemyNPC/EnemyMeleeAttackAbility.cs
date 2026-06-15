using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyMeleeAttackAbility : MonoBehaviour, IEnemyAbility
{
    [Header("References")]
    [SerializeField]
    private EnemyActor actor;

    [SerializeField]
    private EnemyMovement movement;

    [SerializeField]
    private EnemyAnimator enemyAnimator;

    [Header("Profile")]
    [SerializeField]
    private EnemyMeleeAttackProfile profile;

    [Header("Query")]
    [SerializeField, Min(1)]
    private int maxHitResults = 12;

    [Header("Debug")]
    [SerializeField]
    private bool drawDebugGizmos = true;

    private readonly HashSet<ITopDownDamageable> processedTargets = new HashSet<ITopDownDamageable>();
    private Collider[] hitResults;
    private Coroutine selfRoutine;
    private bool isRunning;
    private bool cancelRequested;
    private float nextAvailableTime;
    private Vector3 lastHitCenter;
    private bool hasLastHitCenter;

    public bool IsRunning => isRunning;
    public bool IsBusy => isRunning;
    public bool IsOnCooldown => Time.time < nextAvailableTime;
    public EnemyMeleeAttackProfile Profile => profile;

    private void Awake()
    {
        if (actor == null) actor = GetComponent<EnemyActor>();
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (enemyAnimator == null) enemyAnimator = GetComponent<EnemyAnimator>();
        hitResults = new Collider[Mathf.Max(1, maxHitResults)];
    }

    private void Start()
    {
        if (profile == null)
        {
            Debug.LogWarning($"[{nameof(EnemyMeleeAttackAbility)}] {name} has no EnemyMeleeAttackProfile assigned. Melee attack is disabled.", this);
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

        return IsTargetInRange(target);
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

        enemyAnimator?.PlayMeleeAttack();

        yield return WaitWhileUsable(profile.StartupTime);
        if (!CanContinueAttack())
        {
            Finish(false);
            yield break;
        }

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        PerformHit();

        yield return WaitWhileUsable(profile.ActiveTime + profile.RecoveryTime);
        Finish(true);
    }

    public bool TryStartAttack(Transform target)
    {
        if (!CanUse(target))
        {
            return false;
        }

        selfRoutine = StartCoroutine(SelfExecuteRoutine(target));
        return true;
    }

    public void CancelCurrentAttack()
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

        processedTargets.Clear();
        isRunning = false;
    }

    public bool IsTargetInRange(Transform target)
    {
        if (profile == null || target == null)
        {
            return false;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        return toTarget.sqrMagnitude <= profile.AttackRange * profile.AttackRange;
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
            if (!CanContinueAttack())
            {
                yield break;
            }

            yield return null;
        }
    }

    private bool CanContinueAttack()
    {
        return !cancelRequested
            && actor != null
            && actor.IsAlive
            && profile != null
            && isActiveAndEnabled
            && gameObject.activeInHierarchy;
    }

    private void PerformHit()
    {
        if (profile == null)
        {
            return;
        }

        processedTargets.Clear();

        Vector3 hitCenter = profile.GetHitCenter(transform);
        lastHitCenter = hitCenter;
        hasLastHitCenter = true;

        int hitCount = Physics.OverlapSphereNonAlloc(
            hitCenter,
            profile.HitRadius,
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
            Vector3 directionFromSourceToTarget = targetPosition - transform.position;
            directionFromSourceToTarget.y = 0f;

            TMJ_DamageInfo damageInfo = new TMJ_DamageInfo(
                profile.Damage,
                hitCenter,
                gameObject,
                gameObject,
                TMJ_DamageUtility.GetSafeClosestPoint(targetCollider, hitCenter),
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

            bool targetStillAlive = targetHealth == null || targetHealth.IsAlive;
            if (targetStillAlive && profile.ApplyImpact)
            {
                ApplyImpact(targetCollider, hitCenter);
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

        Vector3 direction = GetImpactDirection(targetCollider);
        ImpactInfo impactInfo = new ImpactInfo(
            gameObject,
            sourcePosition,
            direction,
            profile.KnockbackDistance,
            profile.KnockbackDuration,
            profile.StunDuration,
            profile.InterruptCurrentAction);

        impactReceiver.ReceiveImpact(impactInfo);
    }

    private Vector3 GetImpactDirection(Collider targetCollider)
    {
        Vector3 targetPosition = targetCollider.attachedRigidbody != null
            ? targetCollider.attachedRigidbody.position
            : targetCollider.transform.position;

        Vector3 direction = targetPosition - transform.position;
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

    private void Finish(bool completedNormally)
    {
        if (completedNormally && profile != null)
        {
            nextAvailableTime = Time.time + profile.Cooldown;
        }

        processedTargets.Clear();
        isRunning = false;
        cancelRequested = false;
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
        Gizmos.DrawWireSphere(transform.position, profile.AttackRange);

        Gizmos.color = Color.magenta;
        Vector3 center = hasLastHitCenter ? lastHitCenter : profile.GetHitCenter(transform);
        Gizmos.DrawWireSphere(center, profile.HitRadius);
    }
}
