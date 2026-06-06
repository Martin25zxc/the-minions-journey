using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Vive en el prefab del jefe.
/// Expone GoTo() y JumpTo() con evento OnArrived, y los métodos de activación de cada ataque.
/// NO decide qué ataque hacer — eso es el FSM del manager.
/// </summary>
public class BossController : MonoBehaviour
{
    // ── Eventos ───────────────────────────────────────────────────────────
    public event Action        OnArrived;
    public event Action<float> OnHealthChanged;
    public event Action        OnDeath;

    // ── Estado público ────────────────────────────────────────────────────
    public bool IsAlive  => currentHealth > 0f;
    public bool IsMoving => moveRoutine != null;

    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float epsilon   = 0.05f;

    // ── Runtime ───────────────────────────────────────────────────────────
    private float     currentHealth;
    private float     maxHealth;
    private Coroutine moveRoutine;

    // ── Animator ──────────────────────────────────────────────────────────
    private Animator animator;
    private bool     isRunning = false;

    // ─────────────────────────────────────────────────────────────────────
    //  Awake
    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogError("[BossController] No Animator found on the boss prefab.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Init
    // ─────────────────────────────────────────────────────────────────────
    public void Initialize(BossDataSO data)
    {
        if (data == null) { Debug.LogError("[BossController] BossDataSO null."); return; }
        maxHealth     = data.maxHealth;
        currentHealth = maxHealth;
        
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Movimiento — el FSM espera OnArrived
    // ─────────────────────────────────────────────────────────────────────
    public void GoTo(Vector3 worldPos)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveRoutine(worldPos));
    }

    public void JumpTo(Vector3 worldPos, float height, float duration)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(JumpRoutine(worldPos, height, duration));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Rutinas de movimiento
    // ─────────────────────────────────────────────────────────────────────
    private IEnumerator MoveRoutine(Vector3 target)
    {
        target.y = transform.position.y;

        // Esperar un frame para asegurarse que Awake terminó
        yield return null;

        if (animator != null)
        {
            isRunning = true;
            animator.SetBool("IsRunning", isRunning);
        }

        while (Vector3.Distance(transform.position, target) > epsilon)
        {
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

        if (animator != null)
        {
            isRunning = false;
            animator.SetBool("IsRunning", isRunning);
        }

        OnArrived?.Invoke();
    }

    private IEnumerator JumpRoutine(Vector3 target, float height, float duration)
    {
        target.y = transform.position.y;
        Vector3 startPos = transform.position;
        float   elapsed  = 0f;

        // Rotar hacia el objetivo antes de saltar
        Vector3 dir = (target - startPos).normalized;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);

        while (elapsed < duration)
        {
            float   t          = elapsed / duration;
            Vector3 horizontal = Vector3.Lerp(startPos, target, t);
            float   arc        = height * 4f * t * (1f - t);
            transform.position = new Vector3(horizontal.x, startPos.y + arc, horizontal.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
        moveRoutine = null;
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