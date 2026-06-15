using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LootSpawner : MonoBehaviour
{
    [Header("Loot Prefab")]
    [Tooltip("Prefab base del pickup. LootSpawner lo instancia y luego llama Initialize(itemData).")]
    [SerializeField]
    private LootPickup lootPickupPrefab;

    [Header("Spawn Spread")]
    [Tooltip("Cuando hay más de un drop, los items aparecen alrededor del centro usando este radio.")]
    [SerializeField, Min(0f)]
    private float spawnRadius = 1.2f;

    [Header("Ground Projection")]
    [Tooltip("Recomendado si el nivel tiene pendientes, escaleras o terreno irregular.")]
    [SerializeField]
    private bool projectToGround = true;

    [Tooltip("Layers considerados suelo válido para colocar loot. Evitar Player, Enemy, Projectile, Hitbox y Loot.")]
    [SerializeField]
    private LayerMask groundLayers = 1;

    [Tooltip("Altura desde donde empieza el raycast hacia abajo para encontrar el suelo.")]
    [SerializeField, Min(0f)]
    private float raycastStartHeight = 2f;

    [Tooltip("Distancia hacia abajo que revisa el raycast desde su origen.")]
    [SerializeField, Min(0.1f)]
    private float raycastDistance = 5f;

    [Tooltip("Offset vertical pequeño luego de encontrar suelo para evitar que el pickup quede incrustado.")]
    [SerializeField, Min(0f)]
    private float groundOffset = 0.08f;

    [Header("Fallback")]
    [Tooltip("Se usa si Project To Ground está desactivado o si el raycast no encuentra suelo.")]
    [SerializeField]
    private float spawnHeightOffset = 0.1f;

    [Tooltip("Si está activo, avisa cuando no pudo proyectar el loot al suelo y usó fallback.")]
    [SerializeField]
    private bool warnWhenGroundNotFound = true;

    public LootPickup SpawnLoot(ItemData itemData, Vector3 position)
    {
        if (lootPickupPrefab == null)
        {
            Debug.LogError("LootPickup prefab is not assigned.", this);
            return null;
        }

        if (itemData == null)
        {
            Debug.LogWarning("Tried to spawn loot with null ItemData.", this);
            return null;
        }

        Vector3 spawnPosition = ResolveSpawnPosition(position);

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
            Debug.LogWarning("Tried to spawn a null LootDropTable.", this);
            return;
        }

        IReadOnlyList<LootDropEntry> drops = lootDropTable.GuaranteedDrops;

        if (drops == null || drops.Count == 0)
        {
            Debug.LogWarning($"{lootDropTable.name} has no guaranteed drops.", this);
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

            int amount = Mathf.Max(1, entry.Amount);

            for (int i = 0; i < amount; i++)
            {
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
        if (totalCount <= 1 || spawnRadius <= 0f)
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

    private Vector3 ResolveSpawnPosition(Vector3 desiredPosition)
    {
        if (!projectToGround)
        {
            return desiredPosition + Vector3.up * spawnHeightOffset;
        }

        if (groundLayers.value == 0)
        {
            if (warnWhenGroundNotFound)
            {
                Debug.LogWarning("LootSpawner has Project To Ground enabled, but Ground Layers is empty. Using fallback height offset.", this);
            }

            return desiredPosition + Vector3.up * spawnHeightOffset;
        }

        Vector3 rayOrigin = desiredPosition + Vector3.up * raycastStartHeight;

        bool foundGround = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            raycastDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        if (foundGround)
        {
            return hit.point + Vector3.up * groundOffset;
        }

        // Este fallback es intencional y visible: si no encuentra suelo,
        // no cancelamos el drop, pero avisamos para corregir layers/altura/raycast.
        if (warnWhenGroundNotFound)
        {
            Debug.LogWarning($"LootSpawner could not find ground below {desiredPosition}. Using fallback height offset.", this);
        }

        return desiredPosition + Vector3.up * spawnHeightOffset;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
#endif
}
