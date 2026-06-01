using System.Collections;
using UnityEngine;

/// <summary>
/// FSM del minion mele.
/// Estados: Idle → Chase → Attack → Cooldown → (repeat)
/// Toda la comunicación es por eventos — no hay polling ni IsBusy checks.
/// </summary>
public class MinionMeleeFSM : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  FSM
    // ─────────────────────────────────────────────────────────────────────
    private enum State { Idle, Chase, Attack, Cooldown, Dead }
    private State currentState = State.Idle;

    // ─────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────
    [Header("Setup")]
    [SerializeField] private MinionDataSO data;

    // ─────────────────────────────────────────────────────────────────────
    //  Referencias runtime
    // ─────────────────────────────────────────────────────────────────────
    private MinionController  controller;
    private MinionMeleeAttack meleeAttack;
    private Transform         player;

    private float detectionRange;
    private float detectionHalfAngle;
    private float attackRange;
    private float loseAggroRange;   // detectionRange * 1.5 — evita flicker en el borde

    [SerializeField] private LayerMask obstacleLayerMask = ~0; // todo por defecto

    // ─────────────────────────────────────────────────────────────────────
    //  Awake / Start
    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        controller  = GetComponent<MinionController>();
        meleeAttack = GetComponentInChildren<MinionMeleeAttack>();
    }

    private void Start()
    {
        if (data == null) { Debug.LogError("[MinionFSM] MinionDataSO no asignado."); return; }

        controller.Initialize(data);
        meleeAttack?.Initialize(data.damageAmount, data.attackDuration);

        detectionRange     = data.detectionRange;
        detectionHalfAngle = data.detectionAngle * 0.5f;
        attackRange        = data.attackRange;
        loseAggroRange     = data.detectionRange * 1.5f;

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) player = playerGO.transform;

        // Suscribir eventos
        controller.OnDeath += HandleDeath;
        if (meleeAttack != null) meleeAttack.OnAttackEnded += HandleAttackEnded;

        EnterIdle();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Update — solo evalúa transiciones de distancia en Idle y Chase
    // ─────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (currentState == State.Dead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                if (CanSeePlayer())
                    EnterChase();
                break;

            case State.Chase:
                if (dist <= attackRange)
                    EnterAttack();
                else if (dist > loseAggroRange)
                    EnterIdle();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Detección: cono de visión + line of sight
    // ─────────────────────────────────────────────────────────────────────
    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 toPlayer = player.position - transform.position;
        if (toPlayer.magnitude > detectionRange) return false;

        if (Vector3.Angle(transform.forward, toPlayer) > detectionHalfAngle) return false;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 target = player.position   + Vector3.up * 0.5f;
        if (Physics.Linecast(origin, target, out RaycastHit hit, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            return hit.collider.CompareTag("Player");

        return true;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  IDLE — parado, esperando detección
    // ─────────────────────────────────────────────────────────────────────
    private void EnterIdle()
    {
        currentState = State.Idle;
        controller.StopMoving();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CHASE — perseguir al jugador
    // ─────────────────────────────────────────────────────────────────────
    private void EnterChase()
    {
        currentState = State.Chase;
        controller.StartChasing(player);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ATTACK — golpear
    // ─────────────────────────────────────────────────────────────────────
    private void EnterAttack()
    {
        currentState = State.Attack;
        controller.StopMoving();
        controller.FaceTarget(player.position);

        if (meleeAttack != null)
            meleeAttack.StartAttack();
        else
            StartCoroutine(FallbackAttackEnd());
        // Espera evento OnAttackEnded → HandleAttackEnded
    }

    private void HandleAttackEnded()
    {
        if (currentState != State.Attack) return;
        EnterCooldown();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  COOLDOWN — pausa breve antes de volver a Idle
    // ─────────────────────────────────────────────────────────────────────
    private void EnterCooldown()
    {
        currentState = State.Cooldown;
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(Random.Range(data.minCooldown, data.maxCooldown));
        if (currentState == State.Cooldown) EnterIdle();
    }

    // Fallback si no hay MinionMeleeAttack en el prefab
    private IEnumerator FallbackAttackEnd()
    {
        yield return new WaitForSeconds(0.5f);
        HandleAttackEnded();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  DEAD
    // ─────────────────────────────────────────────────────────────────────
    private void HandleDeath()
    {
        currentState = State.Dead;
        StopAllCoroutines();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Limpieza de eventos
    // ─────────────────────────────────────────────────────────────────────
    private void OnDestroy()
    {
        if (controller  != null) controller.OnDeath         -= HandleDeath;
        if (meleeAttack != null) meleeAttack.OnAttackEnded   -= HandleAttackEnded;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Gizmos — visualizar rangos en la escena
    // ─────────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        // cono de deteccion
        float half = data.detectionAngle * 0.5f;
        Vector3 fwd = transform.forward;
        Vector3 left  = Quaternion.Euler(0, -half, 0) * fwd;
        Vector3 right = Quaternion.Euler(0,  half, 0) * fwd;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, left  * data.detectionRange);
        Gizmos.DrawRay(transform.position, right * data.detectionRange);
        Gizmos.DrawWireSphere(transform.position, data.detectionRange);

        // rango de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);
    }
}
