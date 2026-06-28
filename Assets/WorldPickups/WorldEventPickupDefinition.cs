using UnityEngine;

/// <summary>
/// Definición data-driven de un pickeable que produce un evento del mundo.
///
/// Solución experimental:
/// este asset NO representa inventario. Representa un objeto que, al recolectarse,
/// informa un GameWorldEvent. Puede usarse para hongos de misión, herramientas,
/// artefactos, llaves conceptuales o reliquias narrativas.
///
/// La Definition describe qué es el pickup y qué evento emite.
/// La composición exacta del visual puede venir desde WorldModelPrefab o desde un visual ya puesto en escena.
/// </summary>
[CreateAssetMenu(menuName = "TMJ/Pickups/World Event Pickup Definition")]
public sealed class WorldEventPickupDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("ID estable del pickeable como contenido. Se usa como SourceId del GameWorldEvent si está asignado.")]
    [SerializeField] private string pickupId;

    [Tooltip("Nombre legible para debug, prompts o documentación de contenido.")]
    [SerializeField] private string displayName;

    [TextArea(2, 4)]
    [Tooltip("Descripción de autoría. No modifica lógica.")]
    [SerializeField] private string description;

    [Header("Visual")]
    [Tooltip("Prefab visual opcional. Se usa cuando WorldEventPickup está en modo InstantiateFromDefinition. Puede quedar vacío si el pickup usa un visual ya colocado en escena.")]
    [SerializeField] private GameObject worldModelPrefab;

    [Tooltip("Ajuste local aplicado al modelo instanciado. No mueve el root del pickup ni el collider.")]
    [SerializeField] private Vector3 modelLocalPositionOffset = Vector3.zero;

    [Tooltip("Rotación local en grados aplicada al modelo instanciado.")]
    [SerializeField] private Vector3 modelLocalEulerAngles = Vector3.zero;

    [Tooltip("Escala local aplicada al modelo instanciado. Sirve para corregir prefabs visuales demasiado chicos/grandes sin tocar el prefab original.")]
    [SerializeField] private Vector3 modelLocalScale = Vector3.one;

    [Tooltip("Perfil visual reutilizable: color/intensidad de luz, partículas y movimiento. No define misión, inventario ni recompensa.")]
    [SerializeField] private PickupVisualProfile visualProfile;

    [Header("Default Collection")]
    [Tooltip("Modo de recolección sugerido por esta Definition. Puede ser sobreescrito por la instancia de WorldEventPickup.")]
    [SerializeField] private WorldEventPickupCollectMode defaultCollectMode = WorldEventPickupCollectMode.Trigger;

    [Tooltip("Verbo semántico. La UI de interacción decide cómo mostrar tecla + verbo. Ejemplo: Recoger.")]
    [SerializeField] private string defaultPromptVerb = "Recoger";

    [Header("World Event")]
    [Tooltip("Tipo de evento que se reportará al MissionManager.")]
    [SerializeField] private GameWorldEventType eventType = GameWorldEventType.ItemCollected;

    [Tooltip("TargetId que debe coincidir con el objetivo de MissionDefinition. Ejemplo: luminous_mushroom, hook_artifact.")]
    [SerializeField] private string targetId;

    [Tooltip("Cantidad reportada en el GameWorldEvent. Para un pickup individual suele ser 1.")]
    [SerializeField, Min(1)] private int amount = 1;

    public string PickupId => pickupId;
    public string DisplayName => displayName;
    public string Description => description;

    public GameObject WorldModelPrefab => worldModelPrefab;
    public Vector3 ModelLocalPositionOffset => modelLocalPositionOffset;
    public Vector3 ModelLocalEulerAngles => modelLocalEulerAngles;
    public Vector3 ModelLocalScale => modelLocalScale;
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
        modelLocalScale.x = Mathf.Max(0.01f, modelLocalScale.x);
        modelLocalScale.y = Mathf.Max(0.01f, modelLocalScale.y);
        modelLocalScale.z = Mathf.Max(0.01f, modelLocalScale.z);

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
