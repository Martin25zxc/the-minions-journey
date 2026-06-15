using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsabilidad:
/// - filtrar por layer;
/// - evitar self/owner damage;
/// - resolver ITopDownDamageable desde colliders hijos;
/// - aplicar TMJ_DamageInfo.
///
/// No decide animaciones, FSM, stun, knockback, fases del boss ni cooldowns.
/// </summary>
public static class TMJ_DamageUtility
{
    public static bool TryDamageCollider(
        Collider targetCollider,
        float damage,
        Vector3 sourcePosition,
        GameObject source,
        LayerMask targetLayers,
        GameObject owner = null,
        ICollection<ITopDownDamageable> processedTargets = null)
    {
        return TryDamageCollider(
            targetCollider,
            damage,
            sourcePosition,
            source,
            targetLayers,
            owner,
            processedTargets,
            out _);
    }

    public static bool TryDamageCollider(
        Collider targetCollider,
        float damage,
        Vector3 sourcePosition,
        GameObject source,
        LayerMask targetLayers,
        GameObject owner,
        ICollection<ITopDownDamageable> processedTargets,
        out ITopDownDamageable damageable)
    {
        TMJ_DamageInfo damageInfo = BuildLegacyDamageInfo(targetCollider, damage, sourcePosition, source);
        return TryDamageCollider(targetCollider, damageInfo, targetLayers, owner, processedTargets, out damageable);
    }

    public static bool TryDamageCollider(
        Collider targetCollider,
        TMJ_DamageInfo damageInfo,
        LayerMask targetLayers,
        GameObject owner = null,
        ICollection<ITopDownDamageable> processedTargets = null)
    {
        return TryDamageCollider(targetCollider, damageInfo, targetLayers, owner, processedTargets, out _);
    }

    public static bool TryDamageCollider(
        Collider targetCollider,
        TMJ_DamageInfo damageInfo,
        LayerMask targetLayers,
        GameObject owner,
        ICollection<ITopDownDamageable> processedTargets,
        out ITopDownDamageable damageable)
    {
        damageable = null;

        if (damageInfo.Damage <= 0f)
        {
            return false;
        }

        if (!TryGetDamageable(targetCollider, targetLayers, owner, out damageable))
        {
            return false;
        }

        if (processedTargets != null)
        {
            if (processedTargets.Contains(damageable))
            {
                return false;
            }

            processedTargets.Add(damageable);
        }

        damageable.TakeDamage(damageInfo);
        return true;
    }

    public static bool TryGetDamageable(
        Collider targetCollider,
        LayerMask targetLayers,
        GameObject owner,
        out ITopDownDamageable damageable)
    {
        damageable = null;

        if (targetCollider == null)
        {
            return false;
        }

        if (!IsInLayerMask(targetCollider.gameObject.layer, targetLayers))
        {
            return false;
        }

        if (IsOwnerOrChild(targetCollider, owner))
        {
            return false;
        }

        MonoBehaviour[] behaviours = targetCollider.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ITopDownDamageable foundDamageable)
            {
                damageable = foundDamageable;
                return true;
            }
        }

        return false;
    }

    public static bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    public static Vector3 GetTargetReferencePosition(Collider targetCollider)
    {
        if (targetCollider == null)
        {
            return Vector3.zero;
        }

        if (targetCollider.attachedRigidbody != null)
        {
            return targetCollider.attachedRigidbody.position;
        }

        return targetCollider.bounds.center;
    }

    public static Vector3 GetSafeClosestPoint(Collider targetCollider, Vector3 originPosition)
    {
        if (targetCollider == null)
        {
            return originPosition;
        }

        return targetCollider.ClosestPoint(originPosition);
    }

    private static TMJ_DamageInfo BuildLegacyDamageInfo(
        Collider targetCollider,
        float damage,
        Vector3 sourcePosition,
        GameObject source)
    {
        Vector3 targetPosition = targetCollider != null
            ? GetTargetReferencePosition(targetCollider)
            : sourcePosition;

        Vector3 direction = targetPosition - sourcePosition;
        direction.y = 0f;

        Vector3? directionFromSourceToTarget = direction.sqrMagnitude > 0.0001f
            ? (Vector3?)direction.normalized
            : null;

        return new TMJ_DamageInfo(
            damage,
            sourcePosition,
            source,
            source,
            targetCollider != null ? (Vector3?)GetSafeClosestPoint(targetCollider, sourcePosition) : null,
            directionFromSourceToTarget);
    }

    private static bool IsOwnerOrChild(Collider targetCollider, GameObject owner)
    {
        if (owner == null || targetCollider == null)
        {
            return false;
        }

        Transform ownerTransform = owner.transform;
        Transform targetTransform = targetCollider.attachedRigidbody != null
            ? targetCollider.attachedRigidbody.transform
            : targetCollider.transform;

        if (targetTransform == null || ownerTransform == null)
        {
            return false;
        }

        return targetTransform == ownerTransform
            || targetTransform.IsChildOf(ownerTransform)
            || ownerTransform.IsChildOf(targetTransform);
    }
}
