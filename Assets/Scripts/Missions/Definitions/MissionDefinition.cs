using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TMJ/Missions/Mission Definition", fileName = "MD_NewMission")]
public sealed class MissionDefinition : ScriptableObject
{
    [Header("Identidad")]
    [SerializeField, Tooltip("ID estable de la misión. Usar snake_case. No debería cambiar cuando ya esté usado por saves o referencias.")]
    private string missionId;

    [SerializeField, Tooltip("Main para progreso central. Optional para contenido no obligatorio.")]
    private MissionCategory category = MissionCategory.Optional;

    [SerializeField, Tooltip("Orden sugerido para mostrar en Journal o listas futuras. Menor aparece antes.")]
    private int displayOrder;

    [Header("Texto")]
    [SerializeField, Tooltip("Título visible de la misión.")]
    private string title;

    [SerializeField, TextArea(2, 4), Tooltip("Resumen corto para HUD o lista de misiones.")]
    private string shortDescription;

    [SerializeField, TextArea(4, 10), Tooltip("Descripción más completa para Journal futuro.")]
    private string fullDescription;

    [Header("Flujo")]
    [SerializeField, Tooltip("Cómo se inicia esta misión. En esta etapa solo se define el dato.")]
    private MissionStartMode startMode = MissionStartMode.OnInteraction;

    [SerializeField, Tooltip("AutoComplete completa al terminar objetivos required. RequiresTurnIn pasa a ReadyToTurnIn y pide una entrega/interacción final.")]
    private MissionCompletionMode completionMode = MissionCompletionMode.AutoComplete;

    [SerializeField, Tooltip("Solo se usa si Completion Mode es Requires Turn In. Si la misión es Auto Complete, se normaliza como None y se ignora.")]
    private MissionTurnInTargetMode turnInTargetMode = MissionTurnInTargetMode.OriginalGiver;

    [SerializeField, Tooltip("Solo se usa con Requires Turn In y target SpecificActor o SpecificWorldObject. Con OriginalGiver o AutoComplete debe quedar vacío.")]
    private string turnInTargetId;

    [SerializeField, Tooltip("Marca si esta misión debería contar para completar el nivel. Normalmente solo Main. No usar para fases internas si el nivel continúa.")]
    private bool requiredForLevelCompletion;

    [Header("Objetivos")]
    [SerializeField, Tooltip("Objetivos de autoría. No guardan progreso runtime.")]
    private MissionObjectiveDefinition[] objectives = Array.Empty<MissionObjectiveDefinition>();

    [Header("Recompensas")]
    [SerializeField, Tooltip("Recompensas declaradas. En esta etapa no se entregan todavía.")]
    private MissionRewardDefinition[] rewards = Array.Empty<MissionRewardDefinition>();

    [Header("UI")]
    [SerializeField, Tooltip("Si está activo, esta misión puede aparecer en el HUD cuando esté trackeada.")]
    private bool showInHUD = true;

    [SerializeField, Tooltip("Si está activo, esta misión puede aparecer en el Journal futuro.")]
    private bool showInJournal = true;

    [SerializeField, Tooltip("Si está activo, la misión puede trackearse automáticamente al aceptarse.")]
    private bool autoTrackOnAccept = true;

    public string MissionId => missionId;
    public MissionCategory Category => category;
    public int DisplayOrder => displayOrder;
    public string Title => title;
    public string ShortDescription => shortDescription;
    public string FullDescription => fullDescription;
    public MissionStartMode StartMode => startMode;
    public MissionCompletionMode CompletionMode => completionMode;
    public MissionTurnInTargetMode TurnInTargetMode => turnInTargetMode;
    public string TurnInTargetId => turnInTargetId;
    public bool RequiredForLevelCompletion => requiredForLevelCompletion;
    public IReadOnlyList<MissionObjectiveDefinition> Objectives => objectives ?? Array.Empty<MissionObjectiveDefinition>();
    public IReadOnlyList<MissionRewardDefinition> Rewards => rewards ?? Array.Empty<MissionRewardDefinition>();
    public bool ShowInHUD => showInHUD;
    public bool ShowInJournal => showInJournal;
    public bool AutoTrackOnAccept => autoTrackOnAccept;
    public bool RequiresTurnIn => completionMode == MissionCompletionMode.RequiresTurnIn;
    public bool UsesSpecificTurnInTarget => RequiresTurnIn &&
                                           (turnInTargetMode == MissionTurnInTargetMode.SpecificActor ||
                                            turnInTargetMode == MissionTurnInTargetMode.SpecificWorldObject);

