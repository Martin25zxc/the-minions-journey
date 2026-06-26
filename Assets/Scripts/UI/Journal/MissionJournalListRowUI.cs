using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fila visual de misión dentro de la lista izquierda del Journal.
/// 
/// Responsabilidad:
/// - Mostrar título, estado y progreso resumido.
/// - Avisar cuando fue clickeada.
/// </summary>
[DisallowMultipleComponent]
public sealed class MissionJournalListRowUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject selectedMarker;

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

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }

    public void Render(MissionRuntimeState state, bool isSelected)
    {
        missionState = state;

        if (state == null || state.Definition == null)
        {
            SetText(titleText, "Misión inválida");
            SetText(statusText, string.Empty);
            SetText(progressText, string.Empty);
            SetSelected(isSelected);
            return;
        }

        SetText(titleText, GetTitle(state));
        SetText(statusText, BuildStatus(state));
        SetText(progressText, BuildProgress(state));
        SetSelected(isSelected);
    }

    private void HandleClicked()
    {
        if (missionState != null)
        {
            Clicked?.Invoke(missionState);
        }
    }

    private void SetSelected(bool value)
    {
        if (selectedMarker != null)
        {
            selectedMarker.SetActive(value);
        }
    }

    private static string GetTitle(MissionRuntimeState state)
    {
        string title = state.Definition.Title;
        return string.IsNullOrWhiteSpace(title) ? state.MissionId : title.Trim();
    }

    private static string BuildStatus(MissionRuntimeState state)
    {
        string category = state.Definition.Category == MissionCategory.Main ? "Principal" : "Opcional";
        string status = state.State switch
        {
            MissionState.Inactive => "Inactiva",
            MissionState.Available => "Disponible",
            MissionState.Active => "Activa",
            MissionState.ReadyToTurnIn => "Lista para entregar",
            MissionState.Completed => "Completada",
            _ => state.State.ToString()
        };

        return $"{category} · {status}";
    }

    private static string BuildProgress(MissionRuntimeState state)
    {
        int completedRequired = state.GetCompletedRequiredObjectiveCount();
        int requiredTotal = state.GetRequiredObjectiveCount();

        if (requiredTotal <= 0)
        {
            return string.Empty;
        }

        return $"{completedRequired}/{requiredTotal}";
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}