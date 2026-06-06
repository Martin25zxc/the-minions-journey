using System;
using UnityEngine;

/// <summary>
/// Ataque 8 — Jump.
/// El FSM llama JumpTo en BossController y espera OnArrived.
/// Al llegar, el FSM llama OnLanded() que instancia el anillo y dispara OnCycleEnded/OnAttackEnded.
/// </summary>
public class Attack08_Jump : MonoBehaviour
{
    public event Action OnCycleEnded;
    public event Action OnAttackEnded;

    [Header("Prefab anillo")]
    public GameObject ringPrefab;

    [Header("Parámetros")]
    public int   repeatCount  = 3;
    public float jumpHeight   = 4f;
    public float jumpDuration = 0.8f;
    [Tooltip("Debe coincidir con RingController.expandDuration.")]
    public float ringDuration = 1f;
    [Tooltip("Pausa extra tras el anillo antes del siguiente ciclo.")]
    public float landingPause = 0.3f;

    // Runtime
    private int      cyclesDone;
    private Animator anim;

    public bool IsActive     => cyclesDone > 0 && cyclesDone < repeatCount;
    public int  CyclesDone   => cyclesDone;

    private void Awake()
    {
        anim = GetComponentInParent<Animator>();
    }

    // ─────────────────────────────────────────
    //  API pública — llamada por el FSM
    // ─────────────────────────────────────────
    public void StartAttack()
    {
        cyclesDone = 0;
        PrepareJump();
    }

    /// <summary>Dispara la animación de salto. Llamado antes de cada JumpTo.</summary>
    public void PrepareJump()
    {
        anim?.SetTrigger("JumpStart");
        anim?.SetBool("OnAir", true);
    }

    /// <summary>Llamado por el FSM cuando OnArrived se dispara (aterrizó).</summary>
    public void OnLanded()
    {
        anim?.SetBool("OnAir", false);
        anim?.SetTrigger("JumpEnd");

        if (ringPrefab != null)
            Instantiate(ringPrefab, transform.position, Quaternion.identity);

        cyclesDone++;

        if (cyclesDone >= repeatCount)
            StartCoroutine(WaitThenEnd(OnAttackEnded));
        else
            StartCoroutine(WaitThenEnd(OnCycleEnded));
    }

    private System.Collections.IEnumerator WaitThenEnd(Action callback)
    {
        yield return new UnityEngine.WaitForSeconds(ringDuration + landingPause);
        callback?.Invoke();
    }
}