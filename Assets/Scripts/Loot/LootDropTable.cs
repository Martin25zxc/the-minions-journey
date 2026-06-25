using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Define como un LootDropGroup interpreta sus entradas.
/// </summary>
public enum LootDropGroupMode
{
    /// <summary>
    /// Primero pasa la chance del grupo. Si pasa, elige una o mas entradas usando Weight.
    /// Uso recomendado: "35% de chance de tirar 1 item normal de una lista".
    /// </summary>
    [InspectorName("Weighted One - elegir por peso")]
    WeightedOne,

    /// <summary>
    /// Primero pasa la chance del grupo. Si pasa, cada entrada hace su propia tirada con Chance Percent.
    /// Uso recomendado: materiales o drops que pueden acumularse entre si.
    /// </summary>
    [InspectorName("Independent Chance - cada entrada tira su chance")]
    IndependentChance,

    /// <summary>
    /// Primero pasa la chance del grupo. Si pasa, agrega todas las entradas validas.
    /// Uso recomendado: recompensas garantizadas de un bloque, cofres o bosses.
    /// </summary>
    [InspectorName("All - tirar todo el grupo")]
    All
}

/// <summary>
/// Resultado ya resuelto de una loot table.
/// Separar resultado de configuracion evita que LootSpawner conozca pesos, chances o reglas de roll.
/// </summary>
public sealed class LootDropResult
{
    public LootDropResult(ItemData itemData, int amount)
    {
        ItemData = itemData;
        Amount = Mathf.Max(1, amount);
    }

    public ItemData ItemData { get; }
    public int Amount { get; }
}

/// <summary>
/// Grupo de tiradas dentro de una LootDropTable.
///
/// Flujo:
/// 1) Se evalua Group Chance Percent.
/// 2) Si falla, este grupo no agrega nada.
/// 3) Si pasa, Mode decide como se leen las Entries.
///
/// Ejemplos:
/// - WeightedOne: grupo "Normal", 35%, elige 1 item por peso.
/// - IndependentChance: grupo "Materiales", 100%, cada material tira su propia chance.
/// - All: grupo "BossReward", 100%, agrega todo lo configurado.
/// </summary>
[System.Serializable]
public sealed class LootDropGroup
{
    [Tooltip("Identificador solo para orden/debug. Ejemplos: normal_item, materials, rare_bonus, boss_reward.")]
    [SerializeField]
    private string groupId = "loot_group";

    [Tooltip("Chance de que este grupo se ejecute. Si falla, no sale nada de este grupo. Ejemplo: 35 = este grupo solo intenta dropear el 35% de las veces.")]
    [SerializeField, Range(0f, 100f)]
    private float groupChancePercent = 100f;

    [Tooltip("Como se resuelven las Entries si el grupo pasa su chance:\n\nWeighted One: elige una o mas entradas usando Weight.\nIndependent Chance: cada entrada tira su Chance Percent.\nAll: agrega todas las entradas validas.")]
    [SerializeField]
    private LootDropGroupMode mode = LootDropGroupMode.WeightedOne;

    [Header("Rolls")]
    [Tooltip("Cantidad minima de tiradas para Weighted One. Si Min=1 y Max=1, este grupo elige un solo item.")]
    [SerializeField, Min(1)]
    private int minRolls = 1;

    [Tooltip("Cantidad maxima de tiradas para Weighted One. No afecta a Independent Chance ni All.")]
    [SerializeField, Min(1)]
    private int maxRolls = 1;

    [Tooltip("Solo afecta a Weighted One. Si esta apagado, el mismo LootDropEntry no puede salir dos veces dentro de este grupo.")]
    [SerializeField]
    private bool allowDuplicateItems = false;

    [Header("Entries")]
    [Tooltip("Items candidatos de este grupo. Segun Mode se usan Weight, Chance Percent o simplemente se agregan todos.")]
    [SerializeField]
    private List<LootDropEntry> entries = new();

    public string GroupId => groupId;
    public float GroupChancePercent => groupChancePercent;
    public LootDropGroupMode Mode => mode;
    public IReadOnlyList<LootDropEntry> Entries => entries;

