using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class VisualInventoryManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private TMJ_WeaponLoadout weaponLoadout;

    [Header("Containers")]
    [SerializeField] private Transform inventoryContainer;
    [SerializeField] private Transform equippedContainer;

    [Header("Equipped Slot Anchors (optional but recommended)")]
    [Tooltip("Fixed UI anchor for the LightAttack equipped weapon. Assign this to keep the visual slot stable even when HeavyAttack is empty.")]
    [SerializeField] private Transform lightAttackEquippedSlotRoot;

    [Tooltip("Fixed UI anchor for the HeavyAttack equipped weapon. Assign this to keep the visual slot stable even when LightAttack is empty.")]
    [SerializeField] private Transform heavyAttackEquippedSlotRoot;

    [Tooltip("When true, equipped item visuals stretch to fill the assigned Light/Heavy slot root.")]
    [SerializeField] private bool stretchEquippedSlotToRoot = true;

    [Header("Slot Prefab")]
    [SerializeField] private ItemVisualSlot slotPrefab;

    [Header("Details Panel")]
    [SerializeField] private InventoryItemDetailsPanel detailsPanel;

    [Header("Toggle")]
    [SerializeField] private GameObject inventoryScrollView;
    [SerializeField] private GameObject equippedScrollView;
    [SerializeField] private Key toggleKey = Key.I;

    private readonly Dictionary<int, ItemVisualSlot> _slotMap = new();

    private readonly Dictionary<TMJ_WeaponUseSlot, ItemVisualSlot> _equippedSlots = new()
    {
        { TMJ_WeaponUseSlot.LightAttack, null },
        { TMJ_WeaponUseSlot.HeavyAttack, null }
    };

    private Coroutine _feedbackCoroutine;

    private void Awake()
    {
        if (inventoryManager == null)
        {
            inventoryManager = GetComponent<InventoryManager>();
        }

        if (weaponLoadout == null)
        {
            weaponLoadout = GetComponent<TMJ_WeaponLoadout>();
        }

        Debug.Assert(inventoryManager != null, "[VisualInventoryManager] InventoryManager not assigned.");
        Debug.Assert(slotPrefab != null, "[VisualInventoryManager] Slot prefab not assigned.");
        Debug.Assert(inventoryContainer != null, "[VisualInventoryManager] Inventory container not assigned.");
        Debug.Assert(equippedContainer != null, "[VisualInventoryManager] Equipped container not assigned.");
    }

    private void OnEnable()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnItemAdded += HandleExternalItemAdded;
            inventoryManager.OnItemRemoved += HandleExternalItemRemoved;
        }

        if (weaponLoadout != null)
        {
            weaponLoadout.OnLoadoutChanged += SyncWithWeaponLoadout;
        }
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnItemAdded -= HandleExternalItemAdded;
            inventoryManager.OnItemRemoved -= HandleExternalItemRemoved;
        }

        if (weaponLoadout != null)
        {
            weaponLoadout.OnLoadoutChanged -= SyncWithWeaponLoadout;
        }
    }

    private void Start()
    {
        if (inventoryManager != null)
        {
            foreach (ItemData item in inventoryManager.Items)
            {
                CreateSlot(item, inventoryContainer, isEquipped: false);
            }
        }

        SyncWithWeaponLoadout();

        detailsPanel?.Clear();
        detailsPanel?.SetVisible(IsInventoryOpen());
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            return;
        }

        bool show = inventoryScrollView != null && !inventoryScrollView.activeSelf;

        inventoryScrollView?.SetActive(show);
        equippedScrollView?.SetActive(show);
        detailsPanel?.SetVisible(show);

        if (show)
        {
            detailsPanel?.Clear();
        }
        else
        {
            CloseAllSlotActions();
        }
    }

    private void HandleExternalItemAdded(ItemData item)
    {
        CreateSlot(item, inventoryContainer, isEquipped: false);
        SyncWithWeaponLoadout();
    }

    private void HandleExternalItemRemoved(ItemData item)
    {
        ItemVisualSlot target = FindSlotForExternalRemoval(item);

        if (target != null)
        {
            DestroySlotDirect(target, updateLoadout: true);
            detailsPanel?.Clear();
        }
    }

    private ItemVisualSlot CreateSlot(ItemData item, Transform parent, bool isEquipped)
    {
        if (item == null || slotPrefab == null || parent == null)
        {
            return null;
        }

        ItemVisualSlot slot = Instantiate(slotPrefab, parent);
        slot.Bind(item);
        slot.SetEquipped(isEquipped);

        slot.OnEquipToUseSlotRequested += HandleEquipToUseSlot;
        slot.OnUnequipRequested += HandleUnequipRequested;
        slot.OnDiscardRequested += HandleDiscard;
        slot.OnHoverEnter += HandleSlotHoverEnter;
        slot.OnHoverExit += HandleSlotHoverExit;

        _slotMap[slot.GetInstanceID()] = slot;
        return slot;
    }

    private void DestroySlotDirect(ItemVisualSlot slot, bool updateLoadout)
    {
        if (slot == null)
        {
            return;
        }

        if (TryGetUseSlotForSlot(slot, out TMJ_WeaponUseSlot useSlot))
        {
            _equippedSlots[useSlot] = null;

            if (updateLoadout)
            {
                weaponLoadout?.EquipWeapon(useSlot, null);
            }
        }

        UnsubscribeSlot(slot);
        _slotMap.Remove(slot.GetInstanceID());
        Destroy(slot.gameObject);
    }

    private void HandleEquipToUseSlot(ItemVisualSlot slot, TMJ_WeaponUseSlot targetUseSlot)
    {
        if (slot == null)
        {
            return;
        }

        if (slot.Data is not WeaponData weapon)
        {
            ShowFeedback($"{GetItemDisplayName(slot.Data)} no es equipable.");
            return;
        }

        if (weaponLoadout == null)
        {
            ShowFeedback("No hay un WeaponLoadout asignado.");
            return;
        }

        if (slot.IsEquipped)
        {
            ShowFeedback($"{weapon.DisplayName} ya está equipada. Desequipala antes de cambiarla de slot.");
            return;
        }

        // Remove the selected weapon from the normal inventory first.
        // This frees one inventory position before returning a displaced equipped weapon.
        RemoveFromInventorySilently(slot.Data);

        ItemVisualSlot displaced = _equippedSlots[targetUseSlot];
        if (displaced != null)
        {
            bool returned = MoveToInventory(displaced, targetUseSlot, updateLoadout: false);
            if (!returned)
            {
                // Rollback best-effort: put the selected item back in inventory.
                AddToInventorySilently(slot.Data);
                ShowFeedback("Inventario lleno, no se puede cambiar el arma equipada.");
                return;
            }
        }

        PlaceInEquipped(slot, targetUseSlot, weapon, updateLoadout: true);
        detailsPanel?.Show(slot.Data);
        ShowFeedback($"{weapon.DisplayName} equipado en {FormatUseSlot(targetUseSlot)}.");
    }

    private void HandleUnequipRequested(ItemVisualSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        if (!TryGetUseSlotForSlot(slot, out TMJ_WeaponUseSlot useSlot))
        {
            return;
        }

        bool moved = MoveToInventory(slot, useSlot, updateLoadout: true);
        if (!moved)
        {
            ShowFeedback("Inventario lleno, no se puede desequipar.");
            return;
        }

        detailsPanel?.Show(slot.Data);
        //ShowFeedback($"{GetItemDisplayName(slot.Data)} desequipado.");
    }

    private void PlaceInEquipped(ItemVisualSlot slot, TMJ_WeaponUseSlot useSlot, WeaponData weapon, bool updateLoadout)
    {
        if (slot == null || weapon == null)
        {
            return;
        }

        Transform targetParent = GetEquippedSlotRoot(useSlot);
        ParentSlot(slot, targetParent, ShouldStretchInEquippedRoot(targetParent));

        slot.SetEquipped(true);
        _equippedSlots[useSlot] = slot;

        if (updateLoadout)
        {
            weaponLoadout?.EquipWeapon(useSlot, weapon);
        }
    }

    private bool MoveToInventory(ItemVisualSlot slot, TMJ_WeaponUseSlot useSlot, bool updateLoadout)
    {
        if (slot == null)
        {
            return false;
        }

        if (inventoryManager != null)
        {
            bool added = AddToInventorySilently(slot.Data);
            if (!added)
            {
                return false;
            }
        }

        ParentSlot(slot, inventoryContainer, stretchToParent: false);
        slot.SetEquipped(false);
        _equippedSlots[useSlot] = null;

        if (updateLoadout)
        {
            weaponLoadout?.EquipWeapon(useSlot, null);
        }

        return true;
    }

    private void SyncWithWeaponLoadout()
    {
        if (weaponLoadout == null)
        {
            return;
        }

        SyncEquippedSlot(TMJ_WeaponUseSlot.LightAttack);
        SyncEquippedSlot(TMJ_WeaponUseSlot.HeavyAttack);
    }

    private void SyncEquippedSlot(TMJ_WeaponUseSlot useSlot)
    {
        WeaponData desiredWeapon = weaponLoadout.GetWeapon(useSlot);
        ItemVisualSlot currentSlot = _equippedSlots[useSlot];

        if (desiredWeapon == null)
        {
            if (currentSlot != null)
            {
                MoveToInventory(currentSlot, useSlot, updateLoadout: false);
            }

            return;
        }

        if (currentSlot != null && currentSlot.Data == desiredWeapon)
        {
            EnsureSlotLooksEquipped(currentSlot);
            return;
        }

        if (currentSlot != null)
        {
            MoveToInventory(currentSlot, useSlot, updateLoadout: false);
        }

        ItemVisualSlot availableSlot = FindAvailableSlotForItem(desiredWeapon);

        // This can happen with starting weapons assigned directly in TMJ_WeaponLoadout.
        // We still create an equipped visual slot so the UI mirrors the real loadout.
        if (availableSlot == null)
        {
            availableSlot = CreateSlot(desiredWeapon, equippedContainer, isEquipped: true);
        }
        else
        {
            RemoveFromInventorySilently(availableSlot.Data);
        }

        PlaceInEquipped(availableSlot, useSlot, desiredWeapon, updateLoadout: false);
    }

    private void EnsureSlotLooksEquipped(ItemVisualSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        if (TryGetUseSlotForSlot(slot, out TMJ_WeaponUseSlot useSlot))
        {
            Transform targetParent = GetEquippedSlotRoot(useSlot);
            ParentSlot(slot, targetParent, ShouldStretchInEquippedRoot(targetParent));
        }
        else
        {
            ParentSlot(slot, equippedContainer, stretchToParent: false);
        }

        slot.SetEquipped(true);
    }

    private Transform GetEquippedSlotRoot(TMJ_WeaponUseSlot useSlot)
    {
        Transform target = useSlot switch
        {
            TMJ_WeaponUseSlot.LightAttack => lightAttackEquippedSlotRoot,
            TMJ_WeaponUseSlot.HeavyAttack => heavyAttackEquippedSlotRoot,
            _ => null
        };

        return target != null ? target : equippedContainer;
    }

    private bool ShouldStretchInEquippedRoot(Transform targetParent)
    {
        if (!stretchEquippedSlotToRoot || targetParent == null)
        {
            return false;
        }

        // Do not stretch when using the legacy common equipped container fallback.
        // That container may still have a GridLayoutGroup controlling its children.
        return targetParent != equippedContainer;
    }

    private static void ParentSlot(ItemVisualSlot slot, Transform parent, bool stretchToParent)
    {
        if (slot == null || parent == null)
        {
            return;
        }

        Transform slotTransform = slot.transform;
        slotTransform.SetParent(parent, false);
        slotTransform.localScale = Vector3.one;

        if (slotTransform is not RectTransform rectTransform)
        {
            return;
        }

        if (stretchToParent)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            return;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private ItemVisualSlot FindAvailableSlotForItem(ItemData item)
    {
        foreach (ItemVisualSlot slot in _slotMap.Values)
        {
            if (slot == null || slot.Data != item || IsSlotEquippedAnywhere(slot))
            {
                continue;
            }

            return slot;
        }

        return null;
    }

    private ItemVisualSlot FindSlotForExternalRemoval(ItemData item)
    {
        ItemVisualSlot equippedFallback = null;

        foreach (ItemVisualSlot slot in _slotMap.Values)
        {
            if (slot == null || slot.Data != item)
            {
                continue;
            }

            if (!IsSlotEquippedAnywhere(slot))
            {
                return slot;
            }

            equippedFallback ??= slot;
        }

        return equippedFallback;
    }

    private bool IsSlotEquippedAnywhere(ItemVisualSlot slot)
    {
        foreach (ItemVisualSlot equippedSlot in _equippedSlots.Values)
        {
            if (equippedSlot == slot)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetUseSlotForSlot(ItemVisualSlot slot, out TMJ_WeaponUseSlot useSlot)
    {
        foreach (KeyValuePair<TMJ_WeaponUseSlot, ItemVisualSlot> pair in _equippedSlots)
        {
            if (pair.Value == slot)
            {
                useSlot = pair.Key;
                return true;
            }
        }

        useSlot = default;
        return false;
    }

    private void HandleDiscard(ItemVisualSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        if (slot.IsEquipped)
        {
            ShowFeedback("Desequipá el arma antes de descartarla.");
            return;
        }

        ItemData item = slot.Data;
        RemoveFromInventorySilently(item);
        DestroySlotDirect(slot, updateLoadout: true);
        detailsPanel?.Clear();
        ShowFeedback($"{GetItemDisplayName(item)} descartado.");
    }

    private void RemoveFromInventorySilently(ItemData item)
    {
        if (inventoryManager == null || item == null)
        {
            return;
        }

        inventoryManager.OnItemRemoved -= HandleExternalItemRemoved;
        inventoryManager.RemoveItem(item);
        inventoryManager.OnItemRemoved += HandleExternalItemRemoved;
    }

    private bool AddToInventorySilently(ItemData item)
    {
        if (inventoryManager == null || item == null)
        {
            return false;
        }

        inventoryManager.OnItemAdded -= HandleExternalItemAdded;
        bool added = inventoryManager.AddItem(item, allowAutoEquip: false);
        inventoryManager.OnItemAdded += HandleExternalItemAdded;
        return added;
    }

    private void HandleSlotHoverEnter(ItemVisualSlot slot)
    {
        if (!IsInventoryOpen() || slot == null)
        {
            return;
        }

        detailsPanel?.Show(slot.Data);
    }

    private void HandleSlotHoverExit(ItemVisualSlot slot)
    {
        // Intentionally left empty.
        // Keeping the last hovered item visible lets the player read long descriptions.
        // not keep
    }

    private bool IsInventoryOpen()
    {
        return inventoryScrollView != null && inventoryScrollView.activeSelf;
    }

    private void CloseAllSlotActions()
    {
        foreach (ItemVisualSlot slot in _slotMap.Values)
        {
            if (slot != null)
            {
                slot.CloseActions();
            }
        }
    }

    private void UnsubscribeSlot(ItemVisualSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.OnEquipToUseSlotRequested -= HandleEquipToUseSlot;
        slot.OnUnequipRequested -= HandleUnequipRequested;
        slot.OnDiscardRequested -= HandleDiscard;
        slot.OnHoverEnter -= HandleSlotHoverEnter;
        slot.OnHoverExit -= HandleSlotHoverExit;
    }

    private static string FormatUseSlot(TMJ_WeaponUseSlot useSlot)
    {
        return useSlot switch
        {
            TMJ_WeaponUseSlot.LightAttack => "ataque ligero",
            TMJ_WeaponUseSlot.HeavyAttack => "ataque pesado",
            _ => useSlot.ToString()
        };
    }

    private static string GetItemDisplayName(ItemData item)
    {
        if (item == null)
        {
            return "Item";
        }

        if (!string.IsNullOrWhiteSpace(item.DisplayName))
        {
            return item.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(item.ItemName))
        {
            return item.ItemName;
        }

        return "Item";
    }

    private void ShowFeedback(string message)
    {
        TMJNotifications.ShowInventory(
            message,
            NotificationPriority.Normal,
            "Inventario",
            $"inventory_feedback:{message}",
            this);
    }
}
