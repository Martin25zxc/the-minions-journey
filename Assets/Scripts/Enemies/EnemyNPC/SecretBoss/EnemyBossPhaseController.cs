using UnityEngine;

/// <summary>
/// Controlador mínimo de fase híbrida para boss secreto.
///
/// La fase 2 puede activarse por vida del boss o por derrota de la escolta.
/// No decide ataques: solo cambia EnemyBossPhaseState. Las abilities se bloquean por fase.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyBossPhaseState))]
public sealed class EnemyBossPhaseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private EnemyBossPhaseState phaseState;

    [SerializeField]
    private EnemyActor bossActor;

    [SerializeField]
    private EnemyBossAnimatorBridge animatorBridge;

    [SerializeField]
    private EnemyBossSpecialCooldownGate specialCooldownGate;

    [Header("Retinue")]
    [Tooltip("Acompañantes que pueden disparar fase 2 al morir todos. No reviven en este scope.")]
    [SerializeField]
    private EnemyActor[] retinueActors;

    [Header("Phase 2 Conditions")]
    [SerializeField, Min(2)]
    private int phase2 = 2;

    [Tooltip("Si está activo, Phase 2 se activa al bajar de cierto porcentaje de vida.")]
    [SerializeField]
    private bool enableHealthThreshold = true;

    [SerializeField, Range(0.01f, 1f)]
    private float phase2HealthPercent = 0.55f;

    [Tooltip("Si está activo, Phase 2 se activa cuando todos los acompañantes asignados están muertos.")]
    [SerializeField]
    private bool enableAllRetinueDefeatedCondition = true;

    [Header("Transition Feedback")]
    [SerializeField]
    private bool playTransitionAnimation = true;

    [Tooltip("Bloquea especiales durante la transición. No bloquea el melee común actual.")]
    [SerializeField, Min(0f)]
    private float specialBlockDuringTransition = 1.1f;

    [Header("Debug")]
    [SerializeField]
    private bool logPhaseTriggers;

    [SerializeField]
    private int debugAliveRetinueCount;

    [SerializeField]
    private float debugBossHealthPercent;

    private bool phase2Triggered;

    public bool IsPhase2Triggered => phase2Triggered || phaseState != null && phaseState.CurrentPhase >= phase2;
    public int AliveRetinueCount => CountAliveRetinue();
    public bool AnyRetinueDead => CountDeadRetinue() > 0;
    public bool AllRetinueDefeated => HasRetinue() && CountAliveRetinue() <= 0;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        RefreshDebug();

        if (phase2Triggered || phaseState == null || phaseState.CurrentPhase >= phase2)
        {
            phase2Triggered = true;
            return;
        }

        if (bossActor == null || !bossActor.IsAlive)
        {
            return;
        }

        if (ShouldEnterPhase2())
        {
            TriggerPhase2("hybrid condition");
        }
    }

    public void ForcePhase2(string reason = "forced")
    {
        if (phaseState == null)
        {
            ResolveReferences();
        }

        TriggerPhase2(reason);
    }

    private bool ShouldEnterPhase2()
    {
        if (enableHealthThreshold && GetBossHealthPercent() <= phase2HealthPercent)
        {
            return true;
        }

        if (enableAllRetinueDefeatedCondition && AllRetinueDefeated)
        {
            return true;
        }

        return false;
    }

    private void TriggerPhase2(string reason)
    {
        if (phaseState == null)
        {
            return;
        }

        if (phase2Triggered && phaseState.CurrentPhase >= phase2)
        {
            return;
        }

        phase2Triggered = true;
        phaseState.EnsureAtLeastPhase(phase2);

        if (playTransitionAnimation)
        {
            animatorBridge?.PlayPhaseTransition();
        }

        if (specialBlockDuringTransition > 0f)
        {
            specialCooldownGate?.ForceBlock(specialBlockDuringTransition);
        }

        if (logPhaseTriggers)
        {
            Debug.Log($"[{nameof(EnemyBossPhaseController)}] {name} entered phase {phase2}. Reason: {reason}.", this);
        }
    }

    private bool HasRetinue()
    {
        return retinueActors != null && retinueActors.Length > 0;
    }

    private int CountAliveRetinue()
    {
        if (retinueActors == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < retinueActors.Length; i++)
        {
            EnemyActor actor = retinueActors[i];
            if (actor != null && actor.IsAlive)
            {
                count++;
            }
        }

        return count;
    }

    private int CountDeadRetinue()
    {
        if (retinueActors == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < retinueActors.Length; i++)
        {
            EnemyActor actor = retinueActors[i];
            if (actor != null && !actor.IsAlive)
            {
                count++;
            }
        }

        return count;
    }

    private float GetBossHealthPercent()
    {
        if (bossActor == null || bossActor.Health == null || bossActor.Health.MaxHealth <= 0f)
        {
            return 1f;
        }

        return bossActor.Health.CurrentHealth / bossActor.Health.MaxHealth;
    }

    private void ResolveReferences()
    {
        if (phaseState == null) phaseState = GetComponent<EnemyBossPhaseState>();
        if (bossActor == null) bossActor = GetComponent<EnemyActor>();
        if (animatorBridge == null) animatorBridge = GetComponent<EnemyBossAnimatorBridge>();
        if (specialCooldownGate == null) specialCooldownGate = GetComponent<EnemyBossSpecialCooldownGate>();
    }

    private void RefreshDebug()
    {
        debugAliveRetinueCount = CountAliveRetinue();
        debugBossHealthPercent = GetBossHealthPercent();
    }

    private void OnValidate()
    {
        phase2 = Mathf.Max(2, phase2);
        phase2HealthPercent = Mathf.Clamp(phase2HealthPercent, 0.01f, 1f);
        specialBlockDuringTransition = Mathf.Max(0f, specialBlockDuringTransition);
        ResolveReferences();
        RefreshDebug();
    }
}
