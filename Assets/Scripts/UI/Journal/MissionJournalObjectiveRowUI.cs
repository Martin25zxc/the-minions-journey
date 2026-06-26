using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fila de objetivo específica para el Journal.
/// 
/// A diferencia del HUD:
/// - Usa una sola línea visual: marcador + descripción + progreso.
/// - Muestra progreso incluso para objetivos 0/1.
/// - Mantiene ProgressText activo para conservar el ancho de la columna.
/// - No es interactiva.
/// - Está pensada para lectura dentro del Mission Journal.
/// 
/// Jerarquía esperada:
/// PF_MissionJournalObjectiveRow
/// ├── StateMarkerText
/// ├── DescriptionText
/// └── ProgressText
/// </summary>
[DisallowMultipleComponent]
public sealed class MissionJournalObjectiveRowUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Texto de marcador visual. Ejemplo: ○ o X.")]
    private TMP_Text stateMarkerText;

    [SerializeField, Tooltip("Texto principal del objetivo.")]
    private TMP_Text descriptionText;

    [SerializeField, Tooltip("Texto de progreso. Ejemplo: 0/1, 2/3, 3/3.")]
    private TMP_Text progressText;

    [SerializeField, Tooltip("CanvasGroup del root de la fila. La fila no debería bloquear raycasts.")]
    private CanvasGroup canvasGroup;

    [SerializeField, Tooltip("LayoutElement del root de la fila. Permite fijar altura estable dentro del Journal.")]
    private LayoutElement layoutElement;

    [Header("Presentación")]
    [SerializeField, Min(36f), Tooltip("Altura estable de la fila dentro del Journal. Sin MetaText, 52-60 suele ser suficiente.")]
    private float preferredHeight = 56f;

    [SerializeField, Tooltip("Marcador usado cuando el objetivo está pendiente.")]
    private string pendingMarker = "○";

    [SerializeField, Tooltip("Marcador usado cuando el objetivo está completado. Se usa X si el font no tiene buen soporte para otros símbolos.")]
    private string completedMarker = "X";

    [SerializeField, Tooltip("Si está activo, los objetivos Bonus agregan un prefijo visible en la descripción.")]
    private bool prefixBonusObjectives = true;

    [SerializeField, Tooltip("Prefijo usado para objetivos Bonus. Solo se usa si Prefix Bonus Objectives está activo.")]
    private string bonusPrefix = "Opcional: ";

    [Header("Progreso")]
    [SerializeField, Tooltip("Si está activo, el Journal muestra progreso numérico.")]
    private bool showProgressInJournal = true;

    [SerializeField, Tooltip("Si está activo, respeta ObjectiveDefinition.ShowProgress. Para Journal suele convenir dejarlo apagado si querés ver siempre 0/1, 1/1, 0/3, etc.")]
    private bool respectObjectiveShowProgress;

    [SerializeField, Tooltip("Si está activo, muestra también objetivos de una sola unidad como 0/1 o 1/1.")]
    private bool showSingleStepProgress = true;

    [SerializeField, Tooltip("Mantiene ProgressText activo aunque esté vacío para conservar el layout.")]
    private bool keepProgressColumnWhenEmpty = true;

    [Header("Texto")]
    [SerializeField, Tooltip("Si está activo, configura wrapping/ellipsis usando textWrappingMode, evitando enableWordWrapping obsoleto.")]
    private bool configureTextComponents = true;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            string textName = texts[i].name;

            if (stateMarkerText == null && textName.Contains("Marker"))
            {
                stateMarkerText = texts[i];
            }
            else if (descriptionText == null && textName.Contains("Description"))
            {
                descriptionText = texts[i];
            }
            else if (progressText == null && textName.Contains("Progress"))
            {
                progressText = texts[i];
            }
        }
    }

    public void Render(MissionObjectiveRuntimeState objectiveState)
    {
        if (objectiveState == null || objectiveState.Definition == null)
        {
            RenderFallback("Objetivo no disponible.");
            return;
        }

        SetText(stateMarkerText, ResolveMarker(objectiveState));
        SetText(descriptionText, ResolveDescription(objectiveState));
        SetProgress(BuildProgressText(objectiveState));

        ConfigureTexts();
        ApplyLayoutHeight();
        SetVisible(true);
        RebuildRowLayout();
    }

    public void RenderFallback(string message)
    {
        SetText(stateMarkerText, pendingMarker);
        SetText(descriptionText, string.IsNullOrWhiteSpace(message) ? "Sin objetivos visibles." : message);
        SetProgress(string.Empty);

        ConfigureTexts();
        ApplyLayoutHeight();
        SetVisible(true);
        RebuildRowLayout();
    }

    private string ResolveMarker(MissionObjectiveRuntimeState objectiveState)
    {
        return objectiveState.IsCompleted ? completedMarker : pendingMarker;
    }

    private string ResolveDescription(MissionObjectiveRuntimeState objectiveState)
    {
        string description = objectiveState.Definition.Description;

        if (string.IsNullOrWhiteSpace(description))
        {
            description = string.IsNullOrWhiteSpace(objectiveState.ObjectiveId)
                ? "Objetivo sin descripción."
                : objectiveState.ObjectiveId;
        }
        else
        {
            description = description.Trim();
        }

        if (prefixBonusObjectives && objectiveState.IsBonus && !description.StartsWith(bonusPrefix))
        {
            return $"{bonusPrefix}{description}";
        }

        return description;
    }

    private string BuildProgressText(MissionObjectiveRuntimeState objectiveState)
    {
        if (!showProgressInJournal)
        {
            return string.Empty;
        }

        if (respectObjectiveShowProgress && !objectiveState.Definition.ShowProgress)
        {
            return string.Empty;
        }

        if (!showSingleStepProgress && objectiveState.RequiredAmount <= 1)
        {
            return string.Empty;
        }

        return objectiveState.GetProgressText();
    }

    private void SetProgress(string value)
    {
        if (progressText == null)
        {
            return;
        }

        bool hasValue = !string.IsNullOrWhiteSpace(value);

        // En Journal mantenemos la columna activa para que DescriptionText no cambie de ancho
        // entre objetivos con progreso visible y objetivos sin progreso visible.
        progressText.gameObject.SetActive(hasValue || keepProgressColumnWhenEmpty);
        progressText.text = hasValue ? value : string.Empty;
    }

    private void ConfigureTexts()
    {
        if (!configureTextComponents)
        {
            return;
        }

        if (descriptionText != null)
        {
            descriptionText.textWrappingMode = TextWrappingModes.Normal;
            descriptionText.overflowMode = TextOverflowModes.Ellipsis;
            descriptionText.raycastTarget = false;
        }

        if (stateMarkerText != null)
        {
            stateMarkerText.textWrappingMode = TextWrappingModes.NoWrap;
            stateMarkerText.overflowMode = TextOverflowModes.Ellipsis;
            stateMarkerText.raycastTarget = false;
        }

        if (progressText != null)
        {
            progressText.textWrappingMode = TextWrappingModes.NoWrap;
            progressText.overflowMode = TextOverflowModes.Ellipsis;
            progressText.raycastTarget = false;
        }
    }

    private void ApplyLayoutHeight()
    {
        if (layoutElement == null)
        {
            return;
        }

        float height = Mathf.Max(36f, preferredHeight);

        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
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

    private void RebuildRowLayout()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        // Se llama solo al renderizar, no en Update.
        // Evita que TMP/LayoutGroup/ContentSizeFitter resuelvan tarde la fila instanciada.
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
