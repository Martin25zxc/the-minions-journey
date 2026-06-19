using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InventoryManager : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField, Min(1)] private int maxItems = 16;

    [Header("Auto Equip")]
    [Tooltip("If enabled, picked up WeaponData items will be equipped automatically only when a weapon use slot is empty.")]
    [SerializeField] private bool autoEquipWeaponsOnPickup = true;

    [Header("References")]
    [Tooltip("Optional. If left empty, this component will search for TMJ_WeaponLoadout on the same GameObject.")]
    [SerializeField] private TMJ_WeaponLoadout weaponLoadout;

    [Header("Debug Inventory")]
    [SerializeField] private List<ItemData> items = new();

    public IReadOnlyList<ItemData> Items => items;
    public bool IsFull => items.Count >= maxItems;

    public event Action<ItemData> OnItemAdded;
    public event Action<ItemData> OnItemRemoved;

    private void Awake()
    {
        if (weaponLoadout == null)
        {
            weaponLoadout = GetComponent<TMJ_WeaponLoadout>();
        }
    }

    /// <summary>
    /// Default pickup entry point.
    /// Keeps the old API usable while allowing internal calls to suppress auto-equip.
    /// </summary>
    public bool AddItem(ItemData itemData)
    {
        return AddItem(itemData, allowAutoEquip: true);
    }

    /// <summary>
    /// Adds an item to the inventory.
    /// allowAutoEquip should be false when the UI is only moving an equipped item back to the inventory,
    /// otherwise unequipping would immediately auto-equip the item again.
    /// </summary>
    public bool AddItem(ItemData itemData, bool allowAutoEquip)
    {
        if (itemData == null)
        {
            Debug.LogWarning("Se intentó agregar un item null al inventario.");
            return false;
        }

        if (IsFull)
        {
            Debug.LogWarning($"Inventario lleno ({maxItems} items).");
            return false;
        }

        items.Add(itemData);

        // Important order:
        // 1) Notify UI so it can create the visual slot.
        // 2) Auto-equip after that, so VisualInventoryManager can move that same slot to equipped.
        OnItemAdded?.Invoke(itemData);

        if (allowAutoEquip && autoEquipWeaponsOnPickup)
        {
            TryAutoEquipItem(itemData);
        }

        Debug.Log($"Se agregó el item: {itemData.ItemName}");
        return true;
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
            OnItemRemoved?.Invoke(itemData);
            Debug.Log($"Se eliminó el item: {itemData.ItemName}");
        }

        return removed;
    }

    public int GetItemCount()
    {
        return items.Count;
    }

    private bool TryAutoEquipItem(ItemData itemData)
    {
        // Safety guard: InventoryManager works with ItemData, but only WeaponData is equipable here.
        // Other item types such as materials, consumables or quest items are simply stored.
        if (itemData is not WeaponData weaponData)
        {
            return false;
        }

        if (weaponLoadout == null)
        {
            Debug.LogWarning("No hay TMJ_WeaponLoadout asignado. No se puede auto-equipar el arma.");
            return false;
        }

        // Option A: first empty slot wins. WeaponType is intentionally ignored.
        if (weaponLoadout.GetWeapon(TMJ_WeaponUseSlot.LightAttack) == null)
        {
            weaponLoadout.EquipWeapon(TMJ_WeaponUseSlot.LightAttack, weaponData);
            Debug.Log($"Se equipó automáticamente en LightAttack: {weaponData.ItemName}");
            return true;
        }

        if (weaponLoadout.GetWeapon(TMJ_WeaponUseSlot.HeavyAttack) == null)
        {
            weaponLoadout.EquipWeapon(TMJ_WeaponUseSlot.HeavyAttack, weaponData);
            Debug.Log($"Se equipó automáticamente en HeavyAttack: {weaponData.ItemName}");
            return true;
        }

        Debug.Log($"{weaponData.ItemName} se agregó al inventario, pero no se equipó porque no hay slots libres.");
        return false;
    }
}
