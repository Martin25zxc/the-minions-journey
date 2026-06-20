using System;

public sealed class NotificationData
{
    private static int nextId;

    public int NotificationId { get; }
    public string Title { get; }
    public string Message { get; }
    public NotificationChannel Channel { get; }
    public NotificationPriority Priority { get; }
    public float Duration { get; private set; }
    public string GroupKey { get; }
    public float CreatedAtTime { get; internal set; }

    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
    public bool HasGroupKey => !string.IsNullOrWhiteSpace(GroupKey);

    public NotificationData(
        string title,
        string message,
        NotificationChannel channel,
        NotificationPriority priority,
        float duration,
        string groupKey = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("La notificación necesita un mensaje.", nameof(message));
        }

        NotificationId = ++nextId;
        Title = title?.Trim() ?? string.Empty;
        Message = message.Trim();
        Channel = channel;
        Priority = priority;
        Duration = duration;
        GroupKey = string.IsNullOrWhiteSpace(groupKey) ? BuildDefaultGroupKey(channel, message) : groupKey.Trim();
    }

    public static NotificationData Create(
        string message,
        NotificationChannel channel = NotificationChannel.System,
        NotificationPriority priority = NotificationPriority.Normal,
        float duration = -1f,
        string title = null,
        string groupKey = null)
    {
        return new NotificationData(title, message, channel, priority, duration, groupKey);
    }

    internal void ApplyDefaultDuration(float fallbackDuration)
    {
        if (Duration <= 0f)
        {
            Duration = fallbackDuration;
        }
    }

    private static string BuildDefaultGroupKey(NotificationChannel channel, string message)
    {
        return $"{channel}:{message.Trim().ToLowerInvariant()}";
    }
}
