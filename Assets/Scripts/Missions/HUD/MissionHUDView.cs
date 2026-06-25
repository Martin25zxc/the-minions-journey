using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MissionHUDView : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("CanvasGroup del panel raíz. Se usa para mostrar/ocultar el HUD sin destruirlo.")]
    private CanvasGroup rootCanvasGroup;

    [SerializeField, Tooltip("Texto principal con el título de la misión trackeada.")]
    private TMP_Text missionTitleText;

    [SerializeField, Tooltip("Renglón secundario debajo del título. Se usa para tipo de misión o estado de entrega.")]
    private TMP_Text statusText;

    [SerializeField, Tooltip("Contenedor donde se instancian las filas de objetivos.")]
    private RectTransform objectiveRowsContainer;

    [SerializeField, Tooltip("Prefab de una fila de objetivo. Debe tener MissionObjectiveRowUI.")]
    private MissionObjectiveRowUI objectiveRowPrefab;

    [Header("Contenido")]
    [SerializeField, Tooltip("Muestra objetivos Bonus. Para el HUD inicial conviene dejarlo apagado.")]
    private bool showBonusObjectives;

    [SerializeField, Tooltip("Muestra objetivos ya completados. Para HUD limpio conviene dejarlo apagado y usar toasts para feedback.")]
    private bool showCompletedObjectives;

    [SerializeField, Min(1), Tooltip("Cantidad máxima de filas cuando el HUD está expandido.")]
    private int maxExpandedRows = 4;

    [SerializeField, Min(1), Tooltip("Cantidad máxima de filas cuando el HUD está compacto.")]
    private int maxCompactRows = 1;

    [Header("Status Row")]
    [SerializeField, Tooltip("Si está activo, muestra un renglón extra debajo del título cuando la misión está activa.")]
    private bool showStatusWhenActive = true;

    [SerializeField, Tooltip("Texto mostrado en el renglón secundario para misiones principales activas.")]
    private string mainMissionStatusText = "Misión principal";

    [SerializeField, Tooltip("Texto mostrado en el renglón secundario para misiones opcionales activas.")]
    private string optionalMissionStatusText = "Misión opcional";

    [Header("Objetivos")]
    [SerializeField, Tooltip("Permite que el texto del objetivo use hasta dos líneas dentro de cada fila.")]
    private bool allowObjectiveDoubleLine = true;

    [SerializeField, Min(24f), Tooltip("Altura recomendada para filas de una sola línea.")]
    private float singleLineRowHeight = 36f;

    [SerializeField, Min(36f), Tooltip("Altura recomendada para filas de doble línea.")]
    private float doubleLineRowHeight = 56f;

    [Header("Textos")]
    [SerializeField, Tooltip("Texto mostrado cuando la misión está lista para entregar.")]
    private string readyToTurnInText = "Vuelve para entregar la misión.";

    [SerializeField, Tooltip("Texto mostrado si no hay objetivos visibles para esta misión.")]
    private string noVisibleObjectivesText = "Sigue la misión activa.";

    [Header("Debug")]
    [SerializeField, Tooltip("Avisa en consola si faltan referencias del HUD.")]
    private bool logMissingReferences = true;

    private readonly List<MissionObjectiveRowUI> activeRows = new List<MissionObjectiveRowUI>();
    private readonly List<MissionObjectiveRowUI> pooledRows = new List<MissionObjectiveRowUI>();

    private void Reset()
    {
        rootCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        HideInstant();
    }

    public void Render(MissionRuntimeState missionState, MissionHUDDisplayMode displayMode)
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        if (displayMode == MissionHUDDisplayMode.Hidden || missionState == null || missionState.Definition == null)
        {
            HideInstant();
            return;
        }

        rootCanvasGroup.alpha = 1f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;

        missionTitleText.text = GetMissionTitle(missionState);
        RenderStatus(missionState);
        RenderObjectives(missionState, displayMode);
    }

    public void HideInstant()
    {
        ReturnAllRowsToPool();

        if (missionTitleText != null)
        {
            missionTitleText.text = string.Empty;
        }

        if (statusText != null)
        {
            statusText.text = string.Empty;
            statusText.gameObject.SetActive(false);
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }
    }

    private void RenderStatus(MissionRuntimeState missionState)
    {
        if (statusText == null)
        {
            return;
        }

        string resolvedStatus = GetStatusText(missionState);
        bool hasStatus = !string.IsNullOrWhiteSpace(resolvedStatus);

        statusText.gameObject.SetActive(hasStatus);
        statusText.text = hasStatus ? resolvedStatus : string.Empty;
    }

    private string GetStatusText(MissionRuntimeState missionState)
    {
        if (missionState == null || missionState.Definition == null)
        {
            return string.Empty;
        }

        if (missionState.State == MissionState.ReadyToTurnIn)
        {
            return readyToTurnInText;
        }

        if (!showStatusWhenActive)
        {
            return string.Empty;
        }

        if (missionState.Definition.Category == MissionCategory.Main)
        {
            return mainMissionStatusText;
        }

        if (missionState.Definition.Category == MissionCategory.Optional)
        {
            return optionalMissionStatusText;
        }

        return string.Empty;
    }

    private void RenderObjectives(MissionRuntimeState missionState, MissionHUDDisplayMode displayMode)
    {
        ReturnAllRowsToPool();

        if (missionState.State == MissionState.ReadyToTurnIn)
        {
            return;
        }

        int maxRows = displayMode == MissionHUDDisplayMode.Compact ? maxCompactRows : maxExpandedRows;
        int shownRows = 0;

        IReadOnlyList<MissionObjectiveRuntimeState> objectives = missionState.Objectives;
        for (int i = 0; i < objectives.Count; i++)
        {
            if (shownRows >= maxRows)
            {
                break;
            }

            MissionObjectiveRuntimeState objectiveState = objectives[i];
            if (!ShouldShowObjective(objectiveState))
            {
                continue;
            }

            MissionObjectiveRowUI row = GetRow();
            row.Render(objectiveState, allowObjectiveDoubleLine, singleLineRowHeight, doubleLineRowHeight);
            activeRows.Add(row);
            shownRows++;
        }

        if (shownRows == 0 && displayMode == MissionHUDDisplayMode.Expanded)
        {
            MissionObjectiveRowUI row = GetRow();
            row.RenderFallback(noVisibleObjectivesText, allowObjectiveDoubleLine, singleLineRowHeight, doubleLineRowHeight);
            activeRows.Add(row);
        }
    }

    private bool ShouldShowObjective(MissionObjectiveRuntimeState objectiveState)
    {
        if (objectiveState == null || objectiveState.Definition == null)
        {
            return false;
        }

        if (objectiveState.Definition.HiddenUntilActive)
        {
            // Todavía no tenemos lógica de objetivos por fases internas. En este HUD inicial,
            // si el objetivo pertenece a una misión activa ya puede mostrarse.
        }

        if (objectiveState.Definition.Importance == ObjectiveImportance.Bonus && !showBonusObjectives)
        {
            return false;
        }

        if (objectiveState.IsCompleted && !showCompletedObjectives)
        {
            return false;
        }

        return true;
    }

    private MissionObjectiveRowUI GetRow()
    {
        MissionObjectiveRowUI row;

        if (pooledRows.Count > 0)
        {
            int lastIndex = pooledRows.Count - 1;
            row = pooledRows[lastIndex];
            pooledRows.RemoveAt(lastIndex);
            row.gameObject.SetActive(true);
            return row;
        }

        row = Instantiate(objectiveRowPrefab, objectiveRowsContainer);
        row.gameObject.SetActive(true);
        return row;
    }

    private void ReturnAllRowsToPool()
    {
        for (int i = 0; i < activeRows.Count; i++)
        {
            MissionObjectiveRowUI row = activeRows[i];
            if (row == null)
            {
                continue;
            }

            row.gameObject.SetActive(false);
            pooledRows.Add(row);
        }

        activeRows.Clear();
    }

    private bool HasRequiredReferences()
    {
        bool hasReferences = rootCanvasGroup != null
            && missionTitleText != null
            && objectiveRowsContainer != null
            && objectiveRowPrefab != null;

        if (!hasReferences && logMissingReferences)
        {
            Debug.LogWarning($"{nameof(MissionHUDView)} tiene referencias faltantes.", this);
        }

        return hasReferences;
    }

    private static string GetMissionTitle(MissionRuntimeState missionState)
    {
        if (missionState == null || missionState.Definition == null)
        {
            return string.Empty;
        }

        string title = missionState.Definition.Title;
        return string.IsNullOrWhiteSpace(title) ? missionState.MissionId : title.Trim();
    }
}