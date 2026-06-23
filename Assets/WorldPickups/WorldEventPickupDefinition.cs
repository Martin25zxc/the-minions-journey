using UnityEngine;

/// <summary>
/// Definición data-driven de un pickeable que produce un evento del mundo.
///
/// Solución experimental:
/// este asset NO representa inventario. Representa un objeto que, al recolectarse,
/// informa un GameWorldEvent. Puede usarse para manzanas de misión, herramientas,
/// artefactos, llaves conceptuales o reliquias narrativas.
///
/// Si en el futuro se unifica LootPickup + WorldEventPickup bajo una abstracción común,
/// este asset será una base útil para migrar datos sin hardcodear pickups por escena.
/// </summary>
[CreateAssetMenu(menuName = "Game/Pickups/World Event Pickup Definition")]
public sealed class WorldEventPickupDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string pickupId;
    [SerializeField] private string displayName;

    [TextArea(2, 4)]
    [SerializeField] private string description;

    [Header("Visual")]
    [SerializeField] private GameObject worldModelPrefab;
    [SerializeField] private PickupVisualProfile visualProfile;

    [Header("Default Collection")]
    [SerializeField] private WorldEventPickupCollectMode defaultCollectMode = WorldEventPickupCollectMode.Trigger;

    [Tooltip("Verbo semántico. La UI de interacción decide cómo mostrar tecla + verbo.")]
    [SerializeField] private string defaultPromptVerb = "Recoger";

    [Header("World Event")]
    [SerializeField] private GameWorldEventType eventType = GameWorldEventType.ItemCollected;
    [SerializeField] private string targetId;
    [SerializeField, Min(1)] private int amount = 1;

    public string PickupId => pickupId;
    public string DisplayName => displayName;
    public string Description => description;
    public GameObject WorldModelPrefab => worldModelPrefab;
    public PickupVisualProfile VisualProfile => visualProfile;
    public WorldEventPickupCollectMode DefaultCollectMode => defaultCollectMode;
    public string DefaultPromptVerb => defaultPromptVerb;
    public GameWorldEventType EventType => eventType;
    public string TargetId => targetId;
    public int Amount => amount;

#if UNITY_EDITOR
    private void OnValidate()
    {
        amount = Mathf.Max(1, amount);

        if (string.IsNullOrWhiteSpace(pickupId))
        {
            Debug.LogWarning($"{name} has no PickupId assigned.", this);
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            Debug.LogWarning($"{name} has no DisplayName assigned.", this);
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            Debug.LogWarning($"{name} has no TargetId assigned. Mission objectives will not be able to match this pickup.", this);
        }

        if (worldModelPrefab == null)
        {
            Debug.LogWarning($"{name} has no WorldModelPrefab assigned.", this);
        }

        if (visualProfile == null)
        {
            Debug.LogWarning($"{name} has no PickupVisualProfile assigned.", this);
        }

        if (eventType == GameWorldEventType.ArtifactAcquired &&
            visualProfile != null &&
            visualProfile.Importance != PickupVisualImportance.Major)
        {
            Debug.LogWarning(
                $"{name} reports ArtifactAcquired but uses a non-Major visual profile. This can be valid, but review the intended player read.",
                this
            );
        }
    }
#endif
}
