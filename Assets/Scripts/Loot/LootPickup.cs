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

    private GameObject currentVisualInstance;
    private bool wasPickedUp;

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
        Color rarityColor = GetRarityColor(itemData.Rarity);

        if (auraLight != null)
        {
            auraLight.color = rarityColor;
        }
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => Color.white,
            ItemRarity.Uncommon => Color.green,
            ItemRarity.Rare => Color.blue,
            ItemRarity.Epic => new Color(0.6f, 0f, 1f),
            ItemRarity.Legendary => new Color(1f, 0.55f, 0f),
            _ => Color.white
        };
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