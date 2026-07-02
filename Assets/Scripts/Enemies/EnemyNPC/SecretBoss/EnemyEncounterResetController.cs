using UnityEngine;

/// <summary>
/// Reset anti-cheese para encuentros de boss sin arena.
///
/// Al salir de combate:
/// - Cura al 100% a los actores vivos configurados.
/// - No revive muertos.
/// - Puede forzar fase 2 si el jugador mató parte de la escolta y salió.
/// - Pide limpieza a componentes/hazards que implementen IEnemyEncounterResettable.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyEncounterResetController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerThreatTracker playerThreatTracker;

    [SerializeField]
    private EnemyBossPhaseController phaseController;

    [SerializeField]
    private EnemyBossSpecialCooldownGate specialCooldownGate;

    [Header("Actors")]
    [Tooltip("Todos los actores vivos del encounter que deben curarse al salir de combate. Incluye boss y acompañantes.")]
    [SerializeField]
    private EnemyActor[] actorsToHealOnDisengage;

    [Header("Phase Anti-Cheese")]
    [Tooltip("Si el jugador mata cualquier acompañante y sale de combate, el boss queda en fase 2 para el siguiente intento.")]
    [SerializeField]
    private bool forcePhase2IfAnyRetinueDiedOnDisengage = true;

    [Header("Resettable Components")]
    [Tooltip("Componentes extra que deben limpiar hazards temporales. Deben implementar IEnemyEncounterResettable.")]
    [SerializeField]
    private MonoBehaviour[] resettableComponents;

    [Header("Debug")]
    [SerializeField]
    private bool logResets;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (playerThreatTracker != null)
        {
            playerThreatTracker.CombatStateChanged += HandlePlayerCombatStateChanged;
        }
    }

    private void OnDisable()
    {
        if (playerThreatTracker != null)
        {
            playerThreatTracker.CombatStateChanged -= HandlePlayerCombatStateChanged;
        }
    }

    public void ResetEncounterAfterDisengage()
    {
        HealAliveActorsToFull();
        ResetTemporarySystems();
        specialCooldownGate?.Clear();

        if (forcePhase2IfAnyRetinueDiedOnDisengage && phaseController != null && phaseController.AnyRetinueDead)
        {
            phaseController.ForcePhase2("disengage after retinue casualty");
        }

        if (logResets)
        {
            Debug.Log($"[{nameof(EnemyEncounterResetController)}] {name} reset after player disengage.", this);
        }
    }

    private void HandlePlayerCombatStateChanged(bool isInCombat)
    {
        if (isInCombat)
        {
            return;
        }

        ResetEncounterAfterDisengage();
    }

    private void HealAliveActorsToFull()
    {
        if (actorsToHealOnDisengage == null)
        {
            return;
        }

        for (int i = 0; i < actorsToHealOnDisengage.Length; i++)
        {
            EnemyActor actor = actorsToHealOnDisengage[i];
            if (actor == null || actor.Health == null || !actor.IsAlive)
            {
                continue;
            }

            float missingHealth = actor.Health.MaxHealth - actor.Health.CurrentHealth;
            if (missingHealth > 0f)
            {
                actor.Health.Heal(missingHealth);
            }
        }
    }

    private void ResetTemporarySystems()
    {
        if (resettableComponents == null)
        {
            return;
        }

        for (int i = 0; i < resettableComponents.Length; i++)
        {
            MonoBehaviour behaviour = resettableComponents[i];
            if (behaviour is IEnemyEncounterResettable resettable)
            {
                resettable.ResetForEncounter();
            }
        }
    }

    private void ResolveReferences()
    {
        if (phaseController == null)
        {
            phaseController = GetComponentInChildren<EnemyBossPhaseController>();
        }

        if (specialCooldownGate == null)
        {
            specialCooldownGate = GetComponentInChildren<EnemyBossSpecialCooldownGate>();
        }
    }
}
