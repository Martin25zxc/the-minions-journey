using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared details panel for the inventory UI.
///
/// This replaces the old per-slot description tooltip for readable item data.
/// The slot still owns its action buttons; this panel only displays information.
///
/// Suggested hierarchy:
/// InventoryItemDetailsPanel (this script)
///  ├── IconImage
///  ├── NameLabel
///  ├── RarityLabel
///  ├── StatsLabel
///  └── DescriptionLabel
/// </summary>
[DisallowMultipleComponent]
public sealed class InventoryItemDetailsPanel : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("Optional. If empty, this GameObject is used as the panel root.")]
    [SerializeField] private GameObject root;

    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI rarityLabel;
    [SerializeField] private TextMeshProUGUI statsLabel;
    [SerializeField] private TextMeshProUGUI descriptionLabel;

    [Header("Empty State")]
    [SerializeField] private string emptyTitle = "Inventario";
    [SerializeField] private string emptyDescription = "Pasá el cursor sobre un item para ver sus detalles.";

    private void Awake()
    {
        if (root == null)
        {
            root = gameObject;
        }

        Clear();
    }

    public void SetVisible(bool visible)
    {
        if (root != null)
        {
            root.SetActive(visible);
        }
    }

    public void Show(ItemData item)
    {
        if (item == null)
        {
            Clear();
            return;
        }

        SetVisible(true);

        if (iconImage != null)
        {
            iconImage.sprite = item.Icon;
            iconImage.enabled = item.Icon != null;
        }

        if (nameLabel != null)
        {
            nameLabel.text = GetDisplayName(item);
        }

        if (rarityLabel != null)
        {
            rarityLabel.text = FormatRarity(item.Rarity);
        }

        if (statsLabel != null)
        {
            statsLabel.text = BuildStatsText(item);
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = string.IsNullOrWhiteSpace(item.Description)
                ? "Sin descripción."
                : item.Description;
        }
    }

    public void Clear()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (nameLabel != null)
        {
            nameLabel.text = emptyTitle;
        }

        if (rarityLabel != null)
        {
            rarityLabel.text = string.Empty;
        }

        if (statsLabel != null)
        {
            statsLabel.text = string.Empty;
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = emptyDescription;
        }
    }

    private static string GetDisplayName(ItemData item)
    {
        if (!string.IsNullOrWhiteSpace(item.DisplayName))
        {
            return item.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(item.ItemName))
        {
            return item.ItemName;
        }

        return "Item sin nombre";
    }

    private static string BuildStatsText(ItemData item)
    {
        if (item is WeaponData weapon)
        {
            return $"Daño adicional: {weapon.DamageBonusMin:0.#} – {weapon.DamageBonusMax:0.#}";
        }

        return string.Empty;
    }

    private static string FormatRarity(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => "Común",
            ItemRarity.Uncommon => "Poco común",
            ItemRarity.Rare => "Rara",
            ItemRarity.Epic => "Épica",
            ItemRarity.Legendary => "Legendaria",
            _ => rarity.ToString()
        };
    }
}
