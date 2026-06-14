using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

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

    [Header("Feedback (optional)")]
    [SerializeField] private TextMeshProUGUI feedbackLabel;
    [SerializeField] private float feedbackDuration = 2f;

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
        Debug.Assert(inventoryManager   != null, "[VisualInventoryManager] InventoryManager not assigned.");
        Debug.Assert(slotPrefab         != null, "[VisualInventoryManager] Slot prefab not assigned.");
        Debug.Assert(inventoryContainer != null, "[VisualInventoryManager] Inventory container not assigned.");
        Debug.Assert(equippedContainer  != null, "[VisualInventoryManager] Equipped container not assigned.");
    }

    private void OnEnable()
    {
        // OnItemAdded: only fired by external pickup (e.g. Pick system calling AddItem).
        // OnItemRemoved: only fired by external removal outside this manager.
        // Equip/Unequip/Discard/Destroy are handled directly — no event roundtrip.
        inventoryManager.OnItemAdded   += HandleExternalItemAdded;
        inventoryManager.OnItemRemoved += HandleExternalItemRemoved;
    }

    private void OnDisable()
    {
        inventoryManager.OnItemAdded   -= HandleExternalItemAdded;
        inventoryManager.OnItemRemoved -= HandleExternalItemRemoved;
    }

    private void Start()
    {
        foreach (var item in inventoryManager.Items)
            CreateSlot(item);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current[toggleKey].wasPressedThisFrame) return;

        bool show = inventoryScrollView != null && !inventoryScrollView.activeSelf;
        inventoryScrollView?.SetActive(show);
        equippedScrollView?.SetActive(show);
    }

    // ── External InventoryManager event handlers ──────────────────────────
    // These only fire when something OUTSIDE this manager touches InventoryManager.
    // Internal actions (equip/unequip/discard/destroy) bypass these entirely.
    private void HandleExternalItemAdded(ItemData item)
    {
        CreateSlot(item);
    }

    private void HandleExternalItemRemoved(ItemData item)
    {
        // Destroy the slot if something external removed the item.
        ItemVisualSlot target = null;
        foreach (var slot in _slotMap.Values)
            if (slot.Data == item) { target = slot; break; }

        if (target != null)
            DestroySlotDirect(target);
    }

    // ── Slot creation / destruction ───────────────────────────────────────
    private void CreateSlot(ItemData item)
    {
        ItemVisualSlot slot = Instantiate(slotPrefab, inventoryContainer);
        slot.Bind(item);
        slot.SetEquipped(false);

        slot.OnEquipRequested   += HandleEquip;
        slot.OnDiscardRequested += HandleDiscard;
        slot.OnDestroyRequested += HandleDestroy;

        _slotMap[slot.GetInstanceID()] = slot;
    }

    private void DestroySlotDirect(ItemVisualSlot slot)
    {
        // Remove from equipped tracking if needed.
        foreach (TMJ_WeaponUseSlot useSlot in System.Enum.GetValues(typeof(TMJ_WeaponUseSlot)))
        {
            if (_equippedSlots[useSlot] == slot)
            {
                _equippedSlots[useSlot] = null;
                weaponLoadout?.EquipWeapon(useSlot, null);
                break;
            }
        }

        UnsubscribeSlot(slot);
        _slotMap.Remove(slot.GetInstanceID());
        Destroy(slot.gameObject);
    }

    // ── Equip / Unequip ──────────────────────────────────────────────────
    private void HandleEquip(ItemVisualSlot slot)
    {
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

        TMJ_WeaponUseSlot targetUseSlot = weapon.WeaponType == WeaponType.MainHand
            ? TMJ_WeaponUseSlot.LightAttack
            : TMJ_WeaponUseSlot.HeavyAttack;

        // Swap: displace current occupant back to inventory first (net zero, no limit check).
        ItemVisualSlot displaced = _equippedSlots[targetUseSlot];
        if (displaced != null)
            MoveToInventory(displaced, targetUseSlot);

        MoveToEquipped(slot, targetUseSlot, weapon);
        ShowFeedback($"{weapon.DisplayName} equipado.");
    }

    private void Unequip(ItemVisualSlot slot)
    {
        if (inventoryManager.IsFull)
        {
            ShowFeedback("Inventario lleno, no se puede desequipar.");
            return;
        }

        foreach (TMJ_WeaponUseSlot useSlot in System.Enum.GetValues(typeof(TMJ_WeaponUseSlot)))
        {
            if (_equippedSlots[useSlot] != slot) continue;

            MoveToInventory(slot, useSlot);
            ShowFeedback($"{slot.Data.DisplayName} desequipado.");
            return;
        }
    }

    private void MoveToEquipped(ItemVisualSlot slot, TMJ_WeaponUseSlot useSlot, WeaponData weapon)
    {
        // Remove from InventoryManager without triggering visual destruction.
        inventoryManager.OnItemRemoved -= HandleExternalItemRemoved;
        inventoryManager.RemoveItem(slot.Data);
        inventoryManager.OnItemRemoved += HandleExternalItemRemoved;

        slot.transform.SetParent(equippedContainer, false);
        slot.SetEquipped(true);
        _equippedSlots[useSlot] = slot;
        weaponLoadout.EquipWeapon(useSlot, weapon);
    }

    private void MoveToInventory(ItemVisualSlot slot, TMJ_WeaponUseSlot useSlot)
    {
        // Add back to InventoryManager without triggering visual slot creation.
        inventoryManager.OnItemAdded -= HandleExternalItemAdded;
        inventoryManager.AddItem(slot.Data);
        inventoryManager.OnItemAdded += HandleExternalItemAdded;

        slot.transform.SetParent(inventoryContainer, false);
        slot.SetEquipped(false);
        _equippedSlots[useSlot] = null;
        weaponLoadout.EquipWeapon(useSlot, null);
    }

    // ── Discard / Destroy ────────────────────────────────────────────────
    private void HandleDiscard(ItemVisualSlot slot)
    {
        ItemData item = slot.Data;

        // Remove from InventoryManager without triggering HandleExternalItemRemoved.
        inventoryManager.OnItemRemoved -= HandleExternalItemRemoved;
        inventoryManager.RemoveItem(item);
        inventoryManager.OnItemRemoved += HandleExternalItemRemoved;

        // Instantiate(item.WorldModelPrefab, playerTransform.position, Quaternion.identity);
        DestroySlotDirect(slot);
        ShowFeedback($"{item.DisplayName} descartado.");
    }

    private void HandleDestroy(ItemVisualSlot slot)
    {
        ItemData item = slot.Data;

        inventoryManager.OnItemRemoved -= HandleExternalItemRemoved;
        inventoryManager.RemoveItem(item);
        inventoryManager.OnItemRemoved += HandleExternalItemRemoved;

        DestroySlotDirect(slot);
        ShowFeedback($"{item.DisplayName} destruido.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    private void UnsubscribeSlot(ItemVisualSlot slot)
    {
        slot.OnEquipRequested   -= HandleEquip;
        slot.OnDiscardRequested -= HandleDiscard;
        slot.OnDestroyRequested -= HandleDestroy;
    }

    private void ShowFeedback(string message)
    {
        if (feedbackLabel == null) return;

        feedbackLabel.text = message;
        feedbackLabel.gameObject.SetActive(true);

        if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
        _feedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay());
    }

    private System.Collections.IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDuration);
        if (feedbackLabel != null)
            feedbackLabel.gameObject.SetActive(false);
        _feedbackCoroutine = null;
    }
}