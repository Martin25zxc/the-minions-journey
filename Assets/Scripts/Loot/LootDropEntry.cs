using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Entrada editable de una LootDropTable.
///
/// La misma entrada puede ser usada por distintos modos:
/// - Guaranteed Drops: usa ItemData + Min/Max Amount.
/// - Weighted One: usa ItemData + Min/Max Amount + Weight.
/// - Independent Chance: usa ItemData + Min/Max Amount + Chance Percent.
/// - All: usa ItemData + Min/Max Amount.
/// </summary>
[System.Serializable]
public sealed class LootDropEntry
{
    [Tooltip("Item que se va a instanciar si esta entrada resulta elegida.")]
    [SerializeField]
    private ItemData itemData;

    [Header("Amount")]
    [Tooltip("Cantidad minima que puede salir cuando esta entrada es elegida.")]
    [SerializeField, FormerlySerializedAs("amount"), Min(1)]
    private int minAmount = 1;

    [Tooltip("Cantidad maxima que puede salir cuando esta entrada es elegida. Si es menor que Min Amount, se corrige automaticamente.")]
    [SerializeField, Min(1)]
    private int maxAmount = 1;

    [Header("Weighted One")]
    [Tooltip("Peso relativo usado por grupos Weighted One. No tiene que sumar 100. Ejemplo: 70, 20, 10 significa 70/100, 20/100 y 10/100 dentro de ese grupo.")]
    [SerializeField, Min(0f)]
    private float weight = 1f;

    [Header("Independent Chance")]
    [Tooltip("Chance individual usada por grupos Independent Chance. No se usa en Weighted One ni en Guaranteed Drops.")]
    [SerializeField, Range(0f, 100f)]
    private float chancePercent = 100f;

    public ItemData ItemData => itemData;
    public int MinAmount => minAmount;
    public int MaxAmount => Mathf.Max(minAmount, maxAmount);

    /// <summary>
    /// Compatibilidad con la version anterior, donde existia un unico Amount.
    /// Devuelve MaxAmount para no romper codigo viejo que solo leia Amount.
    /// </summary>
    public int Amount => MaxAmount;

    public float Weight => weight;
    public float ChancePercent => chancePercent;

    public bool IsValid => itemData != null && MaxAmount > 0;

    /// <summary>
    /// Resuelve la cantidad final de esta entrada usando MinAmount y MaxAmount.
    /// Random.Range con int usa max exclusivo, por eso se llama con max + 1.
    /// </summary>
    public int RollAmount()
    {
        if (!IsValid)
        {
            return 0;
        }

        int resolvedMinAmount = Mathf.Max(1, minAmount);
        int resolvedMaxAmount = Mathf.Max(resolvedMinAmount, maxAmount);

        return Random.Range(resolvedMinAmount, resolvedMaxAmount + 1);
    }

    /// <summary>
    /// Normaliza valores editados desde Inspector para evitar cantidades, pesos o chances invalidas.
    /// </summary>
    public void Validate()
    {
        minAmount = Mathf.Max(1, minAmount);
        maxAmount = Mathf.Max(minAmount, maxAmount);
        weight = Mathf.Max(0f, weight);
        chancePercent = Mathf.Clamp(chancePercent, 0f, 100f);
    }
}
