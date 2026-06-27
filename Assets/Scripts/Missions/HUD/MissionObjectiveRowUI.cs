using TMPro;
using UnityEngine;

/// <summary>
/// Fila de objetivo para el HUD de misiones.
/// </summary>
[DisallowMultipleComponent]
public sealed class MissionObjectiveRowUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Texto principal del objetivo.")]
    private TMP_Text descriptionText;

    [SerializeField, Tooltip("Texto de progreso, por ejemplo 0/3. Puede quedar vacío si el objetivo no muestra progreso.")]
    private TMP_Text progressText;

    [SerializeField, Tooltip("Marcador visual. Ejemplo: • para pendiente, X para completado.")]
    private TMP_Text stateMarkerText;

    [Header("Marcadores")]
    [SerializeField, Tooltip("Marcador mostrado para objetivos pendientes.")]
    private string pendingMarker = "•";

    [SerializeField, Tooltip("Marcador mostrado para objetivos completados.")]
    private string completedMarker = "X";

    [Header("Progreso")]
    [SerializeField, Tooltip("Si está activo, ProgressText queda siempre activo aunque esté vacío para mantener una columna estable.")]
    private bool keepProgressColumnWhenEmpty = true;

    private void Reset()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            string textName = texts[i].name;

            if (descriptionText == null && textName.Contains("Description"))
            {
                descriptionText = texts[i];
            }
            else if (progressText == null && textName.Contains("Progress"))
            {
                progressText = texts[i];
            }
            else if (stateMarkerText == null &&
                     (textName.Contains("State") ||
                      textName.Contains("Marker") ||
                      textName.Contains("Completed")))
            {
                stateMarkerText = texts[i];
            }
        }
    }

    public void Render(MissionObjectiveRuntimeState objectiveState)
    {
        if (objectiveState == null || objectiveState.Definition == null)
        {
            RenderFallback(string.Empty);
            return;
        }

        string description = objectiveState.Definition.Description;
        if (string.IsNullOrWhiteSpace(description))
        {
            description = objectiveState.ObjectiveId;
        }

        SetText(descriptionText, description?.Trim());
        SetText(stateMarkerText, objectiveState.IsCompleted ? completedMarker : pendingMarker);
        SetProgress(GetProgressText(objectiveState));
    }

    public void RenderFallback(string text)
    {
        SetText(descriptionText, text);
        SetText(stateMarkerText, pendingMarker);
        SetProgress(string.Empty);
    }

    private void SetProgress(string value)
    {
        if (progressText == null)
        {
            return;
        }

        bool hasProgress = !string.IsNullOrWhiteSpace(value);

        progressText.gameObject.SetActive(hasProgress || keepProgressColumnWhenEmpty);
        progressText.text = hasProgress ? value : string.Empty;
    }

    private static string GetProgressText(MissionObjectiveRuntimeState objectiveState)
    {
        if (!objectiveState.Definition.ShowProgress)
        {
            return string.Empty;
        }

        if (objectiveState.Definition.RequiredAmount <= 1)
        {
            return string.Empty;
        }

        return $"{objectiveState.CurrentAmount}/{objectiveState.Definition.RequiredAmount}";
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
