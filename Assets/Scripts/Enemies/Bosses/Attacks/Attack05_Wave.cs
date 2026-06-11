using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Ataque 5 — Wave.
/// El ataque apunta y spawnea proyectiles. El daño vive en ProjectileAttackController.
/// </summary>
public class Attack05_Wave : MonoBehaviour
{
    public event Action OnAttackEnded;

    [Header("Proyectil")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Targets del proyectil")]
    public LayerMask targetLayers;
    public LayerMask blockingLayers;

    [SerializeField]
    GameObject damageOwner;

    [Header("Parámetros")]
    public int bulletCount = 6;
    public float delayBetween = 1.1f;
    public float projectileSpeed = 12f;
    public float rotateSpeed      = 360f; // grados/segundo durante la fase de apuntado
    public float animationDelay    = 0.3f; // segundos entre el trigger de animación y el spawn del proyectil
    public float aimDuration = 0.6f;

    Coroutine routine;
    Transform playerTransform;
    Animator anim;

    void Awake()
    {
        anim = GetComponentInParent<Animator>();

        if (damageOwner == null)
        {
            BossController boss = GetComponentInParent<BossController>();
            damageOwner = boss != null ? boss.gameObject : transform.root.gameObject;
        }
    }

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public void StartAttack()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        // Disparar bulletCount proyectiles, reapuntando antes de cada uno
        for (int i = 0; i < bulletCount; i++)
        {
            if (playerTransform != null)
            {
                float elapsed = 0f;
                while (elapsed < aimDuration)
                {
                    RotateTowardsPlayer();
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            if (anim != null)
            {
                anim.SetTrigger("DoubleSlash");
            }

            yield return new WaitForSeconds(animationDelay);

            Transform origin = firePoint != null ? firePoint : transform;
            SpawnBullet(origin.position, origin.forward);

            if (i < bulletCount - 1)
            {
                yield return new WaitForSeconds(delayBetween);
            }
        }

        routine = null;
        OnAttackEnded?.Invoke();
    }

    void SpawnBullet(Vector3 pos, Vector3 dir)
    {
        if (bulletPrefab == null)
        {
            return;
        }

        GameObject go = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
        ProjectileAttackController projectile = go.GetComponent<ProjectileAttackController>();
        if (projectile != null)
        {
            projectile.Configure(damageOwner, targetLayers, blockingLayers, projectileSpeed);
        }
    }

    void RotateTowardsPlayer()
    {
        if (playerTransform == null)
        {
            return;
        }

        Transform root = transform.parent != null ? transform.parent : transform;
        Vector3 dir = playerTransform.position - root.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(dir);
        root.rotation = Quaternion.RotateTowards(root.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }
}
