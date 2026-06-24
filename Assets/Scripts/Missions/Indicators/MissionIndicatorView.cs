using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MissionIndicatorView : MonoBehaviour
{
    [Header("Referencias visuales")]
    [SerializeField, Tooltip("CanvasGroup del indicador. Sirve para mostrar/ocultar sin destruir el objeto.")]
    private CanvasGroup canvasGroup;

    [SerializeField, Tooltip("Texto TMP que muestra el símbolo, por ejemplo ! o ?.")]
    private TMP_Text indicatorText;

    [Header("Símbolos")]
    [SerializeField, Tooltip("Símbolo para misión disponible.")]
    private string availableSymbol = "!";

    [SerializeField, Tooltip("Símbolo para misión relacionada pero todavía no lista para entregar.")]
    private string pendingTurnInSymbol = "?";

    [SerializeField, Tooltip("Símbolo para misión lista para entregar.")]
    private string readyToTurnInSymbol = "?";

    [Header("Colores")]
    [SerializeField, Tooltip("Color para misión disponible. Amarillo/dorado suele ser lo más reconocible.")]
    private Color availableColor = new Color(1f, 0.82f, 0.18f, 1f);

    [SerializeField, Tooltip("Color para misión relacionada pero todavía incompleta. Mantenerlo apagado para que no parezca entregable.")]
    private Color pendingTurnInColor = new Color(0.65f, 0.65f, 0.65f, 0.75f);

    [SerializeField, Tooltip("Color para misión lista para entregar. Amarillo/dorado comunica prioridad.")]
    private Color readyToTurnInColor = new Color(1f, 0.82f, 0.18f, 1f);

    [Header("Comportamiento")]
    [SerializeField, Tooltip("Si está activo, el GameObject se activa/desactiva además de cambiar el alpha. Útil para no dibujar nada cuando no hay indicador.")]
    private bool toggleGameObjectWhenHidden;

    [SerializeField, Tooltip("Estado inicial para probar en Editor. En gameplay lo controla MissionActorIndicator.")]
    private MissionIndicatorState initialState = MissionIndicatorState.None;

    public MissionIndicatorState CurrentState { get; private set; } = MissionIndicatorState.None;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        indicatorText = GetComponentInChildren<TMP_Text>();
    }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (indicatorText == null)
        {
            indicatorText = GetComponentInChildren<TMP_Text>();
        }

        SetState(initialState);
    }

    public void SetState(MissionIndicatorState state)
    {
        CurrentState = state;

        if (state == MissionIndicatorState.None)
        {
            Hide();
            return;
        }

        Show();

        if (indicatorText == null)
        {
            return;
        }

        switch (state)
        {
            case MissionIndicatorState.MissionAvailable:
                indicatorText.text = availableSymbol;
                indicatorText.color = availableColor;
                break;

            case MissionIndicatorState.TurnInPending:
                indicatorText.text = pendingTurnInSymbol;
                indicatorText.color = pendingTurnInColor;
                break;

            case MissionIndicatorState.ReadyToTurnIn:
                indicatorText.text = readyToTurnInSymbol;
                indicatorText.color = readyToTurnInColor;
                break;
        }
    }

    public void Hide()
    {
        if (indicatorText != null)
        {
            indicatorText.text = string.Empty;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (toggleGameObjectWhenHidden)
        {
            gameObject.SetActive(false);
        }
    }

    private void Show()
    {
        if (toggleGameObjectWhenHidden && !gameObject.activeSelf)
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
}
