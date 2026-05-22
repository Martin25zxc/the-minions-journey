using UnityEngine;

[CreateAssetMenu(menuName = "Game/Loot/Rarity Visual Database"
)]
public sealed class RarityVisualDatabase : ScriptableObject
{
    [System.Serializable]
    public class RarityVisual
    {
        public ItemRarity rarity;
        public Color auraColor = Color.white;
        public Color lightColor = Color.white;
        [Min(0f)]
        public float lightIntensity = 1f;
    }

    [SerializeField]
    private RarityVisual[] visuals;

    public RarityVisual GetVisual(ItemRarity rarity)
    {
        foreach (RarityVisual visual in visuals)
        {
            if (visual.rarity == rarity)
            {
                return visual;
            }
        }

        Debug.LogWarning($"No rarity visual configured for rarity: {rarity}");
        return null;
    }
}