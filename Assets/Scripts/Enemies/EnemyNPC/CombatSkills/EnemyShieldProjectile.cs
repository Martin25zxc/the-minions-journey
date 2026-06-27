using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyShieldProjectilePhase
{
    None,
    Outbound,
    Returning,
    Finished
}

public struct EnemyShieldProjectileLaunchData
{
    public GameObject Owner;
    public GameObject Instigator;
    public Transform ReturnAnchor;
    public Vector3 Origin;
    public Vector3 Direction;

    public float OutboundSpeed;
    public float ReturnSpeed;
    public float MaxOutboundDistance;
    public float ReturnArrivalDistance;
    public float HitRadius;
    public float MaxLifetime;

    public float OutboundDamage;
    public float ReturnDamage;
    public bool CanHitSameTargetOnReturn;
    public float SameTargetReturnHitGraceTime;

    public LayerMask TargetLayers;
    public LayerMask BlockingLayers;

    public bool SnapBackOnBlockingHit;

    public bool ApplyImpact;
    public bool ApplyImpactOnlyOncePerTargetPerThrow;
    public float KnockbackDistance;
    public float KnockbackDuration;
    public float StunDuration;
    public bool InterruptCurrentAction;

    public EnemyShieldProjectileLaunchData(
        GameObject owner,
        GameObject instigator,
        Transform returnAnchor,
        Vector3 origin,
        Vector3 direction,
        float outboundSpeed,
        float returnSpeed,
        float maxOutboundDistance,
        float returnArrivalDistance,
        float hitRadius,
        float maxLifetime,
        float outboundDamage,
        float returnDamage,
        bool canHitSameTargetOnReturn,
        float sameTargetReturnHitGraceTime,
        LayerMask targetLayers,
        LayerMask blockingLayers,
        bool snapBackOnBlockingHit,
        bool applyImpact,
        bool applyImpactOnlyOncePerTargetPerThrow,
        float knockbackDistance,
        float knockbackDuration,
        float stunDuration,
        bool interruptCurrentAction)
    {
        Owner = owner;
        Instigator = instigator;
        ReturnAnchor = returnAnchor;
        Origin = origin;
        Direction = direction;

        OutboundSpeed = outboundSpeed;
        ReturnSpeed = returnSpeed;
        MaxOutboundDistance = maxOutboundDistance;
        ReturnArrivalDistance = returnArrivalDistance;
        HitRadius = hitRadius;
        MaxLifetime = maxLifetime;

        OutboundDamage = outboundDamage;
        ReturnDamage = returnDamage;
        CanHitSameTargetOnReturn = canHitSameTargetOnReturn;
        SameTargetReturnHitGraceTime = Mathf.Max(0f, sameTargetReturnHitGraceTime);

        TargetLayers = targetLayers;
        BlockingLayers = blockingLayers;

        SnapBackOnBlockingHit = snapBackOnBlockingHit;

        ApplyImpact = applyImpact;
        ApplyImpactOnlyOncePerTargetPerThrow = applyImpactOnlyOncePerTargetPerThrow;
        KnockbackDistance = knockbackDistance;
        KnockbackDuration = knockbackDuration;
        StunDuration = stunDuration;
        InterruptCurrentAction = interruptCurrentAction;
    }
}

