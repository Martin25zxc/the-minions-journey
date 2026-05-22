using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Loot/Rarity Visual Database")]
public sealed class RarityVisualDatabase : ScriptableObject
{
    [Header("Visuals By Rarity")]
    [SerializeField] private List<RarityVisualData> rarityVisuals = new();

    public RarityVisualData GetVisualData(ItemRarity rarity)
    {
        foreach (RarityVisualData visualData in rarityVisuals)
        {
            if (visualData != null && visualData.Rarity == rarity)
            {
                return visualData;
            }
        }

        Debug.LogWarning($"No visual data found for rarity: {rarity}");
        return null;
    }
}