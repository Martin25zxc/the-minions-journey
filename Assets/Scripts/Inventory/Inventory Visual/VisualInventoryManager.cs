using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class VisualInventoryManager : MonoBehaviour
{
    // ── Inspector wiring ─────────────────────────────────────────────────
    [Header("Dependencies")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private TMJ_WeaponLoadout weaponLoadout;

    [Header("Containers")]
    [SerializeField] private Transform inventoryContainer;
    [SerializeField] private Transform equippedContainer;

    [Header("Slot Prefab")]
    [SerializeField] private ItemVisualSlot slotPrefab;

    [Header("Toggle")]
    [SerializeField] private GameObject inventoryScrollView;
    [SerializeField] private GameObject equippedScrollView;
    [SerializeField] private Key toggleKey = Key.I;

    [Header("Equip Settings")]
    [Tooltip("If true, manual equip ignores WeaponType and uses the first free slot when possible. Auto-equip is handled by InventoryManager Option A.")]
    [SerializeField] private bool ignoreWeaponType = false;

    [Header("Feedback (optional)")]
    [SerializeField] private TextMeshProUGUI feedbackLabel;
    [SerializeField, Min(0.1f)] private float feedbackDuration = 2f;

    // ── Runtime state ────────────────────────────────────────────────────
    private readonly Dictionary<int, ItemVisualSlot> _slotMap = new();

    private readonly Dictionary<TMJ_WeaponUseSlot, ItemVisualSlot> _equippedSlots = new()
    {
        { TMJ_WeaponUseSlot.LightAttack, null },
        { TMJ_WeaponUseSlot.HeavyAttack, null }
    };

    private Coroutine _feedbackCoroutine;

    // ── Unity lifecycle ──────────────────────────────────────────────────
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
            // Best practice: the UI mirrors the loadout instead of assuming equip state by itself.
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
    }

    // ── External InventoryManager event handlers ──────────────────────────
    private void HandleExternalItemAdded(ItemData item)
    {
        CreateSlot(item, inventoryContainer, isEquipped: false);

        // Harmless before auto-equip. Useful if a caller modifies the loadout before/without AddItem events.
        SyncWithWeaponLoadout();
    }

    private void HandleExternalItemRemoved(ItemData item)
    {
        // With ItemData-only inventories, duplicates are ambiguous.
        // Best-effort rule: remove a non-equipped matching visual first, then an equipped one if necessary.
        ItemVisualSlot target = FindSlotForExternalRemoval(item);

        if (target != null)
        {
            DestroySlotDirect(target, updateLoadout: true);
        }
    }

    // ── Slot creation / destruction ───────────────────────────────────────
    private ItemVisualSlot CreateSlot(ItemData item, Transform parent, bool isEquipped)
    {
        if (item == null || slotPrefab == null || parent == null)
        {
            return null;
        }

        ItemVisualSlot slot = Instantiate(slotPrefab, parent);
        slot.Bind(item);
        slot.SetEquipped(isEquipped);

        slot.OnEquipRequested += HandleEquip;
        slot.OnDiscardRequested += HandleDiscard;
        slot.OnDestroyRequested += HandleDestroy;

        _slotMap[slot.GetInstanceID()] = slot;
        return slot;
    }

    private void DestroySlotDirect(ItemVisualSlot slot, bool updateLoadout)
    {
        if (slot == null)
        {
            return;
        }

        foreach (TMJ_WeaponUseSlot useSlot in System.Enum.GetValues(typeof(TMJ_WeaponUseSlot)))
        {
            if (_equippedSlots[useSlot] != slot)
            {
                continue;
            }

            _equippedSlots[useSlot] = null;

            if (updateLoadout)
            {
                weaponLoadout?.EquipWeapon(useSlot, null);
            }

            break;
        }

        UnsubscribeSlot(slot);
        _slotMap.Remove(slot.GetInstanceID());
        Destroy(slot.gameObject);
    }

    // ── Equip / Unequip ──────────────────────────────────────────────────
    private void HandleEquip(ItemVisualSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        if (slot.IsEquipped)
        {
            Unequip(slot);
            return;
        }

        if (slot.Data is not WeaponData weapon)
        {
            ShowFeedback($"{slot.Data.DisplayName} no es equipable.");
            return;
        }

        if (weaponLoadout == null)
        {
            ShowFeedback("No hay un WeaponLoadout asignado.");
            return;
        }

        TMJ_WeaponUseSlot targetUseSlot = ResolveManualEquipSlot(weapon);

        // Swap: displace current occupant back to inventory first.
        ItemVisualSlot displaced = _equippedSlots[targetUseSlot];
        if (displaced != null)
        {
            MoveToInventory(displaced, targetUseSlot, updateLoadout: false);
        }

        MoveToEquipped(slot, targetUseSlot, weapon, updateLoadout: true);
        ShowFeedback($"{weapon.DisplayName} equipado.");
    }

    private TMJ_WeaponUseSlot ResolveManualEquipSlot(WeaponData weapon)
    {
        // If allowed, manual equip behaves like Option A: first empty slot wins.
        if (ignoreWeaponType && TryGetFirstEmptyUseSlot(out TMJ_WeaponUseSlot freeSlot))
        {
            return freeSlot;
        }

        if (!ignoreWeaponType)
        {
            return weapon.WeaponType == WeaponType.MainHand
                ? TMJ_WeaponUseSlot.LightAttack
                : TMJ_WeaponUseSlot.HeavyAttack;
        }

        // Both slots occupied and ignoreWeaponType is true: keep old swap fallback.
        return TMJ_WeaponUseSlot.HeavyAttack;
    }

    private void Unequip(ItemVisualSlot slot)
    {
        if (inventoryManager != null && inventoryManager.IsFull)
        {
            ShowFeedback("Inventario lleno, no se puede desequipar.");
            return;
        }

        foreach (TMJ_WeaponUseSlot useSlot in System.Enum.GetValues(typeof(TMJ_WeaponUseSlot)))
        {
            if (_equippedSlots[useSlot] != slot)
            {
                continue;
            }

            MoveToInventory(slot, useSlot, updateLoadout: true);
            ShowFeedback($"{slot.Data.DisplayName} desequipado.");
            return;
        }
    }

    private void MoveToEquipped(ItemVisualSlot slot, TMJ_WeaponUseSlot useSlot, WeaponData weapon, bool updateLoadout)
    {
        if (slot == null || weapon == null)
        {
            return;
        }

        // Equipped items are removed from InventoryManager's normal item list.
        // Suppress the visual event because this same slot is being moved, not destroyed.
        if (inventoryManager != null)
        {
            inventoryManager.OnItemRemoved -= HandleExternalItemRemoved;
            inventoryManager.RemoveItem(slot.Data);
            inventoryManager.OnItemRemoved += HandleExternalItemRemoved;
        }

        slot.transform.SetParent(equippedContainer, false);
        slot.SetEquipped(true);
        _equippedSlots[useSlot] = slot;

        if (updateLoadout)
        {
            weaponLoadout?.EquipWeapon(useSlot, weapon);
        }
    }

    private void MoveToInventory(ItemVisualSlot slot, TMJ_WeaponUseSlot useSlot, bool updateLoadout)
    {
        if (slot == null)
        {
            return;
        }

        // Important: allowAutoEquip:false prevents an unequipped item from immediately re-equipping itself.
        if (inventoryManager != null)
        {
            inventoryManager.OnItemAdded -= HandleExternalItemAdded;
            inventoryManager.AddItem(slot.Data, allowAutoEquip: false);
            inventoryManager.OnItemAdded += HandleExternalItemAdded;
        }

        slot.transform.SetParent(inventoryContainer, false);
        slot.SetEquipped(false);
        _equippedSlots[useSlot] = null;

        if (updateLoadout)
        {
            weaponLoadout?.EquipWeapon(useSlot, null);
        }
    }

    // ── Loadout/UI synchronization ────────────────────────────────────────
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

        MoveToEquipped(availableSlot, useSlot, desiredWeapon, updateLoadout: false);
    }

    private void EnsureSlotLooksEquipped(ItemVisualSlot slot)
    {
        slot.transform.SetParent(equippedContainer, false);
        slot.SetEquipped(true);
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

    private bool TryGetFirstEmptyUseSlot(out TMJ_WeaponUseSlot useSlot)
    {
        if (_equippedSlots[TMJ_WeaponUseSlot.LightAttack] == null)
        {
            useSlot = TMJ_WeaponUseSlot.LightAttack;
            return true;
        }

        if (_equippedSlots[TMJ_WeaponUseSlot.HeavyAttack] == null)
        {
            useSlot = TMJ_WeaponUseSlot.HeavyAttack;
            return true;
        }

        useSlot = default;
        return false;
    }

    // ── Discard / Destroy ────────────────────────────────────────────────
    private void HandleDiscard(ItemVisualSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        ItemData item = slot.Data;
        RemoveFromInventorySilently(item);
        DestroySlotDirect(slot, updateLoadout: true);
        ShowFeedback($"{item.DisplayName} descartado.");
    }

    private void HandleDestroy(ItemVisualSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        ItemData item = slot.Data;
        RemoveFromInventorySilently(item);
        DestroySlotDirect(slot, updateLoadout: true);
        ShowFeedback($"{item.DisplayName} destruido.");
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

    // ── Helpers ──────────────────────────────────────────────────────────
    private void UnsubscribeSlot(ItemVisualSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.OnEquipRequested -= HandleEquip;
        slot.OnDiscardRequested -= HandleDiscard;
        slot.OnDestroyRequested -= HandleDestroy;
    }

    private void ShowFeedback(string message)
    {
        TMJNotifications.ShowInventory(
            message,
            NotificationPriority.Normal,
            "Inventario",
            $"inventory_feedback:{message}",
            this);

    //     if (feedbackLabel == null)
    //     {
    //         return;
    //     }

    //     feedbackLabel.text = message;
    //     feedbackLabel.gameObject.SetActive(true);

    //     if (_feedbackCoroutine != null)
    //     {
    //         StopCoroutine(_feedbackCoroutine);
    //     }

    //     _feedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay());
    }

    private System.Collections.IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDuration);

        if (feedbackLabel != null)
        {
            feedbackLabel.gameObject.SetActive(false);
        }

        _feedbackCoroutine = null;
    }
}