    public bool HasRequiredObjectives
    {
        get
        {
            if (objectives == null)
            {
                return false;
            }

            for (int i = 0; i < objectives.Length; i++)
            {
                if (objectives[i] != null && objectives[i].Importance == ObjectiveImportance.Required)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void OnValidate()
    {
        missionId = MissionAuthoringValidation.CleanId(missionId);
        turnInTargetId = MissionAuthoringValidation.CleanId(turnInTargetId);

        if (objectives == null)
        {
            objectives = Array.Empty<MissionObjectiveDefinition>();
        }
        if (rewards == null)
        {
            rewards = Array.Empty<MissionRewardDefinition>();
        }

        NormalizeFlowForAuthoring();
        ValidateIdentity();
        ValidateFlow();
        ValidateObjectives();
        ValidateRewards();
    }

    private void NormalizeFlowForAuthoring()
    {
        if (completionMode == MissionCompletionMode.AutoComplete)
        {
            turnInTargetMode = MissionTurnInTargetMode.None;
            turnInTargetId = string.Empty;
            return;
        }

        if (completionMode == MissionCompletionMode.RequiresTurnIn && turnInTargetMode == MissionTurnInTargetMode.None)
        {
            turnInTargetMode = MissionTurnInTargetMode.OriginalGiver;
        }
    }

    private void ValidateIdentity()
    {
        if (MissionAuthoringValidation.IsNullOrWhiteSpace(missionId))
        {
            Debug.LogWarning($"{name}: falta MissionId. Usá un ID estable, por ejemplo: main_find_hook.", this);
        }
        else if (!MissionAuthoringValidation.IsStableId(missionId))
        {
            Debug.LogWarning($"{name}: {MissionAuthoringValidation.BuildStableIdWarning(nameof(missionId), missionId)}", this);
        }

        if (MissionAuthoringValidation.IsNullOrWhiteSpace(title))
        {
            Debug.LogWarning($"{name}: falta Title. La misión necesita un título visible.", this);
        }

        if (requiredForLevelCompletion && category != MissionCategory.Main)
        {
            Debug.LogWarning($"{name}: RequiredForLevelCompletion está activo en una misión Optional. Esto puede ser válido, pero debería ser una excepción explícita.", this);
        }
    }

    private void ValidateFlow()
    {
        if (completionMode == MissionCompletionMode.AutoComplete)
        {
            return;
        }

        if (completionMode != MissionCompletionMode.RequiresTurnIn)
        {
            return;
        }

        if (turnInTargetMode == MissionTurnInTargetMode.None)
        {
            Debug.LogWarning($"{name}: RequiresTurnIn no puede usar TurnInTargetMode None. Usá OriginalGiver, SpecificActor o SpecificWorldObject.", this);
            return;
        }

        if (turnInTargetMode == MissionTurnInTargetMode.OriginalGiver)
        {
            if (!MissionAuthoringValidation.IsNullOrWhiteSpace(turnInTargetId))
            {
                Debug.LogWarning($"{name}: TurnInTargetMode es OriginalGiver. TurnInTargetId no hace falta y puede confundir.", this);
            }

            return;
        }

        if (MissionAuthoringValidation.IsNullOrWhiteSpace(turnInTargetId))
        {
            Debug.LogWarning($"{name}: RequiresTurnIn con {turnInTargetMode} necesita TurnInTargetId.", this);
        }
        else if (!MissionAuthoringValidation.IsStableId(turnInTargetId))
        {
            Debug.LogWarning($"{name}: {MissionAuthoringValidation.BuildStableIdWarning(nameof(turnInTargetId), turnInTargetId)}", this);
        }
    }

    private void ValidateObjectives()
    {
        if (objectives.Length == 0)
        {
            Debug.LogWarning($"{name}: no tiene Objectives. Una misión sin objetivos todavía no tiene forma clara de completarse.", this);
            return;
        }

        HashSet<string> usedObjectiveIds = new HashSet<string>();

        for (int i = 0; i < objectives.Length; i++)
        {
            if (objectives[i] == null)
            {
                Debug.LogWarning($"{name}: Objective #{i} está vacío/null.", this);
                continue;
            }

            objectives[i].Validate(missionId, i, usedObjectiveIds);
        }

        if (!HasRequiredObjectives)
        {
            Debug.LogWarning($"{name}: todos los objetivos son Bonus. La misión necesita al menos un objetivo Required para completar el flujo base.", this);
        }
    }

    private void ValidateRewards()
    {
        for (int i = 0; i < rewards.Length; i++)
        {
            if (rewards[i] == null)
            {
                Debug.LogWarning($"{name}: Reward #{i} está vacío/null.", this);
                continue;
            }

            rewards[i].Validate(missionId, i);
        }
    }
}
