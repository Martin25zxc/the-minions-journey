using UnityEngine;

public static class TMJNotifications
{
    private static NotificationManager manager;
    private static bool hasWarnedMissingManager;

    public static void Register(NotificationManager notificationManager)
    {
        if (notificationManager == null)
        {
            return;
        }

        if (manager != null && manager != notificationManager)
        {
            Debug.LogWarning(
                $"Se registró más de un {nameof(NotificationManager)}. Se usará el último activo.",
                notificationManager);
        }

        manager = notificationManager;
        hasWarnedMissingManager = false;
    }

    public static void Unregister(NotificationManager notificationManager)
    {
        if (manager == notificationManager)
        {
            manager = null;
        }
    }

    public static bool Show(
        string message,
        NotificationChannel channel = NotificationChannel.System,
        NotificationPriority priority = NotificationPriority.Normal,
        string title = null,
        string groupKey = null,
        Object context = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        NotificationManager resolvedManager = ResolveManager(context);

        if (resolvedManager == null)
        {
            return false;
        }

        return resolvedManager.Show(NotificationData.Create(message, channel, priority, -1f, title, groupKey));
    }

    public static bool ShowSystem(string message, NotificationPriority priority = NotificationPriority.Normal, string title = null, string groupKey = null, Object context = null)
    {
        return Show(message, NotificationChannel.System, priority, title, groupKey, context);
    }

    public static bool ShowMission(string message, NotificationPriority priority = NotificationPriority.Normal, string title = null, string groupKey = null, Object context = null)
    {
        return Show(message, NotificationChannel.Mission, priority, title, groupKey, context);
    }

    public static bool ShowInventory(string message, NotificationPriority priority = NotificationPriority.Normal, string title = null, string groupKey = null, Object context = null)
    {
        return Show(message, NotificationChannel.Inventory, priority, title, groupKey, context);
    }

    public static bool ShowCombat(string message, NotificationPriority priority = NotificationPriority.Normal, string title = null, string groupKey = null, Object context = null)
    {
        return Show(message, NotificationChannel.Combat, priority, title, groupKey, context);
    }

    public static bool ShowWorld(string message, NotificationPriority priority = NotificationPriority.Normal, string title = null, string groupKey = null, Object context = null)
    {
        return Show(message, NotificationChannel.World, priority, title, groupKey, context);
    }

    public static bool ShowLore(string message, NotificationPriority priority = NotificationPriority.Normal, string title = null, string groupKey = null, Object context = null)
    {
        return Show(message, NotificationChannel.Lore, priority, title, groupKey, context);
    }

    private static NotificationManager ResolveManager(Object context)
    {
        if (manager != null)
        {
            return manager;
        }

        manager = Object.FindFirstObjectByType<NotificationManager>();

        if (manager == null && !hasWarnedMissingManager)
        {
            Debug.LogWarning(
                $"No hay {nameof(NotificationManager)} activo en escena. Las notificaciones se ignorarán.",
                context);

            hasWarnedMissingManager = true;
        }

        return manager;
    }
}
