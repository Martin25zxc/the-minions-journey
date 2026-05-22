using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InventoryManager : MonoBehaviour
{
    [Header("Debug Inventory")]
    [SerializeField] private List<ItemData> items = new();

    public IReadOnlyList<ItemData> Items => items;

    public void AddItem(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning("Se intentó agregar un item null al inventario.");
            return;
        }

        items.Add(itemData);

        Debug.Log($"Se agregó el item: {itemData.ItemName}");
    }

    public bool RemoveItem(ItemData itemData)
    {
        if (itemData == null)
        {
            return false;
        }

        bool removed = items.Remove(itemData);

        if (removed)
        {
            Debug.Log($"Se eliminó el item: {itemData.ItemName}");
        }

        return removed;
    }

    public int GetItemCount()
    {
        return items.Count;
    }
}