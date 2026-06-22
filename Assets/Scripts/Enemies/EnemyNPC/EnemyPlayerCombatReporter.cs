using UnityEngine;

/// <summary>
/// Puente entre la memoria tactica del enemigo y el estado agregado de combate del jugador.
///
/// Responsabilidad:
/// - Escuchar EnemyAwareness.CombatTargetChanged.
/// - Registrar este enemigo como fuente de aggro en el PlayerThreatTracker del target.
/// - Desregistrar cuando pierde target, muere o se desactiva.
///
/// No decide IA, no mueve, no ataca, no abre UI y no modifica misiones.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
[RequireComponent(typeof(EnemyAwareness))]
public sealed class EnemyPlayerCombatReporter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyActor actor;
    [SerializeField] private EnemyAwareness awareness;

    [Header("Behaviour")]
    [Tooltip("Si esta activo, al habilitar el componente sincroniza el target actual de EnemyAwareness por si ya existia.")]
    [SerializeField] private bool syncCurrentTargetOnEnable = true;

    [Tooltip("Si esta activo, busca PlayerThreatTracker en el target y tambien en sus padres. Recomendado si el target real puede ser un hijo del Player.")]
    [SerializeField] private bool searchPlayerStateInParents = true;

    [Header("Debug")]
    [SerializeField] private bool logChanges;

    [Tooltip("Solo lectura conceptual en Play Mode.")]
    [SerializeField] private PlayerThreatTracker debugRegisteredPlayerState;

    [Tooltip("Solo lectura conceptual en Play Mode.")]
    [SerializeField] private Transform debugRegisteredTarget;

    private PlayerThreatTracker registeredPlayerState;
    private Transform registeredTarget;
    private UnityEngine.Object AggroSource => actor != null ? actor : this;

    private void Reset()
    {
        actor = GetComponent<EnemyActor>();
        awareness = GetComponent<EnemyAwareness>();
    }

    private void Awake()
    {
        if (actor == null)
        {
            actor = GetComponent<EnemyActor>();
        }

        if (awareness == null)
        {
            awareness = GetComponent<EnemyAwareness>();
        }
    }

    private void OnEnable()
    {
        if (awareness != null)
        {
            awareness.CombatTargetChanged += HandleCombatTargetChanged;
        }

        if (actor != null)
        {
            actor.Died += HandleActorDied;
        }

        if (syncCurrentTargetOnEnable && awareness != null && awareness.CombatTarget != null)
        {
            RegisterForTarget(awareness.CombatTarget);
        }
    }

    private void OnDisable()
    {
        UnregisterCurrentTarget();

        if (awareness != null)
        {
            awareness.CombatTargetChanged -= HandleCombatTargetChanged;
        }

        if (actor != null)
        {
            actor.Died -= HandleActorDied;
        }
    }

    private void HandleCombatTargetChanged(EnemyAwareness sourceAwareness, Transform newTarget)
    {
        RegisterForTarget(newTarget);
    }

    private void HandleActorDied(EnemyActor deadActor)
    {
        UnregisterCurrentTarget();
    }

    private void RegisterForTarget(Transform newTarget)
    {
        if (newTarget == registeredTarget)
        {
            return;
        }

        UnregisterCurrentTarget();

        if (newTarget == null)
        {
            return;
        }

        PlayerThreatTracker playerThreatTracker = ResolvePlayerThreatTracker(newTarget);
        if (playerThreatTracker == null)
        {
            return;
        }

        registeredTarget = newTarget;
        registeredPlayerState = playerThreatTracker;
        registeredPlayerState.RegisterAggroSource(AggroSource);
        RefreshDebugState();

        if (logChanges)
        {
            Debug.Log($"[{nameof(EnemyPlayerCombatReporter)}] {name} registered aggro against {registeredPlayerState.name}.", this);
        }
    }

    private void UnregisterCurrentTarget()
    {
        if (registeredPlayerState != null)
        {
            registeredPlayerState.UnregisterAggroSource(AggroSource);

            if (logChanges)
            {
                Debug.Log($"[{nameof(EnemyPlayerCombatReporter)}] {name} unregistered aggro.", this);
            }
        }

        registeredPlayerState = null;
        registeredTarget = null;
        RefreshDebugState();
    }

    private PlayerThreatTracker ResolvePlayerThreatTracker(Transform target)
    {
        if (target == null)
        {
            return null;
        }

        if (searchPlayerStateInParents)
        {
            return target.GetComponentInParent<PlayerThreatTracker>();
        }

        return target.GetComponent<PlayerThreatTracker>();
    }

    private void RefreshDebugState()
    {
        debugRegisteredPlayerState = registeredPlayerState;
        debugRegisteredTarget = registeredTarget;
    }
}
