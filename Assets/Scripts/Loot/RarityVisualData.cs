using UnityEngine;

[System.Serializable]
public sealed class RarityVisualData
{
    [Header("Rarity")]
    [SerializeField] private ItemRarity rarity;

    [Header("Color")]
    [SerializeField] private Color auraColor = Color.white;

    [Header("Light")]
    [SerializeField, Min(0f)] private float lightIntensity = 1f;
    [SerializeField, Min(0f)] private float lightRange = 2f;

    [Header("Particles")]
    [SerializeField, Min(0f)] private float particleRateOverTime = 15f;
    [SerializeField, Min(0f)] private float particleStartSize = 0.2f;
    [SerializeField, Min(0f)] private float particleStartSpeed = 0.25f;
    [SerializeField, Min(0.01f)] private float particleStartLifetime = 0.8f;

    public ItemRarity Rarity => rarity;
    public Color AuraColor => auraColor;

    public float LightIntensity => lightIntensity;
    public float LightRange => lightRange;

    public float ParticleRateOverTime => particleRateOverTime;
    public float ParticleStartSize => particleStartSize;
    public float ParticleStartSpeed => particleStartSpeed;
    public float ParticleStartLifetime => particleStartLifetime;
}