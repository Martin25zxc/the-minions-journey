using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Visual representation of a single inventory slot.
/// Used for both inventory and equipped containers — behavior
/// adapts via SetEquipped(bool).
///
///  [Root] ItemVisualSlot.cs  (Image + RectTransform)
///   ├── [Image]      BackgroundImage
///   ├── [Image]      FrameImage
///   ├── [Image]      IconImage
///   ├── [GameObject] HoverPanel
///   │    ├── [TMP] NameLabel
///   │    ├── [TMP] DescriptionLabel
///   │    └── [TMP] StatsLabel
///   └── [GameObject] ButtonsPanel
///        ├── [Button] EquipButton      ← "Equipar" / "Desequipar"
///        ├── [Button] DiscardButton    ← hidden when equipped
///        └── [Button] DestroyButton   ← hidden when equipped
/// </summary>
[DisallowMultipleComponent]
public sealed class ItemVisualSlot : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // ── Inspector wiring ─────────────────────────────────────────────────
    [Header("Slot Images")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image iconImage;

    [Header("Hover Panel")]
    [SerializeField] private GameObject hoverPanel;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI descriptionLabel;
    [SerializeField] private TextMeshProUGUI statsLabel;

    [Header("Action Buttons Panel")]
    [SerializeField] private GameObject buttonsPanel;
    [SerializeField] private Button equipButton;
    [SerializeField] private TextMeshProUGUI equipButtonLabel;
    [SerializeField] private Button discardButton;
    [SerializeField] private Button destroyButton;

    [Header("Rarity Frame Sprites")]
    [SerializeField] private Sprite frameCommon;
    [SerializeField] private Sprite frameUncommon;
    [SerializeField] private Sprite frameRare;
    [SerializeField] private Sprite frameEpic;
    [SerializeField] private Sprite frameLegendary;

    [Header("Background Sprites")]
    [Tooltip("When true, all rarities share backgroundUniversal. " +
             "When false, each rarity uses its own background sprite.")]
    [SerializeField] private bool useUniversalBackground = true;

    [Tooltip("Used for all rarities when useUniversalBackground is true.")]
    [SerializeField] private Sprite backgroundUniversal;

    [SerializeField] private Sprite backgroundCommon;
    [SerializeField] private Sprite backgroundUncommon;
    [SerializeField] private Sprite backgroundRare;
    [SerializeField] private Sprite backgroundEpic;
    [SerializeField] private Sprite backgroundLegendary;

    // ── Runtime state ────────────────────────────────────────────────────
    private ItemData _data;
    private bool _isEquipped;

    // ── Events ───────────────────────────────────────────────────────────
    public event Action<ItemVisualSlot> OnEquipRequested;
    public event Action<ItemVisualSlot> OnDiscardRequested;
    public event Action<ItemVisualSlot> OnDestroyRequested;

    public ItemData Data => _data;
    public bool IsEquipped => _isEquipped;

    // ── Initialization ───────────────────────────────────────────────────
    private void Awake()
    {
        equipButton.onClick.AddListener(() => { HideButtons(); OnEquipRequested?.Invoke(this); });
        discardButton.onClick.AddListener(() => { HideButtons(); OnDiscardRequested?.Invoke(this); });
        destroyButton.onClick.AddListener(() => { HideButtons(); OnDestroyRequested?.Invoke(this); });

        hoverPanel.SetActive(false);
        buttonsPanel.SetActive(false);
    }

    // ── Public API ───────────────────────────────────────────────────────
    public void Bind(ItemData itemData)
    {
        _data = itemData;

        iconImage.sprite = itemData.Icon;
        iconImage.enabled = itemData.Icon != null;

        frameImage.sprite = RarityToFrameSprite(itemData.Rarity);

        backgroundImage.sprite = useUniversalBackground
            ? backgroundUniversal
            : RarityToBackgroundSprite(itemData.Rarity);

        nameLabel.text = itemData.DisplayName;
        descriptionLabel.text = itemData.Description;
        statsLabel.text = BuildStatsText(itemData);

        hoverPanel.SetActive(false);
        buttonsPanel.SetActive(false);
    }

    /// <summary>
    /// Switches the slot between inventory and equipped visual state.
    /// Equipped slots only show the Unequip button.
    /// </summary>
    public void SetEquipped(bool equipped)
    {
        _isEquipped = equipped;
        equipButtonLabel.text = equipped ? "Desequipar" : "Equipar";
        discardButton.gameObject.SetActive(!equipped);
        destroyButton.gameObject.SetActive(!equipped);
    }

    // ── Pointer callbacks ────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!buttonsPanel.activeSelf)
            hoverPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverPanel.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        hoverPanel.SetActive(false);
        buttonsPanel.SetActive(!buttonsPanel.activeSelf);
    }

    // ── Private helpers ──────────────────────────────────────────────────
    private void HideButtons() => buttonsPanel.SetActive(false);

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

    private static string BuildStatsText(ItemData data)
    {
        if (data is WeaponData weapon)
        {
            string typeText = weapon.WeaponType switch
            {
                WeaponType.MainHand => "Arma ligera",
                WeaponType.OffHand => "Arma pesada",
                _ => "Desconocido"
            };
            return $"Tipo: {typeText}\n" +
                   $"Daño: {weapon.DamageBonusMin:F1} – {weapon.DamageBonusMax:F1}\n";

        }

        return $"Rareza: {data.Rarity}";
    }
}