    /// <summary>
    /// Ejecuta este grupo y agrega sus drops a la lista compartida de resultados.
    ///
    /// Importante:
    /// - No limpia results; solo suma resultados.
    /// - Que no agregue nada es valido: puede fallar la chance del grupo o no haber entradas validas.
    /// - LootSpawner no deberia llamar a los modos internos; solo a LootDropTable.RollDrops().
    /// </summary>
    public void RollInto(List<LootDropResult> results)
    {
        if (results == null)
        {
            return;
        }

        if (!PassesGroupChance())
        {
            return;
        }

        switch (mode)
        {
            case LootDropGroupMode.WeightedOne:
                RollWeightedOneInto(results);
                break;

            case LootDropGroupMode.IndependentChance:
                RollIndependentChanceInto(results);
                break;

            case LootDropGroupMode.All:
                RollAllInto(results);
                break;
        }
    }

    /// <summary>
    /// Normaliza valores editados en Inspector para evitar rangos invalidos.
    /// Se llama desde LootDropTable.OnValidate.
    /// </summary>
    public void Validate()
    {
        groupChancePercent = Mathf.Clamp(groupChancePercent, 0f, 100f);
        minRolls = Mathf.Max(1, minRolls);
        maxRolls = Mathf.Max(minRolls, maxRolls);

        if (entries == null)
        {
            entries = new List<LootDropEntry>();
            return;
        }

        foreach (LootDropEntry entry in entries)
        {
            entry?.Validate();
        }
    }

    /// <summary>
    /// Primera puerta del grupo. Si falla, no se evalua ninguna Entry.
    /// </summary>
    private bool PassesGroupChance()
    {
        if (groupChancePercent <= 0f)
        {
            return false;
        }

        if (groupChancePercent >= 100f)
        {
            return true;
        }

        return Random.Range(0f, 100f) <= groupChancePercent;
    }

    /// <summary>
    /// Elige una o mas entradas segun Weight.
    /// Weight es relativo: 70, 20 y 10 equivalen a 70%, 20%, 10% dentro de este grupo,
    /// pero solo despues de que el grupo paso Group Chance Percent.
    /// </summary>
    private void RollWeightedOneInto(List<LootDropResult> results)
    {
        List<LootDropEntry> availableEntries = GetValidWeightedEntries();

        if (availableEntries.Count == 0)
        {
            return;
        }

        int rollCount = Random.Range(minRolls, maxRolls + 1);

        for (int i = 0; i < rollCount; i++)
        {
            if (availableEntries.Count == 0)
            {
                return;
            }

            LootDropEntry selectedEntry = PickWeightedEntry(availableEntries);

            if (selectedEntry == null)
            {
                return;
            }

            AddResult(results, selectedEntry);

            if (!allowDuplicateItems)
            {
                availableEntries.Remove(selectedEntry);
            }
        }
    }

    /// <summary>
    /// Recorre todas las entradas validas y cada una tira su propia Chance Percent.
    /// Puede agregar cero, una o varias entradas en una misma muerte.
    /// </summary>
    private void RollIndependentChanceInto(List<LootDropResult> results)
    {
        if (entries == null)
        {
            return;
        }

        foreach (LootDropEntry entry in entries)
        {
            if (entry == null || !entry.IsValid)
            {
                continue;
            }

            if (entry.ChancePercent <= 0f)
            {
                continue;
            }

            if (entry.ChancePercent >= 100f || Random.Range(0f, 100f) <= entry.ChancePercent)
            {
                AddResult(results, entry);
            }
        }
    }

    /// <summary>
    /// Agrega todas las entradas validas del grupo.
    /// La unica chance que importa es Group Chance Percent.
    /// </summary>
    private void RollAllInto(List<LootDropResult> results)
    {
        if (entries == null)
        {
            return;
        }

        foreach (LootDropEntry entry in entries)
        {
            if (entry == null || !entry.IsValid)
            {
                continue;
            }

            AddResult(results, entry);
        }
    }

    /// <summary>
    /// Devuelve solo entradas validas para Weighted One.
    /// En este modo, una entrada con Weight 0 nunca puede ser elegida.
    /// </summary>
    private List<LootDropEntry> GetValidWeightedEntries()
    {
        List<LootDropEntry> validEntries = new();

        if (entries == null)
        {
            return validEntries;
        }

        foreach (LootDropEntry entry in entries)
        {
            if (entry == null || !entry.IsValid || entry.Weight <= 0f)
            {
                continue;
            }

            validEntries.Add(entry);
        }

        return validEntries;
    }

