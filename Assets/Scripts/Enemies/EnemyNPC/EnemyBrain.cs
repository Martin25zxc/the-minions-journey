using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
[RequireComponent(typeof(EnemyTargetSensor))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyAwareness))]
public sealed class EnemyBrain : MonoBehaviour, IImpactLockable
{
    private enum State
    {
        Duty,
        Investigate,
        Combat,
        UsingAbility,
        ImpactLocked,
        ReturnToDuty,
        Dead
    }

    [Header("References")]
    [SerializeField]
    private EnemyActor actor;

    [SerializeField]
    private EnemyTargetSensor targetSensor;

    [SerializeField]
    private EnemyAwareness awareness;

    [SerializeField]
    private EnemyMovement movement;

    [Header("Duty - Fuera de combate")]
    [Tooltip("Controla la rutina fuera de combate: Guard o Patrol. Si queda vacio, se intenta buscar en el mismo GameObject.")]
    [SerializeField]
    private EnemyDutyController dutyController;

    [Header("Positioning - Combate")]
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

    [Header("Investigation")]
    [SerializeField, Min(0.05f)]
    private float investigationStopDistance = 1f;

    [SerializeField, Min(0f)]
    private float investigationSpeedMultiplier = 1f;

    [Tooltip("Cuando llega al punto sospechoso, mira hacia ese punto antes de volver a Duty.")]
    [SerializeField]
    private bool faceInvestigationPointOnArrival = true;

    [Header("Debug")]
    [SerializeField]
    private bool logStateChanges;

    [SerializeField]
    private bool logAbilitySelection;

    [SerializeField]
    private bool logConfigurationWarnings = true;

    [Header("Runtime Debug - Solo lectura conceptual")]
    [Tooltip("Estado actual de runtime. Usar para inspeccionar en Play Mode; no editar manualmente.")]
    [SerializeField]
    private string debugCurrentState = State.Duty.ToString();

    [Tooltip("Rutina fuera de combate activa segun EnemyDutyController.")]
    [SerializeField]
    private string debugDutyMode = "None";

    [Tooltip("Indica si EnemyDutyController tiene un destino activo de guardia/patrol.")]
    [SerializeField]
    private bool debugHasDutyDestination;

    [SerializeField]
    private Vector3 debugDutyDestination;

    [Tooltip("Target actual segun EnemyAwareness. Usar para inspeccionar en Play Mode; no editar manualmente.")]
    [SerializeField]
    private Transform debugCombatTarget;

    [Tooltip("Indica si EnemyAwareness tiene un punto de investigacion activo.")]
    [SerializeField]
    private bool debugHasInvestigationPoint;

    [Tooltip("Punto de investigacion actual, si existe.")]
    [SerializeField]
    private Vector3 debugInvestigationPoint;

    private readonly List<IEnemyAbility> abilities = new List<IEnemyAbility>();
    private IEnemyPositioning positioning;
    private State currentState = State.Duty;
    private Coroutine impactLockRoutine;
    private Coroutine activeAbilityRoutine;
    private IEnemyAbility activeAbility;
    private float impactLockEndsAt;

    private EnemyDefinition Definition => actor != null ? actor.Definition : null;

    private void Awake()
    {
        if (actor == null) actor = GetComponent<EnemyActor>();
        if (targetSensor == null) targetSensor = GetComponent<EnemyTargetSensor>();
        if (awareness == null) awareness = GetComponent<EnemyAwareness>();
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (dutyController == null) dutyController = GetComponent<EnemyDutyController>();

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
        awareness?.Initialize(definition);
        movement?.Initialize(definition);
        dutyController?.Initialize(actor, movement);

        if (autoCollectAbilities && abilities.Count == 0)
        {
            RefreshAbilities();
        }

        if (dutyController == null && logConfigurationWarnings)
        {
            Debug.LogWarning($"[{nameof(EnemyBrain)}] {name} has no EnemyDutyController. Add one to configure Guard or Patrol outside combat.", this);
        }

        if (positioning == null && logConfigurationWarnings)
        {
            Debug.LogWarning($"[{nameof(EnemyBrain)}] {name} has no IEnemyPositioning component. Add EnemyChasePositioning for melee or EnemyRangedPositioning for ranged enemies.", this);
        }

        EnterDuty();
    }

