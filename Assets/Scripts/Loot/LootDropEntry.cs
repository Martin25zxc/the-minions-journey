using UnityEngine;

[System.Serializable]
public sealed class LootDropEntry
{
    [SerializeField]
    private ItemData itemData;

    [SerializeField, Min(1)]
    private int amount = 1;

    public ItemData ItemData => itemData;
    public int Amount => amount;
}