using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Capa de ejecución del minion: movimiento, salud y eventos.
/// NO decide qué hacer — eso es responsabilidad del FSM.
/// La vida real vive en TopDownHealth.
/// </summary>
[RequireComponent(typeof(TopDownHealth))]
public class MinionController : MonoBehaviour
{
    public event Action<float> OnHealthChanged;  // porcentaje 0-1
    public event Action OnDeath;

    public bool IsAlive => health != null && health.IsAlive;
    public bool IsMoving => moveRoutine != null;

    [SerializeField]
    TopDownHealth health;

    [Header("Movimiento")]
    [SerializeField]
    float moveSpeed = 3.5f;

    [SerializeField]
    float rotateSpeed = 10f;

    Coroutine moveRoutine;

    void Awake()
    {
        if (health == null)
        {
            health = GetComponent<TopDownHealth>();
        }

        if (health == null)
        {
            Debug.LogError("[MinionController] TopDownHealth no encontrado.");
            return;
        }

        health.OnHealthChanged += HandleHealthChanged;
        health.OnDied += HandleDeath;
    }

    void OnDestroy()
    {
        if (health == null)
        {
            return;
        }

        health.OnHealthChanged -= HandleHealthChanged;
        health.OnDied -= HandleDeath;
    }

    public void Initialize(MinionDataSO data)
    {
        if (data == null)
        {
            Debug.LogError("[MinionController] MinionDataSO null.");
            return;
        }

        health?.Initialize(data.maxHealth);
        moveSpeed = data.moveSpeed;
    }

    public void StartChasing(Transform target)
    {
        if (!IsAlive)
        {
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

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
        {
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }
    }

    IEnumerator ChaseRoutine(Transform target)
    {
        while (target != null && IsAlive)
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
                    transform.position,
                    dest,
                    moveSpeed * Time.deltaTime);
            }

            yield return null;
        }

        moveRoutine = null;
    }

    public void TakeDamage(float amount)
    {
        health?.TakeDamage(new TMJ_DamageInfo(amount, transform.position));
    }

    void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        float healthPercent = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        OnHealthChanged?.Invoke(healthPercent);
    }

    void HandleDeath()
    {
        StopAllCoroutines();
        moveRoutine = null;
        OnDeath?.Invoke();
        gameObject.SetActive(false);
    }
}
