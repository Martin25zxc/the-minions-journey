using UnityEngine;

[DisallowMultipleComponent]
public sealed class MissionNotificationBridge : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Manager de misiones que emite eventos runtime. Si queda vacío, se intenta resolver automáticamente.")]
    private MissionManager missionManager;

    [Header("Qué mostrar")]
    [SerializeField, Tooltip("Muestra toast cuando se acepta una misión.")]
    private bool showMissionAccepted = true;

    [SerializeField, Tooltip("Muestra toast cuando una misión se completa.")]
    private bool showMissionCompleted = true;

    [SerializeField, Tooltip("Opcional. Muestra toast cuando una misión queda lista para entregar. Desactivado por defecto para evitar ruido.")]
    private bool showMissionReadyToTurnIn;

    [Header("Texto")]
    [SerializeField]
    private string newMissionTitle = "Nueva misión";

    [SerializeField, TextArea(1, 2), Tooltip("{0} se reemplaza por el nombre de la misión.")]
    private string newMissionMessageFormat = "{0}. Revisa el Diario de Misiones.";

    [SerializeField]
    private string completedMissionTitle = "Misión completada";

    [SerializeField, TextArea(1, 2), Tooltip("{0} se reemplaza por el nombre de la misión.")]
    private string completedMissionMessageFormat = "Se ha completado: {0}";

    [SerializeField]
    private string readyToTurnInTitle = "Misión lista para entregar";

    [SerializeField, TextArea(1, 2), Tooltip("{0} se reemplaza por el nombre de la misión.")]
    private string readyToTurnInMessageFormat = "{0}. Revisa el Diario de Misiones.";

    private void Reset()
    {
        missionManager = FindFirstObjectByType<MissionManager>();
    }

    private void OnEnable()
    {
        if (missionManager == null)
        {
            missionManager = FindFirstObjectByType<MissionManager>();
        }

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
        missionManager.MissionCompleted += HandleMissionCompleted;
        missionManager.MissionReadyToTurnIn += HandleMissionReadyToTurnIn;
    }

    private void Unsubscribe()
    {
        if (missionManager == null)
        {
            return;
        }

        missionManager.MissionAccepted -= HandleMissionAccepted;
        missionManager.MissionCompleted -= HandleMissionCompleted;
        missionManager.MissionReadyToTurnIn -= HandleMissionReadyToTurnIn;
    }

    private void HandleMissionAccepted(MissionRuntimeState missionState)
    {
        if (!showMissionAccepted)
        {
            return;
        }

        string missionTitle = GetMissionTitle(missionState);
        string message = FormatMessage(newMissionMessageFormat, missionTitle);

        TMJNotifications.ShowMission(
            message: message,
            priority: NotificationPriority.Normal,
            title: newMissionTitle,
            groupKey: $"mission_accepted:{missionState?.MissionId}",
            context: this);
    }

    private void HandleMissionCompleted(MissionRuntimeState missionState)
    {
        if (!showMissionCompleted)
        {
            return;
        }

        string missionTitle = GetMissionTitle(missionState);
        string message = FormatMessage(completedMissionMessageFormat, missionTitle);

        TMJNotifications.ShowMission(
            message: message,
            priority: NotificationPriority.High,
            title: completedMissionTitle,
            groupKey: $"mission_completed:{missionState?.MissionId}",
            context: this);
    }

    private void HandleMissionReadyToTurnIn(MissionRuntimeState missionState)
    {
        if (!showMissionReadyToTurnIn)
        {
            return;
        }

        string missionTitle = GetMissionTitle(missionState);
        string message = FormatMessage(readyToTurnInMessageFormat, missionTitle);

        TMJNotifications.ShowMission(
            message: message,
            priority: NotificationPriority.Normal,
            title: readyToTurnInTitle,
            groupKey: $"mission_ready_to_turn_in:{missionState?.MissionId}",
            context: this);
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

    private static string FormatMessage(string format, string missionTitle)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return missionTitle;
        }

        return string.Format(format, missionTitle);
    }
}
