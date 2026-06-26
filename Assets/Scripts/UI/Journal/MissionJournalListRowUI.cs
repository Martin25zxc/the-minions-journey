using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fila de misión para la lista izquierda del Mission Journal.
///
/// Responsabilidad:
/// - Mostrar título, categoría/estado y progreso resumido.
/// - Avisar click de selección.
/// - No modifica MissionManager ni gameplay.
/// </summary>
[DisallowMultipleComponent]
public sealed class MissionJournalListRowUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Button del root de la fila.")]
    private Button button;

    [SerializeField, Tooltip("Texto principal con el título de la misión.")]
    private TMP_Text titleText;

    [SerializeField, Tooltip("Texto secundario con categoría y estado.")]
    private TMP_Text statusText;

    [SerializeField, Tooltip("Texto de progreso resumido. Ejemplo: 1/3.")]
    private TMP_Text progressText;

    [SerializeField, Tooltip("Marcador visual de fila seleccionada. Puede ser una barra lateral o icono.")]
    private GameObject selectedMarker;

    [Header("Textos")]
    [SerializeField] private string mainCategoryText = "Principal";
    [SerializeField] private string optionalCategoryText = "Opcional";
    [SerializeField] private string availableStatusText = "Disponible";
    [SerializeField] private string activeStatusText = "Activa";
    [SerializeField] private string readyToTurnInStatusText = "Lista para entregar";
    [SerializeField] private string completedStatusText = "Completada";
    [SerializeField] private string inactiveStatusText = "Inactiva";

    private MissionRuntimeState missionState;

    public event Action<MissionRuntimeState> Clicked;

    private void Reset()
    {
        button = GetComponent<Button>();

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            string textName = texts[i].name;

            if (titleText == null && textName.Contains("Title"))
            {
                titleText = texts[i];
            }
            else if (statusText == null && textName.Contains("Status"))
            {
                statusText = texts[i];
            }
            else if (progressText == null && textName.Contains("Progress"))
            {
                progressText = texts[i];
            }
        }
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    public void Render(MissionRuntimeState state, bool selected)
    {
        missionState = state;

        if (missionState == null || missionState.Definition == null)
        {
            SetText(titleText, "Misión no disponible");
            SetText(statusText, string.Empty);
            SetText(progressText, string.Empty);
            SetSelected(selected);
            return;
        }

        SetText(titleText, ResolveTitle(missionState));
        SetText(statusText, BuildStatusText(missionState));
        SetText(progressText, BuildProgressText(missionState));
        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        if (selectedMarker != null)
        {
            selectedMarker.SetActive(selected);
        }
    }

    private void HandleClick()
    {
        if (missionState != null)
        {
            Clicked?.Invoke(missionState);
        }
    }

    private string ResolveTitle(MissionRuntimeState state)
    {
        string title = state.Definition.Title;
        return string.IsNullOrWhiteSpace(title) ? state.MissionId : title.Trim();
    }

    private string BuildStatusText(MissionRuntimeState state)
    {
        string category = state.Definition.Category == MissionCategory.Main
            ? mainCategoryText
            : optionalCategoryText;

        return $"{category} · {ResolveStateText(state.State)}";
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

    private static string BuildProgressText(MissionRuntimeState state)
    {
        int requiredTotal = state.GetRequiredObjectiveCount();
        if (requiredTotal <= 0)
        {
            return string.Empty;
        }

        return $"{state.GetCompletedRequiredObjectiveCount()}/{requiredTotal}";
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
