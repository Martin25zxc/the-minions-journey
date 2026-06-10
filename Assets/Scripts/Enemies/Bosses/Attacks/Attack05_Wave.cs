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
    public float delayBetween     = 1.1f;
    public float projectileSpeed  = 12f;
    public float rotateSpeed      = 360f; // grados/segundo durante la fase de apuntado
    public float animationDelay    = 0.3f; // segundos entre el trigger de animación y el spawn del proyectil

    [Tooltip("Segundos que tarda en rotar hacia el jugador antes de disparar.")]
    public float aimDuration      = 0.6f;

    private Coroutine routine;
    private Transform playerTransform;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponentInParent<Animator>();
    }

     private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

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
                    RotateTowardsPlayer();
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            if (anim != null) anim.SetTrigger("DoubleSlash");
            yield return new WaitForSeconds(animationDelay);
            
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

      private void RotateTowardsPlayer()
    {
        Transform root = transform.parent != null ? transform.parent : transform;
        Vector3 dir = playerTransform.position - root.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        root.rotation = Quaternion.RotateTowards(
            root.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }
}