using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class EndGameMissionResponder : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("MissionManager que emite MissionCompleted. Si queda vacío, se intenta resolver automáticamente.")]
    private MissionManager missionManager;

    [Header("Condición")]
    [SerializeField, Tooltip("MissionId de la última misión. Al completarse, se carga la escena final.")]
    private MissionDefinition finalMission;

    [Header("Carga de escena")]
    [SerializeField, Tooltip("Nombre exacto de la escena de fin de juego en Build Settings.")]
    private string endSceneName = "SC_EndGame";

    [SerializeField, Min(0f), Tooltip("Tiempo de espera antes de cargar la escena final.")]
    private float delayBeforeLoad = 1.5f;

    [SerializeField, Tooltip("Usa tiempo real, no afectado por Time.timeScale.")]
    private bool useUnscaledTime = true;

    [Header("Notificación opcional")]
    [SerializeField]
    private bool showNotificationBeforeLoad = true;

    [SerializeField]
    private string notificationTitle = "Misión final completada";

    [SerializeField, TextArea(1, 3)]
    private string notificationMessage = "El viaje llega a su fin...";

    [Header("Debug")]
    [SerializeField]
    private bool logActions;

    private bool isLoadingEndScene;
    private Coroutine loadRoutine;

    private string finalMissionId => finalMission != null ? finalMission.MissionId : null;

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

        if (missionManager != null)
        {
            missionManager.MissionCompleted += HandleMissionCompleted;
        }
    }

    private void OnDisable()
    {
        if (missionManager != null)
        {
            missionManager.MissionCompleted -= HandleMissionCompleted;
        }

        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
            loadRoutine = null;
        }
    }

    private void HandleMissionCompleted(MissionRuntimeState missionState)
    {
        if (isLoadingEndScene || missionState == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(finalMissionId))
        {
            Debug.LogWarning("EndGameMissionResponder: Final Mission Id está vacío.", this);
            return;
        }

        if (!string.Equals(missionState.MissionId, finalMissionId.Trim(), System.StringComparison.Ordinal))
        {
            return;
        }

        BeginEndGameLoad();
    }

    public void BeginEndGameLoad()
    {
        if (isLoadingEndScene)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(endSceneName))
        {
            Debug.LogWarning("EndGameMissionResponder: End Scene Name está vacío.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(endSceneName))
        {
            Debug.LogError($"EndGameMissionResponder: la escena '{endSceneName}' no existe o no está agregada en Build Settings.", this);
            return;
        }

        isLoadingEndScene = true;

        if (showNotificationBeforeLoad && !string.IsNullOrWhiteSpace(notificationMessage))
        {
            TMJNotifications.ShowMission(
                message: notificationMessage,
                priority: NotificationPriority.Critical,
                title: notificationTitle,
                groupKey: $"end_game:{finalMissionId}",
                context: this);
        }

        if (logActions)
        {
            Debug.Log($"EndGameMissionResponder: cargando escena final '{endSceneName}' por misión '{finalMissionId}'.", this);
        }

        loadRoutine = StartCoroutine(LoadEndSceneAfterDelay());
    }

    private IEnumerator LoadEndSceneAfterDelay()
    {
        if (delayBeforeLoad > 0f)
        {
            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(delayBeforeLoad);
            }
            else
            {
                yield return new WaitForSeconds(delayBeforeLoad);
            }
        }

        SceneManager.LoadSceneAsync(endSceneName, LoadSceneMode.Single);
    }
}
