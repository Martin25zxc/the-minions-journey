using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Ataque 5 — Wave.
/// 1. El FSM mueve al jefe al centro.
/// 2. Rota para apuntar al jugador (snapshot de dirección).
/// 3. Instancia bulletCount proyectiles de a uno con delayBetween entre cada uno,
///    todos en la misma dirección.
/// 4. OnAttackEnded.
///
/// El prefab del proyectil se mueve solo (Rigidbody o script propio).
/// Agregar este componente al prefab del jefe.
/// </summary>
public class Attack05_Wave : MonoBehaviour
{
    public event Action OnAttackEnded;

    [Header("Proyectil")]
    public GameObject bulletPrefab;

    [Tooltip("Punto de origen del disparo (empty hijo). Si es null usa el transform del jefe.")]
    public Transform firePoint;

    [Header("Parámetros")]
    public int   bulletCount      = 6;
    public float delayBetween     = 0.15f;
    public float projectileSpeed  = 12f;

    [Tooltip("Segundos que tarda en rotar hacia el jugador antes de disparar.")]
    public float aimDuration      = 0.6f;

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
    //  Rutina
    // ─────────────────────────────────────────
    private IEnumerator AttackRoutine()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");

        // Disparar bulletCount proyectiles, reapuntando antes de cada uno
        for (int i = 0; i < bulletCount; i++)
        {
            // Reapuntar al jugador
            if (playerGO != null)
            {
                float elapsed = 0f;
                while (elapsed < aimDuration)
                {
                    Vector3 dir = playerGO.transform.position - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        Quaternion target = Quaternion.LookRotation(dir);
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation, target, elapsed / aimDuration);
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            Transform origin  = firePoint != null ? firePoint : transform;
            SpawnBullet(origin.position, origin.forward);

            if (i < bulletCount - 1)
                yield return new WaitForSeconds(delayBetween);
        }

        routine = null;
        OnAttackEnded?.Invoke();
    }

    // ─────────────────────────────────────────
    //  Spawn de proyectil
    // ─────────────────────────────────────────
    private void SpawnBullet(Vector3 pos, Vector3 dir)
    {
        if (bulletPrefab == null) return;

        var go = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
        /*
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity     = false;
            rb.linearVelocity = dir.normalized * projectileSpeed;
        }*/
    }
}