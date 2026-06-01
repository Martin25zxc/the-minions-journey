using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Activa una hitbox por una duración fija y aplica daño al jugador.
/// Dispara OnAttackEnded cuando termina — el FSM escucha este evento.
/// </summary>
public class MinionMeleeAttack : MonoBehaviour
{
    public event Action OnAttackEnded;

    [SerializeField] private Collider attackHitbox;

    private float     damage;
    private float     duration;
    private Coroutine attackRoutine;

    public void Initialize(float damageAmount, float attackDuration)
    {
        damage   = damageAmount;
        duration = attackDuration;
        if (attackHitbox != null) attackHitbox.enabled = false;
    }

    public void StartAttack()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        if (attackHitbox != null) attackHitbox.enabled = true;
        yield return new WaitForSeconds(duration);
        if (attackHitbox != null) attackHitbox.enabled = false;
        attackRoutine = null;
        OnAttackEnded?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        var ph = other.GetComponent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(damage);
    }
}
