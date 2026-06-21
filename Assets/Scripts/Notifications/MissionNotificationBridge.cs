using UnityEngine;

[DisallowMultipleComponent]
public sealed class MissionNotificationBridge : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Manager de misiones que emite eventos runtime.")]
    private MissionManager missionManager;

    [SerializeField, Tooltip("Manager genérico de notificaciones. Este bridge solo traduce eventos de misión a toasts.")]
    private NotificationManager notificationManager;

    [Header("Qué mostrar")]
    [SerializeField, Tooltip("Muestra toast cuando se acepta una misión.")]
    private bool showMissionAccepted = true;

    [SerializeField, Tooltip("Muestra toast cuando cambia el progreso de un objetivo.")]
    private bool showObjectiveUpdated = true;

    [SerializeField, Tooltip("Muestra toast cuando un objetivo se completa.")]
    private bool showObjectiveCompleted = true;

    [SerializeField, Tooltip("Muestra toast cuando una misión queda lista para entregar.")]
    private bool showMissionReadyToTurnIn = true;

    [SerializeField, Tooltip("Muestra toast cuando una misión se completa.")]
    private bool showMissionCompleted = true;

    private void Reset()
    {
        missionManager = FindFirstObjectByType<MissionManager>();
        notificationManager = FindFirstObjectByType<NotificationManager>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (missionManager == null)
        {
            return;
        }

        missionManager.MissionAccepted += HandleMissionAccepted;
        missionManager.ObjectiveUpdated += HandleObjectiveUpdated;
        missionManager.ObjectiveCompleted += HandleObjectiveCompleted;
        missionManager.MissionReadyToTurnIn += HandleMissionReadyToTurnIn;
        missionManager.MissionCompleted += HandleMissionCompleted;
    }

    private void Unsubscribe()
    {
        if (missionManager == null)
        {
            return;
        }

        missionManager.MissionAccepted -= HandleMissionAccepted;
        missionManager.ObjectiveUpdated -= HandleObjectiveUpdated;
        missionManager.ObjectiveCompleted -= HandleObjectiveCompleted;
        missionManager.MissionReadyToTurnIn -= HandleMissionReadyToTurnIn;
        missionManager.MissionCompleted -= HandleMissionCompleted;
    }

    private void HandleMissionAccepted(MissionRuntimeState missionState)
    {
        if (!showMissionAccepted)
        {
            return;
        }

        ShowMissionNotification(
            title: "Misión iniciada",
            message: GetMissionTitle(missionState),
            priority: NotificationPriority.Normal,
            groupKey: $"mission_accepted:{missionState?.MissionId}");
    }

    private void HandleObjectiveUpdated(MissionRuntimeState missionState, MissionObjectiveRuntimeState objectiveState)
    {
        if (!showObjectiveUpdated || objectiveState == null || objectiveState.IsCompleted)
        {
            return;
        }

        if (!objectiveState.Definition.ShowProgress)
        {
            return;
        }

        string description = GetObjectiveDescription(objectiveState);
        string message = $"{description} {objectiveState.GetProgressText()}";

        ShowMissionNotification(
            title: "Objetivo actualizado",
            message: message,
            priority: NotificationPriority.Low,
            groupKey: $"objective_updated:{missionState?.MissionId}:{objectiveState.ObjectiveId}");
    }

    private void HandleObjectiveCompleted(MissionRuntimeState missionState, MissionObjectiveRuntimeState objectiveState)
    {
        if (!showObjectiveCompleted || objectiveState == null)
        {
            return;
        }

        ShowMissionNotification(
            title: "Objetivo completado",
            message: GetObjectiveDescription(objectiveState),
            priority: NotificationPriority.Normal,
            groupKey: $"objective_completed:{missionState?.MissionId}:{objectiveState.ObjectiveId}");
    }

    private void HandleMissionReadyToTurnIn(MissionRuntimeState missionState)
    {
        if (!showMissionReadyToTurnIn)
        {
            return;
        }

        ShowMissionNotification(
            title: "Misión lista para entregar",
            message: GetMissionTitle(missionState),
            priority: NotificationPriority.High,
            groupKey: $"mission_ready:{missionState?.MissionId}");
    }

    private void HandleMissionCompleted(MissionRuntimeState missionState)
    {
        if (!showMissionCompleted)
        {
            return;
        }

        ShowMissionNotification(
            title: "Misión completada",
            message: GetMissionTitle(missionState),
            priority: NotificationPriority.Critical,
            groupKey: $"mission_completed:{missionState?.MissionId}");
    }

    private void ShowMissionNotification(string title, string message, NotificationPriority priority, string groupKey)
    {
        if (notificationManager == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        notificationManager.Show(NotificationData.Create(
            message: message,
            channel: NotificationChannel.Mission,
            priority: priority,
            duration: -1f,
            title: title,
            groupKey: groupKey));
    }

    private static string GetMissionTitle(MissionRuntimeState missionState)
    {
        if (missionState == null || missionState.Definition == null)
        {
            return "Misión";
        }

        if (!string.IsNullOrWhiteSpace(missionState.Definition.Title))
        {
            return missionState.Definition.Title;
        }

        return missionState.MissionId;
    }

    private static string GetObjectiveDescription(MissionObjectiveRuntimeState objectiveState)
    {
        if (objectiveState == null || objectiveState.Definition == null)
        {
            return "Objetivo";
        }

        if (!string.IsNullOrWhiteSpace(objectiveState.Definition.Description))
        {
            return objectiveState.Definition.Description;
        }

        return objectiveState.ObjectiveId;
    }
}
