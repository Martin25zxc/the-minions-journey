using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Visual representation of a single inventory slot.
/// Assign this to the slot prefab root. The prefab structure expected:
///
///  [Root] ItemVisualSlot.cs
///   ├── [Image]  BackgroundImage     ← slot background sprite (universal or per-rarity)
///   ├── [Image]  FrameImage          ← border sprite, one per rarity
///   ├── [Image]  IconImage           ← item icon from ItemData
///   ├── [GameObject] HoverPanel      ← shown on pointer enter/exit
///   │    ├── [TMP]  NameLabel
///   │    ├── [TMP]  DescriptionLabel
///   │    └── [TMP]  StatsLabel
///   └── [GameObject] ButtonsPanel    ← shown on click
///        ├── [Button] EquipButton
///        ├── [Button] DiscardButton
///        └── [Button] DestroyButton
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

    // Shown in Inspector only when useUniversalBackground is false.
    [SerializeField] private Sprite backgroundCommon;
    [SerializeField] private Sprite backgroundUncommon;
    [SerializeField] private Sprite backgroundRare;
    [SerializeField] private Sprite backgroundEpic;
    [SerializeField] private Sprite backgroundLegendary;

    // ── Runtime state ────────────────────────────────────────────────────
    private ItemData _data;

    // ── Events consumed by VisualInventoryManager ────────────────────────
    public event Action<ItemVisualSlot> OnEquipRequested;
    public event Action<ItemVisualSlot> OnDiscardRequested;
    public event Action<ItemVisualSlot> OnDestroyRequested;

    public ItemData Data => _data;

    // ── Initialization ───────────────────────────────────────────────────
    private void Awake()
    {
        equipButton.onClick.AddListener(  () => { HideButtons(); OnEquipRequested?.Invoke(this);  });
        discardButton.onClick.AddListener(() => { HideButtons(); OnDiscardRequested?.Invoke(this); });
        destroyButton.onClick.AddListener(() => { HideButtons(); OnDestroyRequested?.Invoke(this); });

        hoverPanel.SetActive(false);
        buttonsPanel.SetActive(false);
    }

    // ── Public API ───────────────────────────────────────────────────────
    public void Bind(ItemData itemData)
    {
        _data = itemData;

        // Icon
        iconImage.sprite  = itemData.Icon;
        iconImage.enabled = itemData.Icon != null;

        // Frame sprite by rarity
        frameImage.sprite = RarityToFrameSprite(itemData.Rarity);

        // Background sprite — universal or per-rarity
        backgroundImage.sprite = useUniversalBackground
            ? backgroundUniversal
            : RarityToBackgroundSprite(itemData.Rarity);

        // Hover panel content
        nameLabel.text        = itemData.DisplayName;
        descriptionLabel.text = itemData.Description;
        statsLabel.text       = BuildStatsText(itemData);

        hoverPanel.SetActive(false);
        buttonsPanel.SetActive(false);
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
        ItemRarity.Common    => frameCommon,
        ItemRarity.Uncommon  => frameUncommon,
        ItemRarity.Rare      => frameRare,
        ItemRarity.Epic      => frameEpic,
        ItemRarity.Legendary => frameLegendary,
        _                    => frameCommon
    };

    private Sprite RarityToBackgroundSprite(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common    => backgroundCommon,
        ItemRarity.Uncommon  => backgroundUncommon,
        ItemRarity.Rare      => backgroundRare,
        ItemRarity.Epic      => backgroundEpic,
        ItemRarity.Legendary => backgroundLegendary,
        _                    => backgroundCommon
    };

    /// <summary>
    /// Builds the stats string shown on hover.
    /// Extends naturally: add more ItemData subtypes below.
    /// </summary>
    private static string BuildStatsText(ItemData data)
    {
        if (data is WeaponData weapon)
        {
            // Expose whatever public properties WeaponData offers.
            // Adjust property names to match your actual WeaponData fields.
            return $"Rarity: {weapon.Rarity}";
            // Example extended version once WeaponData exposes more:
            // return $"DMG: {weapon.BaseDamage}\n" +
            //        $"SPD: {weapon.AttackSpeed}\n" +
            //        $"Rarity: {weapon.Rarity}";
        }

        return $"Rarity: {data.Rarity}";
    }
}