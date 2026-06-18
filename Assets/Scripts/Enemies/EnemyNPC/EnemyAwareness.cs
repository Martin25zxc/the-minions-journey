using System;
using UnityEngine;

/// <summary>
/// Memoria tactica del enemigo.
///
/// Responsabilidad:
/// - Recibir estimulos de vision, daño, alertas grupales o eventos futuros.
/// - Guardar target actual de combate.
/// - Guardar ultima posicion conocida / punto a investigar.
///
/// No mueve, no ataca, no reproduce animaciones y no calcula paths.
/// EnemyBrain consulta este componente para decidir el estado siguiente.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyAwareness : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private EnemyActor actor;

    [Header("Memory")]
    [Tooltip("Si esta activo, la memoria de target visual usa EnemyDefinition.TargetMemoryDuration.")]
    [SerializeField]
    private bool useDefinitionTargetMemory = true;

    [Tooltip("Duracion fallback de memoria de target si no hay definition o si Use Definition Target Memory esta apagado.")]
    [SerializeField, Min(0f)]
    private float combatTargetMemoryDuration = 1.5f;

    [Tooltip("Cuanto tiempo se conserva un punto sospechoso para investigar cuando no hay target claro.")]
    [SerializeField, Min(0f)]
    private float investigationMemoryDuration = 4f;

    [Header("Debug")]
    [SerializeField]
    private bool logStimuli;

    [SerializeField]
    private bool logMemoryChanges;

    private Transform combatTarget;
    private float combatTargetExpiresAt;

    private Vector3 lastKnownTargetPosition;
    private bool hasLastKnownTargetPosition;

    private Vector3 investigationPoint;
    private bool hasInvestigationPoint;
    private float investigationExpiresAt;

    public event Action<EnemyAwareness, EnemyStimulus> StimulusReceived;
    public event Action<EnemyAwareness, Transform> CombatTargetChanged;
    public event Action<EnemyAwareness, Vector3> InvestigationPointChanged;

    public Transform CombatTarget => combatTarget;
    public bool HasCombatTarget => combatTarget != null;

    public Vector3 LastKnownTargetPosition => lastKnownTargetPosition;
    public bool HasLastKnownTargetPosition => hasLastKnownTargetPosition;

    public Vector3 InvestigationPoint => investigationPoint;
    public bool HasInvestigationPoint => hasInvestigationPoint;

    private float EffectiveCombatMemoryDuration
    {
        get
        {
            EnemyDefinition definition = actor != null ? actor.Definition : null;
            if (useDefinitionTargetMemory && definition != null)
            {
                return definition.TargetMemoryDuration;
            }

            return combatTargetMemoryDuration;
        }
    }

    private void Awake()
    {
        if (actor == null)
        {
            actor = GetComponent<EnemyActor>();
        }
    }

    public void Initialize(EnemyDefinition definition)
    {
        if (useDefinitionTargetMemory && definition != null)
        {
            combatTargetMemoryDuration = definition.TargetMemoryDuration;
        }
    }

    public void Tick()
    {
        if (actor != null && !actor.IsAlive)
        {
            ClearAll();
            return;
        }

        UpdateCombatTargetLifetime();
        UpdateInvestigationLifetime();
    }

    public void ReportVisibleTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        ReceiveStimulus(EnemyStimulus.Sight(target, EffectiveCombatMemoryDuration));
    }

    public void ReceiveStimulus(EnemyStimulus stimulus)
    {
        if (actor != null && !actor.IsAlive)
        {
            return;
        }

        if (logStimuli)
        {
            Debug.Log($"[{nameof(EnemyAwareness)}] {name} received {stimulus.Type}. Target: {stimulus.Target}, HasPosition: {stimulus.HasPosition}", this);
        }

        StimulusReceived?.Invoke(this, stimulus);

        if (stimulus.HasTarget)
        {
            float duration = stimulus.MemoryDuration > 0f
                ? stimulus.MemoryDuration
                : EffectiveCombatMemoryDuration;

            SetCombatTarget(stimulus.Target, stimulus.HasPosition ? stimulus.Position : stimulus.Target.position, duration);
            return;
        }

        if (stimulus.HasPosition)
        {
            float duration = stimulus.MemoryDuration > 0f
                ? stimulus.MemoryDuration
                : investigationMemoryDuration;

            SetInvestigationPoint(stimulus.Position, duration);
        }
    }

    public void ClearCombatTarget(bool convertLastKnownToInvestigation)
    {
        if (convertLastKnownToInvestigation && hasLastKnownTargetPosition)
        {
            SetInvestigationPoint(lastKnownTargetPosition, investigationMemoryDuration);
        }

        if (combatTarget != null && logMemoryChanges)
        {
            Debug.Log($"[{nameof(EnemyAwareness)}] {name} cleared combat target.", this);
        }

        combatTarget = null;
        combatTargetExpiresAt = 0f;
        CombatTargetChanged?.Invoke(this, null);
    }

    public void ClearInvestigation()
    {
        if (!hasInvestigationPoint)
        {
            return;
        }

        hasInvestigationPoint = false;
        investigationExpiresAt = 0f;
        InvestigationPointChanged?.Invoke(this, Vector3.zero);

        if (logMemoryChanges)
        {
            Debug.Log($"[{nameof(EnemyAwareness)}] {name} cleared investigation point.", this);
        }
    }

    public void ClearAll()
    {
        bool hadCombatTarget = combatTarget != null;
        bool hadInvestigation = hasInvestigationPoint;

        combatTarget = null;
        combatTargetExpiresAt = 0f;

        hasLastKnownTargetPosition = false;
        lastKnownTargetPosition = Vector3.zero;

        hasInvestigationPoint = false;
        investigationPoint = Vector3.zero;
        investigationExpiresAt = 0f;

        if (hadCombatTarget)
        {
            CombatTargetChanged?.Invoke(this, null);
        }

        if (hadInvestigation)
        {
            InvestigationPointChanged?.Invoke(this, Vector3.zero);
        }
    }

    private void SetCombatTarget(Transform target, Vector3 knownPosition, float memoryDuration)
    {
        if (target == null)
        {
            return;
        }

        bool changedTarget = combatTarget != target;
        combatTarget = target;
        combatTargetExpiresAt = Time.time + Mathf.Max(0.05f, memoryDuration);

        lastKnownTargetPosition = knownPosition;
        hasLastKnownTargetPosition = true;

        ClearInvestigation();

        if (changedTarget)
        {
            CombatTargetChanged?.Invoke(this, combatTarget);
        }

        if (logMemoryChanges)
        {
            Debug.Log($"[{nameof(EnemyAwareness)}] {name} combat target = {combatTarget.name}.", this);
        }
    }

    private void SetInvestigationPoint(Vector3 position, float memoryDuration)
    {
        investigationPoint = position;
        hasInvestigationPoint = true;
        investigationExpiresAt = Time.time + Mathf.Max(0.05f, memoryDuration);
        InvestigationPointChanged?.Invoke(this, investigationPoint);

        if (logMemoryChanges)
        {
            Debug.Log($"[{nameof(EnemyAwareness)}] {name} investigation point = {investigationPoint}.", this);
        }
    }

    private void UpdateCombatTargetLifetime()
    {
        if (combatTarget == null)
        {
            return;
        }

        if (Time.time <= combatTargetExpiresAt)
        {
            return;
        }

        ClearCombatTarget(convertLastKnownToInvestigation: true);
    }

    private void UpdateInvestigationLifetime()
    {
        if (!hasInvestigationPoint)
        {
            return;
        }

        if (Time.time <= investigationExpiresAt)
        {
            return;
        }

        ClearInvestigation();
    }

    private void OnValidate()
    {
        combatTargetMemoryDuration = Mathf.Max(0f, combatTargetMemoryDuration);
        investigationMemoryDuration = Mathf.Max(0f, investigationMemoryDuration);
    }
}
