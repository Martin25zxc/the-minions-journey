using UnityEngine;

/// <summary>
/// Acción simple de escena para mostrar una notificación narrativa o contextual.
///
/// Está pensada para ser llamada desde MissionSceneResponseBlock.Custom Scene Actions
/// mediante UnityEvent. No decide cuándo ocurre: el MissionSceneResponder lo decide.
/// </summary>
[DisallowMultipleComponent]
public sealed class SceneNotificationAction : MonoBehaviour
{
    [Header("Notification")]
    [SerializeField]
    private string title;

    [SerializeField, TextArea(2, 5)]
    private string message;

    [SerializeField]
    private NotificationChannel channel = NotificationChannel.World;

    [SerializeField]
    private NotificationPriority priority = NotificationPriority.High;

    [SerializeField, Tooltip("Opcional. Si queda vacío, TMJNotifications genera una key según canal y mensaje.")]
    private string groupKey;

    [Header("Execution")]
    [SerializeField, Tooltip("Evita mostrar la misma notificación más de una vez desde este componente durante la sesión.")]
    private bool executeOnlyOnce = true;

    [SerializeField, Tooltip("Solo lectura en Play Mode. Se puede resetear con el Context Menu.")]
    private bool hasExecuted;

    [SerializeField]
    private bool logDebug;

    [ContextMenu("Show")]
    public void Show()
    {
        if (executeOnlyOnce && hasExecuted)
        {
            if (logDebug)
            {
                Debug.Log($"{nameof(SceneNotificationAction)} '{name}' ignored Show because it already executed.", this);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning($"{nameof(SceneNotificationAction)} '{name}' has an empty message.", this);
            return;
        }

        bool shown = TMJNotifications.Show(
            message,
            channel,
            priority,
            title,
            groupKey,
            this);

        if (shown)
        {
            hasExecuted = true;
        }

        if (logDebug)
        {
            string result = shown ? "shown" : "not shown";
            Debug.Log($"{nameof(SceneNotificationAction)} '{name}' {result}: {message}", this);
        }
    }

    [ContextMenu("Reset Execution State")]
    public void ResetExecutionState()
    {
        hasExecuted = false;
    }
}
