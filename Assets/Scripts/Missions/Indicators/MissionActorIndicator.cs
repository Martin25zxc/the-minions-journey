using UnityEngine;

/// <summary>
/// Indicador visual de misiones para un actor/NPC.
///
/// No configura MissionSet ni ActorId directamente: consume MissionActor.
/// Así el indicador y la interacción leen la misma autoría.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MissionActor))]
public sealed class MissionActorIndicator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Componente central del actor. Define ActorId y MissionActorMissionSet.")]
    private MissionActor missionActor;

    [SerializeField, Tooltip("MissionManager de la scene. Es la fuente de verdad del estado de misiones.")]
    private MissionManager missionManager;

    [SerializeField, Tooltip("Vista visual del indicador. Solo muestra símbolos/colores; no decide lógica.")]
    private MissionIndicatorView indicatorView;

    [Header("Comportamiento")]
    [SerializeField, Tooltip("Si está activo, muestra ? gris cuando la misión está activa, requiere entrega, pero todavía no está lista.")]
    private bool showPendingTurnInIndicator = true;

    [SerializeField, Tooltip("Refresca al habilitarse. Recomendado para agarrar el estado inicial de misiones ya disponibles.")]
    private bool refreshOnEnable = true;

    [SerializeField, Tooltip("Muestra warnings útiles si faltan referencias.")]
    private bool logMissingReferences = true;

    private bool subscribed;

    public MissionActor MissionActor => ResolveMissionActor();
    public MissionActorMissionSet MissionSet => ResolveMissionActor() != null ? ResolveMissionActor().MissionSet : null;
    public string ActorId => ResolveMissionActor() != null ? ResolveMissionActor().ActorId : string.Empty;

    private void Reset()
    {
        missionActor = GetComponent<MissionActor>();
        missionManager = FindFirstObjectByType<MissionManager>();
        indicatorView = GetComponentInChildren<MissionIndicatorView>();
    }

    private void Awake()
    {
        ResolveMissionActor();
    }

    private void OnEnable()
    {
        Subscribe();

        if (refreshOnEnable)
        {
            Refresh();
        }
    }

    private void Start()
    {
        // Segundo refresh por si el MissionManager inicializó su catálogo durante Awake/Start.
        Refresh();
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
    }

    public void Refresh()
    {
        if (indicatorView == null)
        {
            if (logMissingReferences)
            {
                Debug.LogWarning($"{nameof(MissionActorIndicator)} no tiene IndicatorView asignado.", this);
            }

            return;
        }

        if (missionManager == null)
        {
            if (logMissingReferences)
            {
                Debug.LogWarning($"{nameof(MissionActorIndicator)} no tiene MissionManager asignado.", this);
            }

            indicatorView.SetState(MissionIndicatorState.None);
            return;
        }

        MissionActor resolvedActor = ResolveMissionActor();

        if (resolvedActor == null)
        {
            if (logMissingReferences)
            {
                Debug.LogWarning($"{nameof(MissionActorIndicator)} no tiene MissionActor asignado.", this);
            }

            indicatorView.SetState(MissionIndicatorState.None);
            return;
        }

        MissionIndicatorState nextState = MissionActorMissionResolver.ResolveBestIndicatorState(
            missionManager,
            resolvedActor.MissionSet,
            resolvedActor.ActorId,
            showPendingTurnInIndicator);

        indicatorView.SetState(nextState);
    }

    private MissionActor ResolveMissionActor()
    {
        if (missionActor == null)
        {
            missionActor = GetComponent<MissionActor>();
        }

        return missionActor;
    }

    private void Subscribe()
    {
        if (subscribed || missionManager == null)
        {
            return;
        }

        missionManager.MissionAvailable += HandleMissionChanged;
        missionManager.MissionAccepted += HandleMissionChanged;
        missionManager.MissionReadyToTurnIn += HandleMissionChanged;
        missionManager.MissionCompleted += HandleMissionChanged;
        missionManager.ObjectiveUpdated += HandleObjectiveChanged;
        missionManager.ObjectiveCompleted += HandleObjectiveChanged;
        missionManager.TrackedMissionChanged += HandleMissionChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || missionManager == null)
        {
            return;
        }

        missionManager.MissionAvailable -= HandleMissionChanged;
        missionManager.MissionAccepted -= HandleMissionChanged;
        missionManager.MissionReadyToTurnIn -= HandleMissionChanged;
        missionManager.MissionCompleted -= HandleMissionChanged;
        missionManager.ObjectiveUpdated -= HandleObjectiveChanged;
        missionManager.ObjectiveCompleted -= HandleObjectiveChanged;
        missionManager.TrackedMissionChanged -= HandleMissionChanged;
        subscribed = false;
    }

    private void HandleMissionChanged(MissionRuntimeState missionState)
    {
        Refresh();
    }

    private void HandleObjectiveChanged(MissionRuntimeState missionState, MissionObjectiveRuntimeState objectiveState)
    {
        Refresh();
    }
}
