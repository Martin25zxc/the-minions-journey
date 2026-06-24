using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Puente entre NpcInteractable y MissionManager para actores de misión.
///
/// No configura MissionSet ni ActorId directamente: consume MissionActor.
/// Así evitamos que interacción e indicador apunten a sets distintos.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NpcInteractable))]
[RequireComponent(typeof(MissionActor))]
public sealed class MissionActorInteraction : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Componente central del actor. Define ActorId y MissionActorMissionSet.")]
    private MissionActor missionActor;

    [SerializeField, Tooltip("Manager runtime de misiones. Es quien guarda estado real, acepta, progresa y entrega misiones.")]
    private MissionManager missionManager;

    [SerializeField, Tooltip("Opcional. Solo se usa para feedback local cuando no hay cambio de estado o falla una interacción.")]
    private NotificationManager notificationManager;

    [Header("Feedback local")]
    [SerializeField, Tooltip("Muestra mensajes locales para estados que no generan eventos de MissionManager, por ejemplo misión activa o ya completada.")]
    private bool showLocalFeedback = true;

    [SerializeField, Tooltip("Canal usado para feedback local de este puente. Los eventos reales de misión los debería mostrar MissionNotificationBridge.")]
    private NotificationChannel localFeedbackChannel = NotificationChannel.Mission;

    [SerializeField, TextArea(2, 4), Tooltip("Mensaje cuando hay una misión activa relacionada con este actor, pero todavía no está lista para entregar.")]
    private string activeMissionMessage = "Todavía no terminaste lo que este personaje te pidió.";

    [SerializeField, TextArea(2, 4), Tooltip("Mensaje cuando solo se encontraron misiones ya completadas y no hay otra acción disponible.")]
    private string completedMissionMessage = "Ya completaste lo que este personaje podía pedirte.";

    [SerializeField, TextArea(2, 4), Tooltip("Mensaje cuando no hay ninguna acción de misión disponible para este actor.")]
    private string unavailableMissionMessage = "Este personaje todavía no tiene una misión disponible.";

    [SerializeField, TextArea(2, 4), Tooltip("Mensaje genérico cuando MissionManager rechaza la acción. Puede ocurrir por combate, estado de juego o target inválido.")]
    private string failedMissionActionMessage = "No se pudo gestionar la misión ahora.";

    [Header("Debug")]
    [SerializeField, Tooltip("Muestra logs útiles durante la integración de actores con múltiples misiones.")]
    private bool logInteractions = true;

    [Header("Eventos")]
    [SerializeField, Tooltip("Se invoca cuando este actor logra aceptar una misión de su MissionActorMissionSet.")]
    private UnityEvent onMissionAccepted;

    [SerializeField, Tooltip("Se invoca cuando este actor logra entregar/completar una misión de su MissionActorMissionSet.")]
    private UnityEvent onMissionTurnedIn;

    [SerializeField, Tooltip("Se invoca cuando el jugador habla con el actor pero la misión relacionada sigue activa/incompleta.")]
    private UnityEvent onMissionAlreadyActive;

    [SerializeField, Tooltip("Se invoca cuando el jugador habla con el actor y solo hay misiones completadas, sin otra acción mejor.")]
    private UnityEvent onMissionAlreadyCompleted;

    [SerializeField, Tooltip("Se invoca cuando no hay ninguna acción de misión disponible para este actor.")]
    private UnityEvent onMissionUnavailable;

    [SerializeField, Tooltip("Se invoca cuando MissionManager rechaza una acción que el resolver consideraba posible.")]
    private UnityEvent onMissionActionFailed;

    private NpcInteractable npcInteractable;
    private NpcInteractable subscribedNpc;
    private bool subscribed;

    public MissionActor MissionActor => ResolveMissionActor();

    public MissionActorMissionSet MissionSet
    {
        get
        {
            MissionActor resolvedActor = ResolveMissionActor();
            return resolvedActor != null ? resolvedActor.MissionSet : null;
        }
    }

    public string ActorId
    {
        get
        {
            MissionActor resolvedActor = ResolveMissionActor();
            return resolvedActor != null ? resolvedActor.ActorId : string.Empty;
        }
    }

    private void Reset()
    {
        missionActor = GetComponent<MissionActor>();
        npcInteractable = GetComponent<NpcInteractable>();
        missionManager = FindFirstObjectByType<MissionManager>();
        notificationManager = FindFirstObjectByType<NotificationManager>();
    }

    private void Awake()
    {
        ResolveMissionActor();
        ResolveNpcInteractable();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        if (missionActor == null)
        {
            missionActor = GetComponent<MissionActor>();
        }

        if (npcInteractable == null)
        {
            npcInteractable = GetComponent<NpcInteractable>();
        }
    }

    /// <summary>
    /// Firma obligatoria para NpcInteractable.TalkedTo: Action&lt;NpcInteractable, InteractionContext&gt;.
    /// Usamos el npc recibido como guard defensivo para evitar procesar eventos de otro actor por error.
    /// </summary>
    public void HandleTalkedTo(NpcInteractable npc, InteractionContext context)
    {
        NpcInteractable expectedNpc = ResolveNpcInteractable();

        if (expectedNpc != null && npc != null && npc != expectedNpc)
        {
            if (logInteractions)
            {
                Debug.LogWarning(
                    $"{nameof(MissionActorInteraction)} ignoró TalkedTo de '{npc.name}' porque esperaba '{expectedNpc.name}'.",
                    this);
            }

            return;
        }

        ResolveAndExecute();
    }

    public void ResolveAndExecute()
    {
        MissionActor resolvedActor = ResolveMissionActor();

        if (resolvedActor == null)
        {
            HandleUnavailable(MissionActorResolvedAction.Unavailable("Falta MissionActor."));
            return;
        }

        string actorId = resolvedActor.ActorId;
        MissionActorMissionSet missionSet = resolvedActor.MissionSet;

        MissionActorResolvedAction resolvedAction = MissionActorMissionResolver.ResolveBestAction(
            missionManager,
            missionSet,
            actorId);

        if (logInteractions)
        {
            Debug.Log($"{nameof(MissionActorInteraction)} actor '{actorId}' resolvió {resolvedAction.ActionType} para misión '{resolvedAction.MissionId}'. {resolvedAction.Reason}", this);
        }

        switch (resolvedAction.ActionType)
        {
            case MissionActorResolvedActionType.TurnInMission:
                ExecuteTurnIn(resolvedAction, actorId);
                break;

            case MissionActorResolvedActionType.AcceptMission:
                ExecuteAccept(resolvedAction, actorId);
                break;

            case MissionActorResolvedActionType.ShowPendingMission:
                HandlePending(resolvedAction);
                break;

            case MissionActorResolvedActionType.ShowCompletedMission:
                HandleCompleted(resolvedAction);
                break;

            case MissionActorResolvedActionType.Unavailable:
            case MissionActorResolvedActionType.None:
            default:
                HandleUnavailable(resolvedAction);
                break;
        }
    }

    private void ExecuteAccept(MissionActorResolvedAction resolvedAction, string actorId)
    {
        MissionRuntimeState runtimeState = resolvedAction.MissionState;

        if (runtimeState == null)
        {
            HandleFailure();
            return;
        }

        bool accepted = missionManager != null && missionManager.TryAcceptMission(runtimeState.MissionId, actorId);

        if (!accepted)
        {
            HandleFailure();
            return;
        }

        onMissionAccepted?.Invoke();
    }

    private void ExecuteTurnIn(MissionActorResolvedAction resolvedAction, string actorId)
    {
        MissionRuntimeState runtimeState = resolvedAction.MissionState;

        if (runtimeState == null)
        {
            HandleFailure();
            return;
        }

        bool turnedIn = missionManager != null && missionManager.TryTurnInMission(runtimeState.MissionId, actorId);

        if (!turnedIn)
        {
            HandleFailure();
            return;
        }

        onMissionTurnedIn?.Invoke();
    }

    private void HandlePending(MissionActorResolvedAction resolvedAction)
    {
        onMissionAlreadyActive?.Invoke();
        ShowLocalFeedback(activeMissionMessage, NotificationPriority.Low, $"mission_actor_active:{resolvedAction.MissionId}:{ActorId}");
    }

    private void HandleCompleted(MissionActorResolvedAction resolvedAction)
    {
        onMissionAlreadyCompleted?.Invoke();
        ShowLocalFeedback(completedMissionMessage, NotificationPriority.Low, $"mission_actor_completed:{resolvedAction.MissionId}:{ActorId}");
    }

    private void HandleUnavailable(MissionActorResolvedAction resolvedAction)
    {
        onMissionUnavailable?.Invoke();
        ShowLocalFeedback(unavailableMissionMessage, NotificationPriority.Low, $"mission_actor_unavailable:{ActorId}");
    }

    private void HandleFailure()
    {
        onMissionActionFailed?.Invoke();
        ShowLocalFeedback(failedMissionActionMessage, NotificationPriority.Normal, $"mission_actor_failed:{ActorId}");
    }

    private MissionActor ResolveMissionActor()
    {
        if (missionActor == null)
        {
            missionActor = GetComponent<MissionActor>();
        }

        return missionActor;
    }

    private NpcInteractable ResolveNpcInteractable()
    {
        if (npcInteractable == null)
        {
            MissionActor resolvedActor = ResolveMissionActor();
            npcInteractable = resolvedActor != null && resolvedActor.NpcInteractable != null
                ? resolvedActor.NpcInteractable
                : GetComponent<NpcInteractable>();
        }

        return npcInteractable;
    }

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        NpcInteractable resolvedNpc = ResolveNpcInteractable();

        if (resolvedNpc == null)
        {
            Debug.LogWarning($"{nameof(MissionActorInteraction)} no encontró NpcInteractable.", this);
            return;
        }

        resolvedNpc.TalkedTo += HandleTalkedTo;
        subscribedNpc = resolvedNpc;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || subscribedNpc == null)
        {
            subscribed = false;
            subscribedNpc = null;
            return;
        }

        subscribedNpc.TalkedTo -= HandleTalkedTo;
        subscribedNpc = null;
        subscribed = false;
    }

    private void ShowLocalFeedback(string message, NotificationPriority priority, string groupKey)
    {
        if (!showLocalFeedback || notificationManager == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        notificationManager.Show(NotificationData.Create(
            message,
            localFeedbackChannel,
            priority,
            duration: -1f,
            title: null,
            groupKey: groupKey));
    }
}
