using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Ataque 2 — Spin de brazos.
/// Activa los 4 brazos → gira → los desactiva → OnAttackEnded.
/// </summary>
public class Attack02_SpinArms : MonoBehaviour
{
    public event Action OnAttackEnded;

    [Header("Brazos")]
    public GameObject[] armMeshes = new GameObject[4];

    [Header("Parámetros")]
    public float spinSpeed = 50f;   // grados/segundo
    public float spinDuration = 6f;    // duración total del ataque
    public int directionChanges = 1;     // cuántas veces cambia de dirección
    public float damage = 10f;
    public float damageCooldown = 0.5f;

    private float damageTimer;
    private Coroutine routine;

    // ─────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────
    public void StartAttack()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(AttackRoutine());
    }

    // ─────────────────────────────────────────
    //  Rutina interna
    // ─────────────────────────────────────────
    private IEnumerator AttackRoutine()
    {
        damageTimer = 0f;
        SetArmsActive(true);

        int segments = directionChanges + 1;
        float segmentTime = spinDuration / segments;
        float direction = 1f;

        for (int i = 0; i < segments; i++)
        {
            float elapsed = 0f;
            while (elapsed < segmentTime)
            {
                GetComponentInParent<BossController>().RotateY(spinSpeed * direction);
                damageTimer += Time.deltaTime;
                elapsed += Time.deltaTime;
                yield return null;
            }
            direction *= -1f;
        }

        SetArmsActive(false);
        routine = null;
        OnAttackEnded?.Invoke();
    }

    private void SetArmsActive(bool active)
    {
        foreach (var arm in armMeshes)
            if (arm != null) arm.SetActive(active);
    }

    // ─────────────────────────────────────────
    //  Daño (llamado desde ArmDamageZone)
    // ─────────────────────────────────────────
    public void OnArmHit(Collider other)
    {
        if (damageTimer < damageCooldown) return;
        damageTimer = 0f;
        other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
    }
}