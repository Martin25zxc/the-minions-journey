using UnityEngine;

/// <summary>
/// Solución experimental para definir cómo se lee visualmente una familia de pickeables.
///
/// No define lógica de misión, inventario ni recompensa. Solo presentación.
/// Naming recomendado para assets:
/// PickupVisual_<Importance>_<Palette>
/// Ejemplos:
/// - PickupVisual_Minor_Nature
/// - PickupVisual_Standard_Warm
/// - PickupVisual_Major_CrimsonGold
///
/// No usar abreviaturas tipo PVP porque en videojuegos suele confundirse con Player vs Player.
/// </summary>
[CreateAssetMenu(menuName = "TMJ/Pickups/Pickup Visual Profile")]
public sealed class PickupVisualProfile : ScriptableObject
{
    [Header("Identity / Communication")]
    [SerializeField] private PickupVisualImportance importance = PickupVisualImportance.Standard;
    [SerializeField] private PickupVisualPalette palette = PickupVisualPalette.Warm;

    [TextArea(2, 4)]
    [SerializeField]
    private string designerNote;

    [Header("Model")]
    [Tooltip("Multiplicador opcional aplicado al VisualRoot. No reemplaza la escala correcta del prefab visual.")]
    [SerializeField, Min(0.01f)] private float visualScaleMultiplier = 1f;

    [Header("Light")]
    [SerializeField] private Color lightColor = Color.white;
    [SerializeField, Min(0f)] private float lightIntensity = 1f;
    [SerializeField, Min(0f)] private float lightRange = 2f;

    [Header("Particles")]
    [SerializeField] private Color particleColor = Color.white;
    [SerializeField, Min(0f)] private float particleRateOverTime = 10f;
    [SerializeField, Min(0f)] private float particleStartSize = 0.18f;
    [SerializeField, Min(0f)] private float particleStartSpeed = 0.2f;
    [SerializeField, Min(0.01f)] private float particleStartLifetime = 0.8f;

    [Header("Idle Motion")]
    [Tooltip("Movimiento vertical opcional del VisualRoot. Puede quedar en 0 si el prefab ya anima por su cuenta.")]
    [SerializeField, Min(0f)] private float bobAmplitude = 0f;

    [SerializeField, Min(0f)] private float bobFrequency = 1f;

    [Tooltip("Rotación opcional en grados por segundo sobre Y. Puede quedar en 0.")]
    [SerializeField] private float rotationSpeedY = 0f;

    public PickupVisualImportance Importance => importance;
    public PickupVisualPalette Palette => palette;
    public string DesignerNote => designerNote;
    public float VisualScaleMultiplier => visualScaleMultiplier;

    public Color LightColor => lightColor;
    public float LightIntensity => lightIntensity;
    public float LightRange => lightRange;

    public Color ParticleColor => particleColor;
    public float ParticleRateOverTime => particleRateOverTime;
    public float ParticleStartSize => particleStartSize;
    public float ParticleStartSpeed => particleStartSpeed;
    public float ParticleStartLifetime => particleStartLifetime;

    public float BobAmplitude => bobAmplitude;
    public float BobFrequency => bobFrequency;
    public float RotationSpeedY => rotationSpeedY;

#if UNITY_EDITOR
    private void OnValidate()
    {
        visualScaleMultiplier = Mathf.Max(0.01f, visualScaleMultiplier);
        lightIntensity = Mathf.Max(0f, lightIntensity);
        lightRange = Mathf.Max(0f, lightRange);
        particleRateOverTime = Mathf.Max(0f, particleRateOverTime);
        particleStartSize = Mathf.Max(0f, particleStartSize);
        particleStartSpeed = Mathf.Max(0f, particleStartSpeed);
        particleStartLifetime = Mathf.Max(0.01f, particleStartLifetime);
        bobAmplitude = Mathf.Max(0f, bobAmplitude);
        bobFrequency = Mathf.Max(0f, bobFrequency);
    }
#endif
}
