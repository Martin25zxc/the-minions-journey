using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hazard temporal que orbita alrededor de un anchor y daña por contacto lógico.
/// No usa IA, no bloquea físicamente y se destruye solo.
/// </summary>
public sealed class EnemyOrbitingOrbHazard : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos;

    private readonly Dictionary<ITopDownDamageable, float> nextDamageByTarget = new Dictionary<ITopDownDamageable, float>();
    private readonly Collider[] hitBuffer = new Collider[16];

    private Transform anchor;
    private GameObject owner;
    private float radius;
    private float degreesPerSecond;
    private float currentAngle;
    private float duration;
    private float heightOffset;
    private float hitRadius;
    private float damage;
    private float damageCooldown;
    private LayerMask targetLayers;
    private bool applyImpact;
    private float knockbackDistance;
    private float knockbackDuration;
    private float stunDuration;
    private bool interruptCurrentAction;
    private float despawnAt;
    private bool initialized;

    public void Initialize(
        Transform newAnchor,
        GameObject newOwner,
        float startAngle,
        float orbitRadius,
        float orbitDegreesPerSecond,
        float lifeDuration,
        float newHeightOffset,
        float newHitRadius,
        float newDamage,
        float newDamageCooldown,
        LayerMask newTargetLayers,
        bool newApplyImpact,
        float newKnockbackDistance,
        float newKnockbackDuration,
        float newStunDuration,
        bool newInterruptCurrentAction)
    {
        anchor = newAnchor;
        owner = newOwner;
        currentAngle = startAngle;
        radius = Mathf.Max(0.1f, orbitRadius);
        degreesPerSecond = orbitDegreesPerSecond;
        duration = Mathf.Max(0.05f, lifeDuration);
        heightOffset = Mathf.Max(0f, newHeightOffset);
        hitRadius = Mathf.Max(0.05f, newHitRadius);
        damage = Mathf.Max(0f, newDamage);
        damageCooldown = Mathf.Max(0f, newDamageCooldown);
        targetLayers = newTargetLayers;
        applyImpact = newApplyImpact;
        knockbackDistance = Mathf.Max(0f, newKnockbackDistance);
        knockbackDuration = Mathf.Max(0f, newKnockbackDuration);
        stunDuration = Mathf.Max(0f, newStunDuration);
        interruptCurrentAction = newInterruptCurrentAction;
        despawnAt = Time.time + duration;
        nextDamageByTarget.Clear();
        initialized = true;
        UpdatePosition();
    }

    private void Update()
    {
        if (!initialized || anchor == null || owner == null)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time >= despawnAt)
        {
            Destroy(gameObject);
            return;
        }

        currentAngle += degreesPerSecond * Time.deltaTime;
        UpdatePosition();
        DamageOverlaps();
    }

    private void UpdatePosition()
    {
        Vector3 offset = Quaternion.Euler(0f, currentAngle, 0f) * Vector3.forward * radius;
        transform.position = anchor.position + offset + Vector3.up * heightOffset;
    }

    private void DamageOverlaps()
    {
        if (damage <= 0f)
        {
            return;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            hitRadius,
            hitBuffer,
            targetLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider targetCollider = hitBuffer[i];
            if (targetCollider == null)
            {
                continue;
            }

            if (!TMJ_DamageUtility.TryGetDamageable(targetCollider, targetLayers, owner, out ITopDownDamageable damageable) || damageable == null)
            {
                continue;
            }

            if (nextDamageByTarget.TryGetValue(damageable, out float nextTime) && Time.time < nextTime)
            {
                continue;
            }

            Vector3 targetPosition = TMJ_DamageUtility.GetTargetReferencePosition(targetCollider);
            Vector3 directionFromSourceToTarget = targetPosition - transform.position;
            directionFromSourceToTarget.y = 0f;

            TMJ_DamageInfo damageInfo = new TMJ_DamageInfo(
                damage,
                transform.position,
                owner,
                gameObject,
                TMJ_DamageUtility.GetSafeClosestPoint(targetCollider, transform.position),
                directionFromSourceToTarget);

            damageable.TakeDamage(damageInfo);
            nextDamageByTarget[damageable] = Time.time + damageCooldown;

            if (applyImpact)
            {
                ApplyImpact(targetCollider, directionFromSourceToTarget);
            }
        }
    }

    private void ApplyImpact(Collider targetCollider, Vector3 fallbackDirection)
    {
        IImpactReceiver receiver = FindImpactReceiver(targetCollider);
        if (receiver == null)
        {
            return;
        }

        Vector3 direction = fallbackDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
        }

        ImpactInfo impactInfo = new ImpactInfo(
            owner,
            transform.position,
            direction.normalized,
            knockbackDistance,
            knockbackDuration,
            stunDuration,
            interruptCurrentAction);

        receiver.ReceiveImpact(impactInfo);
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

        Gizmos.DrawWireSphere(transform.position, hitRadius > 0f ? hitRadius : 0.35f);
    }
}
