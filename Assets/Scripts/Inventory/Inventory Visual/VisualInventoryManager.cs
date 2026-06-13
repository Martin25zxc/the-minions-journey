using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Visual inventory list-view.  Subscribes to InventoryManager events
/// and keeps the UI in sync without polling.
///
/// Expected scene hierarchy:
///
///  [Root] VisualInventoryManager.cs
///   └── [ScrollRect] ScrollView
///        └── [VerticalLayoutGroup] Content   ← slotContainer points here
///
/// The slot prefab root must have ItemVisualSlot attached.
/// </summary>
[DisallowMultipleComponent]
public sealed class VisualInventoryManager : MonoBehaviour
{
    // ── Inspector wiring ─────────────────────────────────────────────────
    [Header("Dependencies")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private TMJ_WeaponLoadout weaponLoadout;

    [Header("Scroll View gameobject")]
    [SerializeField] private GameObject scrollView;

    [Header("Slot Prefab & Container")]
    [SerializeField] private ItemVisualSlot slotPrefab;
    [SerializeField] private Transform slotContainer;

    [Header("Feedback (optional)")]
    [SerializeField] private TextMeshProUGUI feedbackLabel;
    [SerializeField] private float feedbackDuration = 2f;

    // ── Runtime state ────────────────────────────────────────────────────
    private readonly Dictionary<ItemData, ItemVisualSlot> _slotMap = new();
    private Coroutine _feedbackCoroutine;

    // ── Unity lifecycle ──────────────────────────────────────────────────
    private void Awake()
    {
        Debug.Assert(inventoryManager != null, "[VisualInventoryManager] InventoryManager not assigned.");
        Debug.Assert(scrollView       != null, "[VisualInventoryManager] Scroll view not assigned.");
        Debug.Assert(slotPrefab      != null, "[VisualInventoryManager] Slot prefab not assigned.");
        Debug.Assert(slotContainer   != null, "[VisualInventoryManager] Slot container not assigned.");
    }

    private void Update()
    {
        
        // Toggle scroll view visibility with Tab key (for testing).
            if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            scrollView.SetActive(!scrollView.activeSelf);
        }
    }

    private void OnEnable()
    {
        inventoryManager.OnItemAdded   += HandleItemAdded;
        inventoryManager.OnItemRemoved += HandleItemRemoved;
    }

    private void OnDisable()
    {
        inventoryManager.OnItemAdded   -= HandleItemAdded;
        inventoryManager.OnItemRemoved -= HandleItemRemoved;
    }

    private void Start()
    {
        // Populate from whatever is already in the inventory at Start.
        foreach (var item in inventoryManager.Items)
            CreateSlot(item);
    }

    // ── InventoryManager event handlers ──────────────────────────────────
    private void HandleItemAdded(ItemData item)   => CreateSlot(item);
    private void HandleItemRemoved(ItemData item) => RemoveSlot(item);

    // ── Slot management ──────────────────────────────────────────────────
    private void CreateSlot(ItemData item)
    {
        if (_slotMap.ContainsKey(item))
        {
            Debug.LogWarning($"[VisualInventoryManager] Slot already exists for {item.ItemName}.");
            return;
        }

        ItemVisualSlot slot = Instantiate(slotPrefab, slotContainer);
        slot.Bind(item);

        slot.OnEquipRequested   += HandleEquip;
        slot.OnDiscardRequested += HandleDiscard;
        slot.OnDestroyRequested += HandleDestroy;

        _slotMap[item] = slot;
    }

    private void RemoveSlot(ItemData item)
    {
        if (!_slotMap.TryGetValue(item, out ItemVisualSlot slot)) return;

        slot.OnEquipRequested   -= HandleEquip;
        slot.OnDiscardRequested -= HandleDiscard;
        slot.OnDestroyRequested -= HandleDestroy;

        _slotMap.Remove(item);
        Destroy(slot.gameObject);
    }

    // ── Slot action handlers ─────────────────────────────────────────────

    /// <summary>
    /// Equip: assigns the weapon to the loadout (light slot by default).
    /// If it is not a WeaponData, subclass this or extend the switch below.
    /// </summary>
    private void HandleEquip(ItemVisualSlot slot)
    {
        if (slot.Data is WeaponData weapon)
        {
            if (weaponLoadout == null)
            {
                ShowFeedback("No hay un WeaponLoadout asignado.");
                return;
            }

            // Default: equip to light attack. You can present a sub-menu
            // to choose light/heavy instead by extending ItemVisualSlot with
            // two separate equip buttons or a secondary panel.
            weaponLoadout.EquipWeapon(TMJ_WeaponUseSlot.LightAttack, weapon);
            ShowFeedback($"{weapon.DisplayName} equipado en ataque ligero.");
        }
        else
        {
            ShowFeedback($"{slot.Data.DisplayName} no es equipable.");
        }
    }

    /// <summary>
    /// Discard: removes from inventory (e.g. drop to world – spawn logic omitted).
    /// </summary>
    private void HandleDiscard(ItemVisualSlot slot)
    {
        ItemData item = slot.Data;

        // Optional: spawn item.WorldModelPrefab at player position here.
        // Instantiate(item.WorldModelPrefab, playerTransform.position, Quaternion.identity);

        inventoryManager.RemoveItem(item);   // triggers OnItemRemoved → RemoveSlot
        ShowFeedback($"{item.DisplayName} descartado.");
    }

    /// <summary>
    /// Destroy: removes from inventory permanently with no world drop.
    /// </summary>
    private void HandleDestroy(ItemVisualSlot slot)
    {
        ItemData item = slot.Data;
        inventoryManager.RemoveItem(item);   // triggers OnItemRemoved → RemoveSlot
        ShowFeedback($"{item.DisplayName} destruido.");
    }

    // ── Feedback helper ──────────────────────────────────────────────────
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