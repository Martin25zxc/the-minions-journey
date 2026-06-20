using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NotificationManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Opcional. Sirve para aplicar reglas especiales cuando el jugador está en combate.")]
    [SerializeField] private PlayerCombatState playerCombatState;

    [Header("Reglas de visualización")]
    [Tooltip("Cantidad máxima de notificaciones visibles al mismo tiempo.")]
    [SerializeField, Min(1)] private int maxVisibleNotifications = 3;

    [Tooltip("Cantidad máxima de notificaciones esperando en cola.")]
    [SerializeField, Min(0)] private int maxQueuedNotifications = 20;

    [Tooltip("Duración usada cuando la notificación no define una duración propia.")]
    [SerializeField, Min(0.25f)] private float defaultDuration = 3f;

    [Tooltip("Durante esta ventana, las notificaciones con la misma clave se agrupan para evitar spam.")]
    [SerializeField, Min(0f)] private float duplicateGroupingWindow = 0.75f;

    [Header("Combate")]
    [Tooltip("Si está activo, las notificaciones Low no se muestran durante combate. Útil para no ensuciar la pantalla.")]
    [SerializeField] private bool suppressLowPriorityInCombat;

    [Header("Debug")]
    [Tooltip("Muestra logs útiles mientras probamos la integración.")]
    [SerializeField] private bool logNotifications;

    private readonly Queue<NotificationData> queuedNotifications = new();
    private readonly List<NotificationData> visibleNotifications = new();
    private readonly Dictionary<string, float> lastAcceptedTimeByGroupKey = new();

    public int VisibleCount => visibleNotifications.Count;
    public int QueuedCount => queuedNotifications.Count;
    public int MaxVisibleNotifications => maxVisibleNotifications;
    public float DefaultDuration => defaultDuration;

    public event Action<NotificationData> NotificationShown;
    public event Action<NotificationData> NotificationQueued;
    public event Action<NotificationData> NotificationSuppressed;

    private void Reset()
    {
        playerCombatState = FindFirstObjectByType<PlayerCombatState>();
    }

    public bool ShowSystem(string message, NotificationPriority priority = NotificationPriority.Normal, string title = null)
    {
        return Show(NotificationData.Create(message, NotificationChannel.System, priority, defaultDuration, title));
    }

    public bool ShowCombat(string message, NotificationPriority priority = NotificationPriority.Normal, string title = null)
    {
        return Show(NotificationData.Create(message, NotificationChannel.Combat, priority, defaultDuration, title));
    }

    public bool ShowMission(string message, NotificationPriority priority = NotificationPriority.Normal, string title = null)
    {
        return Show(NotificationData.Create(message, NotificationChannel.Mission, priority, defaultDuration, title));
    }

    public bool ShowInventory(string message, NotificationPriority priority = NotificationPriority.Normal, string title = null)
    {
        return Show(NotificationData.Create(message, NotificationChannel.Inventory, priority, defaultDuration, title));
    }

    public bool ShowWorld(string message, NotificationPriority priority = NotificationPriority.Normal, string title = null)
    {
        return Show(NotificationData.Create(message, NotificationChannel.World, priority, defaultDuration, title));
    }

    public bool Show(NotificationData notification)
    {
        if (notification == null)
        {
            Debug.LogWarning($"{nameof(NotificationManager)} recibió una notificación null.", this);
            return false;
        }

        notification.CreatedAtTime = Time.unscaledTime;
        notification.ApplyDefaultDuration(defaultDuration);

        if (ShouldSuppress(notification))
        {
            if (logNotifications)
            {
                Debug.Log($"Notificación agrupada/filtrada: {notification.Message}", this);
            }

            NotificationSuppressed?.Invoke(notification);
            return false;
        }

        RegisterAccepted(notification);

        if (visibleNotifications.Count < maxVisibleNotifications)
        {
            ShowImmediately(notification);
            return true;
        }

        Enqueue(notification);
        return true;
    }

    public void NotifyToastFinished(NotificationData notification)
    {
        if (notification == null)
        {
            return;
        }

        visibleNotifications.RemoveAll(item => item.NotificationId == notification.NotificationId);
        TryShowNextQueuedNotification();
    }

    public void ClearAll()
    {
        visibleNotifications.Clear();
        queuedNotifications.Clear();
    }

    private bool ShouldSuppress(NotificationData notification)
    {
        if (suppressLowPriorityInCombat &&
            notification.Priority == NotificationPriority.Low &&
            playerCombatState != null &&
            playerCombatState.IsInCombat)
        {
            return true;
        }

        if (!notification.HasGroupKey || duplicateGroupingWindow <= 0f)
        {
            return false;
        }

        if (!lastAcceptedTimeByGroupKey.TryGetValue(notification.GroupKey, out float lastAcceptedTime))
        {
            return false;
        }

        return Time.unscaledTime - lastAcceptedTime <= duplicateGroupingWindow;
    }

    private void RegisterAccepted(NotificationData notification)
    {
        if (!notification.HasGroupKey)
        {
            return;
        }

        lastAcceptedTimeByGroupKey[notification.GroupKey] = Time.unscaledTime;
    }

    private void ShowImmediately(NotificationData notification)
    {
        visibleNotifications.Add(notification);

        if (logNotifications)
        {
            Debug.Log($"Mostrando notificación: [{notification.Channel}] {notification.Message}", this);
        }

        NotificationShown?.Invoke(notification);
    }

    private void Enqueue(NotificationData notification)
    {
        while (maxQueuedNotifications > 0 && queuedNotifications.Count >= maxQueuedNotifications)
        {
            queuedNotifications.Dequeue();
        }

        if (maxQueuedNotifications == 0)
        {
            if (logNotifications)
            {
                Debug.Log($"Cola deshabilitada. Se descarta: {notification.Message}", this);
            }

            NotificationSuppressed?.Invoke(notification);
            return;
        }

        queuedNotifications.Enqueue(notification);

        if (logNotifications)
        {
            Debug.Log($"Notificación enviada a cola: {notification.Message}", this);
        }

        NotificationQueued?.Invoke(notification);
    }

    private void TryShowNextQueuedNotification()
    {
        while (visibleNotifications.Count < maxVisibleNotifications && queuedNotifications.Count > 0)
        {
            ShowImmediately(queuedNotifications.Dequeue());
        }
    }
}
