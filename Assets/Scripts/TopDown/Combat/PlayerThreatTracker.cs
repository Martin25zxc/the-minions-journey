using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estado agregado de amenaza/combat del jugador.
///
/// Responsabilidad:
/// - No detecta enemigos por su cuenta.
/// - No consulta EnemyBrain, paths ni sensores.
/// - Solo agrega fuentes que otros sistemas registran/desregistran.
///
/// Fuente recomendada actual:
/// EnemyAwareness.CombatTargetChanged -> EnemyPlayerCombatReporter -> PlayerThreatTracker.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerThreatTracker : MonoBehaviour
{
    [Header("Salida de combate")]
    [Tooltip("Segundos que espera antes de salir de combate cuando ya no hay enemigos con aggro. Evita parpadeos raros.")]
    [SerializeField, Min(0f)] private float exitCombatDelay = 2.5f;

    [Header("Seguridad")]
    [Tooltip("Si un enemigo se destruye sin avisar, esto limpia referencias rotas para no dejar al jugador pegado en combate.")]
    [SerializeField] private bool pruneDestroyedSourcesEveryFrame = true;

    [Header("Debug")]
    [Tooltip("Activalo solo mientras probamos. Después conviene apagarlo.")]
    [SerializeField] private bool logChanges;

    [Tooltip("Solo lectura conceptual en Play Mode. Ayuda a validar qué enemigos están manteniendo al jugador en combate.")]
    [SerializeField] private List<string> debugAggroSourceNames = new();

    private readonly HashSet<UnityEngine.Object> aggroSources = new();

    private Coroutine exitCombatRoutine;
    private bool isInCombat;

    public bool IsInCombat => isInCombat;
    public int AggroEnemyCount => aggroSources.Count;

    public event Action<bool> CombatStateChanged;

    private void Update()
    {
        if (pruneDestroyedSourcesEveryFrame)
        {
            PruneDestroyedSources();
        }
    }

    public void RegisterAggroSource(UnityEngine.Object source)
    {
        if (source == null)
        {
            Debug.LogWarning($"{nameof(PlayerThreatTracker)} recibió una fuente de aggro vacía.", this);
            return;
        }

        if (!aggroSources.Add(source))
        {
            return;
        }

        RefreshDebugAggroSources();
        StopExitCombatRoutineIfNeeded();
        SetCombatState(true);
    }

    public void UnregisterAggroSource(UnityEngine.Object source)
    {
        if (source == null)
        {
            return;
        }

        if (!aggroSources.Remove(source))
        {
            return;
        }

        RefreshDebugAggroSources();
        ScheduleExitCombatIfNoSources();
    }

    public void ClearAllAggroSources()
    {
        aggroSources.Clear();
        RefreshDebugAggroSources();
        StopExitCombatRoutineIfNeeded();
        SetCombatState(false);
    }

    private void PruneDestroyedSources()
    {
        if (aggroSources.Count == 0)
        {
            return;
        }

        bool removedAny = aggroSources.RemoveWhere(source => source == null) > 0;

        if (!removedAny)
        {
            return;
        }

        RefreshDebugAggroSources();
        ScheduleExitCombatIfNoSources();
    }

    private void ScheduleExitCombatIfNoSources()
    {
        if (aggroSources.Count > 0)
        {
            return;
        }

        StopExitCombatRoutineIfNeeded();
        exitCombatRoutine = StartCoroutine(ExitCombatAfterDelay());
    }

    private IEnumerator ExitCombatAfterDelay()
    {
        yield return new WaitForSeconds(exitCombatDelay);

        exitCombatRoutine = null;

        if (aggroSources.Count == 0)
        {
            SetCombatState(false);
        }
    }

    private void StopExitCombatRoutineIfNeeded()
    {
        if (exitCombatRoutine == null)
        {
            return;
        }

        StopCoroutine(exitCombatRoutine);
        exitCombatRoutine = null;
    }

    private void SetCombatState(bool value)
    {
        if (isInCombat == value)
        {
            return;
        }

        isInCombat = value;

        if (logChanges)
        {
            Debug.Log($"Estado de combate del jugador: {isInCombat}. Fuentes: {AggroEnemyCount}", this);
        }

        CombatStateChanged?.Invoke(isInCombat);
    }

    private void RefreshDebugAggroSources()
    {
        debugAggroSourceNames.Clear();

        foreach (UnityEngine.Object source in aggroSources)
        {
            if (source == null)
            {
                continue;
            }

            debugAggroSourceNames.Add(source.name);
        }
    }

    private void OnDisable()
    {
        aggroSources.Clear();
        RefreshDebugAggroSources();
        StopExitCombatRoutineIfNeeded();
        SetCombatState(false);
    }
}
