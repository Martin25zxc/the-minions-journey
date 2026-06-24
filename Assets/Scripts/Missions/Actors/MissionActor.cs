using UnityEngine;

/// <summary>
/// Componente central del actor de misión.
///
/// Responsabilidad:
/// - Resolver el ActorId del NPC/actor de escena.
/// - Exponer el MissionActorMissionSet que usa este actor.
/// - Ser la fuente común para MissionActorInteraction y MissionActorIndicator.
///
/// No acepta misiones, no entrega misiones y no muestra UI.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NpcInteractable))]
public sealed class MissionActor : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("NPC interactuable que contiene el InteractableId. Normalmente está en el mismo GameObject.")]
    private NpcInteractable npcInteractable;

    [SerializeField, Tooltip("Set de autoría que define qué misiones puede ofrecer este actor.")]
    private MissionActorMissionSet missionSet;

    [Header("Identidad")]
    [SerializeField, Tooltip("Override avanzado del ActorId. Dejar vacío para usar NpcInteractable.InteractableId.")]
    private string actorIdOverride;

    [SerializeField, Tooltip("Muestra warnings útiles si falta ID o MissionSet.")]
    private bool logWarnings = true;

    public NpcInteractable NpcInteractable => ResolveNpcInteractable();
    public MissionActorMissionSet MissionSet => missionSet;
    public string ActorId => ResolveActorId();
    public bool HasMissionSet => missionSet != null && missionSet.EntryCount > 0;

    private void Reset()
    {
        npcInteractable = GetComponent<NpcInteractable>();
    }

    private void Awake()
    {
        ResolveNpcInteractable();
    }

    private void OnValidate()
    {
        actorIdOverride = CleanId(actorIdOverride);

        if (npcInteractable == null)
        {
            npcInteractable = GetComponent<NpcInteractable>();
        }
    }

    public bool TryGetMissionSet(out MissionActorMissionSet resolvedMissionSet)
    {
        resolvedMissionSet = missionSet;
        return resolvedMissionSet != null;
    }

    public bool TryGetActorId(out string resolvedActorId)
    {
        resolvedActorId = ResolveActorId();
        return !string.IsNullOrEmpty(resolvedActorId);
    }

    public void ValidateRuntimeSetup(Object context = null)
    {
        Object logContext = context != null ? context : this;

        if (!logWarnings)
        {
            return;
        }

        if (ResolveNpcInteractable() == null)
        {
            Debug.LogWarning($"{nameof(MissionActor)} no encontró NpcInteractable.", logContext);
        }

        if (string.IsNullOrEmpty(ActorId))
        {
            Debug.LogWarning($"{nameof(MissionActor)} no tiene ActorId. Configurá NpcInteractable.InteractableId o ActorIdOverride.", logContext);
        }

        if (missionSet == null)
        {
            Debug.LogWarning($"{nameof(MissionActor)} no tiene MissionActorMissionSet asignado.", logContext);
        }
    }

    private NpcInteractable ResolveNpcInteractable()
    {
        if (npcInteractable == null)
        {
            npcInteractable = GetComponent<NpcInteractable>();
        }

        return npcInteractable;
    }

    private string ResolveActorId()
    {
        if (!string.IsNullOrWhiteSpace(actorIdOverride))
        {
            return CleanId(actorIdOverride);
        }

        NpcInteractable resolvedNpc = ResolveNpcInteractable();

        if (resolvedNpc != null && !string.IsNullOrWhiteSpace(resolvedNpc.InteractableId))
        {
            return CleanId(resolvedNpc.InteractableId);
        }

        return string.Empty;
    }

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
