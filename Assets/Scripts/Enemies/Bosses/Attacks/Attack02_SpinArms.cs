using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ataque 2 — Spin de brazos.
/// Activa los 4 brazos → gira → los desactiva → OnAttackEnded.
/// Los brazos solo detectan colliders; este script centraliza targetLayers, cooldown y daño.
/// </summary>
public class Attack02_SpinArms : MonoBehaviour
{
    public event Action OnAttackEnded;

    [Header("Brazos")]
    public GameObject[] armMeshes = new GameObject[4];

    [Header("Armas principales")]
    public GameObject[] mainWeaponMeshes;

    [Header("Targets")]
    public LayerMask targetLayers;

    [SerializeField]
    GameObject damageOwner;

    [Header("Parámetros")]
    public float spinSpeed = 50f;
    public float spinDuration = 6f;
    public int directionChanges = 1;
    public float damage = 10f;
    public float damageCooldown = 0.5f;

    Coroutine routine;
    Animator animator;
    readonly Dictionary<ITopDownDamageable, float> nextDamageTimeByTarget = new Dictionary<ITopDownDamageable, float>();

    void Awake()
    {
        animator = GetComponentInParent<Animator>();

        if (damageOwner == null)
        {
            BossController boss = GetComponentInParent<BossController>();
            damageOwner = boss != null ? boss.gameObject : transform.root.gameObject;
        }
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
        nextDamageTimeByTarget.Clear();
        SetArmsActive(true);

        if (animator != null)
        {
            animator.SetBool("IsRotating", true);
        }

        int segments = directionChanges + 1;
        float segmentTime = spinDuration / segments;
        float direction = 1f;
        BossController boss = GetComponentInParent<BossController>();

        for (int i = 0; i < segments; i++)
        {
            float elapsed = 0f;
            while (elapsed < segmentTime)
            {
                boss?.RotateY(spinSpeed * direction);
                elapsed += Time.deltaTime;
                yield return null;
            }

            direction *= -1f;
        }

        SetArmsActive(false);

        if (animator != null)
        {
            animator.SetBool("IsRotating", false);
        }

        nextDamageTimeByTarget.Clear();
        routine = null;
        OnAttackEnded?.Invoke();
    }

    void SetArmsActive(bool active)
    {
        if (mainWeaponMeshes != null)
        {
            foreach (GameObject main in mainWeaponMeshes)
            {
                if (main != null)
                {
                    main.SetActive(!active);
                }
            }
        }

        foreach (GameObject arm in armMeshes)
        {
            if (arm != null)
            {
                arm.SetActive(active);
            }
        }
    }

    public void OnArmHit(Collider other)
    {
        if (routine == null)
        {
            return;
        }

        if (!TMJ_DamageUtility.TryGetDamageable(other, targetLayers, damageOwner, out ITopDownDamageable damageable))
        {
            return;
        }

        if (nextDamageTimeByTarget.TryGetValue(damageable, out float nextDamageTime) && Time.time < nextDamageTime)
        {
            return;
        }

        if (TMJ_DamageUtility.TryDamageCollider(other, damage, transform.position, gameObject, targetLayers, damageOwner, null))
        {
            nextDamageTimeByTarget[damageable] = Time.time + damageCooldown;
        }
    }
}
