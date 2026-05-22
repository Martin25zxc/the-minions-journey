using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]

// Componente responsable de instanciar pickups de loot en el mundo, basándose en datos. Se usa en la escena.
public sealed class LootSpawner : MonoBehaviour
{
    [Header("Loot Prefab")]
    [SerializeField] 
    private LootPickup lootPickupPrefab;

    [Header("Spawn Settings")]
    [SerializeField, Min(0f)]
    // los items aparecen alrededor del punto central
    private float spawnRadius = 1.2f;

    [SerializeField]
    private float spawnHeightOffset = 0.1f;

    public LootPickup SpawnLoot(ItemData itemData, Vector3 position)
    {
        if (lootPickupPrefab == null)
        {
            Debug.LogError("LootPickup prefab is not assigned.");
            return null;
        }

        if (itemData == null)
        {
            Debug.LogWarning("Tried to spawn loot with null ItemData.");
            return null;
        }

        Vector3 spawnPosition = position;
        spawnPosition.y += spawnHeightOffset;

        LootPickup lootPickup = Instantiate(
            lootPickupPrefab,
            spawnPosition,
            Quaternion.identity
        );

        lootPickup.Initialize(itemData);

        return lootPickup;
    }

    public void SpawnLootTable(LootDropTable lootDropTable, Vector3 centerPosition)
    {
        if (lootDropTable == null)
        {
            Debug.LogWarning("Tried to spawn a null LootDropTable.");
            return;
        }

        IReadOnlyList<LootDropEntry> drops = lootDropTable.GuaranteedDrops;

        if (drops == null || drops.Count == 0)
        {
            Debug.LogWarning($"{lootDropTable.name} has no guaranteed drops.");
            return;
        }

        int totalSpawnCount = GetTotalSpawnCount(drops);
        int currentSpawnIndex = 0;

        foreach (LootDropEntry entry in drops)
        {
            if (entry == null || entry.ItemData == null)
            {
                continue;
            }

            for (int i = 0; i < entry.Amount; i++)
            {
                // Que caigan alrededor
                Vector3 spawnPosition = GetPositionAroundCenter(
                    centerPosition,
                    currentSpawnIndex,
                    totalSpawnCount
                );

                SpawnLoot(entry.ItemData, spawnPosition);

                currentSpawnIndex++;
            }
        }
    }

    private int GetTotalSpawnCount(IReadOnlyList<LootDropEntry> drops)
    {
        int total = 0;

        foreach (LootDropEntry entry in drops)
        {
            if (entry == null || entry.ItemData == null)
            {
                continue;
            }

            total += Mathf.Max(1, entry.Amount);
        }

        return total;
    }

    private Vector3 GetPositionAroundCenter(Vector3 centerPosition, int index, int totalCount)
    {
        if (totalCount <= 1)
        {
            return centerPosition;
        }

        float angleStep = 360f / totalCount;
        float angle = angleStep * index;

        float radians = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(radians),
            0f,
            Mathf.Sin(radians)
        ) * spawnRadius;

        return centerPosition + offset;
    }
}