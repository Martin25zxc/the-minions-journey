using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a single inventory/equipped item slot.
///
/// This slot is intentionally UI-only:
/// - Shows icon, rarity frame/background and optional rarity text.
/// - Shows contextual action buttons.
/// - Emits events to VisualInventoryManager.
/// - Does not build item descriptions; those belong to InventoryItemDetailsPanel.
///
/// Recommended prefab hierarchy:
/// ItemVisualSlot
/// ├── BackgroundImage
/// ├── FrameImage
/// ├── IconImage
/// ├── RarityLabel                 optional
/// └── ButtonsPanel
///     ├── LightAttackButton        shown for inventory WeaponData
///     ├── HeavyAttackButton        shown for inventory WeaponData
///     └── DiscardButton            shown for inventory items, with inline confirmation
///
/// Equipped slots show only the LightAttackButton field, relabelled as "Desequipar".
/// </summary>
[DisallowMultipleComponent]
public sealed class ItemVisualSlot : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Slot Images")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image iconImage;

    [Header("Rarity Label (optional)")]
    [SerializeField] private TextMeshProUGUI rarityLabel;
    [SerializeField] private bool showRarityLabel = true;

    [Header("Action Buttons Panel")]
    [SerializeField] private GameObject buttonsPanel;

    [Tooltip("Inventory weapon: equips to LightAttack. Equipped item: unequips.")]
    [SerializeField] private Button lightAttackButton;
    [SerializeField] private TextMeshProUGUI lightAttackButtonLabel;

    [Tooltip("Inventory weapon: equips to HeavyAttack. Hidden for equipped and non-weapon items.")]
    [SerializeField] private Button heavyAttackButton;
    [SerializeField] private TextMeshProUGUI heavyAttackButtonLabel;

    [Tooltip("Removes the item from inventory. Hidden for equipped items.")]
    [SerializeField] private Button discardButton;
    [SerializeField] private TextMeshProUGUI discardButtonLabel;

    [Header("Button Text")]
    [SerializeField] private string equipLightText = "Slot.L";
    [SerializeField] private string equipHeavyText = "Slot.P";
    [SerializeField] private string unequipText = "Desequipar";
    [SerializeField] private string discardText = "Descartar";
    [SerializeField] private string discardConfirmText = "¿Seguro?";

    [Header("Discard Confirmation")]
    [SerializeField] private bool requireDiscardConfirmation = true;
    [SerializeField, Min(0.1f)] private float discardConfirmationSeconds = 1.5f;

    [Header("Rarity Frame Sprites")]
    [SerializeField] private Sprite frameCommon;
    [SerializeField] private Sprite frameUncommon;
    [SerializeField] private Sprite frameRare;
    [SerializeField] private Sprite frameEpic;
    [SerializeField] private Sprite frameLegendary;

    [Header("Background Sprites")]
    [Tooltip("When true, all rarities share backgroundUniversal. When false, each rarity uses its own background sprite.")]
    [SerializeField] private bool useUniversalBackground = true;

    [Tooltip("Used for all rarities when useUniversalBackground is true.")]
    [SerializeField] private Sprite backgroundUniversal;

    [SerializeField] private Sprite backgroundCommon;
    [SerializeField] private Sprite backgroundUncommon;
    [SerializeField] private Sprite backgroundRare;
    [SerializeField] private Sprite backgroundEpic;
    [SerializeField] private Sprite backgroundLegendary;

    private ItemData _data;
    private bool _isEquipped;
    private bool _discardConfirmationArmed;
    private Coroutine _discardConfirmationCoroutine;

    public event Action<ItemVisualSlot, TMJ_WeaponUseSlot> OnEquipToUseSlotRequested;
    public event Action<ItemVisualSlot> OnUnequipRequested;
    public event Action<ItemVisualSlot> OnDiscardRequested;
    public event Action<ItemVisualSlot> OnHoverEnter;
    public event Action<ItemVisualSlot> OnHoverExit;

    public ItemData Data => _data;
    public bool IsEquipped => _isEquipped;

    private void Awake()
    {
        if (lightAttackButton != null)
        {
            lightAttackButton.onClick.AddListener(HandlePrimaryButtonClicked);
        }

        if (heavyAttackButton != null)
        {
            heavyAttackButton.onClick.AddListener(HandleHeavyAttackButtonClicked);
        }

        if (discardButton != null)
        {
            discardButton.onClick.AddListener(HandleDiscardButtonClicked);
        }

        HideButtons();
    }

    private void OnDisable()
    {
        ResetDiscardConfirmation();
    }

    private void OnDestroy()
    {
        if (lightAttackButton != null)
        {
            lightAttackButton.onClick.RemoveListener(HandlePrimaryButtonClicked);
        }

        if (heavyAttackButton != null)
        {
            heavyAttackButton.onClick.RemoveListener(HandleHeavyAttackButtonClicked);
        }

        if (discardButton != null)
        {
            discardButton.onClick.RemoveListener(HandleDiscardButtonClicked);
        }
    }

    public void Bind(ItemData itemData)
    {
        _data = itemData;

        if (_data == null)
        {
            ClearVisuals();
            HideButtons();
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = _data.Icon;
            iconImage.enabled = _data.Icon != null;
        }

        if (frameImage != null)
        {
            frameImage.sprite = RarityToFrameSprite(_data.Rarity);
        }

        if (backgroundImage != null)
        {
            backgroundImage.sprite = useUniversalBackground
                ? backgroundUniversal
                : RarityToBackgroundSprite(_data.Rarity);
        }

        RefreshRarityLabel();
        ResetDiscardConfirmation();
        HideButtons();
    }

    /// <summary>
    /// Switches the slot between inventory and equipped visual state.
    /// Inventory weapon slots show Light/Heavy/Discard.
    /// Equipped slots show only Unequip.
    /// </summary>
    public void SetEquipped(bool equipped)
    {
        _isEquipped = equipped;
        ResetDiscardConfirmation();
        RefreshButtonState();
    }

    public void CloseActions()
    {
        HideButtons();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEnter?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (buttonsPanel == null)
        {
            return;
        }

        bool shouldShow = !buttonsPanel.activeSelf;
        buttonsPanel.SetActive(shouldShow);

        if (shouldShow)
        {
            RefreshButtonState();
        }
        else
        {
            ResetDiscardConfirmation();
        }
    }

    private void HandlePrimaryButtonClicked()
    {
        if (_data == null)
        {
            HideButtons();
            return;
        }

        if (_isEquipped)
        {
            HideButtons();
            OnUnequipRequested?.Invoke(this);
            return;
        }

        if (_data is WeaponData)
        {
            HideButtons();
            OnEquipToUseSlotRequested?.Invoke(this, TMJ_WeaponUseSlot.LightAttack);
        }
    }

    private void HandleHeavyAttackButtonClicked()
    {
        if (_data is not WeaponData || _isEquipped)
        {
            HideButtons();
            return;
        }

        HideButtons();
        OnEquipToUseSlotRequested?.Invoke(this, TMJ_WeaponUseSlot.HeavyAttack);
    }

    private void HandleDiscardButtonClicked()
    {
        if (_data == null || _isEquipped)
        {
            HideButtons();
            return;
        }

        if (!requireDiscardConfirmation)
        {
            ConfirmDiscard();
            return;
        }

        if (!_discardConfirmationArmed)
        {
            ArmDiscardConfirmation();
            return;
        }

        ConfirmDiscard();
    }

    private void ConfirmDiscard()
    {
        ResetDiscardConfirmation();
        HideButtons();
        OnDiscardRequested?.Invoke(this);
    }

    private void ArmDiscardConfirmation()
    {
        _discardConfirmationArmed = true;

        if (discardButtonLabel != null)
        {
            discardButtonLabel.text = discardConfirmText;
        }

        if (_discardConfirmationCoroutine != null)
        {
            StopCoroutine(_discardConfirmationCoroutine);
        }

        _discardConfirmationCoroutine = StartCoroutine(ResetDiscardConfirmationAfterDelay());
    }

    private IEnumerator ResetDiscardConfirmationAfterDelay()
    {
        yield return new WaitForSecondsRealtime(discardConfirmationSeconds);
        ResetDiscardConfirmation();
    }

    private void ResetDiscardConfirmation()
    {
        _discardConfirmationArmed = false;

        if (_discardConfirmationCoroutine != null)
        {
            StopCoroutine(_discardConfirmationCoroutine);
            _discardConfirmationCoroutine = null;
        }

        if (discardButtonLabel != null)
        {
            discardButtonLabel.text = discardText;
        }
    }

    private void RefreshButtonState()
    {
        bool isWeapon = _data is WeaponData;

        if (_isEquipped)
        {
            SetButtonVisible(lightAttackButton, true);
            SetButtonVisible(heavyAttackButton, false);
            SetButtonVisible(discardButton, false);
            SetLabel(lightAttackButtonLabel, unequipText);
            return;
        }

        SetButtonVisible(lightAttackButton, isWeapon);
        SetButtonVisible(heavyAttackButton, isWeapon);
        SetButtonVisible(discardButton, true);

        SetLabel(lightAttackButtonLabel, equipLightText);
        SetLabel(heavyAttackButtonLabel, equipHeavyText);
        SetLabel(discardButtonLabel, discardText);
    }

    private void HideButtons()
    {
        ResetDiscardConfirmation();
        buttonsPanel?.SetActive(false);
    }

    private void RefreshRarityLabel()
    {
        if (rarityLabel == null)
        {
            return;
        }

        bool show = showRarityLabel && _data != null;
        rarityLabel.gameObject.SetActive(show);
        rarityLabel.text = show ? FormatRarity(_data.Rarity) : string.Empty;
    }

    private void ClearVisuals()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (frameImage != null)
        {
            frameImage.sprite = null;
        }

        if (backgroundImage != null)
        {
            backgroundImage.sprite = null;
        }

        if (rarityLabel != null)
        {
            rarityLabel.text = string.Empty;
            rarityLabel.gameObject.SetActive(false);
        }
    }

    private Sprite RarityToFrameSprite(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => frameCommon,
        ItemRarity.Uncommon => frameUncommon,
        ItemRarity.Rare => frameRare,
        ItemRarity.Epic => frameEpic,
        ItemRarity.Legendary => frameLegendary,
        _ => frameCommon
    };

    private Sprite RarityToBackgroundSprite(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => backgroundCommon,
        ItemRarity.Uncommon => backgroundUncommon,
        ItemRarity.Rare => backgroundRare,
        ItemRarity.Epic => backgroundEpic,
        ItemRarity.Legendary => backgroundLegendary,
        _ => backgroundCommon
    };

    private static string FormatRarity(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => "Común",
        ItemRarity.Uncommon => "Poco común",
        ItemRarity.Rare => "Rara",
        ItemRarity.Epic => "Épica",
        ItemRarity.Legendary => "Legendaria",
        _ => rarity.ToString()
    };

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
        {
            button.gameObject.SetActive(visible);
        }
    }

    private static void SetLabel(TextMeshProUGUI label, string text)
    {
        if (label != null)
        {
            label.text = text;
        }
    }
}
