using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ataque 4 — Drops de rocas.
/// El ataque decide dónde spawnear. El daño vive en FallAttackController.
/// </summary>
public class Attack04_Drops : MonoBehaviour
{
    public event Action OnAttackEnded;

    [Header("Prefab")]
    public GameObject dropPrefab;

    [Header("Targets del drop")]
    public LayerMask targetLayers;
    public LayerMask impactLayers;

    [SerializeField]
    GameObject damageOwner;

    [Header("Parámetros")]
    public int dropCount = 5;
    public float delayBetweenDrops = 0.4f;
    public GameObject magicCircle;
    public float cleanupDelay = 1f;

    readonly List<GameObject> activeDrops = new List<GameObject>();
    Coroutine routine;
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
        activeDrops.Clear();

        if (anim != null)
        {
            anim.SetBool("IsCasting", true);
        }

        magicCircle?.SetActive(true);
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");

        for (int i = 0; i < dropCount; i++)
        {
            Vector3 spawnPos;
            if (playerGO != null)
            {
                spawnPos = playerGO.transform.position;
                spawnPos.y = 0f;
            }
            else
            {
                spawnPos = new Vector3(transform.position.x, 0f, transform.position.z);
            }

            if (dropPrefab != null)
            {
                GameObject drop = Instantiate(dropPrefab, spawnPos, Quaternion.identity);
                FallAttackController fallController = drop.GetComponent<FallAttackController>();
                if (fallController != null)
                {
                    fallController.Configure(damageOwner, targetLayers, impactLayers);
                }

                activeDrops.Add(drop);
            }

            if (i < dropCount - 1)
            {
                yield return new WaitForSeconds(delayBetweenDrops);
            }
        }

        yield return new WaitForSeconds(cleanupDelay);

        foreach (GameObject drop in activeDrops)
        {
            if (drop != null)
            {
                Destroy(drop);
            }
        }

        activeDrops.Clear();
        routine = null;

        if (anim != null)
        {
            anim.SetBool("IsCasting", false);
        }

        magicCircle?.SetActive(false);
        OnAttackEnded?.Invoke();
    }

    public void ForceStop()
    {
        StopAllCoroutines();
        foreach (GameObject drop in activeDrops)
        {
            if (drop != null)
            {
                Destroy(drop);
            }
        }

        activeDrops.Clear();
        routine = null;

        if (anim != null)
        {
            anim.SetBool("IsCasting", false);
        }

        magicCircle?.SetActive(false);
        OnAttackEnded?.Invoke();
        

    }
}
