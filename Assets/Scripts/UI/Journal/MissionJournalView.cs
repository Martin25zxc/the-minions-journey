using System;
using System.Collections.Generic;
using System.Text;
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
/// </summary>
[DisallowMultipleComponent]
public sealed class MissionJournalView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField, Tooltip("CanvasGroup del root MissionJournalRoot.")]
    private CanvasGroup rootCanvasGroup;

    [Header("Lista de misiones")]
    [SerializeField, Tooltip("Content del ScrollRect de la lista izquierda.")]
    private RectTransform missionListContent;

    [SerializeField, Tooltip("Prefab de fila para la lista izquierda del Journal.")]
    private MissionJournalListRowUI missionListRowPrefab;

    [SerializeField, Tooltip("Texto opcional mostrado si no hay misiones visibles.")]
    private TMP_Text emptyListText;

    [Header("Detalle")]
    [SerializeField] private TMP_Text detailTitleText;
    [SerializeField] private TMP_Text detailStatusText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Detalle Scroll")]
    [SerializeField, Tooltip("ScrollRect del panel derecho. Se resetea arriba al cambiar de misión.")]
    private ScrollRect detailScrollRect;

    [SerializeField, Tooltip("Content del DetailScrollRect. Usado para forzar rebuild después de renderizar.")]
    private RectTransform detailContent;

    [Header("Objetivos")]
    [SerializeField, Tooltip("Contenedor donde se instancian los objetivos del Journal.")]
    private RectTransform objectiveRowsContainer;

    [SerializeField, Tooltip("Prefab de fila de objetivo específico para Journal. No usar el prefab compacto del HUD.")]
    private MissionJournalObjectiveRowUI journalObjectiveRowPrefab;

    [SerializeField, Tooltip("Si está activo, muestra objetivos bonus/opcionales en el Journal.")]
    private bool showBonusObjectives = true;

    [Header("Recompensas")]
    [SerializeField, Tooltip("Texto donde se listan rewards o el fallback sin rewards.")]
    private TMP_Text rewardsPlaceholderText;

    [SerializeField, Tooltip("Texto mostrado cuando la misión no tiene rewards visibles.")]
    private string noRewardsText = "Sin recompensas visibles.";

    [Header("Botones")]
    [SerializeField] private Button trackButton;
    [SerializeField] private TMP_Text trackButtonText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button headerCloseButton;

    [Header("Visibilidad de misiones")]
    [SerializeField, Tooltip("Preset de visibilidad usado por el Journal. Para MVP usar MvpActivesAvailableCompleted.")]
    private MissionJournalVisibilityPreset visibilityPreset = MissionJournalVisibilityPreset.MvpActivesAvailableCompleted;

    [SerializeField, Tooltip("Solo se usa si Visibility Preset = Custom.")]
    private bool showAvailableMissions = true;

    [SerializeField, Tooltip("Solo se usa si Visibility Preset = Custom.")]
    private bool showActiveMissions = true;

    [SerializeField, Tooltip("Solo se usa si Visibility Preset = Custom.")]
    private bool showReadyToTurnInMissions = true;

    [SerializeField, Tooltip("Solo se usa si Visibility Preset = Custom.")]
    private bool showCompletedMissions = true;

    [SerializeField, Tooltip("Normalmente debe quedar apagado para no revelar misiones futuras.")]
    private bool showInactiveMissions;

    [Header("Textos de estado")]
    [SerializeField] private string mainCategoryText = "Principal";
    [SerializeField] private string optionalCategoryText = "Opcional";
    [SerializeField] private string availableStatusText = "Disponible";
    [SerializeField] private string activeStatusText = "Activa";
    [SerializeField] private string readyToTurnInStatusText = "Lista para entregar";
    [SerializeField] private string completedStatusText = "Completada";
    [SerializeField] private string inactiveStatusText = "Inactiva";

    [Header("Textos vacíos")]
    [SerializeField] private string emptyAllMissionsText = "No hay misiones registradas.";
    [SerializeField] private string noSelectedMissionTitle = "Sin misión seleccionada";
    [SerializeField] private string noSelectedMissionDescription = "Selecciona una misión de la lista.";
    [SerializeField] private string noVisibleObjectivesText = "Sin objetivos visibles.";

    [Header("Track")]
    [SerializeField] private string trackMissionText = "Seguir misión";
    [SerializeField] private string untrackMissionText = "Dejar de seguir";

    [Header("Debug")]
    [SerializeField, Tooltip("Avisa en consola si faltan referencias críticas.")]
    private bool logMissingReferences = true;

    private readonly List<MissionJournalListRowUI> missionRows = new();
    private readonly List<MissionJournalObjectiveRowUI> objectiveRows = new();
    private readonly List<MissionRuntimeState> visibleMissions = new();

    private MissionRuntimeState selectedMission;
    private MissionRuntimeState currentTrackedMission;

    public event Action<MissionRuntimeState> TrackRequested;
    public event Action CloseRequested;

    private void Reset()
    {
        rootCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        WireButtons();
        HideInstant();
    }

    private void OnEnable()
    {
        WireButtons();
    }

    private void OnDisable()
    {
        UnwireButtons();
    }

    public void Show()
    {
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

    public void Render(IReadOnlyList<MissionRuntimeState> allMissions, MissionRuntimeState trackedMission)
    {
        currentTrackedMission = trackedMission;

        if (!HasRequiredReferences())
        {
            return;
        }

        RebuildVisibleMissions(allMissions);
        selectedMission = ResolveSelectedMissionAfterFilter(trackedMission);

        RenderMissionList();
        RenderSelectedMission();
        RebuildDetailLayout();
        ResetDetailScrollToTop();
    }

    private void RebuildVisibleMissions(IReadOnlyList<MissionRuntimeState> allMissions)
    {
        visibleMissions.Clear();

        if (allMissions == null)
        {
            return;
        }

        for (int i = 0; i < allMissions.Count; i++)
        {
            MissionRuntimeState missionState = allMissions[i];
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

        switch (visibilityPreset)
        {
            case MissionJournalVisibilityPreset.MvpActivesAvailableCompleted:
                return missionState.IsReadyToTurnIn ||
                       missionState.IsActive ||
                       missionState.IsAvailable ||
                       missionState.IsCompleted;

            case MissionJournalVisibilityPreset.OnlyActives:
                return missionState.IsReadyToTurnIn || missionState.IsActive;

            case MissionJournalVisibilityPreset.ActivesAndCompleted:
                return missionState.IsReadyToTurnIn || missionState.IsActive || missionState.IsCompleted;

            case MissionJournalVisibilityPreset.EverythingExceptInactive:
                return !missionState.IsInactive;

            case MissionJournalVisibilityPreset.Custom:
                return ShouldShowMissionCustom(missionState);

            default:
                return false;
        }
    }

    private bool ShouldShowMissionCustom(MissionRuntimeState missionState)
    {
        switch (missionState.State)
        {
            case MissionState.Inactive:
                return showInactiveMissions;

            case MissionState.Available:
                return showAvailableMissions;

            case MissionState.Active:
                return showActiveMissions;

            case MissionState.ReadyToTurnIn:
                return showReadyToTurnInMissions;

            case MissionState.Completed:
                return showCompletedMissions;

            default:
                return false;
        }
    }

    private int CompareMissionsForJournal(MissionRuntimeState a, MissionRuntimeState b)
    {
        int stateCompare = GetMissionStateSortPriority(a).CompareTo(GetMissionStateSortPriority(b));
        if (stateCompare != 0)
        {
            return stateCompare;
        }

        int displayOrderA = a != null && a.Definition != null ? a.Definition.DisplayOrder : int.MaxValue;
        int displayOrderB = b != null && b.Definition != null ? b.Definition.DisplayOrder : int.MaxValue;

        int displayOrderCompare = displayOrderA.CompareTo(displayOrderB);
        if (displayOrderCompare != 0)
        {
            return displayOrderCompare;
        }

        MissionCategory categoryA = a != null && a.Definition != null ? a.Definition.Category : MissionCategory.Optional;
        MissionCategory categoryB = b != null && b.Definition != null ? b.Definition.Category : MissionCategory.Optional;

        int categoryCompare = categoryA.CompareTo(categoryB);
        if (categoryCompare != 0)
        {
            return categoryCompare;
        }

        string titleA = ResolveTitle(a);
        string titleB = ResolveTitle(b);

        int titleCompare = string.Compare(titleA, titleB, StringComparison.CurrentCultureIgnoreCase);
        if (titleCompare != 0)
        {
            return titleCompare;
        }

        string idA = a != null ? a.MissionId : string.Empty;
        string idB = b != null ? b.MissionId : string.Empty;
        return string.Compare(idA, idB, StringComparison.Ordinal);
    }

    private int GetMissionStateSortPriority(MissionRuntimeState missionState)
    {
        if (missionState == null)
        {
            return 999;
        }

        switch (missionState.State)
        {
            case MissionState.ReadyToTurnIn:
                return 0;

            case MissionState.Active:
                return 1;

            case MissionState.Available:
                return 2;

            case MissionState.Completed:
                return 3;

            case MissionState.Inactive:
            default:
                return 999;
        }
    }

    private MissionRuntimeState ResolveSelectedMissionAfterFilter(MissionRuntimeState trackedMission)
    {
        if (selectedMission != null && visibleMissions.Contains(selectedMission))
        {
            return selectedMission;
        }

        if (trackedMission != null && visibleMissions.Contains(trackedMission))
        {
            return trackedMission;
        }

        MissionRuntimeState best = FindFirstVisibleByState(MissionState.ReadyToTurnIn);
        if (best != null)
        {
            return best;
        }

        best = FindFirstVisibleByState(MissionState.Active);
        if (best != null)
        {
            return best;
        }

        best = FindFirstVisibleByState(MissionState.Available);
        if (best != null)
        {
            return best;
        }

        best = FindFirstVisibleByState(MissionState.Completed);
        if (best != null)
        {
            return best;
        }

        return visibleMissions.Count > 0 ? visibleMissions[0] : null;
    }

    private MissionRuntimeState FindFirstVisibleByState(MissionState state)
    {
        for (int i = 0; i < visibleMissions.Count; i++)
        {
            if (visibleMissions[i] != null && visibleMissions[i].State == state)
            {
                return visibleMissions[i];
            }
        }

        return null;
    }

    private void RenderMissionList()
    {
        ClearMissionRows();

        bool hasVisibleMissions = visibleMissions.Count > 0;
        if (emptyListText != null)
        {
            emptyListText.gameObject.SetActive(!hasVisibleMissions);
            emptyListText.text = hasVisibleMissions ? string.Empty : emptyAllMissionsText;
        }

        if (!hasVisibleMissions)
        {
            return;
        }

        for (int i = 0; i < visibleMissions.Count; i++)
        {
            MissionRuntimeState missionState = visibleMissions[i];
            MissionJournalListRowUI row = Instantiate(missionListRowPrefab, missionListContent);
            row.Clicked += HandleMissionRowClicked;
            row.Render(missionState, ReferenceEquals(missionState, selectedMission));
            missionRows.Add(row);
        }
    }

    private void RenderSelectedMission()
    {
        if (selectedMission == null || selectedMission.Definition == null)
        {
            RenderEmptyDetail();
            return;
        }

        SetText(detailTitleText, ResolveTitle(selectedMission));
        SetText(detailStatusText, BuildDetailStatusText(selectedMission));
        SetText(descriptionText, ResolveDescription(selectedMission));

        RenderObjectives(selectedMission);
        RenderRewards(selectedMission.Definition);
        RenderTrackButton(selectedMission);
    }

    private void RenderEmptyDetail()
    {
        SetText(detailTitleText, noSelectedMissionTitle);
        SetText(detailStatusText, string.Empty);
        SetText(descriptionText, noSelectedMissionDescription);
        RenderNoVisibleObjectives();

        if (rewardsPlaceholderText != null)
        {
            rewardsPlaceholderText.text = string.Empty;
        }

        if (trackButton != null)
        {
            trackButton.gameObject.SetActive(false);
        }
    }

    private void RenderObjectives(MissionRuntimeState missionState)
    {
        ClearObjectiveRows();

        IReadOnlyList<MissionObjectiveRuntimeState> objectives = missionState.Objectives;
        for (int i = 0; i < objectives.Count; i++)
        {
            MissionObjectiveRuntimeState objectiveState = objectives[i];
            if (!ShouldShowObjective(missionState, objectiveState))
            {
                continue;
            }

            MissionJournalObjectiveRowUI row = Instantiate(journalObjectiveRowPrefab, objectiveRowsContainer);
            row.Render(objectiveState);
            objectiveRows.Add(row);
        }

        if (objectiveRows.Count == 0)
        {
            RenderNoVisibleObjectives();
        }
    }

    private bool ShouldShowObjective(MissionRuntimeState missionState, MissionObjectiveRuntimeState objectiveState)
    {
        if (missionState == null || objectiveState == null || objectiveState.Definition == null)
        {
            return false;
        }

        if (objectiveState.IsBonus && !showBonusObjectives)
        {
            return false;
        }

        if (objectiveState.Definition.HiddenUntilActive && missionState.IsAvailable)
        {
            return false;
        }

        return true;
    }

    private void RenderNoVisibleObjectives()
    {
        ClearObjectiveRows();

        if (journalObjectiveRowPrefab == null || objectiveRowsContainer == null)
        {
            return;
        }

        MissionJournalObjectiveRowUI row = Instantiate(journalObjectiveRowPrefab, objectiveRowsContainer);
        row.RenderFallback(noVisibleObjectivesText);
        objectiveRows.Add(row);
    }

    private void RenderRewards(MissionDefinition definition)
    {
        if (rewardsPlaceholderText == null)
        {
            return;
        }

        if (definition == null || definition.Rewards == null || definition.Rewards.Count == 0)
        {
            rewardsPlaceholderText.text = noRewardsText;
            return;
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < definition.Rewards.Count; i++)
        {
            MissionRewardDefinition reward = definition.Rewards[i];
            if (reward == null || !reward.ShowInJournal || reward.RewardType == MissionRewardType.None)
            {
                continue;
            }

            string displayName = reward.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = reward.RewardId;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            //builder.Append("• ");
            builder.Append(displayName);
        }

        rewardsPlaceholderText.text = builder.Length > 0 ? builder.ToString() : noRewardsText;
    }

    private void RenderTrackButton(MissionRuntimeState missionState)
    {
        if (trackButton == null)
        {
            return;
        }

        bool canTrack = CanTrackFromJournal(missionState);
        trackButton.gameObject.SetActive(canTrack);

        if (!canTrack)
        {
            return;
        }

        trackButton.interactable = true;

        if (trackButtonText != null)
        {
            trackButtonText.text = missionState.IsTracked ? untrackMissionText : trackMissionText;
        }
    }

    private static bool CanTrackFromJournal(MissionRuntimeState missionState)
    {
        return missionState != null &&
               missionState.Definition != null &&
               (missionState.IsActive || missionState.IsReadyToTurnIn);
    }

    private void HandleMissionRowClicked(MissionRuntimeState missionState)
    {
        if (missionState == null)
        {
            return;
        }

        selectedMission = missionState;
        RenderMissionList();
        RenderSelectedMission();
        RebuildDetailLayout();
        ResetDetailScrollToTop();
    }

    private void HandleTrackButtonClicked()
    {
        if (!CanTrackFromJournal(selectedMission))
        {
            return;
        }

        TrackRequested?.Invoke(selectedMission);
    }

    private void HandleCloseButtonClicked()
    {
        CloseRequested?.Invoke();
    }

    private string BuildDetailStatusText(MissionRuntimeState missionState)
    {
        if (missionState == null || missionState.Definition == null)
        {
            return string.Empty;
        }

        string category = missionState.Definition.Category == MissionCategory.Main
            ? mainCategoryText
            : optionalCategoryText;

        return $"{category} · {ResolveStateText(missionState.State)}";
    }

    private string ResolveStateText(MissionState state)
    {
        switch (state)
        {
            case MissionState.Available:
                return availableStatusText;

            case MissionState.Active:
                return activeStatusText;

            case MissionState.ReadyToTurnIn:
                return readyToTurnInStatusText;

            case MissionState.Completed:
                return completedStatusText;

            case MissionState.Inactive:
            default:
                return inactiveStatusText;
        }
    }

    private string ResolveDescription(MissionRuntimeState missionState)
    {
        if (missionState == null || missionState.Definition == null)
        {
            return string.Empty;
        }

        string fullDescription = missionState.Definition.FullDescription;
        if (!string.IsNullOrWhiteSpace(fullDescription))
        {
            return fullDescription.Trim();
        }

        string shortDescription = missionState.Definition.ShortDescription;
        if (!string.IsNullOrWhiteSpace(shortDescription))
        {
            return shortDescription.Trim();
        }

        return string.Empty;
    }

    private static string ResolveTitle(MissionRuntimeState missionState)
    {
        if (missionState == null || missionState.Definition == null)
        {
            return string.Empty;
        }

        string title = missionState.Definition.Title;
        return string.IsNullOrWhiteSpace(title) ? missionState.MissionId : title.Trim();
    }

    private void ClearMissionRows()
    {
        for (int i = 0; i < missionRows.Count; i++)
        {
            if (missionRows[i] == null)
            {
                continue;
            }

            missionRows[i].Clicked -= HandleMissionRowClicked;
            Destroy(missionRows[i].gameObject);
        }

        missionRows.Clear();
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

    private void WireButtons()
    {
        if (trackButton != null)
        {
            trackButton.onClick.RemoveListener(HandleTrackButtonClicked);
            trackButton.onClick.AddListener(HandleTrackButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
            closeButton.onClick.AddListener(HandleCloseButtonClicked);
        }

        if (headerCloseButton != null)
        {
            headerCloseButton.onClick.RemoveListener(HandleCloseButtonClicked);
            headerCloseButton.onClick.AddListener(HandleCloseButtonClicked);
        }
    }

    private void UnwireButtons()
    {
        if (trackButton != null)
        {
            trackButton.onClick.RemoveListener(HandleTrackButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
        }

        if (headerCloseButton != null)
        {
            headerCloseButton.onClick.RemoveListener(HandleCloseButtonClicked);
        }
    }

    private void RebuildDetailLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (objectiveRowsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(objectiveRowsContainer);
        }

        if (detailContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(detailContent);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void ResetDetailScrollToTop()
    {
        if (detailScrollRect == null)
        {
            return;
        }

        detailScrollRect.verticalNormalizedPosition = 1f;
    }

    private bool HasRequiredReferences()
    {
        bool hasRequired = true;

        if (missionListContent == null)
        {
            hasRequired = false;
            LogMissingReference(nameof(missionListContent));
        }

        if (missionListRowPrefab == null)
        {
            hasRequired = false;
            LogMissingReference(nameof(missionListRowPrefab));
        }

        if (objectiveRowsContainer == null)
        {
            hasRequired = false;
            LogMissingReference(nameof(objectiveRowsContainer));
        }

        if (journalObjectiveRowPrefab == null)
        {
            hasRequired = false;
            LogMissingReference(nameof(journalObjectiveRowPrefab));
        }

        return hasRequired;
    }

    private void LogMissingReference(string fieldName)
    {
        if (!logMissingReferences)
        {
            return;
        }

        Debug.LogWarning($"{nameof(MissionJournalView)}: falta asignar '{fieldName}'.", this);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