    /// <summary>
    /// Seleccion ponderada clasica.
    /// Suma todos los Weight, tira un valor entre 0 y total, y devuelve la entrada cuyo rango contiene ese valor.
    /// </summary>
    private LootDropEntry PickWeightedEntry(List<LootDropEntry> validEntries)
    {
        float totalWeight = 0f;

        foreach (LootDropEntry entry in validEntries)
        {
            totalWeight += entry.Weight;
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        foreach (LootDropEntry entry in validEntries)
        {
            cumulativeWeight += entry.Weight;

            if (roll <= cumulativeWeight)
            {
                return entry;
            }
        }

        return validEntries[validEntries.Count - 1];
    }

    /// <summary>
    /// Convierte una Entry configurada en Inspector en un LootDropResult concreto.
    /// Aca se resuelve tambien la cantidad final con Entry.RollAmount().
    /// </summary>
    private void AddResult(List<LootDropResult> results, LootDropEntry entry)
    {
        int amount = entry.RollAmount();

        if (amount <= 0)
        {
            return;
        }

        results.Add(new LootDropResult(entry.ItemData, amount));
    }
}

[CreateAssetMenu(menuName = "Game/Loot/Loot Drop Table")]
public sealed class LootDropTable : ScriptableObject
{
    [Header("Guaranteed Drops")]
    [Tooltip("Drops que se agregan siempre que se resuelve esta tabla. Usar para recompensas obligatorias, boss cores, quest rewards fisicas o cofres fijos.")]
    [SerializeField]
    private List<LootDropEntry> guaranteedDrops = new();

    [Header("Roll Groups")]
    [Tooltip("Grupos porcentuales. Cada grupo puede fallar sin dropear nada, o agregar drops segun su Mode.")]
    [SerializeField]
    private List<LootDropGroup> rollGroups = new();

    public IReadOnlyList<LootDropEntry> GuaranteedDrops => guaranteedDrops;
    public IReadOnlyList<LootDropGroup> RollGroups => rollGroups;

    /// <summary>
    /// Resuelve toda la tabla y devuelve una lista de resultados concretos.
    ///
    /// Flujo:
    /// 1) Agrega Guaranteed Drops.
    /// 2) Ejecuta cada Roll Group.
    /// 3) Devuelve la lista final, que puede estar vacia.
    ///
    /// Una lista vacia es valida: significa que esta vez no cayo nada.
    /// </summary>
    public List<LootDropResult> RollDrops()
    {
        List<LootDropResult> results = new();

        AddGuaranteedDrops(results);
        AddRollGroupDrops(results);

        return results;
    }

    /// <summary>
    /// Agrega todos los drops garantizados validos.
    /// No usa Weight ni Chance Percent; solo respeta Min/Max Amount.
    /// </summary>
    private void AddGuaranteedDrops(List<LootDropResult> results)
    {
        if (guaranteedDrops == null)
        {
            return;
        }

        foreach (LootDropEntry entry in guaranteedDrops)
        {
            if (entry == null || !entry.IsValid)
            {
                continue;
            }

            int amount = entry.RollAmount();

            if (amount <= 0)
            {
                continue;
            }

            results.Add(new LootDropResult(entry.ItemData, amount));
        }
    }

    /// <summary>
    /// Ejecuta todos los grupos de roll configurados.
    /// Cada grupo decide internamente si agrega cero, uno o varios resultados.
    /// </summary>
    private void AddRollGroupDrops(List<LootDropResult> results)
    {
        if (rollGroups == null)
        {
            return;
        }

        foreach (LootDropGroup group in rollGroups)
        {
            group?.RollInto(results);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (guaranteedDrops != null)
        {
            foreach (LootDropEntry entry in guaranteedDrops)
            {
                entry?.Validate();
            }
        }

        if (rollGroups != null)
        {
            foreach (LootDropGroup group in rollGroups)
            {
                group?.Validate();
            }
        }
    }
#endif
}
