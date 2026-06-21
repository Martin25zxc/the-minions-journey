using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class MissionObjectiveDefinition
{
    [Header("Identidad")]
    [SerializeField, Tooltip("ID estable del objetivo dentro de esta misión. Usar snake_case, por ejemplo: acquire_hook.")]
    private string objectiveId;

    [SerializeField, Tooltip("Qué tipo de evento del mundo puede avanzar este objetivo.")]
    private MissionObjectiveType objectiveType;

    [SerializeField, Tooltip("Required bloquea el avance de la misión. Bonus es opcional y no bloquea el completado.")]
    private ObjectiveImportance importance = ObjectiveImportance.Required;

    [Header("Texto")]
    [SerializeField, TextArea(1, 3), Tooltip("Texto corto que verá el jugador en HUD o Journal.")]
    private string description;

    [Header("Progreso")]
    [SerializeField, Min(1), Tooltip("Cantidad necesaria para completar este objetivo. Para objetivos únicos, dejar en 1.")]
    private int requiredAmount = 1;

    [SerializeField, Tooltip("ID estable del objetivo del mundo: item, zona, actor, grupo enemigo u objeto interactuable.")]
    private string targetId;

    [Header("Visibilidad")]
    [SerializeField, Tooltip("Si está activo, este objetivo no se muestra hasta que otro sistema lo habilite en runtime futuro.")]
    private bool hiddenUntilActive;

    [SerializeField, Tooltip("Si está activo, el HUD puede mostrar progreso tipo 0/3.")]
    private bool showProgress = true;

    public string ObjectiveId => objectiveId;
    public MissionObjectiveType ObjectiveType => objectiveType;
    public ObjectiveImportance Importance => importance;
    public string Description => description;
    public int RequiredAmount => requiredAmount;
    public string TargetId => targetId;
    public bool HiddenUntilActive => hiddenUntilActive;
    public bool ShowProgress => showProgress;

    internal void Validate(string missionId, int index, HashSet<string> usedObjectiveIds)
    {
        objectiveId = MissionAuthoringValidation.CleanId(objectiveId);
        targetId = MissionAuthoringValidation.CleanId(targetId);

        if (requiredAmount < 1)
        {
            requiredAmount = 1;
        }

        string objectiveContext = $"Mission '{missionId}', Objective #{index}";

        if (MissionAuthoringValidation.IsNullOrWhiteSpace(objectiveId))
        {
            Debug.LogWarning($"{objectiveContext}: falta ObjectiveId. Usá un ID estable, por ejemplo: acquire_hook.");
        }
        else
        {
            if (!MissionAuthoringValidation.IsStableId(objectiveId))
            {
                Debug.LogWarning($"{objectiveContext}: {MissionAuthoringValidation.BuildStableIdWarning(nameof(objectiveId), objectiveId)}");
            }

            if (!usedObjectiveIds.Add(objectiveId))
            {
                Debug.LogWarning($"{objectiveContext}: ObjectiveId duplicado dentro de la misión: '{objectiveId}'.");
            }
        }

        if (MissionAuthoringValidation.IsNullOrWhiteSpace(description))
        {
            Debug.LogWarning($"{objectiveContext}: falta Description. El jugador necesita entender qué hacer.");
        }

        if (RequiresTargetId(objectiveType) && MissionAuthoringValidation.IsNullOrWhiteSpace(targetId))
        {
            Debug.LogWarning($"{objectiveContext}: falta TargetId para un objetivo de tipo {objectiveType}. Esto después no va a poder matchear eventos del mundo.");
        }
        else if (!MissionAuthoringValidation.IsNullOrWhiteSpace(targetId) && !MissionAuthoringValidation.IsStableId(targetId))
        {
            Debug.LogWarning($"{objectiveContext}: {MissionAuthoringValidation.BuildStableIdWarning(nameof(targetId), targetId)}");
        }
    }

    private static bool RequiresTargetId(MissionObjectiveType type)
    {
        switch (type)
        {
            case MissionObjectiveType.ReachArea:
            case MissionObjectiveType.CollectItem:
            case MissionObjectiveType.DefeatEnemies:
            case MissionObjectiveType.InteractWithObject:
            case MissionObjectiveType.AcquireItem:
            case MissionObjectiveType.TalkToActor:
                return true;
            default:
                return false;
        }
    }
}
