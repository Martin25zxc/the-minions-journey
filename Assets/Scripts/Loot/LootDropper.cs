using UnityEngine;

[DisallowMultipleComponent]

// Componente responsable de saber qué loot droppear, dónde y cuándo. Se usa en la escena.
public sealed class LootDropper : MonoBehaviour
{
    [Header("Loot")]
    [SerializeField]
    private LootDropTable lootDropTable;

    [SerializeField]
    private LootSpawner lootSpawner;

    [Header("Spawn Point")]
    [SerializeField]
    private Transform spawnPoint;

    [Header("Debug")]
    [SerializeField]
    private bool dropOnStart;

    private bool hasDropped;

    public void DropLoot()
    {
        if (hasDropped)
        {
            return;
        }

        if (lootSpawner == null)
        {
            Debug.LogWarning($"{name} has no LootSpawner assigned.");
            return;
        }

        if (lootDropTable == null)
        {
            Debug.LogWarning($"{name} has no LootDropTable assigned.");
            return;
        }

        hasDropped = true;
        // ToDo: Probar que el transform.position funciona correctamente, dado que es el que mas se va usar
        Vector3 dropPosition = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        lootSpawner.SpawnLootTable(lootDropTable, dropPosition);

        Debug.Log($"{name} dropped loot table: {lootDropTable.name}");
    }

    private void Start()
    {
        if (dropOnStart)
        {
            DropLoot();
        }
    }
}