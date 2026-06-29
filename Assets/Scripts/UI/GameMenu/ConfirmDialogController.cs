using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Modal simple de confirmación para acciones peligrosas del PauseMenu.
/// </summary>
[DisallowMultipleComponent]
public sealed class ConfirmDialogController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField, Tooltip("CanvasGroup del ConfirmDialogRoot.")]
    private CanvasGroup rootCanvasGroup;

    [SerializeField, Tooltip("Si está activo, además del CanvasGroup activa/desactiva el GameObject del root. Recomendado: false si este script vive en el root.")]
    private bool setRootGameObjectActive;

    [Header("Textos")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private TMP_Text cancelButtonText;

    [Header("Botones")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Salida")]
    [SerializeField, Tooltip("Qué hace ConfirmButton al confirmar salida.")]
    private ConfirmDialogExitMode exitMode = ConfirmDialogExitMode.NotificationOnly;

    [SerializeField, Tooltip("Nombre de escena a cargar si Exit Mode = Load Scene. Debe estar en Build Settings.")]
    private string mainMenuSceneName = "";

    [SerializeField, Tooltip("Mensaje para WebGL/prototipo si no se quiere cerrar/cambiar escena todavía.")]
    private string notificationOnlyMessage = "Salida confirmada. La salida real se conectará cuando esté definido el flujo final.";

    [Header("Contenido por defecto")]
    [SerializeField] private string defaultTitle = "Salir";
    [SerializeField] private string defaultMessage = "¿Seguro que quieres salir?";
    [SerializeField] private string defaultConfirmText = "Sí, salir";
    [SerializeField] private string defaultCancelText = "Cancelar";

    [Header("Selección UI")]
    [SerializeField, Tooltip("Limpia el selected del EventSystem al cerrar/confirmar para evitar botones marcados.")]
    private bool clearSelectionOnClose = true;

    [Header("Debug")]
    [SerializeField] private bool logDebug;

    public bool IsOpen { get; private set; }

    public event Action<ConfirmDialogController> DialogShown;
    public event Action<ConfirmDialogController> DialogHidden;

    private void Reset()
    {
        rootCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = GetComponent<CanvasGroup>();
        }

        WireButtons();
        HideInstant();
    }

    private void OnEnable()
    {
        WireButtons();
    }

    private void OnDisable()
    {
        UnwireButtons();
    }

    public void ShowExitConfirmation()
    {
        Show(defaultTitle, defaultMessage, defaultConfirmText, defaultCancelText);
    }

    public void Show(string title, string message, string confirmText, string cancelText)
    {
        SetText(titleText, string.IsNullOrWhiteSpace(title) ? defaultTitle : title);
        SetText(messageText, string.IsNullOrWhiteSpace(message) ? defaultMessage : message);
        SetText(confirmButtonText, string.IsNullOrWhiteSpace(confirmText) ? defaultConfirmText : confirmText);
        SetText(cancelButtonText, string.IsNullOrWhiteSpace(cancelText) ? defaultCancelText : cancelText);

        SetVisible(true);

        if (logDebug)
        {
            Debug.Log("ConfirmDialog abierto.", this);
        }
    }

    public void Cancel()
    {
        HideInstant();
    }

    public void Confirm()
    {
        ClearSelectionIfNeeded();

        if (logDebug)
        {
            Debug.Log($"ConfirmDialog confirmado. ExitMode: {exitMode}.", this);
        }

        switch (exitMode)
        {
            case ConfirmDialogExitMode.LoadScene:
                ConfirmLoadScene();
                break;

            case ConfirmDialogExitMode.QuitApplication:
                ConfirmQuitApplication();
                break;

            case ConfirmDialogExitMode.NotificationOnly:
            default:
                ConfirmNotificationOnly();
                break;
        }
    }

    public void HideInstant()
    {
        SetVisible(false);
    }

    private void ConfirmLoadScene()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            TMJNotifications.ShowSystem(
                "No hay escena de menú configurada para salir.",
                NotificationPriority.Normal,
                title: "Salir",
                groupKey: "exit_missing_scene",
                context: this);
            HideInstant();
            return;
        }

        // Si venimos de PauseMenu, TimeScale suele estar en 0.
        // Antes de cargar escena conviene restaurarlo para no contaminar la escena siguiente.
        Time.timeScale = 1f;
        ClearSelectionIfNeeded();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ConfirmQuitApplication()
    {
        Time.timeScale = 1f;
        ClearSelectionIfNeeded();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ConfirmNotificationOnly()
    {
        if (!string.IsNullOrWhiteSpace(notificationOnlyMessage))
        {
            TMJNotifications.ShowSystem(
                notificationOnlyMessage,
                NotificationPriority.Normal,
                title: "Salir",
                groupKey: "exit_notification_only",
                context: this);
        }

        HideInstant();
    }

    private void SetVisible(bool visible)
    {
        bool changed = IsOpen != visible;
        IsOpen = visible;

        if (setRootGameObjectActive && gameObject.activeSelf != visible)
        {
            gameObject.SetActive(visible);
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }

        if (!visible)
        {
            ClearSelectionIfNeeded();
        }

        if (!changed)
        {
            return;
        }

        if (visible)
        {
            DialogShown?.Invoke(this);
        }
        else
        {
            DialogHidden?.Invoke(this);
        }
    }

    private void WireButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(Confirm);
            confirmButton.onClick.AddListener(Confirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(Cancel);
            cancelButton.onClick.AddListener(Cancel);
        }
    }

    private void UnwireButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(Confirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(Cancel);
        }
    }

    private void ClearSelectionIfNeeded()
    {
        if (!clearSelectionOnClose || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
