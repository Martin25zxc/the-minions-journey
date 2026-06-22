using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Puente entre un NpcInteractable y el sistema de misiones.
/// No reemplaza al NPC interactuable: escucha cuando el jugador habla con el NPC y delega
/// aceptación/entrega/progreso al MissionManager.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NpcInteractable))]
public sealed class MissionActorInteraction : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("NPC interactuable que emite el evento TalkedTo. Normalmente está en el mismo GameObject.")]
    [SerializeField]
    private NpcInteractable npcInteractable;

    [Tooltip("Manager runtime de misiones. Es quien guarda estado real, acepta, progresa y entrega misiones.")]
    [SerializeField]
    private MissionManager missionManager;

    [Tooltip("Opcional. Solo se usa para feedback local cuando no hay cambio de estado o falla una interacción.")]
    [SerializeField]
    private NotificationManager notificationManager;

    [Header("Misión")]
    [Tooltip("Misión asociada a este actor. Recomendado para evitar escribir MissionId a mano. Debe estar en el MissionCatalog del nivel para contenido estable.")]
    [SerializeField]
    private MissionDefinition missionDefinition;

    [Tooltip("Fallback si no se asigna MissionDefinition. Usar solo para prototipos o pruebas rápidas.")]
    [SerializeField]
    private string missionIdOverride;

    [Tooltip("Si se completa, reemplaza el InteractableId del NPC como giver/turn-in target. Dejar vacío para usar el ID del NpcInteractable.")]
    [SerializeField]
    private string actorIdOverride;

    [Header("Comportamiento")]
    [Tooltip("Si está activo, al hablar con el NPC se acepta la misión cuando su estado es Available.")]
    [SerializeField]
    private bool acceptWhenAvailable = true;

    [Tooltip("Si está activo, al hablar con el NPC se entrega la misión cuando está ReadyToTurnIn y este actor coincide con el target esperado.")]
    [SerializeField]
    private bool turnInWhenReady = true;

    [Tooltip("Permite aceptar la misión incluso si sigue Inactive. Mantener apagado salvo prototipos controlados, para no saltar condiciones futuras.")]
    [SerializeField]
    private bool acceptFromInactiveState;

    [Tooltip("Reporta un GameWorldEvent ActorTalkedTo al MissionManager después de resolver aceptación/entrega. Mantener apagado salvo objetivos que realmente dependan de hablar con este actor.")]
    [SerializeField]
    private bool reportActorTalkedToEvent;

    [Header("Feedback local")]
    [Tooltip("Muestra mensajes locales para estados que no generan eventos de MissionManager, por ejemplo misión activa o ya completada.")]
    [SerializeField]
    private bool showLocalFeedback = true;

    [Tooltip("Si está activo, también muestra feedback local en aceptación/entrega exitosa. Normalmente conviene dejarlo apagado porque MissionNotificationBridge ya muestra esos toasts.")]
    [SerializeField]
    private bool showSuccessFeedback;

    [Tooltip("Canal usado para feedback local de este puente. Los eventos reales de misión los debería mostrar MissionNotificationBridge.")]
    [SerializeField]
    private NotificationChannel localFeedbackChannel = NotificationChannel.Mission;

    [Tooltip("Mensaje cuando la misión ya está activa y el NPC no tiene una acción adicional configurada.")]
    [SerializeField, TextArea(2, 4)]
    private string activeMissionMessage = "Ya tienes esta misión activa.";

    [Tooltip("Mensaje cuando la misión ya fue completada.")]
    [SerializeField, TextArea(2, 4)]
    private string completedMissionMessage = "Ya completaste esta misión.";

    [Tooltip("Mensaje cuando la misión todavía no está disponible para este actor.")]
    [SerializeField, TextArea(2, 4)]
    private string unavailableMissionMessage = "Este personaje todavía no tiene una misión disponible.";

    [Tooltip("Mensaje genérico cuando MissionManager rechaza la acción. Puede ocurrir por combate, estado de juego o target inválido.")]
    [SerializeField, TextArea(2, 4)]
    private string failedMissionActionMessage = "No se pudo gestionar la misión ahora.";

    [Header("Debug")]
    [Tooltip("Muestra logs útiles durante la integración del primer quest giver.")]
    [SerializeField]
    private bool logInteractions = true;

    [Header("Eventos")]
    [Tooltip("Se invoca cuando este actor logra aceptar la misión configurada.")]
    [SerializeField]
    private UnityEvent onMissionAccepted;

    [Tooltip("Se invoca cuando este actor logra entregar/completar la misión configurada.")]
    [SerializeField]
    private UnityEvent onMissionTurnedIn;

    [Tooltip("Se invoca cuando el jugador habla con el actor pero la misión ya está activa.")]
    [SerializeField]
    private UnityEvent onMissionAlreadyActive;

    [Tooltip("Se invoca cuando el jugador habla con el actor pero la misión ya está completada.")]
    [SerializeField]
    private UnityEvent onMissionAlreadyCompleted;

    [Tooltip("Se invoca cuando la misión no está disponible para este actor.")]
    [SerializeField]
    private UnityEvent onMissionUnavailable;

    [Tooltip("Se invoca cuando MissionManager rechaza la acción solicitada.")]
    [SerializeField]
    private UnityEvent onMissionActionFailed;

    public string MissionId => ResolveMissionId();
    public string ActorId => ResolveActorId();

    public event Action<MissionActorInteraction, MissionRuntimeState> MissionAcceptedByActor;
    public event Action<MissionActorInteraction, MissionRuntimeState> MissionTurnedInByActor;
    public event Action<MissionActorInteraction, MissionRuntimeState> MissionAlreadyActive;
    public event Action<MissionActorInteraction, MissionRuntimeState> MissionAlreadyCompleted;
    public event Action<MissionActorInteraction> MissionUnavailable;
    public event Action<MissionActorInteraction> MissionActionFailed;

    private void Reset()
    {
        npcInteractable = GetComponent<NpcInteractable>();
        missionManager = FindFirstObjectByType<MissionManager>();
        notificationManager = FindFirstObjectByType<NotificationManager>();
    }

    private void OnEnable()
    {
        if (npcInteractable == null)
        {
            npcInteractable = GetComponent<NpcInteractable>();
        }

        if (npcInteractable != null)
        {
            npcInteractable.TalkedTo += HandleNpcTalkedTo;
        }
    }

    private void OnDisable()
    {
        if (npcInteractable != null)
        {
            npcInteractable.TalkedTo -= HandleNpcTalkedTo;
        }
    }

    private void OnValidate()
    {
        missionIdOverride = CleanId(missionIdOverride);
        actorIdOverride = CleanId(actorIdOverride);
    }

    private void HandleNpcTalkedTo(NpcInteractable npc, InteractionContext context)
    {
        if (!CanUseMissionInteraction())
        {
            return;
        }

        string missionId = ResolveMissionId();
        string actorId = ResolveActorId();

        if (string.IsNullOrWhiteSpace(missionId))
        {
            Debug.LogWarning($"{nameof(MissionActorInteraction)} en '{name}' no tiene misión configurada.", this);
            HandleMissionUnavailable();
            return;
        }

        if (string.IsNullOrWhiteSpace(actorId))
        {
            Debug.LogWarning($"{nameof(MissionActorInteraction)} en '{name}' no pudo resolver ActorId. Configurar InteractableId en NpcInteractable o ActorId Override.", this);
            HandleMissionUnavailable();
            return;
        }

        MissionRuntimeState runtimeState = missionManager.GetMissionState(missionId);

        if (runtimeState == null)
        {
            Debug.LogWarning($"La misión '{missionId}' no está registrada en MissionManager. Revisar MissionCatalog del nivel.", this);
            HandleMissionUnavailable();
            return;
        }

        if (logInteractions)
        {
            Debug.Log($"{nameof(MissionActorInteraction)} actor '{actorId}' habló para misión '{missionId}' en estado {runtimeState.State}.", this);
        }

        switch (runtimeState.State)
        {
            case MissionState.Inactive:
                HandleInactiveMission(runtimeState, missionId, actorId);
                break;

            case MissionState.Available:
                HandleAvailableMission(runtimeState, missionId, actorId);
                break;

            case MissionState.Active:
                HandleActiveMission(runtimeState);
                break;

            case MissionState.ReadyToTurnIn:
                HandleReadyToTurnInMission(runtimeState, missionId, actorId);
                break;

            case MissionState.Completed:
                HandleCompletedMission(runtimeState);
                break;

            default:
                HandleMissionUnavailable();
                break;
        }

        if (reportActorTalkedToEvent)
        {
            ReportActorTalkedTo(actorId);
        }
    }

    private bool CanUseMissionInteraction()
    {
        if (missionManager != null)
        {
            return true;
        }

        Debug.LogWarning($"Falta MissionManager en {nameof(MissionActorInteraction)} de '{name}'.", this);
        HandleMissionActionFailed();
        return false;
    }

    private void HandleInactiveMission(MissionRuntimeState runtimeState, string missionId, string actorId)
    {
        if (!acceptFromInactiveState)
        {
            HandleMissionUnavailable();
            return;
        }

        TryAcceptMission(runtimeState, missionId, actorId);
    }

    private void HandleAvailableMission(MissionRuntimeState runtimeState, string missionId, string actorId)
    {
        if (!acceptWhenAvailable)
        {
            HandleMissionUnavailable();
            return;
        }

        TryAcceptMission(runtimeState, missionId, actorId);
    }

    private void HandleActiveMission(MissionRuntimeState runtimeState)
    {
        ShowLocalFeedback(activeMissionMessage, NotificationPriority.Low, $"mission_actor_active:{runtimeState.MissionId}:{ActorId}");
        onMissionAlreadyActive?.Invoke();
        MissionAlreadyActive?.Invoke(this, runtimeState);
    }

    private void HandleReadyToTurnInMission(MissionRuntimeState runtimeState, string missionId, string actorId)
    {
        if (!turnInWhenReady)
        {
            HandleActiveMission(runtimeState);
            return;
        }

        bool turnedIn = missionManager.TryTurnInMission(missionId, actorId);

        if (!turnedIn)
        {
            HandleMissionActionFailed();
            return;
        }

        ShowLocalFeedbackIfSuccess($"Misión entregada: {GetMissionTitle(runtimeState)}", NotificationPriority.High, $"mission_actor_turnin:{missionId}");
        onMissionTurnedIn?.Invoke();
        MissionTurnedInByActor?.Invoke(this, runtimeState);
    }

    private void HandleCompletedMission(MissionRuntimeState runtimeState)
    {
        ShowLocalFeedback(completedMissionMessage, NotificationPriority.Low, $"mission_actor_completed:{runtimeState.MissionId}:{ActorId}");
        onMissionAlreadyCompleted?.Invoke();
        MissionAlreadyCompleted?.Invoke(this, runtimeState);
    }

    private void TryAcceptMission(MissionRuntimeState runtimeState, string missionId, string actorId)
    {
        bool accepted = missionManager.TryAcceptMission(missionId, actorId);

        if (!accepted)
        {
            HandleMissionActionFailed();
            return;
        }

        ShowLocalFeedbackIfSuccess($"Misión iniciada: {GetMissionTitle(runtimeState)}", NotificationPriority.Normal, $"mission_actor_accept:{missionId}");
        onMissionAccepted?.Invoke();
        MissionAcceptedByActor?.Invoke(this, runtimeState);
    }

    private void HandleMissionUnavailable()
    {
        ShowLocalFeedback(unavailableMissionMessage, NotificationPriority.Low, $"mission_actor_unavailable:{MissionId}:{ActorId}");
        onMissionUnavailable?.Invoke();
        MissionUnavailable?.Invoke(this);
    }

    private void HandleMissionActionFailed()
    {
        ShowLocalFeedback(failedMissionActionMessage, NotificationPriority.Normal, $"mission_actor_failed:{MissionId}:{ActorId}");
        onMissionActionFailed?.Invoke();
        MissionActionFailed?.Invoke(this);
    }

    private void ReportActorTalkedTo(string actorId)
    {
        if (string.IsNullOrWhiteSpace(actorId) || missionManager == null)
        {
            return;
        }

        GameWorldEvent worldEvent = new GameWorldEvent(
            GameWorldEventType.ActorTalkedTo,
            actorId,
            1,
            name);

        missionManager.TryReportWorldEvent(worldEvent);
    }

    private void ShowLocalFeedbackIfSuccess(string message, NotificationPriority priority, string groupKey)
    {
        if (!showSuccessFeedback)
        {
            return;
        }

        ShowLocalFeedback(message, priority, groupKey);
    }

    private void ShowLocalFeedback(string message, NotificationPriority priority, string groupKey)
    {
        if (!showLocalFeedback || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (notificationManager != null)
        {
            notificationManager.Show(NotificationData.Create(
                message: message,
                channel: localFeedbackChannel,
                priority: priority,
                duration: -1f,
                title: "Misión",
                groupKey: groupKey));
            return;
        }

        Debug.Log($"[MissionActorInteraction] {message}", this);
    }

    private string ResolveMissionId()
    {
        if (missionDefinition != null && !string.IsNullOrWhiteSpace(missionDefinition.MissionId))
        {
            return CleanId(missionDefinition.MissionId);
        }

        return CleanId(missionIdOverride);
    }

    private string ResolveActorId()
    {
        if (!string.IsNullOrWhiteSpace(actorIdOverride))
        {
            return CleanId(actorIdOverride);
        }

        if (npcInteractable != null && !string.IsNullOrWhiteSpace(npcInteractable.InteractableId))
        {
            return CleanId(npcInteractable.InteractableId);
        }

        return string.Empty;
    }

    private static string GetMissionTitle(MissionRuntimeState runtimeState)
    {
        if (runtimeState == null || runtimeState.Definition == null)
        {
            return "Misión";
        }

        if (!string.IsNullOrWhiteSpace(runtimeState.Definition.Title))
        {
            return runtimeState.Definition.Title;
        }

        return runtimeState.MissionId;
    }

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
