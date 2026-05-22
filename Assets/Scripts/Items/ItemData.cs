using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string itemName;

    [SerializeField, TextArea]
    private string description;

    [Header("Visual")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject worldModelPrefab;

    [Header("Rarity")]
    [SerializeField] private ItemRarity rarity;

    public string ItemName => itemName;
    public string Description => description;
    public Sprite Icon => icon;
    public GameObject WorldModelPrefab => worldModelPrefab;
    public ItemRarity Rarity => rarity;
}