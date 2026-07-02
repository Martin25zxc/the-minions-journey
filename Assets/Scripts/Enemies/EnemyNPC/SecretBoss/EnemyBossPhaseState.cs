using System;
using UnityEngine;

/// <summary>
/// Estado simple de fase para bosses/enemigos especiales.
///
/// Responsabilidad:
/// - Guardar la fase actual.
/// - Exponer eventos cuando cambia.
/// - Permitir que las abilities se bloqueen por fase sin que EnemyBrain conozca bosses.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyBossPhaseState : MonoBehaviour
{
    [Header("Phase")]
    [SerializeField, Min(1)]
    private int startingPhase = 1;

    [SerializeField, Min(1)]
    private int currentPhase = 1;

    [Header("Debug")]
    [SerializeField]
    private bool logPhaseChanges;

    public event Action<int, int> PhaseChanged;

    public int CurrentPhase => currentPhase;

    private void Awake()
    {
        currentPhase = Mathf.Max(1, startingPhase);
    }

    public bool IsAtLeast(int phase)
    {
        return currentPhase >= Mathf.Max(1, phase);
    }

    public bool IsInsideRange(int minPhase, int maxPhase)
    {
        minPhase = Mathf.Max(1, minPhase);
        maxPhase = Mathf.Max(minPhase, maxPhase);
        return currentPhase >= minPhase && currentPhase <= maxPhase;
    }

    public void SetPhase(int newPhase)
    {
        newPhase = Mathf.Max(1, newPhase);
        if (currentPhase == newPhase)
        {
            return;
        }

        int previousPhase = currentPhase;
        currentPhase = newPhase;

        if (logPhaseChanges)
        {
            Debug.Log($"[{nameof(EnemyBossPhaseState)}] {name} phase {previousPhase} -> {currentPhase}.", this);
        }

        PhaseChanged?.Invoke(previousPhase, currentPhase);
    }

    public void EnsureAtLeastPhase(int phase)
    {
        if (currentPhase < phase)
        {
            SetPhase(phase);
        }
    }

    private void OnValidate()
    {
        startingPhase = Mathf.Max(1, startingPhase);
        currentPhase = Mathf.Max(1, currentPhase);
    }
}
