using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Vive en el prefab del jefe.
/// Expone GoTo() con evento OnArrived, y los métodos de activación de cada ataque.
/// NO decide qué ataque hacer — eso es el FSM del manager.
/// </summary>
public class BossController : MonoBehaviour
{
    // ── Eventos ───────────────────────────────────────────────────────────
    public event Action        OnArrived;          // llegó al destino de GoTo
    public event Action<float> OnHealthChanged;    // 0-1
    public event Action        OnDeath;

    // ── Estado público ────────────────────────────────────────────────────
    public bool IsAlive   => currentHealth > 0f;
    public bool IsMoving  => moveRoutine != null;

    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed  = 4f;
    [SerializeField] private float epsilon    = 0.05f;

    // ── Runtime ───────────────────────────────────────────────────────────
    private float     currentHealth;
    private float     maxHealth;
    private Coroutine moveRoutine;

    // ─────────────────────────────────────────────────────────────────────
    //  Init
    // ─────────────────────────────────────────────────────────────────────
    public void Initialize(BossDataSO data)
    {
        if (data == null) { Debug.LogError("[BossController] BossDataSO null."); return; }
        maxHealth     = data.maxHealth;
        currentHealth = maxHealth;
        moveSpeed     = data.moveSpeed;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Movimiento — el FSM espera OnArrived
    // ─────────────────────────────────────────────────────────────────────
    public void GoTo(Vector3 worldPos)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveRoutine(worldPos));
    }

    private IEnumerator MoveRoutine(Vector3 target)
    {
        target.y = transform.position.y;

        while (Vector3.Distance(transform.position, target) > epsilon)
        {
            // Rotar hacia el destino
            Vector3 dir = (target - transform.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

            transform.position = Vector3.MoveTowards(
                transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = target;
        moveRoutine = null;
        //Debug.Log("[BossController] Arrived at destination.");
        OnArrived?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Rotación en Y (usada por los ataques de giro)
    // ─────────────────────────────────────────────────────────────────────
    public void RotateY(float degreesPerSecond)
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
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