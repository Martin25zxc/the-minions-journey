using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vista visual del Journal.
/// 
/// Responsabilidad:
/// - Renderizar lista de misiones.
/// - Renderizar detalle de la misión seleccionada.
/// - Renderizar objetivos.
/// - Renderizar bloque de recompensas.
/// 
/// No responsabilidad:
/// - No cambia estados de misión directamente.
/// - No aplica recompensas.
/// - No decide si se puede abrir el Journal.
/// - No maneja input global.
/// </summary>
[DisallowMultipleComponent]
public sealed class MissionJournalView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField, Tooltip("CanvasGroup del MissionJournalRoot.")]
    private CanvasGroup rootCanvasGroup;

    [Header("Lista")]
    [SerializeField, Tooltip("Content del MissionListScrollRect/Viewport/Content.")]
    private RectTransform missionListContent;

    [SerializeField, Tooltip("Prefab de fila de misión para la lista izquierda.")]
    private MissionJournalListRowUI missionListRowPrefab;

    [SerializeField, Tooltip("Texto opcional mostrado si no hay misiones visibles.")]
    private TMP_Text emptyListText;

    [Header("Detalle")]
    [SerializeField] private TMP_Text detailTitleText;
    [SerializeField] private TMP_Text detailStatusText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Objetivos")]
    [SerializeField, Tooltip("Contenedor donde se instancian filas de objetivos.")]
    private RectTransform objectiveRowsContainer;

    [SerializeField, Tooltip("Prefab de fila de objetivo específico del Journal.")]
    private MissionJournalObjectiveRowUI journalObjectiveRowPrefab;

    [Header("Recompensas")]
    [SerializeField, Tooltip("Texto placeholder cuando no hay recompensas visibles.")]
    private TMP_Text rewardsPlaceholderText;

    [SerializeField, Tooltip("Texto usado cuando una misión no tiene recompensas visibles.")]
    private string noRewardsText = "Sin recompensas visibles.";

    [Header("Botones")]
    [SerializeField] private Button trackButton;
    [SerializeField] private TMP_Text trackButtonText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button headerCloseButton;

    [Header("Reglas de visualización")]
    [SerializeField, Tooltip("Si está apagado, las misiones Inactive no aparecen para evitar spoilers.")]
    private bool showInactiveMissions;

    [SerializeField, Tooltip("Si está activo, las misiones Completed siguen apareciendo en el Journal.")]
    private bool showCompletedMissions = true;

    [SerializeField, Tooltip("Si está activo, se muestran objetivos Bonus.")]
    private bool showBonusObjectives = true;

    [Header("Debug")]
    [SerializeField] private bool logMissingReferences = true;

    private readonly List<MissionJournalListRowUI> listRows = new();
    private readonly List<MissionJournalObjectiveRowUI> objectiveRows = new();
    private readonly List<MissionRuntimeState> visibleMissions = new();

    private MissionRuntimeState selectedMission;

    public event Action<MissionRuntimeState> TrackRequested;
    public event Action CloseRequested;

    private void Reset()
    {
        rootCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (trackButton != null)
        {
            trackButton.onClick.AddListener(HandleTrackClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        if (headerCloseButton != null)
        {
            headerCloseButton.onClick.AddListener(HandleCloseClicked);
        }
    }

    private void OnDestroy()
    {
        if (trackButton != null)
        {
            trackButton.onClick.RemoveListener(HandleTrackClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        if (headerCloseButton != null)
        {
            headerCloseButton.onClick.RemoveListener(HandleCloseClicked);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (rootCanvasGroup == null)
        {
            return;
        }

        rootCanvasGroup.alpha = 1f;
        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;
    }

    public void HideInstant()
    {
        if (rootCanvasGroup == null)
        {
            return;
        }

        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;
    }

    public void Render(IReadOnlyList<MissionRuntimeState> missions, MissionRuntimeState trackedMission)
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        BuildVisibleMissionList(missions);
        ResolveSelectedMission(trackedMission);

        RenderMissionList();
        RenderSelectedMission();
    }

    public void SelectMission(MissionRuntimeState missionState)
    {
        if (missionState == selectedMission)
        {
            return;
        }

        selectedMission = missionState;
        RenderMissionList();
        RenderSelectedMission();
    }

    private void BuildVisibleMissionList(IReadOnlyList<MissionRuntimeState> missions)
    {
        visibleMissions.Clear();

        if (missions == null)
        {
            return;
        }

        for (int i = 0; i < missions.Count; i++)
        {
            MissionRuntimeState missionState = missions[i];

            if (!ShouldShowMission(missionState))
            {
                continue;
            }

            visibleMissions.Add(missionState);
        }

        visibleMissions.Sort(CompareMissionsForJournal);
    }

    private bool ShouldShowMission(MissionRuntimeState missionState)
    {
        if (missionState == null || missionState.Definition == null)
        {
            return false;
        }

        if (!missionState.Definition.ShowInJournal)
        {
            return false;
        }

        if (missionState.IsInactive && !showInactiveMissions)
        {
            return false;
        }

        if (missionState.IsCompleted && !showCompletedMissions)
        {
            return false;
        }

        return true;
    }

    private void ResolveSelectedMission(MissionRuntimeState trackedMission)
    {
        if (selectedMission != null && visibleMissions.Contains(selectedMission))
        {
            return;
        }

        if (trackedMission != null && visibleMissions.Contains(trackedMission))
        {
            selectedMission = trackedMission;
            return;
        }

        selectedMission = FindFirstByState(MissionState.Active)
            ?? FindFirstByState(MissionState.ReadyToTurnIn)
            ?? FindFirstByState(MissionState.Available)
            ?? FindFirstByState(MissionState.Completed)
            ?? (visibleMissions.Count > 0 ? visibleMissions[0] : null);
    }

    private MissionRuntimeState FindFirstByState(MissionState state)
    {
        for (int i = 0; i < visibleMissions.Count; i++)
        {
            if (visibleMissions[i].State == state)
            {
                return visibleMissions[i];
            }
        }

        return null;
    }

    private void RenderMissionList()
    {
        ClearListRows();

        if (emptyListText != null)
        {
            bool isEmpty = visibleMissions.Count == 0;
            emptyListText.gameObject.SetActive(isEmpty);
            emptyListText.text = isEmpty ? "No hay misiones visibles." : string.Empty;
        }

        for (int i = 0; i < visibleMissions.Count; i++)
        {
            MissionRuntimeState missionState = visibleMissions[i];

            MissionJournalListRowUI row = Instantiate(missionListRowPrefab, missionListContent);
            row.Render(missionState, missionState == selectedMission);
            row.Clicked += HandleRowClicked;

            listRows.Add(row);
        }
    }

    private void RenderSelectedMission()
    {
        ClearObjectiveRows();

        if (selectedMission == null || selectedMission.Definition == null)
        {
            SetText(detailTitleText, "Selecciona una misión");
            SetText(detailStatusText, string.Empty);
            SetText(descriptionText, string.Empty);
            SetRewardsPlaceholder(string.Empty);
            SetTrackButton(false, "Trackear");
            return;
        }

        MissionDefinition definition = selectedMission.Definition;

        SetText(detailTitleText, GetMissionTitle(selectedMission));
        SetText(detailStatusText, BuildDetailStatus(selectedMission));
        SetText(descriptionText, GetMissionDescription(definition));

        RenderObjectives(selectedMission);
        RenderRewards(definition);
        RenderTrackButton(selectedMission);
    }

    private void RenderObjectives(MissionRuntimeState missionState)
    {
        ClearObjectiveRows();

        IReadOnlyList<MissionObjectiveRuntimeState> objectives = missionState.Objectives;

        for (int i = 0; i < objectives.Count; i++)
        {
            MissionObjectiveRuntimeState objectiveState = objectives[i];

            if (!ShouldShowObjective(objectiveState))
            {
                continue;
            }

            MissionJournalObjectiveRowUI row = Instantiate(journalObjectiveRowPrefab, objectiveRowsContainer);
            row.Render(objectiveState);
            objectiveRows.Add(row);
        }

        if (objectiveRows.Count == 0)
        {
            MissionJournalObjectiveRowUI row = Instantiate(journalObjectiveRowPrefab, objectiveRowsContainer);
            row.RenderFallback("Sin objetivos visibles.");
            objectiveRows.Add(row);
        }
    }

    private bool ShouldShowObjective(MissionObjectiveRuntimeState objectiveState)
    {
        if (objectiveState == null || objectiveState.Definition == null)
        {
            return false;
        }

        if (objectiveState.Definition.Importance == ObjectiveImportance.Bonus && !showBonusObjectives)
        {
            return false;
        }

        return true;
    }

    private void RenderRewards(MissionDefinition definition)
    {
        bool hasVisibleRewards = false;
        IReadOnlyList<MissionRewardDefinition> rewards = definition.Rewards;

        for (int i = 0; i < rewards.Count; i++)
        {
            MissionRewardDefinition reward = rewards[i];

            if (reward == null || !reward.ShowInJournal || reward.RewardType == MissionRewardType.None)
            {
                continue;
            }

            hasVisibleRewards = true;
            break;
        }

        SetRewardsPlaceholder(hasVisibleRewards ? BuildRewardsSummary(rewards) : noRewardsText);
    }

    private string BuildRewardsSummary(IReadOnlyList<MissionRewardDefinition> rewards)
    {
        if (rewards == null || rewards.Count == 0)
        {
            return noRewardsText;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        for (int i = 0; i < rewards.Count; i++)
        {
            MissionRewardDefinition reward = rewards[i];

            if (reward == null || !reward.ShowInJournal || reward.RewardType == MissionRewardType.None)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append("• ");
            builder.Append(string.IsNullOrWhiteSpace(reward.DisplayName) ? reward.RewardId : reward.DisplayName);
        }

        return builder.Length > 0 ? builder.ToString() : noRewardsText;
    }

    private void RenderTrackButton(MissionRuntimeState missionState)
    {
        bool canTrack = missionState.IsActive || missionState.IsReadyToTurnIn;

        if (!canTrack)
        {
            SetTrackButton(false, "Trackear");
            return;
        }

        SetTrackButton(true, missionState.IsTracked ? "Dejar de trackear" : "Trackear");
    }

    private void SetTrackButton(bool interactable, string label)
    {
        if (trackButton != null)
        {
            trackButton.interactable = interactable;
        }

        SetText(trackButtonText, label);
    }

    private void SetRewardsPlaceholder(string value)
    {
        if (rewardsPlaceholderText == null)
        {
            return;
        }

        bool hasText = !string.IsNullOrWhiteSpace(value);
        rewardsPlaceholderText.gameObject.SetActive(hasText);
        rewardsPlaceholderText.text = hasText ? value : string.Empty;
    }

    private void HandleRowClicked(MissionRuntimeState missionState)
    {
        SelectMission(missionState);
    }

    private void HandleTrackClicked()
    {
        if (selectedMission == null)
        {
            return;
        }

        TrackRequested?.Invoke(selectedMission);
    }

    private void HandleCloseClicked()
    {
        CloseRequested?.Invoke();
    }

    private void ClearListRows()
    {
        for (int i = 0; i < listRows.Count; i++)
        {
            if (listRows[i] != null)
            {
                listRows[i].Clicked -= HandleRowClicked;
                Destroy(listRows[i].gameObject);
            }
        }

        listRows.Clear();
    }

    private void ClearObjectiveRows()
    {
        for (int i = 0; i < objectiveRows.Count; i++)
        {
            if (objectiveRows[i] != null)
            {
                Destroy(objectiveRows[i].gameObject);
            }
        }

        objectiveRows.Clear();
    }

    private bool HasRequiredReferences()
    {
        bool hasReferences =
            rootCanvasGroup != null &&
            missionListContent != null &&
            missionListRowPrefab != null &&
            detailTitleText != null &&
            detailStatusText != null &&
            descriptionText != null &&
            objectiveRowsContainer != null &&
            journalObjectiveRowPrefab != null;

        if (!hasReferences && logMissingReferences)
        {
            Debug.LogWarning("MissionJournalView tiene referencias faltantes.", this);
        }

        return hasReferences;
    }

    private static string GetMissionTitle(MissionRuntimeState missionState)
    {
        string title = missionState.Definition.Title;
        return string.IsNullOrWhiteSpace(title) ? missionState.MissionId : title.Trim();
    }

    private static string GetMissionDescription(MissionDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(definition.FullDescription))
        {
            return definition.FullDescription.Trim();
        }

        if (!string.IsNullOrWhiteSpace(definition.ShortDescription))
        {
            return definition.ShortDescription.Trim();
        }

        return "Sin descripción.";
    }

    private static string BuildDetailStatus(MissionRuntimeState missionState)
    {
        return $"{GetCategoryText(missionState.Definition.Category)} · {GetStateText(missionState.State)}";
    }

    private static string GetCategoryText(MissionCategory category)
    {
        return category switch
        {
            MissionCategory.Main => "Principal",
            MissionCategory.Optional => "Opcional",
            _ => category.ToString()
        };
    }

    private static string GetStateText(MissionState state)
    {
        return state switch
        {
            MissionState.Inactive => "Inactiva",
            MissionState.Available => "Disponible",
            MissionState.Active => "Activa",
            MissionState.ReadyToTurnIn => "Lista para entregar",
            MissionState.Completed => "Completada",
            _ => state.ToString()
        };
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }

    private static int CompareMissionsForJournal(MissionRuntimeState left, MissionRuntimeState right)
    {
        if (left == null && right == null) return 0;
        if (left == null) return 1;
        if (right == null) return -1;

        int categoryCompare = left.Definition.Category.CompareTo(right.Definition.Category);
        if (categoryCompare != 0)
        {
            return categoryCompare;
        }

        int orderCompare = left.Definition.DisplayOrder.CompareTo(right.Definition.DisplayOrder);
        if (orderCompare != 0)
        {
            return orderCompare;
        }

        return string.Compare(left.MissionId, right.MissionId, StringComparison.Ordinal);
    }
}