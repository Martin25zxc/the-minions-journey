using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MissionObjectiveRowUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Texto principal del objetivo.")]
    private TMP_Text descriptionText;

    [SerializeField, Tooltip("Texto de progreso, por ejemplo 2/3. Puede quedar vacío si el objetivo no muestra progreso.")]
    private TMP_Text progressText;

    [SerializeField, Tooltip("Marca visual para objetivos completados. Puede ser un texto con ✓ o un ícono.")]
    private GameObject completedMarker;

    [SerializeField, Tooltip("CanvasGroup del root de la fila. Permite apagar raycast y asegurar alpha.")]
    private CanvasGroup canvasGroup;

    [SerializeField, Tooltip("LayoutElement del root de la fila. Se usa para ajustar altura de una o dos líneas.")]
    private LayoutElement layoutElement;

    [Header("Texto")]
    [SerializeField, Tooltip("Si está activo, el script fuerza wrapping/ellipsis para que los objetivos puedan ocupar dos líneas sin desbordar.")]
    private bool configureDescriptionText = true;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();

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
            else if (completedMarker == null && textName.Contains("Completed"))
            {
                completedMarker = texts[i].gameObject;
            }
        }
    }

    public void Render(MissionObjectiveRuntimeState objectiveState, bool allowDoubleLine, float singleLineHeight, float doubleLineHeight)
    {
        if (objectiveState == null || objectiveState.Definition == null)
        {
            RenderFallback(string.Empty, allowDoubleLine, singleLineHeight, doubleLineHeight);
            return;
        }

        string description = objectiveState.Definition.Description;
        if (string.IsNullOrWhiteSpace(description))
        {
            description = objectiveState.ObjectiveId;
        }

        SetDescription(description.Trim(), allowDoubleLine);
        SetProgress(GetProgressText(objectiveState));
        SetCompleted(objectiveState.IsCompleted);
        SetHeight(allowDoubleLine ? doubleLineHeight : singleLineHeight);
        SetVisible(true);
    }

    public void RenderFallback(string text, bool allowDoubleLine, float singleLineHeight, float doubleLineHeight)
    {
        SetDescription(text, allowDoubleLine);
        SetProgress(string.Empty);
        SetCompleted(false);
        SetHeight(allowDoubleLine ? doubleLineHeight : singleLineHeight);
        SetVisible(true);
    }

    private void SetDescription(string value, bool allowDoubleLine)
    {
        if (descriptionText == null)
        {
            return;
        }

        descriptionText.text = value;

        if (!configureDescriptionText)
        {
            return;
        }

        descriptionText.enableWordWrapping = allowDoubleLine;
        descriptionText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void SetProgress(string value)
    {
        if (progressText == null)
        {
            return;
        }

        bool hasProgress = !string.IsNullOrWhiteSpace(value);
        progressText.gameObject.SetActive(hasProgress);
        progressText.text = hasProgress ? value : string.Empty;
    }

    private void SetCompleted(bool isCompleted)
    {
        if (completedMarker != null)
        {
            completedMarker.SetActive(isCompleted);
        }
    }

    private void SetHeight(float preferredHeight)
    {
        if (layoutElement == null)
        {
            return;
        }

        layoutElement.minHeight = Mathf.Max(24f, preferredHeight - 8f);
        layoutElement.preferredHeight = Mathf.Max(24f, preferredHeight);
        layoutElement.flexibleHeight = 0f;
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
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
}