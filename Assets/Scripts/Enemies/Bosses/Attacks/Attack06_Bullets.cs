using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Ataque 6 — Bullets rotatorios.
/// 1. El FSM mueve al jefe al centro.
/// 2. Dispara desde todos los spawnPoints simultáneamente (cada uno en su forward).
/// 3. Rota el contenedor de spawnPoints en Y por rotationPerVolley grados.
/// 4. Repite volleyCount veces con delayBetweenVolleys entre cada salva.
/// 5. OnAttackEnded.
///
/// Setup en el prefab del jefe:
///   - Crear un empty hijo llamado "BulletSpawnRoot" (asignarlo a spawnRoot).
///   - Agregar como hijos de BulletSpawnRoot los cubos/empties de spawn,
///     distribuidos en círculo apuntando hacia afuera.
///   - Los cubos se pueden desactivar el mesh renderer si no se quieren ver.
/// </summary>
public class Attack06_Bullets : MonoBehaviour
{
    public event Action OnAttackEnded;

    [Header("Referencias")]
    [Tooltip("Origen de los proyectiles. Se rotará para cambiar la dirección de disparo.")]
    public Transform spawnRoot;

    [Tooltip("Prefab del proyectil")]
    public GameObject bulletPrefab;

    [Header("Parámetros de salva")]
    [Tooltip("Cuántas salvas dispara en total.")]
    public int   volleyCount          = 4;

    [Tooltip("Segundos entre cada salva.")]
    public float delayBetweenVolleys  = 0.5f;

    [Tooltip("Grados que rota spawnRoot entre salva y salva.")]
    public float rotationPerVolley    = 30f;

    [Tooltip("Velocidad inicial de cada proyectil.")]
    public float projectileSpeed      = 10f;

    private Coroutine routine;

    // ─────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────
    public void StartAttack()
    {
        if (spawnRoot == null)
        {
            Debug.LogWarning("[Attack06_Bullets] spawnRoot no asignado.");
            OnAttackEnded?.Invoke();
            return;
        }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(AttackRoutine());
    }

    // ─────────────────────────────────────────
    //  Rutina
    // ─────────────────────────────────────────
    private IEnumerator AttackRoutine()
    {
        for (int v = 0; v < volleyCount; v++)
        {
            // Disparar desde todos los puntos hijo de spawnRoot
            FireVolley();

            // Rotar el root para la próxima salva
            spawnRoot.Rotate(0f, rotationPerVolley, 0f, Space.World);

            if (v < volleyCount - 1)
                yield return new WaitForSeconds(delayBetweenVolleys);
        }

        routine = null;
        OnAttackEnded?.Invoke();
    }

    // ─────────────────────────────────────────
    //  Disparo de una salva
    // ─────────────────────────────────────────
    private void FireVolley()
    {
        if (bulletPrefab == null) return;

        foreach (Transform spawnPoint in spawnRoot)
        {
            var go = Instantiate(bulletPrefab, spawnPoint.position,
                                 spawnPoint.rotation);
            /*
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity     = false;
                rb.linearVelocity = spawnPoint.forward * projectileSpeed;
            }*/
        }
    }
}