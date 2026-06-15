using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyProjectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Rigidbody rb;

    [Header("Movement")]
    [SerializeField]
    private bool useRigidbodyVelocity = false;

    [Header("Collision")]
    [SerializeField]
    private bool deactivateOnTargetHit = true;

    [SerializeField]
    private bool deactivateOnBlockingHit = true;

    [Header("Debug")]
    [SerializeField]
    private bool logHits;

    private readonly HashSet<ITopDownDamageable> processedTargets = new HashSet<ITopDownDamageable>();
    private EnemyProjectileLaunchData launchData;
    private Coroutine lifetimeRoutine;
    private bool launched;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void OnDisable()
    {
        StopLifetimeRoutine();
        processedTargets.Clear();
        launched = false;

       if (rb != null && !rb.isKinematic)
       {
           rb.linearVelocity = Vector3.zero;
           rb.angularVelocity = Vector3.zero;
       }
    }

    private void Update()
    {
        if (!launched || CanUseRigidbodyVelocity())
        {
            return;
        }

        transform.position += launchData.Direction * launchData.Speed * Time.deltaTime;
    }

    public void Launch(EnemyProjectileLaunchData data)
    {
        launchData = data;
        launched = true;
        processedTargets.Clear();

        transform.position = data.Origin;
        transform.rotation = Quaternion.LookRotation(data.Direction, Vector3.up);

        if (CanUseRigidbodyVelocity())
        {
            rb.linearVelocity = data.Direction * data.Speed;
        }

        StopLifetimeRoutine();
        lifetimeRoutine = StartCoroutine(LifetimeRoutine(data.Lifetime));
    }

    private IEnumerator LifetimeRoutine(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollider(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
        {
            return;
        }

        HandleCollider(collision.collider);
    }

    private void HandleCollider(Collider other)
    {
        if (!launched || other == null)
        {
            return;
        }

        if (IsOwnerOrChild(other))
        {
            return;
        }

        bool isTarget = TMJ_DamageUtility.IsInLayerMask(other.gameObject.layer, launchData.TargetLayers);
        bool isBlocking = TMJ_DamageUtility.IsInLayerMask(other.gameObject.layer, launchData.BlockingLayers);

        if (isTarget)
        {
            TryDamageTarget(other);

            if (deactivateOnTargetHit)
            {
                Despawn();
            }

            return;
        }

        if (isBlocking && deactivateOnBlockingHit)
        {
            if (logHits)
            {
                Debug.Log($"[{nameof(EnemyProjectile)}] {name} blocked by {other.name}.", this);
            }

            Despawn();
        }
    }

    private void TryDamageTarget(Collider targetCollider)
    {
        Vector3 sourcePosition = transform.position;
        TopDownHealth targetHealth = targetCollider.GetComponentInParent<TopDownHealth>();

        bool damaged = TMJ_DamageUtility.TryDamageCollider(
            targetCollider,
            launchData.Damage,
            sourcePosition,
            launchData.Owner,
            launchData.TargetLayers,
            launchData.Owner,
            processedTargets,
            out ITopDownDamageable damageable);

        if (!damaged)
        {
            return;
        }

        if (logHits)
        {
            Debug.Log($"[{nameof(EnemyProjectile)}] {name} damaged {targetCollider.name} for {launchData.Damage}.", this);
        }

        bool targetStillAlive = targetHealth == null || targetHealth.IsAlive;
        if (!targetStillAlive || !launchData.ApplyImpact)
        {
            return;
        }

        IImpactReceiver receiver = FindImpactReceiver(targetCollider);
        if (receiver == null)
        {
            return;
        }

        Vector3 direction = targetCollider.transform.position - sourcePosition;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = launchData.Direction;
        }

        ImpactInfo impactInfo = new ImpactInfo(
            launchData.Owner,
            sourcePosition,
            direction.normalized,
            launchData.KnockbackDistance,
            launchData.KnockbackDuration,
            launchData.StunDuration,
            launchData.InterruptCurrentAction);

        receiver.ReceiveImpact(impactInfo);
    }

    private bool IsOwnerOrChild(Collider targetCollider)
    {
        if (launchData.Owner == null || targetCollider == null)
        {
            return false;
        }

        Transform ownerTransform = launchData.Owner.transform;
        Transform targetTransform = targetCollider.attachedRigidbody != null
            ? targetCollider.attachedRigidbody.transform
            : targetCollider.transform;

        return targetTransform == ownerTransform
            || targetTransform.IsChildOf(ownerTransform)
            || ownerTransform.IsChildOf(targetTransform);
    }

    private static IImpactReceiver FindImpactReceiver(Collider targetCollider)
    {
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

    private void StopLifetimeRoutine()
    {
        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }
    }

    private void Despawn()
    {
        StopLifetimeRoutine();
        Destroy(gameObject);
    }
    private bool CanUseRigidbodyVelocity()
    {
        return useRigidbodyVelocity && rb != null && !rb.isKinematic;
    }
}
