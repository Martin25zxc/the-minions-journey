using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyMeleeAttackAbility : MonoBehaviour
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
    private Coroutine attackRoutine;
    private float nextAvailableTime;
    private Vector3 lastHitCenter;
    private bool hasLastHitCenter;

    public bool IsBusy => attackRoutine != null;
    public bool IsOnCooldown => Time.time < nextAvailableTime;
    public EnemyMeleeAttackProfile Profile => profile;

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

        hitResults = new Collider[Mathf.Max(1, maxHitResults)];
    }

    private void Start()
    {
        if (profile == null)
        {
            Debug.LogWarning($"[{nameof(EnemyMeleeAttackAbility)}] {name} has no EnemyMeleeAttackProfile assigned. The enemy can still move/leap, but melee attack is disabled.", this);
        }
    }

    private void OnDisable()
    {
        CancelCurrentAttack();
    }

    public void Initialize(EnemyMeleeAttackProfile newProfile)
    {
        if (newProfile != null)
        {
            profile = newProfile;
        }
    }

    public bool CanStartAttack(Transform target)
    {
        if (actor == null || !actor.IsAlive || profile == null || target == null)
        {
            return false;
        }

        if (IsBusy || IsOnCooldown)
        {
            return false;
        }

        return IsTargetInRange(target);
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

    public float GetPreferredStopDistance(float fallback)
    {
        return profile != null ? profile.AttackRange : fallback;
    }

    public bool TryStartAttack(Transform target)
    {
        if (!CanStartAttack(target))
        {
            return false;
        }

        attackRoutine = StartCoroutine(AttackRoutine(target));
        return true;
    }

    public void CancelCurrentAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        processedTargets.Clear();
    }

    private IEnumerator AttackRoutine(Transform target)
    {
        movement?.Stop();

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        enemyAnimator?.PlayMeleeAttack();

        yield return WaitInterruptible(profile.StartupTime);

        if (!CanContinueAttack())
        {
            FinishAttack(false);
            yield break;
        }

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        PerformHit();

        yield return WaitInterruptible(profile.ActiveTime + profile.RecoveryTime);

        FinishAttack(true);
    }

    private IEnumerator WaitInterruptible(float duration)
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
        return actor != null
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

            bool damaged = TMJ_DamageUtility.TryDamageCollider(
                targetCollider,
                profile.Damage,
                hitCenter,
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

    private void FinishAttack(bool completedNormally)
    {
        nextAvailableTime = Time.time + (profile != null ? profile.Cooldown : 0f);
        processedTargets.Clear();
        attackRoutine = null;
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
