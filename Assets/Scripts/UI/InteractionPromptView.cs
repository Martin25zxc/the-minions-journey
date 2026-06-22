using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractionPromptView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private TextMeshProUGUI promptText;

    [Header("Behaviour")]
    [SerializeField]
    private bool hideGameObjectWhenHidden;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        promptText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (promptText == null)
        {
            promptText = GetComponentInChildren<TextMeshProUGUI>();
        }

        Hide();
    }

    public void Show(string text)
    {
        if (promptText != null)
        {
            promptText.text = text;
        }

        if (hideGameObjectWhenHidden && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (hideGameObjectWhenHidden)
        {
            gameObject.SetActive(false);
        }
    }
}
