using UnityEngine;

[DisallowMultipleComponent]
public sealed class NotificationToastPresenter : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Manager que decide qué notificaciones se muestran y cuándo.")]
    [SerializeField] private NotificationManager notificationManager;

    [Tooltip("Prefab visual del toast. Debe tener NotificationToastView.")]
    [SerializeField] private NotificationToastView toastPrefab;

    [Tooltip("Contenedor donde se instancian los toasts. Si está vacío, se usa este transform.")]
    [SerializeField] private Transform toastContainer;

    [Header("Debug")]
    [Tooltip("Muestra warnings cuando falta alguna referencia importante.")]
    [SerializeField] private bool logMissingReferences = true;

    private void Reset()
    {
        notificationManager = FindFirstObjectByType<NotificationManager>();
        toastContainer = transform;
    }

    private void OnEnable()
    {
        if (notificationManager == null)
        {
            notificationManager = FindFirstObjectByType<NotificationManager>();
        }

        if (notificationManager != null)
        {
            notificationManager.NotificationShown += HandleNotificationShown;
        }
        else if (logMissingReferences)
        {
            Debug.LogWarning($"{nameof(NotificationToastPresenter)} no encontró {nameof(NotificationManager)}.", this);
        }
    }

    private void OnDisable()
    {
        if (notificationManager != null)
        {
            notificationManager.NotificationShown -= HandleNotificationShown;
        }
    }

    private void HandleNotificationShown(NotificationData notification)
    {
        if (toastPrefab == null)
        {
            if (logMissingReferences)
            {
                Debug.LogWarning($"Falta asignar el prefab de toast en {nameof(NotificationToastPresenter)}.", this);
            }

            notificationManager.NotifyToastFinished(notification);
            return;
        }

        Transform parent = toastContainer != null ? toastContainer : transform;
        NotificationToastView toast = Instantiate(toastPrefab, parent);
        toast.Show(notification, HandleToastFinished);
    }

    private void HandleToastFinished(NotificationData notification)
    {
        if (notificationManager != null)
        {
            notificationManager.NotifyToastFinished(notification);
        }
    }
}
