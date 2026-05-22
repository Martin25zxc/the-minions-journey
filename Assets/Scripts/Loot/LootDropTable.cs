using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Loot/Loot Drop Table")]
public sealed class LootDropTable : ScriptableObject
{
    [Header("Guaranteed Drops")]
    [SerializeField] private List<LootDropEntry> guaranteedDrops = new();

    public IReadOnlyList<LootDropEntry> GuaranteedDrops => guaranteedDrops;
}