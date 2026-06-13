using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
[RequireComponent(typeof(EnemyTargetSensor))]
[RequireComponent(typeof(EnemyMovement))]
public sealed class EnemyBrain : MonoBehaviour, IImpactLockable
{
    private enum State
    {
        Idle,
        Chase,
        MeleeAttack,
        LeapAttack,
        ImpactLocked,
        Dead
    }

    [Header("References")]
    [SerializeField]
    private EnemyActor actor;

    [SerializeField]
    private EnemyTargetSensor targetSensor;

    [SerializeField]
    private EnemyMovement movement;

    [SerializeField]
    private EnemyMeleeAttackAbility meleeAttack;

    [SerializeField]
    private EnemyLeapAttackAbility leapAttack;

    [Header("Impact Lock")]
    [Tooltip("Si esta activo, un stun/knockback cancela el ataque melee actual. Queda como decision de balance a revisar.")]
    [SerializeField]
    private bool cancelMeleeAttackOnImpactLock = true;

    [Tooltip("Si esta activo, un stun/knockback cancela el Leap actual. En esta primera version evita que el movimiento de leap pelee contra el knockback.")]
    [SerializeField]
    private bool cancelLeapAttackOnImpactLock = true;

    [Header("Debug")]
    [SerializeField]
    private bool logStateChanges;

    private State currentState = State.Idle;
    private Coroutine impactLockRoutine;
    private float impactLockEndsAt;

    private EnemyDefinition Definition => actor != null ? actor.Definition : null;

    private void Awake()
    {
        if (actor == null)
        {
            actor = GetComponent<EnemyActor>();
        }

        if (targetSensor == null)
        {
            targetSensor = GetComponent<EnemyTargetSensor>();
        }

        if (movement == null)
        {
            movement = GetComponent<EnemyMovement>();
        }

        if (meleeAttack == null)
        {
            meleeAttack = GetComponent<EnemyMeleeAttackAbility>();
        }

        if (leapAttack == null)
        {
            leapAttack = GetComponent<EnemyLeapAttackAbility>();
        }
    }

    private void OnEnable()
    {
        if (actor != null)
        {
            actor.Died += HandleDied;
        }
    }

    private void Start()
    {
        EnemyDefinition definition = Definition;
        targetSensor?.Initialize(definition);
        movement?.Initialize(definition);

        // Las abilities conservan sus propios profiles asignados en su componente.
        // EnemyDefinition queda reservado para datos comunes del actor (vida, movimiento y deteccion).

        EnterIdle();
    }

    private void OnDisable()
    {
        if (actor != null)
        {
            actor.Died -= HandleDied;
        }
    }

    private void Update()
    {
        if (currentState == State.Dead || currentState == State.ImpactLocked)
        {
            return;
        }

        if (actor == null || !actor.IsAlive)
        {
            EnterDead();
            return;
        }

        if (Definition == null)
        {
            movement?.Stop();
            return;
        }

        targetSensor.Tick();

        switch (currentState)
        {
            case State.Idle:
                UpdateIdle();
                break;

            case State.Chase:
                UpdateChase();
                break;

            case State.MeleeAttack:
                UpdateMeleeAttack();
                break;

            case State.LeapAttack:
                UpdateLeapAttack();
                break;
        }
    }

    public void ApplyImpactLock(float duration)
    {
        if (duration <= 0f || currentState == State.Dead || actor == null || !actor.IsAlive)
        {
            return;
        }

        if (cancelMeleeAttackOnImpactLock)
        {
            meleeAttack?.CancelCurrentAttack();
        }

        if (cancelLeapAttackOnImpactLock)
        {
            leapAttack?.CancelCurrentLeap();
        }

        impactLockEndsAt = Mathf.Max(impactLockEndsAt, Time.time + duration);

        if (impactLockRoutine == null)
        {
            impactLockRoutine = StartCoroutine(ImpactLockRoutine());
        }
    }

    private void UpdateIdle()
    {
        movement.Stop();

        if (targetSensor.HasTarget)
        {
            EnterChase();
        }
    }

    private void UpdateChase()
    {
        if (!targetSensor.HasTarget || targetSensor.CurrentTarget == null)
        {
            EnterIdle();
            return;
        }

        Transform target = targetSensor.CurrentTarget;

        // Prioridad: si esta a media distancia y el leap esta disponible, intenta Leap.
        // Si esta demasiado cerca, CanStartLeap devuelve false por minRange y cae a melee.
        if (leapAttack != null && leapAttack.TryStartLeap(target))
        {
            EnterLeapAttack();
            return;
        }

        if (meleeAttack != null && meleeAttack.TryStartAttack(target))
        {
            EnterMeleeAttack();
            return;
        }

        float stopDistance = Definition.ChaseStopDistance;
        if (meleeAttack != null)
        {
            stopDistance = meleeAttack.GetPreferredStopDistance(stopDistance);
        }

        movement.MoveTowards(target.position, stopDistance);
    }

    private void UpdateMeleeAttack()
    {
        movement.Stop();

        if (targetSensor.HasTarget && targetSensor.CurrentTarget != null)
        {
            movement.FaceTarget(targetSensor.CurrentTarget.position);
        }

        if (meleeAttack != null && meleeAttack.IsBusy)
        {
            return;
        }

        if (targetSensor.HasTarget)
        {
            EnterChase();
            return;
        }

        EnterIdle();
    }

    private void UpdateLeapAttack()
    {
        movement.Stop();

        if (leapAttack != null && leapAttack.IsBusy)
        {
            return;
        }

        if (targetSensor.HasTarget)
        {
            EnterChase();
            return;
        }

        EnterIdle();
    }

    private void EnterIdle()
    {
        ChangeState(State.Idle);
        movement?.Stop();
    }

    private void EnterChase()
    {
        ChangeState(State.Chase);
    }

    private void EnterMeleeAttack()
    {
        ChangeState(State.MeleeAttack);
        movement?.Stop();
    }

    private void EnterLeapAttack()
    {
        ChangeState(State.LeapAttack);
        movement?.Stop();
    }

    private void EnterDead()
    {
        ChangeState(State.Dead);
        movement?.Stop();
        targetSensor?.ClearTarget();
        meleeAttack?.CancelCurrentAttack();
        leapAttack?.CancelCurrentLeap();

        if (impactLockRoutine != null)
        {
            StopCoroutine(impactLockRoutine);
            impactLockRoutine = null;
        }
    }

    private IEnumerator ImpactLockRoutine()
    {
        ChangeState(State.ImpactLocked);
        movement?.Stop();

        while (Time.time < impactLockEndsAt && currentState != State.Dead)
        {
            yield return null;
        }

        impactLockRoutine = null;

        if (currentState != State.Dead)
        {
            EnterIdle();
        }
    }

    private void HandleDied(EnemyActor deadActor)
    {
        EnterDead();
    }

    private void ChangeState(State newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        if (logStateChanges)
        {
            Debug.Log($"[{nameof(EnemyBrain)}] {name} -> {currentState}", this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        EnemyDefinition definition = Definition;
        if (definition == null && actor != null)
        {
            definition = actor.Definition;
        }

        if (definition == null)
        {
            return;
        }

        // Rojo: distancia fallback a la que deja de acercarse durante Chase.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, definition.ChaseStopDistance);
    }
}
