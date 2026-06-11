using System;
using UnityEngine;

/// <summary>
/// Ataque 8 — Jump.
/// El FSM llama JumpTo en BossController y espera OnArrived.
/// Al llegar, el FSM llama OnLanded(), instancia el anillo y dispara OnCycleEnded/OnAttackEnded.
/// El daño vive en RingController.
/// </summary>
public class Attack08_Jump : MonoBehaviour
{
    public event Action OnCycleEnded;
    public event Action OnAttackEnded;

    [Header("Prefab anillo")]
    public GameObject ringPrefab;

    [Header("Targets del ring")]
    public LayerMask targetLayers;

    [SerializeField]
    GameObject damageOwner;

    [Header("Parámetros")]
    public int repeatCount = 3;
    public float jumpHeight = 4f;
    public float jumpDuration = 0.8f;
    public float ringDuration = 1f;
    public float landingPause = 0.3f;

    int cyclesDone;
    Animator anim;

    public bool IsActive => cyclesDone > 0 && cyclesDone < repeatCount;
    public int CyclesDone => cyclesDone;

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
        cyclesDone = 0;
        PrepareJump();
    }

    public void PrepareJump()
    {
        anim?.SetTrigger("JumpStart");
        anim?.SetBool("OnAir", true);
    }

    public void OnLanded()
    {
        anim?.SetBool("OnAir", false);
        anim?.SetTrigger("JumpEnd");
        CameraShake.Trigger(0.5f, 0.5f, CameraShake.ShakeDirection.Vertical);

        SpawnRing(transform.position);
        cyclesDone++;

        if (cyclesDone >= repeatCount)
        {
            StartCoroutine(WaitThenEnd(OnAttackEnded));
        }
        else
        {
            StartCoroutine(WaitThenEnd(OnCycleEnded));
        }
    }

    System.Collections.IEnumerator WaitThenEnd(Action callback)
    {
        yield return new WaitForSeconds(ringDuration + landingPause);
        callback?.Invoke();
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
