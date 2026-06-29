using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ajusta la altura preferida del DescriptionBlock del Mission Journal según el texto mostrado.
///
/// Uso esperado:
/// DescriptionBlock
/// ├── LayoutElement
/// └── DescriptionText
///
/// Este componente NO usa ContentSizeFitter. Solo escribe minHeight / preferredHeight /
/// flexibleHeight en el LayoutElement del bloque para convivir con el VerticalLayoutGroup
/// del Content del DetailScrollRect.
/// </summary>
[DisallowMultipleComponent]
public sealed class MissionJournalDescriptionBlockSizer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("LayoutElement del DescriptionBlock.")]
    private LayoutElement blockLayoutElement;

    [SerializeField, Tooltip("Texto de descripción de la misión.")]
    private TMP_Text descriptionText;

    [SerializeField, Tooltip("RectTransform usado para calcular el ancho disponible. Si queda vacío, usa DescriptionText.")]
    private RectTransform widthReference;

    [Header("Altura")]
    [SerializeField, Min(40f), Tooltip("Altura mínima del DescriptionBlock. Mantiene presencia visual aun con textos cortos.")]
    private float minHeight = 130f;

    [SerializeField, Min(40f), Tooltip("Altura máxima del DescriptionBlock. Evita que la descripción empuje demasiado Objetivos/Recompensas.")]
    private float maxHeight = 400f;

    [SerializeField, Min(0f), Tooltip("Espacio extra vertical para padding visual del bloque.")]
    private float verticalPadding = 32f;

    [SerializeField, Min(1f), Tooltip("Redondea la altura a múltiplos de este valor para evitar alturas raras.")]
    private float heightStep = 8f;

    [Header("Fallback por largo")]
    [SerializeField, Tooltip("Si TMP no puede calcular bien por ancho 0, usa umbrales por cantidad de caracteres.")]
    private bool useLengthFallback = true;

    [SerializeField, Min(1), Tooltip("A partir de esta cantidad de caracteres usa altura media si el cálculo TMP no está disponible.")]
    private int mediumTextCharacterThreshold = 180;

    [SerializeField, Min(1), Tooltip("A partir de esta cantidad de caracteres usa altura alta si el cálculo TMP no está disponible.")]
    private int longTextCharacterThreshold = 360;

    [SerializeField, Min(40f), Tooltip("Altura fallback para textos cortos.")]
    private float fallbackShortHeight = 150f;

    [SerializeField, Min(40f), Tooltip("Altura fallback para textos medios.")]
    private float fallbackMediumHeight = 240f;

    [SerializeField, Min(40f), Tooltip("Altura fallback para textos largos.")]
    private float fallbackLongHeight = 340f;

    [Header("Debug")]
    [SerializeField, Tooltip("Muestra logs útiles para ajustar alturas durante pruebas.")]
    private bool logDebug;

    private void Reset()
    {
        blockLayoutElement = GetComponent<LayoutElement>();
        descriptionText = GetComponentInChildren<TMP_Text>(true);

        if (descriptionText != null)
        {
            widthReference = descriptionText.rectTransform;
        }
    }

    private void Awake()
    {
        if (blockLayoutElement == null)
        {
            blockLayoutElement = GetComponent<LayoutElement>();
        }

        if (descriptionText == null)
        {
            descriptionText = GetComponentInChildren<TMP_Text>(true);
        }

        if (widthReference == null && descriptionText != null)
        {
            widthReference = descriptionText.rectTransform;
        }
    }

    public void RefreshSize()
    {
        string text = descriptionText != null ? descriptionText.text : string.Empty;
        RefreshSize(text);
    }

    public void RefreshSize(string description)
    {
        if (blockLayoutElement == null)
        {
            return;
        }

        float resolvedHeight = ResolveHeight(description);

        blockLayoutElement.minHeight = resolvedHeight;
        blockLayoutElement.preferredHeight = resolvedHeight;
        blockLayoutElement.flexibleHeight = 0f;

        if (logDebug)
        {
            Debug.Log($"{nameof(MissionJournalDescriptionBlockSizer)} resolved height: {resolvedHeight} for text length {GetLength(description)}.", this);
        }
    }

    private float ResolveHeight(string description)
    {
        float calculatedTextHeight = TryCalculateTextPreferredHeight(description);

        if (calculatedTextHeight <= 0f && useLengthFallback)
        {
            calculatedTextHeight = ResolveFallbackBaseHeight(description);
        }

        if (calculatedTextHeight <= 0f)
        {
            calculatedTextHeight = minHeight;
        }

        float totalHeight = calculatedTextHeight + verticalPadding;
        totalHeight = Mathf.Clamp(totalHeight, minHeight, maxHeight);

        if (heightStep > 1f)
        {
            totalHeight = Mathf.Ceil(totalHeight / heightStep) * heightStep;
        }

        return totalHeight;
    }

    private float TryCalculateTextPreferredHeight(string description)
    {
        if (descriptionText == null)
        {
            return 0f;
        }

        string text = string.IsNullOrWhiteSpace(description)
            ? descriptionText.text
            : description;

        if (string.IsNullOrWhiteSpace(text))
        {
            return 0f;
        }

        float availableWidth = GetAvailableWidth();
        if (availableWidth <= 1f)
        {
            return 0f;
        }

        Vector2 preferred = descriptionText.GetPreferredValues(text, availableWidth, 0f);
        return preferred.y;
    }

    private float GetAvailableWidth()
    {
        if (widthReference != null)
        {
            return widthReference.rect.width;
        }

        RectTransform ownRect = transform as RectTransform;
        return ownRect != null ? ownRect.rect.width : 0f;
    }

    private float ResolveFallbackBaseHeight(string description)
    {
        int length = GetLength(description);

        if (length >= longTextCharacterThreshold)
        {
            return fallbackLongHeight;
        }

        if (length >= mediumTextCharacterThreshold)
        {
            return fallbackMediumHeight;
        }

        return fallbackShortHeight;
    }

    private static int GetLength(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? 0 : value.Length;
    }
}
