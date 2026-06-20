using UnityEngine;
using UnityEngine.InputSystem;

public sealed class NotificationDebugTester : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Manager que vamos a usar para disparar notificaciones de prueba.")]
    [SerializeField] private NotificationManager notificationManager;

    [Header("Teclas de prueba")]
    [Tooltip("Dispara una notificación de misión.")]
    [SerializeField] private Key missionNotificationKey = Key.N;

    [Tooltip("Dispara una notificación de combate.")]
    [SerializeField] private Key combatNotificationKey = Key.V;

    [Tooltip("Dispara varias notificaciones seguidas para probar cola y máximo visible.")]
    [SerializeField] private Key burstNotificationKey = Key.B;

    [Tooltip("Dispara dos notificaciones iguales para probar el anti-spam.")]
    [SerializeField] private Key duplicateNotificationKey = Key.M;

    private void Reset()
    {
        notificationManager = FindFirstObjectByType<NotificationManager>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (WasPressed(keyboard, missionNotificationKey))
        {
            ShowMissionNotification();
        }

        if (WasPressed(keyboard, combatNotificationKey))
        {
            ShowCombatNotification();
        }

        if (WasPressed(keyboard, burstNotificationKey))
        {
            ShowBurstNotifications();
        }

        if (WasPressed(keyboard, duplicateNotificationKey))
        {
            ShowDuplicateNotifications();
        }
    }

    private bool WasPressed(Keyboard keyboard, Key key)
    {
        return keyboard[key].wasPressedThisFrame;
    }

    private void ShowMissionNotification()
    {
        if (!HasManager())
        {
            return;
        }

        notificationManager.ShowMission(
            "Misión iniciada: El camino caído",
            NotificationPriority.High,
            "Misión");
    }

    private void ShowCombatNotification()
    {
        if (!HasManager())
        {
            return;
        }

        notificationManager.ShowCombat(
            "No puedes abrir el diario en combate.",
            NotificationPriority.Normal,
            "Combate");
    }

    private void ShowBurstNotifications()
    {
        if (!HasManager())
        {
            return;
        }

        notificationManager.ShowMission("Objetivo actualizado: Cruza el paso enemigo.", NotificationPriority.Normal, "Misión");
        notificationManager.ShowInventory("Nuevo artefacto obtenido: Hook.", NotificationPriority.High, "Inventario");
        notificationManager.ShowWorld("La puerta antigua parece reaccionar.", NotificationPriority.Normal, "Mundo");
        notificationManager.ShowSystem("Checkpoint futuro pendiente de implementar.", NotificationPriority.Low, "Sistema");
        notificationManager.ShowMission("Misión completada: El camino caído.", NotificationPriority.Critical, "Misión");
    }

    private void ShowDuplicateNotifications()
    {
        if (!HasManager())
        {
            return;
        }

        notificationManager.ShowCombat("No puedes gestionar misiones en combate.", NotificationPriority.Normal, "Combate");
        notificationManager.ShowCombat("No puedes gestionar misiones en combate.", NotificationPriority.Normal, "Combate");
    }

    private bool HasManager()
    {
        if (notificationManager != null)
        {
            return true;
        }

        Debug.LogWarning($"Falta asignar {nameof(NotificationManager)} en {nameof(NotificationDebugTester)}.", this);
        return false;
    }
}
