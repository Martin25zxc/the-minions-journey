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
/// Spawnea proyectiles desde varios puntos. El daño vive en BulletAttackController.
/// </summary>
public class Attack06_Bullets : MonoBehaviour
{
    public event Action OnAttackEnded;

    [Header("Referencias")]
    public Transform spawnRoot;
    public GameObject bulletPrefab;

    [Header("Targets del proyectil")]
    public LayerMask targetLayers;
    public LayerMask wallLayers;

    [SerializeField]
    GameObject damageOwner;

    [Header("Parámetros de salva")]
    public int volleyCount = 4;
    public float delayBetweenVolleys = 0.5f;
    public float rotationPerVolley = 30f;
    public float projectileSpeed = 10f;

    Coroutine routine;
    Animator animator;
    bool isCasting;

    void Awake()
    {
        animator = GetComponentInParent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("[Attack06_Bullets] No se encontró un Animator en el mismo GameObject.");
        }

        if (damageOwner == null)
        {
            BossController boss = GetComponentInParent<BossController>();
            damageOwner = boss != null ? boss.gameObject : transform.root.gameObject;
        }
    }

    public void StartAttack()
    {
        if (spawnRoot == null)
        {
            Debug.LogWarning("[Attack06_Bullets] spawnRoot no asignado.");
            OnAttackEnded?.Invoke();
            return;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        if (animator != null)
        {
            isCasting = true;
            animator.SetBool("IsCasting", isCasting);
        }

        for (int v = 0; v < volleyCount; v++)
        {
            FireVolley();
            spawnRoot.Rotate(0f, rotationPerVolley, 0f, Space.World);

            if (v < volleyCount - 1)
            {
                yield return new WaitForSeconds(delayBetweenVolleys);
            }
        }

        routine = null;
        isCasting = false;

        if (animator != null)
        {
            animator.SetBool("IsCasting", isCasting);
        }

        OnAttackEnded?.Invoke();
    }

    void FireVolley()
    {
        if (bulletPrefab == null)
        {
            return;
        }

        foreach (Transform spawnPoint in spawnRoot)
        {
            GameObject go = Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);
            BulletAttackController bullet = go.GetComponent<BulletAttackController>();
            if (bullet != null)
            {
                bullet.Configure(damageOwner, targetLayers, wallLayers, projectileSpeed);
            }
        }
    }
}
