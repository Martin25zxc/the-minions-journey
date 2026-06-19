using UnityEngine;

/// <summary>
/// Conecta un enemigo individual con un EnemyGroupController.
///
/// Responsabilidad:
/// - Publicar alertas locales del enemigo: vision y daño recibido.
/// - Recibir alertas del grupo y traducirlas en estimulos para EnemyAwareness/EnemyBrain.
///
/// No mueve, no ataca, no selecciona habilidades y no decide estados directamente.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
[RequireComponent(typeof(EnemyAwareness))]
public sealed class EnemyGroupMember : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Actor principal del enemigo. Si queda vacio, se busca automaticamente en el mismo GameObject.")]
    [SerializeField]
    private EnemyActor actor;

    [Tooltip("Awareness tactica del enemigo. Si queda vacio, se busca automaticamente en el mismo GameObject.")]
    [SerializeField]
    private EnemyAwareness awareness;

    [Tooltip("Brain del enemigo. Si esta asignado, las alertas grupales entran por EnemyBrain.ReceiveStimulus para reevaluar estado inmediatamente.")]
    [SerializeField]
    private EnemyBrain brain;

    [Header("Group")]
    [Tooltip("Grupo al que pertenece este enemigo. Si queda vacio, el enemigo funciona como individual y no emite ni recibe alertas grupales.")]
    [SerializeField]
    private EnemyGroupController groupController;

    [Tooltip("Si no hay Group Controller asignado, intenta buscar un EnemyGroupController en los padres. Sirve si organizas enemigos como hijos del objeto de grupo.")]
    [SerializeField]
    private bool findGroupInParentsIfMissing = true;

    [Header("Broadcast Local Alerts")]
    [Tooltip("Si esta activo, cuando este enemigo ve un target lo avisa al grupo.")]
    [SerializeField]
    private bool broadcastSightAlerts = true;

    [Tooltip("Si esta activo, cuando este enemigo recibe daño lo avisa al grupo.")]
    [SerializeField]
    private bool broadcastDamageAlerts = true;

    [Tooltip("Si esta activo, el daño sin target claro tambien alerta al grupo como punto de investigacion.")]
    [SerializeField]
    private bool broadcastUnknownDamageAlerts = true;

    [Tooltip("Tiempo minimo entre alertas publicadas por este miembro. Evita spamear al grupo cada frame mientras ve al jugador.")]
    [SerializeField, Min(0f)]
    private float broadcastCooldown = 0.75f;

    [Header("Alert Memory")]
    [Tooltip("Duracion fallback de memoria para alertas de vision si el estimulo original no trae duracion.")]
    [SerializeField, Min(0f)]
    private float sightAlertMemoryDuration = 3f;

    [Tooltip("Duracion fallback de memoria para alertas por daño si el estimulo original no trae duracion.")]
    [SerializeField, Min(0f)]
    private float damageAlertMemoryDuration = 4f;

    [Header("Receive Group Alerts")]
    [Tooltip("Si esta activo, este enemigo puede recibir alertas de su grupo.")]
    [SerializeField]
    private bool receiveGroupAlerts = true;

    [Header("Debug")]
    [Tooltip("Muestra logs cuando este miembro publica alertas al grupo.")]
    [SerializeField]
    private bool logBroadcastAlerts;

    [Tooltip("Muestra logs cuando este miembro recibe alertas del grupo.")]
    [SerializeField]
    private bool logReceivedAlerts;

    [Tooltip("Muestra advertencias de configuracion incompleta.")]
    [SerializeField]
    private bool logConfigurationWarnings = true;

    [Header("Runtime Debug - Solo lectura conceptual")]
    [Tooltip("Nombre del grupo actual. 'None' significa enemigo individual.")]
    [SerializeField]
    private string debugGroupName = "None";

    [Tooltip("Ultima razon de alerta publicada por este miembro.")]
    [SerializeField]
    private string debugLastBroadcastReason = "None";

    [Tooltip("Ultima razon de alerta recibida por este miembro.")]
    [SerializeField]
    private string debugLastReceivedReason = "None";

    [Tooltip("Ultimo target recibido por alerta grupal.")]
    [SerializeField]
    private Transform debugLastReceivedTarget;

    [Tooltip("Ultima posicion recibida por alerta grupal.")]
    [SerializeField]
    private Vector3 debugLastReceivedPosition;

    private float nextAllowedBroadcastTime;

    public EnemyGroupController GroupController => groupController;
    public bool IsAlive => actor == null || actor.IsAlive;

    private void Awake()
    {
        ResolveReferences();
        ResolveGroupIfNeeded();
        RefreshDebugSnapshot();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ResolveGroupIfNeeded();

        if (awareness != null)
        {
            awareness.StimulusReceived += HandleLocalStimulus;
        }

        groupController?.RegisterMember(this);
        RefreshDebugSnapshot();
    }

    private void OnDisable()
    {
        if (awareness != null)
        {
            awareness.StimulusReceived -= HandleLocalStimulus;
        }

        groupController?.UnregisterMember(this);
        RefreshDebugSnapshot();
    }

    public void SetGroup(EnemyGroupController newGroup)
    {
        if (groupController == newGroup)
        {
            RefreshDebugSnapshot();
            return;
        }

        groupController?.UnregisterMember(this);
        groupController = newGroup;

        if (isActiveAndEnabled)
        {
            groupController?.RegisterMember(this);
        }

        RefreshDebugSnapshot();
    }

    public void ReceiveGroupAlert(EnemyAlertContext context)
    {
        if (!receiveGroupAlerts || !IsAlive)
        {
            return;
        }

        debugLastReceivedReason = context.Reason.ToString();
        debugLastReceivedTarget = context.Target;
        debugLastReceivedPosition = context.TryGetReferencePosition(out Vector3 referencePosition)
            ? referencePosition
            : Vector3.zero;

        if (logReceivedAlerts)
        {
            string targetName = context.Target != null ? context.Target.name : "None";
            Debug.Log($"[{nameof(EnemyGroupMember)}] {name} received group alert {context.AlertId} ({context.Reason}). Target: {targetName}.", this);
        }

        EnemyStimulus stimulus = context.ToGroupStimulus();

        if (brain != null)
        {
            brain.ReceiveStimulus(stimulus);
        }
        else
        {
            awareness?.ReceiveStimulus(stimulus);
        }

        RefreshDebugSnapshot();
    }

    private void HandleLocalStimulus(EnemyAwareness sourceAwareness, EnemyStimulus stimulus)
    {
        if (groupController == null || !IsAlive)
        {
            return;
        }

        // Regla importante: una alerta grupal recibida NO se republica.
        // Esto evita loops del tipo A -> B -> A -> B.
        if (stimulus.Type == EnemyStimulusType.GroupAlert)
        {
            return;
        }

        if (Time.time < nextAllowedBroadcastTime)
        {
            return;
        }

        switch (stimulus.Type)
        {
            case EnemyStimulusType.Sight:
                TryBroadcastSightAlert(stimulus);
                break;

            case EnemyStimulusType.Damage:
                TryBroadcastDamageAlert(stimulus);
                break;
        }
    }

    private void TryBroadcastSightAlert(EnemyStimulus stimulus)
    {
        if (!broadcastSightAlerts || !stimulus.HasTarget)
        {
            return;
        }

        EnemyAlertContext context = EnemyAlertContext.FromStimulus(
            EnemyAlertReason.SawTarget,
            this,
            groupController,
            stimulus,
            sightAlertMemoryDuration,
            forceCombat: true);

        BroadcastAlert(context);
    }

    private void TryBroadcastDamageAlert(EnemyStimulus stimulus)
    {
        if (!broadcastDamageAlerts)
        {
            return;
        }

        if (!stimulus.HasTarget && !broadcastUnknownDamageAlerts)
        {
            return;
        }

        EnemyAlertContext context = EnemyAlertContext.FromStimulus(
            EnemyAlertReason.TookDamage,
            this,
            groupController,
            stimulus,
            damageAlertMemoryDuration,
            forceCombat: stimulus.HasTarget);

        BroadcastAlert(context);
    }

    private void BroadcastAlert(EnemyAlertContext context)
    {
        nextAllowedBroadcastTime = Time.time + broadcastCooldown;
        debugLastBroadcastReason = context.Reason.ToString();

        if (logBroadcastAlerts)
        {
            string targetName = context.Target != null ? context.Target.name : "None";
            Debug.Log($"[{nameof(EnemyGroupMember)}] {name} broadcasts alert {context.AlertId} ({context.Reason}). Target: {targetName}.", this);
        }

        groupController?.ReceiveAlert(context);
        RefreshDebugSnapshot();
    }

    private void ResolveReferences()
    {
        if (actor == null)
        {
            actor = GetComponent<EnemyActor>();
        }

        if (awareness == null)
        {
            awareness = GetComponent<EnemyAwareness>();
        }

        if (brain == null)
        {
            brain = GetComponent<EnemyBrain>();
        }
    }

    private void ResolveGroupIfNeeded()
    {
        if (groupController != null || !findGroupInParentsIfMissing)
        {
            return;
        }

        groupController = GetComponentInParent<EnemyGroupController>();
    }

    private void RefreshDebugSnapshot()
    {
        debugGroupName = groupController != null ? groupController.name : "None";
    }

    private void OnValidate()
    {
        broadcastCooldown = Mathf.Max(0f, broadcastCooldown);
        sightAlertMemoryDuration = Mathf.Max(0f, sightAlertMemoryDuration);
        damageAlertMemoryDuration = Mathf.Max(0f, damageAlertMemoryDuration);
        RefreshDebugSnapshot();
    }
}