[DisallowMultipleComponent]
public sealed class EnemyShieldProjectile : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField]
    private Transform visualSpinRoot;

    [SerializeField]
    private Vector3 spinAxis = Vector3.forward;

    [SerializeField, Min(0f)]
    private float spinDegreesPerSecond = 720f;

    [Header("Debug")]
    [SerializeField]
    private bool drawDebugGizmos;

    private readonly HashSet<ITopDownDamageable> damagedOutbound = new HashSet<ITopDownDamageable>();
    private readonly HashSet<ITopDownDamageable> damagedReturning = new HashSet<ITopDownDamageable>();
    private readonly HashSet<ITopDownDamageable> impactedTargets = new HashSet<ITopDownDamageable>();
    private readonly Dictionary<ITopDownDamageable, float> outboundHitTimes = new Dictionary<ITopDownDamageable, float>();
    private readonly RaycastHit[] hitBuffer = new RaycastHit[24];

    private EnemyShieldProjectileLaunchData data;
    private Action<EnemyShieldProjectile> onFinished;

    private EnemyShieldProjectilePhase phase;
    private Vector3 currentDirection;
    private Vector3 lastPosition;
    private float traveledDistance;
    private float lifetime;
    private bool launched;

    public EnemyShieldProjectilePhase Phase => phase;

    public void Launch(
        EnemyShieldProjectileLaunchData launchData,
        Action<EnemyShieldProjectile> finishedCallback)
    {
        data = launchData;
        onFinished = finishedCallback;

        damagedOutbound.Clear();
        damagedReturning.Clear();
        impactedTargets.Clear();
        outboundHitTimes.Clear();

        transform.position = data.Origin;

        currentDirection = data.Direction;
        currentDirection.y = 0f;

        if (currentDirection.sqrMagnitude <= 0.0001f)
        {
            currentDirection = transform.forward;
        }

        currentDirection.Normalize();

        transform.rotation = Quaternion.LookRotation(currentDirection, Vector3.up);

        lastPosition = transform.position;
        traveledDistance = 0f;
        lifetime = 0f;
        phase = EnemyShieldProjectilePhase.Outbound;
        launched = true;
    }

    private void Update()
    {
        if (!launched || phase == EnemyShieldProjectilePhase.Finished)
        {
            return;
        }

        lifetime += Time.deltaTime;
        if (lifetime >= data.MaxLifetime)
        {
            Finish();
            return;
        }

        SpinVisual();

        switch (phase)
        {
            case EnemyShieldProjectilePhase.Outbound:
                UpdateOutbound();
                break;

            case EnemyShieldProjectilePhase.Returning:
                UpdateReturning();
                break;
        }
    }

    private void UpdateOutbound()
    {
        Vector3 previous = transform.position;
        Vector3 next = previous + currentDirection * data.OutboundSpeed * Time.deltaTime;

        if (CheckTravel(previous, next, EnemyShieldProjectilePhase.Outbound))
        {
            return;
        }

        MoveTo(next);

        traveledDistance += Vector3.Distance(previous, next);
        if (traveledDistance >= data.MaxOutboundDistance)
        {
            BeginReturn();
        }
    }

    private void UpdateReturning()
    {
        if (data.ReturnAnchor == null)
        {
            Finish();
            return;
        }

        Vector3 returnTarget = data.ReturnAnchor.position;
        Vector3 toAnchor = returnTarget - transform.position;
        toAnchor.y = 0f;

        if (toAnchor.sqrMagnitude <= data.ReturnArrivalDistance * data.ReturnArrivalDistance)
        {
            Finish();
            return;
        }

        currentDirection = toAnchor.normalized;

        Vector3 previous = transform.position;
        Vector3 next = previous + currentDirection * data.ReturnSpeed * Time.deltaTime;

        if (CheckTravel(previous, next, EnemyShieldProjectilePhase.Returning))
        {
            return;
        }

        MoveTo(next);
    }

    /// <summary>
    /// Revisa el tramo de movimiento del frame.
    ///
    /// Cambio importante:
    /// - Un target que todavia esta en gracia de re-hit de retorno se ignora para este frame.
    /// - Esto evita que el escudo quede clavado encima del Player hasta que venza la gracia y aplique doble hit instantaneo.
    /// - Los obstaculos siguen bloqueando normalmente.
    /// </summary>
    private bool CheckTravel(
        Vector3 previous,
        Vector3 next,
        EnemyShieldProjectilePhase currentPhase)
    {
        Vector3 delta = next - previous;
        float distance = delta.magnitude;

        if (distance <= 0.0001f)
        {
            return false;
        }

        Vector3 direction = delta / distance;
        int mask = data.TargetLayers.value | data.BlockingLayers.value;

        int hitCount = Physics.SphereCastNonAlloc(
            previous,
            data.HitRadius,
            direction,
            hitBuffer,
            distance,
            mask,
            QueryTriggerInteraction.Ignore);

        if (hitCount <= 0)
        {
            return false;
        }

        RaycastHit? nearestBlockingHit = null;
        RaycastHit? nearestEligibleTargetHit = null;
        ITopDownDamageable nearestEligibleDamageable = null;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hitBuffer[i];
            Collider hitCollider = hit.collider;

            if (hitCollider == null || IsOwnerOrChild(hitCollider))
            {
                continue;
            }

            bool isBlocking = TMJ_DamageUtility.IsInLayerMask(hitCollider.gameObject.layer, data.BlockingLayers);
            bool isTarget = TMJ_DamageUtility.IsInLayerMask(hitCollider.gameObject.layer, data.TargetLayers);

            if (isBlocking)
            {
                if (!nearestBlockingHit.HasValue || hit.distance < nearestBlockingHit.Value.distance)
                {
                    nearestBlockingHit = hit;
                }

                continue;
            }

            if (!isTarget)
            {
                continue;
            }

            ITopDownDamageable candidateDamageable;
            if (!CanDamageTargetInCurrentPhase(hitCollider, currentPhase, out candidateDamageable))
            {
                continue;
            }

            if (!nearestEligibleTargetHit.HasValue || hit.distance < nearestEligibleTargetHit.Value.distance)
            {
                nearestEligibleTargetHit = hit;
                nearestEligibleDamageable = candidateDamageable;
            }
        }

        if (!nearestBlockingHit.HasValue && !nearestEligibleTargetHit.HasValue)
        {
            return false;
        }

        bool blockComesFirst =
            nearestBlockingHit.HasValue &&
            (!nearestEligibleTargetHit.HasValue || nearestBlockingHit.Value.distance <= nearestEligibleTargetHit.Value.distance);

        if (blockComesFirst)
        {
            MoveTo(previous + direction * nearestBlockingHit.Value.distance);

            if (data.SnapBackOnBlockingHit)
            {
                Finish();
            }
            else
            {
                BeginReturn();
            }

            return true;
        }

        RaycastHit targetHit = nearestEligibleTargetHit.Value;
        MoveTo(previous + direction * targetHit.distance);
        DamageTarget(targetHit.collider, nearestEligibleDamageable, currentPhase);

        if (currentPhase == EnemyShieldProjectilePhase.Outbound)
        {
            BeginReturn();
        }

        return true;
    }

    private bool CanDamageTargetInCurrentPhase(
        Collider targetCollider,
        EnemyShieldProjectilePhase currentPhase,
        out ITopDownDamageable damageable)
    {
        damageable = null;

        if (targetCollider == null)
        {
            return false;
        }

        if (!TryGetDamageable(targetCollider, out damageable) || damageable == null)
        {
            return false;
        }

        if (currentPhase == EnemyShieldProjectilePhase.Outbound)
        {
            return !damagedOutbound.Contains(damageable);
        }

        if (currentPhase == EnemyShieldProjectilePhase.Returning)
        {
            if (damagedReturning.Contains(damageable))
            {
                return false;
            }

            if (!data.CanHitSameTargetOnReturn && damagedOutbound.Contains(damageable))
            {
                return false;
            }

            if (data.SameTargetReturnHitGraceTime > 0f &&
                outboundHitTimes.TryGetValue(damageable, out float outboundHitTime) &&
                Time.time - outboundHitTime < data.SameTargetReturnHitGraceTime)
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private void DamageTarget(
        Collider targetCollider,
        ITopDownDamageable expectedDamageable,
        EnemyShieldProjectilePhase currentPhase)
    {
        if (targetCollider == null || expectedDamageable == null)
        {
            return;
        }

        float damage = currentPhase == EnemyShieldProjectilePhase.Outbound
            ? data.OutboundDamage
            : data.ReturnDamage;

        if (damage <= 0f)
        {
            return;
        }

        HashSet<ITopDownDamageable> currentPhaseSet = currentPhase == EnemyShieldProjectilePhase.Outbound
            ? damagedOutbound
            : damagedReturning;

        Vector3 hitPoint = TMJ_DamageUtility.GetSafeClosestPoint(targetCollider, transform.position);
        Vector3 targetPosition = TMJ_DamageUtility.GetTargetReferencePosition(targetCollider);
        Vector3 directionFromSourceToTarget = targetPosition - transform.position;
        directionFromSourceToTarget.y = 0f;

        TMJ_DamageInfo damageInfo = new TMJ_DamageInfo(
            damage,
            transform.position,
            data.Instigator,
            gameObject,
            hitPoint,
            directionFromSourceToTarget);

        bool damaged = TMJ_DamageUtility.TryDamageCollider(
            targetCollider,
            damageInfo,
            data.TargetLayers,
            data.Owner,
            currentPhaseSet,
            out ITopDownDamageable actualDamageable);

        if (!damaged || actualDamageable == null)
        {
            return;
        }

        if (currentPhase == EnemyShieldProjectilePhase.Outbound)
        {
            outboundHitTimes[actualDamageable] = Time.time;
        }

        if (data.ApplyImpact && ShouldApplyImpactTo(actualDamageable))
        {
            ApplyImpact(targetCollider, actualDamageable);
        }
    }

    private bool ShouldApplyImpactTo(ITopDownDamageable damageable)
    {
        if (damageable == null)
        {
            return false;
        }

        if (!data.ApplyImpactOnlyOncePerTargetPerThrow)
        {
            return true;
        }

        if (impactedTargets.Contains(damageable))
        {
            return false;
        }

        impactedTargets.Add(damageable);
        return true;
    }

    private void ApplyImpact(Collider targetCollider, ITopDownDamageable damageable)
    {
        IImpactReceiver receiver = FindImpactReceiver(targetCollider);
        if (receiver == null)
        {
            return;
        }

        Vector3 direction = currentDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
        }

        ImpactInfo impactInfo = new ImpactInfo(
            data.Instigator,
            transform.position,
            direction.normalized,
            data.KnockbackDistance,
            data.KnockbackDuration,
            data.StunDuration,
            data.InterruptCurrentAction);

        receiver.ReceiveImpact(impactInfo);
    }

    private void BeginReturn()
    {
        if (phase == EnemyShieldProjectilePhase.Returning)
        {
            return;
        }

        phase = EnemyShieldProjectilePhase.Returning;
    }

    private void Finish()
    {
        if (phase == EnemyShieldProjectilePhase.Finished)
        {
            return;
        }

        phase = EnemyShieldProjectilePhase.Finished;
        launched = false;

        Action<EnemyShieldProjectile> callback = onFinished;
        onFinished = null;

        callback?.Invoke(this);

        Destroy(gameObject);
    }

    private void MoveTo(Vector3 position)
    {
        lastPosition = transform.position;
        transform.position = position;

        Vector3 facing = transform.position - lastPosition;
        facing.y = 0f;

        if (facing.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
        }
    }

    private void SpinVisual()
    {
        if (visualSpinRoot == null || spinDegreesPerSecond <= 0f)
        {
            return;
        }

        Vector3 axis = spinAxis.sqrMagnitude > 0.0001f ? spinAxis.normalized : Vector3.forward;
        visualSpinRoot.Rotate(axis, spinDegreesPerSecond * Time.deltaTime, Space.Self);
    }

    private bool TryGetDamageable(Collider targetCollider, out ITopDownDamageable damageable)
    {
        damageable = null;

        if (targetCollider == null)
        {
            return false;
        }

        MonoBehaviour[] behaviours = targetCollider.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ITopDownDamageable found)
            {
                damageable = found;
                return true;
            }
        }

        return false;
    }

    private bool IsOwnerOrChild(Collider targetCollider)
    {
        if (data.Owner == null || targetCollider == null)
        {
            return false;
        }

        Transform ownerTransform = data.Owner.transform;
        Transform targetTransform = targetCollider.attachedRigidbody != null
            ? targetCollider.attachedRigidbody.transform
            : targetCollider.transform;

        if (ownerTransform == null || targetTransform == null)
        {
            return false;
        }

        return targetTransform == ownerTransform
            || targetTransform.IsChildOf(ownerTransform)
            || ownerTransform.IsChildOf(targetTransform);
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

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        Gizmos.color = phase == EnemyShieldProjectilePhase.Returning
            ? Color.cyan
            : Color.yellow;

        Gizmos.DrawWireSphere(transform.position, data.HitRadius > 0f ? data.HitRadius : 0.35f);
    }
}
