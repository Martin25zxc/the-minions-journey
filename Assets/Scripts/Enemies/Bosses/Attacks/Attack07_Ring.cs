using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Ataque 7 — Ring.
/// Va a la posición del jugador, instancia el anillo,
/// espera a que termine la animación y repite repeatCount veces.
/// Usa OnCycleEnded para que el FSM lo mueva a la nueva posición entre ciclos.
/// </summary>
public class Attack07_Ring : MonoBehaviour
{
    public event Action OnCycleEnded;
    public event Action OnAttackEnded;

    [Header("Prefab")]
    public GameObject ringPrefab;

    [Header("Parámetros")]
    public int repeatCount = 3;
    [Tooltip("Segundos a esperar antes de instanciar el anillo (para coincidir con animación).")]
    public float preSpawnDelay = 0.3f;
    [Tooltip("Debe coincidir con RingController.expandDuration para esperar a que termine.")]
    public float ringDuration = 1f;

    // Runtime
    private int cyclesDone;
    private Coroutine routine;
    private Animator animator;

    public bool IsActive => cyclesDone > 0 && cyclesDone < repeatCount;


    private void Awake()
    {
        if (ringPrefab == null)
            Debug.LogWarning("[Attack07_Ring] ringPrefab no asignado.");

        animator = GetComponentInParent<Animator>();
        if (animator == null)
            Debug.LogWarning("[Attack07_Ring] No se encontró un Animator en el mismo GameObject o en sus padres.");
    }

    // ─────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────
    public void StartAttack()
    {
        cyclesDone = 0;
        StartCycle();
    }

    public void StartCycle()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CycleRoutine());
    }

    // ─────────────────────────────────────────
    //  Rutina de un ciclo
    // ─────────────────────────────────────────
    private IEnumerator CycleRoutine()
    {
        animator.SetTrigger("Raise");
        // Pequeña pausa para coincidir con la animación del jefe
        yield return new WaitForSeconds(preSpawnDelay);

        // Instanciar anillo en la posición del jefe (ignorando Y)
        if (ringPrefab != null)
        {
            Vector3 spawnPos = transform.position;
            Instantiate(ringPrefab, spawnPos, Quaternion.identity);
        }

        // Esperar a que el anillo termine de expandirse
        yield return new WaitForSeconds(ringDuration);

        cyclesDone++;
        routine = null;

        if (cyclesDone >= repeatCount)
            OnAttackEnded?.Invoke();
        else
            OnCycleEnded?.Invoke();
    }
}