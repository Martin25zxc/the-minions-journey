using System.Collections;
using System.Collections.Generic;
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
        Engage,
        UsingAbility,
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

    [Header("Positioning")]
    [Tooltip("Componente que implementa IEnemyPositioning. Ejemplo: EnemyChasePositioning o EnemyRangedPositioning.")]
    [SerializeField]
    private MonoBehaviour positioningComponent;

    [Tooltip("Si no se asigna Positioning Component, intenta encontrar un unico componente IEnemyPositioning en este GameObject.")]
    [SerializeField]
    private bool autoFindPositioning = true;

    [Header("Abilities")]
    [Tooltip("Si esta activo, EnemyBrain busca automaticamente componentes que implementen IEnemyAbility en el mismo GameObject.")]
    [SerializeField]
    private bool autoCollectAbilities = true;

    [Tooltip("Decision inicial: un ImpactLock cancela la ability activa para evitar que dos rutinas peleen por movimiento/estado.")]
    [SerializeField]
    private bool cancelActiveAbilityOnImpactLock = true;

    [Header("Debug")]
    [SerializeField]
    private bool logStateChanges;

    [SerializeField]
    private bool logAbilitySelection;

    [SerializeField]
    private bool logConfigurationWarnings = true;

    private readonly List<IEnemyAbility> abilities = new List<IEnemyAbility>();
    private IEnemyPositioning positioning;
    private State currentState = State.Idle;
    private Coroutine impactLockRoutine;
    private Coroutine activeAbilityRoutine;
    private IEnemyAbility activeAbility;
    private float impactLockEndsAt;

    private EnemyDefinition Definition => actor != null ? actor.Definition : null;

    private void Awake()
    {
        if (actor == null) actor = GetComponent<EnemyActor>();
        if (targetSensor == null) targetSensor = GetComponent<EnemyTargetSensor>();
        if (movement == null) movement = GetComponent<EnemyMovement>();

        ResolvePositioning();

        if (autoCollectAbilities)
        {
            RefreshAbilities();
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

        if (autoCollectAbilities && abilities.Count == 0)
        {
            RefreshAbilities();
        }

        if (positioning == null && logConfigurationWarnings)
        {
            Debug.LogWarning($"[{nameof(EnemyBrain)}] {name} has no IEnemyPositioning component. Add EnemyChasePositioning for melee or EnemyRangedPositioning for ranged enemies.", this);
        }

        EnterIdle();
    }

    private void OnDisable()
    {
        if (actor != null)
        {
            actor.Died -= HandleDied;
        }

        CancelActiveAbility();
    }

    private void Update()
    {
        if (currentState == State.Dead || currentState == State.ImpactLocked || currentState == State.UsingAbility)
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

            case State.Engage:
                UpdateEngage();
                break;
        }
    }

    public void ApplyImpactLock(float duration)
    {
        if (duration <= 0f || currentState == State.Dead || actor == null || !actor.IsAlive)
        {
            return;
        }

        if (cancelActiveAbilityOnImpactLock)
        {
            CancelActiveAbility();
        }

        impactLockEndsAt = Mathf.Max(impactLockEndsAt, Time.time + duration);

        if (impactLockRoutine == null)
        {
            impactLockRoutine = StartCoroutine(ImpactLockRoutine());
        }
    }

    public void RefreshAbilities()
    {
        abilities.Clear();

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IEnemyAbility ability)
            {
                abilities.Add(ability);
            }
        }
    }

    public void ResolvePositioning()
    {
        positioning = null;

        if (positioningComponent != null)
        {
            if (positioningComponent is IEnemyPositioning explicitPositioning)
            {
                positioning = explicitPositioning;
                return;
            }

            if (logConfigurationWarnings)
            {
                Debug.LogWarning($"[{nameof(EnemyBrain)}] {name} Positioning Component does not implement IEnemyPositioning: {positioningComponent.GetType().Name}.", this);
            }
        }

        if (!autoFindPositioning)
        {
            return;
        }

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IEnemyPositioning foundPositioning)
            {
                if (positioning != null && logConfigurationWarnings)
                {
                    Debug.LogWarning($"[{nameof(EnemyBrain)}] {name} has multiple IEnemyPositioning components. Assign Positioning Component explicitly to avoid ambiguity.", this);
                    return;
                }

                positioning = foundPositioning;
            }
        }
    }

    private void UpdateIdle()
    {
        positioning?.StopPositioning();
        movement?.Stop();

        if (targetSensor.HasTarget)
        {
            EnterEngage();
        }
    }

    private void UpdateEngage()
    {
        if (!targetSensor.HasTarget || targetSensor.CurrentTarget == null)
        {
            EnterIdle();
            return;
        }

        Transform target = targetSensor.CurrentTarget;
        IEnemyAbility selectedAbility = SelectBestAbility(target);
        if (selectedAbility != null)
        {
            StartAbility(selectedAbility, target);
            return;
        }

        UpdatePositioning(target);
    }

    private IEnemyAbility SelectBestAbility(Transform target)
    {
        IEnemyAbility bestAbility = null;
        float bestPriority = float.MinValue;

        for (int i = 0; i < abilities.Count; i++)
        {
            IEnemyAbility ability = abilities[i];
            if (ability == null || ability.IsRunning || !ability.CanUse(target))
            {
                continue;
            }

            float priority = ability.GetPriority(target);
            if (priority <= bestPriority)
            {
                continue;
            }

            bestPriority = priority;
            bestAbility = ability;
        }

        return bestAbility;
    }

    private void StartAbility(IEnemyAbility ability, Transform target)
    {
        if (ability == null)
        {
            return;
        }

        ChangeState(State.UsingAbility);
        positioning?.StopPositioning();
        movement?.Stop();
        activeAbility = ability;
        activeAbilityRoutine = StartCoroutine(AbilityRoutine(ability, target));

        if (logAbilitySelection)
        {
            Debug.Log($"[{nameof(EnemyBrain)}] {name} uses {ability.GetType().Name}.", this);
        }
    }

    private IEnumerator AbilityRoutine(IEnemyAbility ability, Transform target)
    {
        yield return ability.Execute(target);

        activeAbilityRoutine = null;
        activeAbility = null;

        if (currentState == State.Dead || currentState == State.ImpactLocked)
        {
            yield break;
        }

        if (targetSensor.HasTarget)
        {
            EnterEngage();
            yield break;
        }

        EnterIdle();
    }

    private void UpdatePositioning(Transform target)
    {
        if (target == null)
        {
            positioning?.StopPositioning();
            movement?.Stop();
            return;
        }

        if (positioning == null)
        {
            movement?.Stop();
            return;
        }

        positioning.UpdatePositioning(target);
    }

    private void CancelActiveAbility()
    {
        if (activeAbility != null)
        {
            activeAbility.Cancel();
        }

        if (activeAbilityRoutine != null)
        {
            StopCoroutine(activeAbilityRoutine);
            activeAbilityRoutine = null;
        }

        for (int i = 0; i < abilities.Count; i++)
        {
            abilities[i]?.Cancel();
        }

        activeAbility = null;
    }

    private void EnterIdle()
    {
        ChangeState(State.Idle);
        positioning?.StopPositioning();
        movement?.Stop();
        movement?.ClearIdleResidualPhysics();
    }

    private void EnterEngage()
    {
        ChangeState(State.Engage);
    }

    private void EnterDead()
    {
        ChangeState(State.Dead);
        positioning?.StopPositioning();
        movement?.ApplyDeathPhysics();
        targetSensor?.ClearTarget();
        CancelActiveAbility();

        if (impactLockRoutine != null)
        {
            StopCoroutine(impactLockRoutine);
            impactLockRoutine = null;
        }
    }

    private IEnumerator ImpactLockRoutine()
    {
        ChangeState(State.ImpactLocked);
        positioning?.StopPositioning();
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
}
