using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Capa de ejecución del minion: movimiento, salud, eventos.
/// NO decide qué hacer — eso es responsabilidad del FSM.
/// </summary>
public class MinionController : MonoBehaviour
{
    // ── Eventos ───────────────────────────────────────────────────────────
    public event Action<float> OnHealthChanged;  // porcentaje 0-1
    public event Action        OnDeath;

    // ── Estado público ────────────────────────────────────────────────────
    public bool IsAlive  => currentHealth > 0f;
    public bool IsMoving => moveRoutine != null;

    // ── Inspector ─────────────────────────────────────────────────────────
    [SerializeField] private float moveSpeed    = 3.5f;
    [SerializeField] private float rotateSpeed  = 10f;

    // ── Runtime ───────────────────────────────────────────────────────────
    private float     currentHealth;
    private float     maxHealth;
    private Coroutine moveRoutine;

    // ─────────────────────────────────────────────────────────────────────
    //  Init
    // ─────────────────────────────────────────────────────────────────────
    public void Initialize(MinionDataSO data)
    {
        maxHealth     = data.maxHealth;
        currentHealth = maxHealth;
        moveSpeed     = data.moveSpeed;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Movimiento
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Persigue un Transform continuamente hasta que se llame StopMoving().</summary>
    public void StartChasing(Transform target)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(ChaseRoutine(target));
    }

    public void StopMoving()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
    }

    /// <summary>Rota hacia el objetivo de forma instantánea (útil al iniciar un ataque).</summary>
    public void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    private IEnumerator ChaseRoutine(Transform target)
    {
        while (target != null)
        {
            Vector3 dest = target.position;
            dest.y = transform.position.y;

            Vector3 dir = dest - transform.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir.normalized),
                    rotateSpeed * Time.deltaTime);

                transform.position = Vector3.MoveTowards(
                    transform.position, dest, moveSpeed * Time.deltaTime);
            }
            yield return null;
        }
        moveRoutine = null;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Daño / muerte
    // ─────────────────────────────────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        StopAllCoroutines();
        OnDeath?.Invoke();
        gameObject.SetActive(false);
    }
}
