using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ataque 4 — Drops de rocas.
/// 1. El FSM mueve al jefe al centro (spawnPoint).
/// 2. Instancia dropPrefab en la posición XZ del jugador (altura fija) con delay entre cada uno.
/// 3. Espera cleanupDelay para dar tiempo a que las rocas aterricen y se destruyan solas.
/// 4. Destruye los padres que queden en la lista → OnAttackEnded.
/// </summary>
public class Attack04_Drops : MonoBehaviour
{
    public event Action OnAttackEnded;

    [Header("Prefab")]
    [Tooltip("Prefab del drop a instanciar.")]
    public GameObject dropPrefab;

    [Header("Parámetros")]
    [Tooltip("Cantidad de drops a instanciar.")]
    public int   dropCount       = 5;

    [Tooltip("Segundos entre cada drop.")]
    public float delayBetweenDrops = 0.4f;


    [Tooltip("Segundos de espera después del último drop antes de limpiar la lista.")]
    public float cleanupDelay    = 3f;

    // Runtime
    private readonly List<GameObject> activeDrops = new List<GameObject>();
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
    //  Rutina principal
    // ─────────────────────────────────────────
    private IEnumerator AttackRoutine()
    {
        activeDrops.Clear();

        var playerGO = GameObject.FindGameObjectWithTag("Player");

        for (int i = 0; i < dropCount; i++)
        {
            // Snapshot de la posición del jugador en este frame (solo XZ)
            Vector3 spawnPos;
            if (playerGO != null)
            {
                spawnPos   = playerGO.transform.position;
                spawnPos.y = 0f;
            }
            else
            {
                spawnPos = new Vector3(transform.position.x, 0f, transform.position.z);
            }

            if (dropPrefab != null)
            {
                var drop = Instantiate(dropPrefab, spawnPos, Quaternion.identity);
                activeDrops.Add(drop);
            }

            // Esperar antes del siguiente (excepto tras el último)
            if (i < dropCount - 1)
                yield return new WaitForSeconds(delayBetweenDrops);
        }

        // Dar tiempo a que las rocas caigan y se destruyan solas
        yield return new WaitForSeconds(cleanupDelay);

        // Limpiar padres que aún existan
        foreach (var drop in activeDrops)
            if (drop != null) Destroy(drop);
        activeDrops.Clear();

        routine = null;
        OnAttackEnded?.Invoke();
    }
}