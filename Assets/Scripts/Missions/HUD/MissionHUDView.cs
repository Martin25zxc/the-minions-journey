using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MissionHUDView : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("CanvasGroup del panel raíz. Permite ocultar/mostrar sin destruir la UI.")]
    private CanvasGroup rootCanvasGroup;

    [SerializeField, Tooltip("Texto del título de la misión trackeada.")]
    private TMP_Text missionTitleText;

    [SerializeField, Tooltip("Texto de estado. Se usa para ReadyToTurnIn, compacto o mensajes genéricos.")]
    private TMP_Text statusText;

    [SerializeField, Tooltip("Contenedor donde se instancian las filas de objetivos. Debe tener Vertical Layout Group.")]
    private RectTransform objectiveRowsContainer;

    [SerializeField, Tooltip("Prefab visual de una fila de objetivo.")]
    private MissionObjectiveRowUI objectiveRowPrefab;

    [Header("Contenido")]
    [SerializeField, Tooltip("Si está activo, muestra objetivos bonus en el HUD. Para MVP conviene dejarlo apagado salvo que queramos probar bonus.")]
    private bool showBonusObjectives;

    [SerializeField, Tooltip("Si está activo, mantiene objetivos completados visibles con marca. Para HUD limpio, conviene dejarlo apagado.")]
    private bool showCompletedObjectives;

    [SerializeField, Tooltip("Cantidad máxima de filas en modo expandido. Evita que el HUD crezca demasiado en pantalla.")]
    private int maxExpandedRows = 4;

    [SerializeField, Tooltip("Cantidad máxima de filas en modo compacto. Normalmente una sola línea.")]
    private int maxCompactRows = 1;

    [Header("Textos")]
    [SerializeField, Tooltip("Texto genérico cuando la misión está lista para entregar y todavía no tenemos un hint específico por misión.")]
    private string readyToTurnInText = "Vuelve para entregar la misión.";

    [SerializeField, Tooltip("Texto usado si no hay objetivos visibles pero la misión sigue activa.")]
    private string noVisibleObjectivesText = "Sigue la misión activa.";

    [Header("Debug")]
    [SerializeField, Tooltip("Muestra warnings cuando faltan referencias importantes de UI.")]
    private bool logMissingReferences = true;

    private readonly List<MissionObjectiveRowUI> rowPool = new List<MissionObjectiveRowUI>();

    private void Reset()
    {
        rootCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        EnsureCanvasGroup();
        Hide();
    }

    public void Render(MissionRuntimeState missionState, MissionHUDDisplayMode displayMode)
    {
        if (displayMode == MissionHUDDisplayMode.Hidden || missionState == null || missionState.IsCompleted)
        {
            Hide();
            return;
        }

        if (!missionState.Definition.ShowInHUD)
        {
            Hide();
            return;
        }

        EnsureCanvasGroup();
        SetRootVisible(true);
        SetTitle(GetMissionTitle(missionState));

        HideAllRows();

        if (missionState.IsReadyToTurnIn)
        {
            SetStatus(readyToTurnInText);
            return;
        }

        SetStatus(displayMode == MissionHUDDisplayMode.Compact ? GetCompactStatusText(missionState) : string.Empty);

        int maxRows = displayMode == MissionHUDDisplayMode.Compact ? maxCompactRows : maxExpandedRows;
        int renderedRows = RenderObjectiveRows(missionState, maxRows, displayMode == MissionHUDDisplayMode.Compact);

        if (renderedRows == 0)
        {
            SetStatus(noVisibleObjectivesText);
        }
    }

    public void Hide()
    {
        HideAllRows();
        SetTitle(string.Empty);
        SetStatus(string.Empty);
        SetRootVisible(false);
    }

    private int RenderObjectiveRows(MissionRuntimeState missionState, int maxRows, bool compact)
    {
        if (maxRows < 1)
        {
            maxRows = 1;
        }

        int renderedRows = 0;
        IReadOnlyList<MissionObjectiveRuntimeState> objectives = missionState.Objectives;

        for (int i = 0; i < objectives.Count; i++)
        {
            MissionObjectiveRuntimeState objectiveState = objectives[i];

            if (!ShouldShowObjective(objectiveState))
            {
                continue;
            }

            MissionObjectiveRowUI row = GetOrCreateRow(renderedRows);
            row.Bind(objectiveState, showCompletedObjectives, forceHideProgress: compact);
            renderedRows++;

            if (renderedRows >= maxRows)
            {
                break;
            }
        }

        return renderedRows;
    }

    private bool ShouldShowObjective(MissionObjectiveRuntimeState objectiveState)
    {
        if (objectiveState == null)
        {
            return false;
        }

        MissionObjectiveDefinition definition = objectiveState.Definition;

        if (definition.HiddenUntilActive)
        {
            return false;
        }

        if (objectiveState.IsBonus && !showBonusObjectives)
        {
            return false;
        }

        if (objectiveState.IsCompleted && !showCompletedObjectives)
        {
            return false;
        }

        return true;
    }

    private MissionObjectiveRowUI GetOrCreateRow(int index)
    {
        while (rowPool.Count <= index)
        {
            if (objectiveRowPrefab == null || objectiveRowsContainer == null)
            {
                if (logMissingReferences)
                {
                    Debug.LogWarning($"{nameof(MissionHUDView)} necesita Objective Row Prefab y Objective Rows Container.", this);
                }

                break;
            }

            MissionObjectiveRowUI row = Instantiate(objectiveRowPrefab, objectiveRowsContainer);
            rowPool.Add(row);
        }

        return rowPool[index];
    }

    private void HideAllRows()
    {
        for (int i = 0; i < rowPool.Count; i++)
        {
            if (rowPool[i] != null)
            {
                rowPool[i].SetVisible(false);
            }
        }
    }

    private void SetTitle(string value)
    {
        if (missionTitleText != null)
        {
            missionTitleText.text = value;
        }
    }

    private void SetStatus(string value)
    {
        if (statusText != null)
        {
            bool hasValue = !string.IsNullOrWhiteSpace(value);
            statusText.text = hasValue ? value.Trim() : string.Empty;
            statusText.gameObject.SetActive(hasValue);
        }
    }

    private void SetRootVisible(bool value)
    {
        if (rootCanvasGroup == null)
        {
            gameObject.SetActive(value);
            return;
        }

        rootCanvasGroup.alpha = value ? 1f : 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;
    }

    private void EnsureCanvasGroup()
    {
        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private string GetMissionTitle(MissionRuntimeState missionState)
    {
        string title = missionState.Definition.Title;
        return string.IsNullOrWhiteSpace(title) ? missionState.MissionId : title.Trim();
    }

    private string GetCompactStatusText(MissionRuntimeState missionState)
    {
        IReadOnlyList<MissionObjectiveRuntimeState> objectives = missionState.Objectives;

        for (int i = 0; i < objectives.Count; i++)
        {
            MissionObjectiveRuntimeState objectiveState = objectives[i];

            if (!ShouldShowObjective(objectiveState))
            {
                continue;
            }

            string description = objectiveState.Definition.Description;
            if (objectiveState.Definition.ShowProgress && objectiveState.RequiredAmount > 1)
            {
                return $"{description} {objectiveState.GetProgressText()}";
            }

            return description;
        }

        return string.Empty;
    }
}