    private void OnDisable()
    {
        if (actor != null)
        {
            actor.Died -= HandleDied;
        }

        CancelActiveAbility();
        dutyController?.StopDuty();
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

        TickPerception();

        switch (currentState)
        {
            case State.Duty:
                UpdateDuty();
                break;

            case State.Investigate:
                UpdateInvestigate();
                break;

            case State.Combat:
                UpdateCombat();
                break;

            case State.ReturnToDuty:
                UpdateReturnToDuty();
                break;
        }

        RefreshDebugSnapshot();
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

    public void ReceiveStimulus(EnemyStimulus stimulus)
    {
        awareness?.ReceiveStimulus(stimulus);

        if (currentState == State.Dead || currentState == State.ImpactLocked || currentState == State.UsingAbility)
        {
            return;
        }

        EvaluateNextState();
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

    private void TickPerception()
    {
        targetSensor?.Tick();

        if (targetSensor != null && targetSensor.HasVisibleTarget && targetSensor.CurrentTarget != null)
        {
            awareness?.ReportVisibleTarget(targetSensor.CurrentTarget);
        }

        awareness?.Tick();
        RefreshDebugSnapshot();
    }

    private void UpdateDuty()
    {
        positioning?.StopPositioning();

        if (awareness != null && awareness.HasCombatTarget)
        {
            EnterCombat();
            return;
        }

        if (awareness != null && awareness.HasInvestigationPoint)
        {
            EnterInvestigate();
            return;
        }

        if (dutyController == null)
        {
            movement?.Stop();
            movement?.ClearIdleResidualPhysics();
            return;
        }

        dutyController.TickDuty();
    }

    private void UpdateInvestigate()
    {
        if (awareness != null && awareness.HasCombatTarget)
        {
            EnterCombat();
            return;
        }

        if (awareness == null || !awareness.HasInvestigationPoint)
        {
            EnterReturnToDuty();
            return;
        }

        Vector3 targetPoint = awareness.InvestigationPoint;
        if (HasReachedHorizontalPoint(targetPoint, investigationStopDistance))
        {
            movement?.Stop();

            if (faceInvestigationPointOnArrival)
            {
                movement?.FaceTarget(targetPoint);
            }

            awareness.ClearInvestigation();
            EnterReturnToDuty();
            return;
        }

        movement?.MoveTowards(targetPoint, investigationStopDistance, investigationSpeedMultiplier);
    }

    private void UpdateCombat()
    {
        Transform target = awareness != null ? awareness.CombatTarget : null;
        if (target == null)
        {
            EvaluateNextState();
            return;
        }

        IEnemyAbility selectedAbility = SelectBestAbility(target);
        if (selectedAbility != null)
        {
            StartAbility(selectedAbility, target);
            return;
        }

        UpdatePositioning(target);
    }

    private void UpdateReturnToDuty()
    {
        if (awareness != null && awareness.HasCombatTarget)
        {
            EnterCombat();
            return;
        }

        if (awareness != null && awareness.HasInvestigationPoint)
        {
            EnterInvestigate();
            return;
        }

        if (dutyController == null)
        {
            EnterDuty();
            return;
        }

        dutyController.TickReturnToDuty();

        if (dutyController.HasReturnedToDuty)
        {
            EnterDuty();
        }
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

        EvaluateNextState();
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

    private void EvaluateNextState()
    {
        if (currentState == State.Dead)
        {
            return;
        }

        if (actor == null || !actor.IsAlive)
        {
            EnterDead();
            return;
        }

        awareness?.Tick();

        if (awareness != null && awareness.HasCombatTarget)
        {
            EnterCombat();
            return;
        }

        if (awareness != null && awareness.HasInvestigationPoint)
        {
            EnterInvestigate();
            return;
        }

        if (ShouldReturnToDuty())
        {
            EnterReturnToDuty();
            return;
        }

        EnterDuty();
    }

    private bool ShouldReturnToDuty()
    {
        if (dutyController == null)
        {
            return false;
        }

        if (currentState == State.Duty)
        {
            return false;
        }

        return !dutyController.HasReturnedToDuty;
    }

    private void EnterDuty()
    {
        ChangeState(State.Duty);
        positioning?.StopPositioning();

        if (dutyController != null)
        {
            dutyController.EnterDuty();
        }
        else
        {
            movement?.Stop();
            movement?.ClearIdleResidualPhysics();
        }
    }

    private void EnterInvestigate()
    {
        ChangeState(State.Investigate);
        positioning?.StopPositioning();
        dutyController?.ExitDuty();
    }

    private void EnterCombat()
    {
        ChangeState(State.Combat);
        dutyController?.ExitDuty();
    }

    private void EnterReturnToDuty()
    {
        ChangeState(State.ReturnToDuty);
        positioning?.StopPositioning();
        movement?.Stop();
        dutyController?.EnterReturnToDuty();

        if (dutyController == null)
        {
            movement?.ClearIdleResidualPhysics();
        }
    }

    private void EnterDead()
    {
        ChangeState(State.Dead);
        positioning?.StopPositioning();
        movement?.ApplyDeathPhysics();
        awareness?.ClearAll();
        targetSensor?.ClearTarget();
        CancelActiveAbility();
        dutyController?.StopDuty();

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
        dutyController?.StopDuty();
        movement?.Stop();

        while (Time.time < impactLockEndsAt && currentState != State.Dead)
        {
            yield return null;
        }

        impactLockRoutine = null;

        if (currentState != State.Dead)
        {
            EvaluateNextState();
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
            RefreshDebugSnapshot();
            return;
        }

        currentState = newState;
        RefreshDebugSnapshot();

        if (logStateChanges)
        {
            Debug.Log($"[{nameof(EnemyBrain)}] {name} -> {currentState}", this);
        }
    }

    private void RefreshDebugSnapshot()
    {
        debugCurrentState = currentState.ToString();
        debugDutyMode = dutyController != null ? dutyController.DebugLabel : "None";
        debugHasDutyDestination = dutyController != null && dutyController.HasActiveDestination;
        debugDutyDestination = debugHasDutyDestination && dutyController != null
            ? dutyController.ActiveDestination
            : Vector3.zero;
        debugCombatTarget = awareness != null ? awareness.CombatTarget : null;
        debugHasInvestigationPoint = awareness != null && awareness.HasInvestigationPoint;
        debugInvestigationPoint = debugHasInvestigationPoint && awareness != null
            ? awareness.InvestigationPoint
            : Vector3.zero;
    }

    private bool HasReachedHorizontalPoint(Vector3 point, float distance)
    {
        Vector3 current = transform.position;
        current.y = 0f;
        point.y = 0f;
        return (current - point).sqrMagnitude <= distance * distance;
    }

    private void OnValidate()
    {
        investigationStopDistance = Mathf.Max(0.05f, investigationStopDistance);
        investigationSpeedMultiplier = Mathf.Max(0f, investigationSpeedMultiplier);
    }
}
