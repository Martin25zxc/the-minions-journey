using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Ataque 7 — Ring.
/// Va a la posición del jugador, instancia el anillo,
/// espera a que termine la animación y repite repeatCount veces.
/// Usa OnCycleEnded para que el FSM lo mueva a la nueva posición entre ciclos.
/// El daño vive en RingController.
/// </summary>
public class Attack07_Ring : MonoBehaviour
{
    public event Action OnCycleEnded;
    public event Action OnAttackEnded;

    [Header("Prefab")]
    public GameObject ringPrefab;

    [Header("Targets del ring")]
    public LayerMask targetLayers;

    [SerializeField]
    GameObject damageOwner;

    [Header("Circulo magico")]
    public GameObject magicCircle;

    [Header("Parámetros")]
    public int repeatCount = 3;
    public float preSpawnDelay = 0.3f;
    public float ringDuration = 1f;

    int cyclesDone;
    Coroutine routine;
    Animator animator;

    public bool IsActive => cyclesDone > 0 && cyclesDone < repeatCount;

    void Awake()
    {
        if (ringPrefab == null)
        {
            Debug.LogWarning("[Attack07_Ring] ringPrefab no asignado.");
        }

        animator = GetComponentInParent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("[Attack07_Ring] No se encontró un Animator en el mismo GameObject o en sus padres.");
        }

        if (damageOwner == null)
        {
            BossController boss = GetComponentInParent<BossController>();
            damageOwner = boss != null ? boss.gameObject : transform.root.gameObject;
        }
    }

    public void StartAttack()
    {
        cyclesDone = 0;
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
        magicCircle?.SetActive(true);
        animator?.SetTrigger("Raise");
        yield return new WaitForSeconds(preSpawnDelay);

        SpawnRing(transform.position);

        yield return new WaitForSeconds(ringDuration / 2f);
        magicCircle?.SetActive(false);
        yield return new WaitForSeconds(ringDuration / 2f);

        cyclesDone++;
        routine = null;

        if (cyclesDone >= repeatCount)
        {
            OnAttackEnded?.Invoke();
        }
        else
        {
            OnCycleEnded?.Invoke();
        }
    }

    void SpawnRing(Vector3 position)
    {
        if (ringPrefab == null)
        {
            return;
        }

        GameObject ring = Instantiate(ringPrefab, position, Quaternion.identity);
        RingController ringController = ring.GetComponent<RingController>();
        if (ringController != null)
        {
            ringController.Configure(damageOwner, targetLayers);
        }
    }
}
