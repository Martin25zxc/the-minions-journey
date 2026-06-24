using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MissionObjectiveRowUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Texto principal del objetivo. Ejemplo: Busca hongos luminosos.")]
    private TMP_Text descriptionText;

    [SerializeField, Tooltip("Texto de progreso. Ejemplo: 2/3. Puede quedar vacío en objetivos únicos.")]
    private TMP_Text progressText;

    [SerializeField, Tooltip("Marca visual simple para objetivos completos. Puede ser un texto con ✓ o un icono.")]
    private GameObject completedMarker;

    [SerializeField, Tooltip("Opcional. Sirve para bajar opacidad sin apagar el objeto entero.")]
    private CanvasGroup canvasGroup;

    [Header("Texto")]
    [SerializeField, Tooltip("Texto que se usa si la descripción del objetivo está vacía.")]
    private string fallbackDescription = "Objetivo";

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length > 0)
        {
            descriptionText = texts[0];
        }

        if (texts.Length > 1)
        {
            progressText = texts[1];
        }
    }

    public void Bind(MissionObjectiveRuntimeState objectiveState, bool showCompletedMarker, bool forceHideProgress)
    {
        if (objectiveState == null)
        {
            SetVisible(false);
            return;
        }

        MissionObjectiveDefinition definition = objectiveState.Definition;

        if (descriptionText != null)
        {
            string description = string.IsNullOrWhiteSpace(definition.Description)
                ? fallbackDescription
                : definition.Description.Trim();

            descriptionText.text = description;
        }

        if (progressText != null)
        {
            bool shouldShowProgress = !forceHideProgress && definition.ShowProgress && objectiveState.RequiredAmount > 1;
            progressText.text = shouldShowProgress ? objectiveState.GetProgressText() : string.Empty;
            progressText.gameObject.SetActive(shouldShowProgress);
        }

        if (completedMarker != null)
        {
            completedMarker.SetActive(showCompletedMarker && objectiveState.IsCompleted);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = objectiveState.IsCompleted ? 0.65f : 1f;
        }

        SetVisible(true);
    }

    public void BindCustomText(string description, string progress, bool completed)
    {
        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrWhiteSpace(description) ? fallbackDescription : description.Trim();
        }

        if (progressText != null)
        {
            bool hasProgress = !string.IsNullOrWhiteSpace(progress);
            progressText.text = hasProgress ? progress.Trim() : string.Empty;
            progressText.gameObject.SetActive(hasProgress);
        }

        if (completedMarker != null)
        {
            completedMarker.SetActive(completed);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = completed ? 0.65f : 1f;
        }

        SetVisible(true);
    }

    public void SetVisible(bool value)
    {
        gameObject.SetActive(value);
    }
}
