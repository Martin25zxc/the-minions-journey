using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Activa una hitbox por una duración fija y aplica daño usando el flujo común:
/// Collider -> TMJ_DamageUtility -> ITopDownDamageable -> TopDownHealth.
/// Dispara OnAttackEnded cuando termina; el FSM escucha este evento.
/// </summary>
public class MinionMeleeAttack : MonoBehaviour
{
    public event Action OnAttackEnded;

    [Header("Hitbox")]
    [SerializeField]
    Collider attackHitbox;

    [SerializeField]
    LayerMask targetLayers;

    [SerializeField]
    GameObject damageOwner;

    float damage;
    float duration;
    Coroutine attackRoutine;
    readonly HashSet<ITopDownDamageable> damagedThisSwing = new HashSet<ITopDownDamageable>();

    void Awake()
    {
        if (damageOwner == null)
        {
            MinionController minion = GetComponentInParent<MinionController>();
            damageOwner = minion != null ? minion.gameObject : transform.root.gameObject;
        }

        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }
    }

    void OnDisable()
    {
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }

        damagedThisSwing.Clear();
    }

    public void Initialize(float damageAmount, float attackDuration)
    {
        damage = damageAmount;
        duration = attackDuration;

        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }
    }

    public void StartAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        damagedThisSwing.Clear();

        if (attackHitbox != null)
        {
            attackHitbox.enabled = true;
        }

        yield return new WaitForSeconds(duration);

        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }

        damagedThisSwing.Clear();
        attackRoutine = null;
        OnAttackEnded?.Invoke();
    }

    void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    void TryDamage(Collider other)
    {
        TMJ_DamageUtility.TryDamageCollider(
            other,
            damage,
            transform.position,
            gameObject,
            targetLayers,
            damageOwner,
            damagedThisSwing);
    }
}
