using UnityEngine;

[DisallowMultipleComponent]
public sealed class LootPickup : MonoBehaviour
{
    [Header("Item")]
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

    private void Awake()
    {
        RefreshVisual();
    }

    private void Start()
    {
        RefreshVisual();
    }

    public void Initialize(ItemData newItemData)
    {
        itemData = newItemData;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (itemData == null)
        {
            Debug.LogWarning($"{name} has no ItemData assigned.");
            return;
        }

        SpawnWorldModel();
        ApplyRarityVisuals();
    }

    private void SpawnWorldModel()
    {
        if (visualRoot == null)
        {
            Debug.LogWarning($"{name} has no VisualRoot assigned.");
            return;
        }

        if (currentVisualInstance != null)
        {
            Destroy(currentVisualInstance);
        }

        if (itemData.WorldModelPrefab == null)
        {
            Debug.LogWarning($"{itemData.ItemName} has no WorldModelPrefab assigned.");
            return;
        }

        currentVisualInstance = Instantiate(itemData.WorldModelPrefab, visualRoot);

        currentVisualInstance.transform.localPosition = Vector3.zero;
        currentVisualInstance.transform.localRotation = Quaternion.identity;
        // Tener en cuenta que si usamos diferentes modelos para cada item,
        //  puede que necesitemos ajustar la escala de cada prefab para que se vean bien al spawnear. 
        // currentVisualInstance.transform.localScale = Vector3.one;
    }

    private void ApplyRarityVisuals()
    {
        if (rarityVisualDatabase == null)
        {
            Debug.LogWarning($"{name} has no RarityVisualDatabase assigned.");
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
            Debug.LogWarning($"{name} has no ItemData assigned.");
            return;
        }

        InventoryManager inventoryManager = player.GetComponent<InventoryManager>();

        if (inventoryManager == null)
        {
            Debug.LogWarning("Player has no InventoryManager.");
            return;
        }

        wasPickedUp = true;

        inventoryManager.AddItem(itemData);

        Debug.Log($"Picked up loot: {itemData.ItemName}");

        Destroy(gameObject);
    }
}