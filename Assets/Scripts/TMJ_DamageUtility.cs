using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsabilidad:
/// - filtrar por layer;
/// - evitar self/owner damage;
/// - resolver ITopDownDamageable desde colliders hijos;
/// - crear TMJ_DamageInfo;
/// - aplicar daño.
///
/// No decide animaciones, FSM, stun, knockback, fases del boss ni cooldowns !!!
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
        damageable = null;

        if (damage <= 0f)
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

        damageable.TakeDamage(new TMJ_DamageInfo(damage, sourcePosition, source));
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

    static bool IsOwnerOrChild(Collider targetCollider, GameObject owner)
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
