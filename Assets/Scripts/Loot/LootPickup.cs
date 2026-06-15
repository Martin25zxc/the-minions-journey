using UnityEngine;

[DisallowMultipleComponent]
public sealed class LootPickup : MonoBehaviour
{
    [Header("Item")]
    [Tooltip("Puede venir asignado en un prefab manual, o ser inyectado por LootSpawner.Initialize().")]
    [SerializeField]
    private ItemData itemData;

    [Header("Pickup")]
    [SerializeField]
    private string playerTag = "Player";

    [Header("Visual")]
    [SerializeField]
    private Transform visualRoot;

    [SerializeField]
    private Light auraLight;

    [SerializeField]
    private ParticleSystem auraParticles;

    [Header("Visual Database")]
    [SerializeField]
    private RarityVisualDatabase rarityVisualDatabase;

    private GameObject currentVisualInstance;
    private bool wasPickedUp;
    private bool wasInitialized;

    private void Start()
    {
        // Caso 1: pickup puesto manualmente en escena con ItemData ya asignado.
        if (itemData != null && currentVisualInstance == null)
        {
            RefreshVisual();
            return;
        }

        // Caso 2: pickup instanciado por LootSpawner.
        // En ese caso Initialize debería haberse llamado inmediatamente después de Instantiate.
        if (itemData == null && !wasInitialized)
        {
            Debug.LogWarning($"{name} has no ItemData assigned. If this pickup is spawned by LootSpawner, make sure Initialize is called.", this);
        }
    }

    public void Initialize(ItemData newItemData)
    {
        wasInitialized = true;
        itemData = newItemData;

        if (itemData == null)
        {
            Debug.LogWarning($"{name} was initialized with null ItemData.", this);
            return;
        }

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (itemData == null)
        {
            return;
        }

        SpawnWorldModel();
        ApplyRarityVisuals();
    }

    private void SpawnWorldModel()
    {
        if (visualRoot == null)
        {
            Debug.LogWarning($"{name} has no VisualRoot assigned.", this);
            return;
        }

        if (currentVisualInstance != null)
        {
            Destroy(currentVisualInstance);
            currentVisualInstance = null;
        }

        if (itemData.WorldModelPrefab == null)
        {
            Debug.LogWarning($"{itemData.ItemName} has no WorldModelPrefab assigned.", this);
            return;
        }

        currentVisualInstance = Instantiate(itemData.WorldModelPrefab, visualRoot);

        currentVisualInstance.transform.localPosition = Vector3.zero;
        currentVisualInstance.transform.localRotation = Quaternion.identity;
        // La escala queda en manos del prefab visual de cada item.
        // Si un item se ve grande/chico, conviene ajustar su WorldModelPrefab, no este script.
    }

    private void ApplyRarityVisuals()
    {
        if (rarityVisualDatabase == null)
        {
            Debug.LogWarning($"{name} has no RarityVisualDatabase assigned.", this);
            return;
        }

        RarityVisualData visualData =
            rarityVisualDatabase.GetVisualData(itemData.Rarity);

        if (visualData == null)
        {
            return;
        }

        ApplyLightVisuals(visualData);
        ApplyParticleVisuals(visualData);
    }

    private void ApplyLightVisuals(RarityVisualData visualData)
    {
        if (auraLight == null)
        {
            return;
        }

        auraLight.color = visualData.AuraColor;
        auraLight.intensity = visualData.LightIntensity;
        auraLight.range = visualData.LightRange;
    }

    private void ApplyParticleVisuals(RarityVisualData visualData)
    {
        if (auraParticles == null)
        {
            return;
        }

        ParticleSystem.MainModule main = auraParticles.main;
        main.startColor = visualData.AuraColor;
        main.startSize = visualData.ParticleStartSize;
        main.startSpeed = visualData.ParticleStartSpeed;
        main.startLifetime = visualData.ParticleStartLifetime;

        ParticleSystem.EmissionModule emission = auraParticles.emission;
        emission.rateOverTime = visualData.ParticleRateOverTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (wasPickedUp)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        TryPickup(other.gameObject);
    }

    private void TryPickup(GameObject player)
    {
        if (itemData == null)
        {
            Debug.LogWarning($"{name} has no ItemData assigned.", this);
            return;
        }

        InventoryManager inventoryManager = player.GetComponent<InventoryManager>();

        if (inventoryManager == null)
        {
            Debug.LogWarning("Player has no InventoryManager.", this);
            return;
        }

        wasPickedUp = true;
        inventoryManager.AddItem(itemData);

        Debug.Log($"Picked up loot: {itemData.ItemName}", this);

        Destroy(gameObject);
    }
}
