using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct EnemyProjectileLaunchData
{
    public EnemyProjectileLaunchData(
        GameObject owner,
        Vector3 origin,
        Vector3 direction,
        float speed,
        float damage,
        float lifetime,
        LayerMask targetLayers,
        LayerMask blockingLayers,
        bool applyImpact,
        float knockbackDistance,
        float knockbackDuration,
        float stunDuration,
        bool interruptCurrentAction)
    {
        Owner = owner;
        Origin = origin;
        Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        Speed = Mathf.Max(0.01f, speed);
        Damage = Mathf.Max(0f, damage);
        Lifetime = Mathf.Max(0.05f, lifetime);
        TargetLayers = targetLayers;
        BlockingLayers = blockingLayers;
        ApplyImpact = applyImpact;
        KnockbackDistance = Mathf.Max(0f, knockbackDistance);
        KnockbackDuration = Mathf.Max(0f, knockbackDuration);
        StunDuration = Mathf.Max(0f, stunDuration);
        InterruptCurrentAction = interruptCurrentAction;
    }

    public GameObject Owner { get; }
    public Vector3 Origin { get; }
    public Vector3 Direction { get; }
    public float Speed { get; }
    public float Damage { get; }
    public float Lifetime { get; }
    public LayerMask TargetLayers { get; }
    public LayerMask BlockingLayers { get; }
    public bool ApplyImpact { get; }
    public float KnockbackDistance { get; }
    public float KnockbackDuration { get; }
    public float StunDuration { get; }
    public bool InterruptCurrentAction { get; }
}