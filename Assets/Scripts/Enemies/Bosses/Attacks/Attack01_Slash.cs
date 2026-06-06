using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Ataque 1 — Slash con repeticiones.
/// Cada ciclo: el FSM mueve al jefe → llama StartCycle() → espera OnCycleEnded.
/// Cuando se completaron todos los ciclos, dispara OnAttackEnded.
/// La espada se activa al inicio del primer ciclo y se apaga al final del último.
/// </summary>
public class Attack01_Slash : MonoBehaviour
{
    public event Action OnCycleEnded;   // un giro terminó → FSM vuelve a Moving
    public event Action OnAttackEnded;  // todos los ciclos terminados → FSM va a Cleanup

    [Header("Meshes")]
    public GameObject swordMesh;
    public GameObject mainWeaponMesh; // para ocultar el arma principal durante el ataque

    [Header("Parámetros")]
    public float spinSpeed      = 300f;
    public float spinDuration   = 2f;
    public int   repeatCount    = 3;    // cuántas veces va hacia el jugador y gira
    public float damage         = 15f;
    public float damageCooldown = 0.35f;

    // Runtime
    private float     damageTimer;
    private int       cyclesDone;
    private Coroutine routine;

    /// <summary>True si el ataque está en curso (ciclos intermedios).</summary>
    public bool IsActive => swordMesh != null && swordMesh.activeSelf;

    // ─────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────

    /// <summary>Llamado por el FSM al inicio del ataque completo (ciclo 0).</summary>
    public void StartAttack()
    {
        cyclesDone = 0;
        damageTimer = 0f;
        if (swordMesh != null) swordMesh.SetActive(true);
        if (mainWeaponMesh != null) mainWeaponMesh.SetActive(false);
        StartCycle();
    }

    /// <summary>Llamado por el FSM cada vez que el jefe llegó a la nueva posición.</summary>
    public void StartCycle()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CycleRoutine());
    }

    // ─────────────────────────────────────────
    //  Rutina de un ciclo (un giro)
    // ─────────────────────────────────────────
    private IEnumerator CycleRoutine()
    {
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            GetComponentInParent<BossController>().RotateY(spinSpeed);
            damageTimer += Time.deltaTime;
            elapsed     += Time.deltaTime;
            yield return null;
        }

        cyclesDone++;
        routine = null;

        if (cyclesDone >= repeatCount)
        {
            // Último ciclo: cleanup y fin del ataque
            if (swordMesh != null) swordMesh.SetActive(false);
            if (mainWeaponMesh != null) mainWeaponMesh.SetActive(true);
            OnAttackEnded?.Invoke();
        }
        else
        {
            // Hay más ciclos: pedirle al FSM que mueva al jefe de nuevo
            OnCycleEnded?.Invoke();
        }
    }

    // ─────────────────────────────────────────
    //  Daño por trigger
    // ─────────────────────────────────────────
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (damageTimer < damageCooldown) return;
        damageTimer = 0f;
        other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
    }
}