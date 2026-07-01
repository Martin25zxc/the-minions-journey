using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Bloque serializable de respuestas de escena para una misión.
///
/// Un bloque define:
/// - cuándo se ejecuta dentro del flujo de la misión;
/// - si debe filtrar por un objetivo específico;
/// - qué objetos activa/desactiva;
/// - qué WorldFlags setea;
/// - qué acciones custom de escena invoca.
///
/// No decide progreso de misión, no entrega recompensas y no modifica UI directamente.
/// Para comportamientos especiales, invoca componentes dedicados mediante Custom Scene Actions.
/// </summary>
[Serializable]
public sealed class MissionSceneResponseBlock
{
    [Header("Identity")]
    [Tooltip("Nombre humano del bloque para leer la secuencia de la misión en el Inspector y en logs. Ejemplos: 'Enable Mushrooms', 'Secret Boss Hint', 'Dragon Farewell'.")]
    [SerializeField]
    private string blockName = "Scene Response";

    [Tooltip("Permite desactivar temporalmente este bloque sin borrar sus referencias. Útil para pruebas A/B o para aislar problemas de escena.")]
    [SerializeField]
    private bool isEnabled = true;

    [Tooltip("Momento del flujo de la misión en el que este bloque debe ejecutarse: al aceptar, al completar un objetivo, al quedar lista para entregar o al completarse.")]
    [SerializeField]
    private MissionSceneTriggerMoment triggerMoment = MissionSceneTriggerMoment.OnAccepted;

    [Header("Objective Filter")]
    [Tooltip("Úsalo solo con Trigger Moment = OnObjectiveCompleted cuando este bloque debe reaccionar a un objetivo concreto. Si está desactivado, el bloque responde a cualquier objetivo completado de esta misión.")]
    [SerializeField]
    private bool filterByObjectiveId;

    [Tooltip("ObjectiveId exacto definido en la MissionDefinition. Solo se usa si Filter By Objective Id está activo. Ejemplo: 'acquire_hook'.")]
    [SerializeField]
    private string objectiveId;

    [Header("Scene Objects")]
    [Tooltip("GameObjects que se activarán cuando el bloque se ejecute. Útil para revelar pickups, zonas, bloqueos visuales, NPCs u objetos precolocados. No pongas aquí el mismo GameObject que contiene al MissionSceneResponder si puede desactivarse.")]
    [SerializeField]
    private GameObject[] objectsToEnable;

    [Tooltip("GameObjects que se desactivarán cuando el bloque se ejecute. Útil para limpiar pickups sobrantes, ocultar bloqueos, desactivar objetos ya usados o cambiar la puesta en escena.")]
    [SerializeField]
    private GameObject[] objectsToDisable;

    [Header("World Flags")]
    [Tooltip("Ids de WorldFlags que se setearán cuando el bloque se ejecute. Usar solo para hechos relevantes del mundo, no para estados triviales de UI o debug. Ejemplos: 'hook_received', 'nature_being_helped'.")]
    [SerializeField]
    private string[] flagsToSet;

    [Tooltip("Valor que se escribirá en todos los flags de Flags To Set. Normalmente true. Usar false solo si realmente se necesita limpiar/desactivar un hecho del mundo.")]
    [SerializeField]
    private bool flagValue = true;

    [Header("Custom Scene Actions")]
    [Tooltip("Acciones especiales de escena invocadas por este bloque. Usar para notificaciones narrativas, retirada de NPCs, activar spawners, reproducir animaciones simples, etc. Evitar poner aquí lógica de rewards o progreso de misión.")]
    [SerializeField]
    private UnityEvent customSceneActions = new UnityEvent();

    [Header("Execution")]
    [Tooltip("Si está activo, este bloque se ejecuta una sola vez por sesión/play mode. Mantener activo para evitar doble activación, doble flag o doble limpieza ante eventos duplicados.")]
    [SerializeField]
    private bool executeOnlyOnce = true;

    [Tooltip("Estado runtime visible para debug. Indica si este bloque ya ejecutó. No editar manualmente salvo pruebas puntuales. Puede resetearse desde el Context Menu del MissionSceneResponder.")]
    [SerializeField]
    private bool hasExecuted;

