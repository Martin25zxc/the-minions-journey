using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ataque 1 — Slash con repeticiones.
/// Cada ciclo: el FSM mueve al jefe → llama StartCycle() → espera OnCycleEnded.
/// Cuando se completaron todos los ciclos, dispara OnAttackEnded.
/// La espada se activa al inicio del primer ciclo y se apaga al final del último.
/// </summary>
public class Attack01_Slash : MonoBehaviour
{
    public event Action OnCycleEnded;
    public event Action OnAttackEnded;

    [Header("Meshes")]
    public GameObject swordMesh;
    public GameObject mainWeaponMesh;

    [Header("Targets")]
    public LayerMask targetLayers;

    [SerializeField]
    GameObject damageOwner;

    [Header("Parámetros")]
    public float spinSpeed = 300f;
    public float spinDuration = 2f;
    public int repeatCount = 3;
    public float damage = 15f;
    public float damageCooldown = 0.35f;

    int cyclesDone;
    Coroutine routine;
    readonly Dictionary<ITopDownDamageable, float> nextDamageTimeByTarget = new Dictionary<ITopDownDamageable, float>();

    public bool IsActive => swordMesh != null && swordMesh.activeSelf;

    void Awake()
    {
        if (damageOwner == null)
        {
            BossController boss = GetComponentInParent<BossController>();
            damageOwner = boss != null ? boss.gameObject : transform.root.gameObject;
        }
    }

    public void StartAttack()
    {
        cyclesDone = 0;
        nextDamageTimeByTarget.Clear();

        if (swordMesh != null)
        {
            swordMesh.SetActive(true);
        }

        if (mainWeaponMesh != null)
        {
            mainWeaponMesh.SetActive(false);
        }

        StartCycle();
    }

    public void StartCycle()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(CycleRoutine());
    }

    IEnumerator CycleRoutine()
    {
        float elapsed = 0f;
        BossController boss = GetComponentInParent<BossController>();

        while (elapsed < spinDuration)
        {
            boss?.RotateY(spinSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cyclesDone++;
        routine = null;

        if (cyclesDone >= repeatCount)
        {
            if (swordMesh != null)
            {
                swordMesh.SetActive(false);
            }

            if (mainWeaponMesh != null)
            {
                mainWeaponMesh.SetActive(true);
            }

            OnAttackEnded?.Invoke();
        }
        else
        {
            OnCycleEnded?.Invoke();
        }
    }

    void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    void TryDamage(Collider other)
    {
        if (!IsActive)
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

    public void ForceStop()
    {
        StopAllCoroutines();
        OnAttackEnded?.Invoke();

    }
}