    public string BlockName => string.IsNullOrWhiteSpace(blockName) ? "Scene Response" : blockName;
    public bool IsEnabled => isEnabled;
    public MissionSceneTriggerMoment TriggerMoment => triggerMoment;
    public bool FilterByObjectiveId => filterByObjectiveId;
    public string ObjectiveId => objectiveId;
    public bool ExecuteOnlyOnce => executeOnlyOnce;
    public bool HasExecuted => hasExecuted;

    public void ResetExecutionState()
    {
        hasExecuted = false;
    }

    public bool CanExecute(MissionSceneTriggerMoment moment, MissionObjectiveRuntimeState objectiveState)
    {
        if (!isEnabled)
        {
            return false;
        }

        if (triggerMoment != moment)
        {
            return false;
        }

        if (executeOnlyOnce && hasExecuted)
        {
            return false;
        }

        if (!filterByObjectiveId)
        {
            return true;
        }

        if (objectiveState == null || objectiveState.Definition == null)
        {
            return false;
        }

        return string.Equals(
            objectiveState.Definition.ObjectiveId,
            objectiveId,
            StringComparison.Ordinal
        );
    }

    public void Execute(WorldFlagRegistry worldFlagRegistry, Component logContext, bool logDebug)
    {
        // Orden intencional:
        // 1) Primero activar objetos necesarios para que las acciones custom puedan operar sobre ellos.
        // 2) Luego setear flags del mundo.
        // 3) Después invocar acciones especiales.
        // 4) Finalmente desactivar objetos de limpieza.
        SetObjectsActive(objectsToEnable, true, logContext, logDebug);
        SetWorldFlags(worldFlagRegistry, logContext, logDebug);
        InvokeCustomSceneActions(logContext, logDebug);
        SetObjectsActive(objectsToDisable, false, logContext, logDebug);

        hasExecuted = true;
    }

    private void SetObjectsActive(GameObject[] targets, bool active, Component logContext, bool logDebug)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            GameObject target = targets[i];
            if (target == null)
            {
                if (logDebug)
                {
                    Debug.LogWarning($"MissionSceneResponseBlock '{BlockName}' has a null GameObject target.", logContext);
                }

                continue;
            }

            if (target.activeSelf == active)
            {
                if (logDebug)
                {
                    string stateText = active ? "enabled" : "disabled";
                    Debug.Log($"MissionSceneResponseBlock '{BlockName}' left '{target.name}' already {stateText}.", logContext);
                }

                continue;
            }

            target.SetActive(active);

            if (logDebug)
            {
                string actionText = active ? "enabled" : "disabled";
                Debug.Log($"MissionSceneResponseBlock '{BlockName}' {actionText} '{target.name}'.", logContext);
            }
        }
    }

    private void SetWorldFlags(WorldFlagRegistry worldFlagRegistry, Component logContext, bool logDebug)
    {
        if (flagsToSet == null || flagsToSet.Length == 0)
        {
            return;
        }

        if (worldFlagRegistry == null)
        {
            Debug.LogWarning($"MissionSceneResponseBlock '{BlockName}' cannot set flags because WorldFlagRegistry is not assigned.", logContext);
            return;
        }

        for (int i = 0; i < flagsToSet.Length; i++)
        {
            string flagId = flagsToSet[i];
            if (string.IsNullOrWhiteSpace(flagId))
            {
                if (logDebug)
                {
                    Debug.LogWarning($"MissionSceneResponseBlock '{BlockName}' has an empty flag id.", logContext);
                }

                continue;
            }

            worldFlagRegistry.SetFlag(flagId, flagValue);

            if (logDebug)
            {
                Debug.Log($"MissionSceneResponseBlock '{BlockName}' set world flag '{flagId}' = {flagValue}.", logContext);
            }
        }
    }

    private void InvokeCustomSceneActions(Component logContext, bool logDebug)
    {
        if (customSceneActions == null || customSceneActions.GetPersistentEventCount() == 0)
        {
            return;
        }

        if (logDebug)
        {
            Debug.Log($"MissionSceneResponseBlock '{BlockName}' invoking {customSceneActions.GetPersistentEventCount()} custom scene action(s).", logContext);
        }

        try
        {
            customSceneActions.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, logContext);
        }
    }

#if UNITY_EDITOR
    public void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(blockName))
        {
            blockName = "Scene Response";
        }

        if (filterByObjectiveId && string.IsNullOrWhiteSpace(objectiveId))
        {
            // Warning centralizado en MissionSceneResponder para tener contexto del GameObject.
        }
    }
#endif
}
