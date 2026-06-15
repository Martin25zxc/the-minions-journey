using System.Collections;
using UnityEngine;

/// <summary>
/// Define cuándo una entidad debe soltar loot.
///
/// Manual  = otro sistema llama DropLoot(). Útil para cofres, eventos o pruebas controladas.
/// OnDeath = escucha TopDownHealth.OnDied. Uso normal para enemigos vivos.
/// OnStart = dropea al iniciar la escena. Uso real para NPCs/cadáveres que ya empiezan muertos.
/// </summary>
public enum LootDropTriggerMode
{
    Manual,
    OnDeath,
    OnStart
}

[DisallowMultipleComponent]
public sealed class LootDropper : MonoBehaviour
{
    [Header("Loot")]
    [Tooltip("Tabla de drops que esta entidad puede soltar.")]
    [SerializeField]
    private LootDropTable lootDropTable;

    [Tooltip("Spawner de escena responsable de instanciar los pickups. Evitamos singleton para que la referencia sea explícita.")]
    [SerializeField]
    private LootSpawner lootSpawner;

    [Header("Trigger")]
    [Tooltip("Define si el loot cae por muerte, al inicio de la escena, o manualmente.")]
    [SerializeField]
    private LootDropTriggerMode dropTriggerMode = LootDropTriggerMode.OnDeath;

    [Tooltip("Delay opcional antes de dropear. Útil para esperar un poco la animación de muerte.")]
    [SerializeField, Min(0f)]
    private float dropDelay = 0f;

    [Header("Death Source")]
    [Tooltip("Opcional. Solo se usa con OnDeath. Si queda vacío, busca TopDownHealth en este mismo GameObject.")]
    [SerializeField]
    private TopDownHealth health;

    [Header("Spawn Point")]
    [Tooltip("Opcional. Si queda vacío, el loot sale desde transform.position. Usar en cadáveres, bosses, cofres o visuales desplazados.")]
    [SerializeField]
    private Transform spawnPoint;

    private bool hasDropped;
    private Coroutine scheduledDropRoutine;

    public bool HasDropped => hasDropped;
    public LootDropTriggerMode DropTriggerMode => dropTriggerMode;

    private void Awake()
    {
        // LootDropper también debe servir para OnStart/Manual sin TopDownHealth.
        // Por eso no usamos RequireComponent(typeof(TopDownHealth)).
        if (health == null && dropTriggerMode == LootDropTriggerMode.OnDeath)
        {
            health = GetComponent<TopDownHealth>();
        }
    }

    private void OnEnable()
    {
        SubscribeToHealthDeath();
    }

    private void Start()
    {
        // Para cuerpos/NPCs ya muertos en escena.
        if (dropTriggerMode == LootDropTriggerMode.OnStart)
        {
            ScheduleDrop();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromHealthDeath();

        if (scheduledDropRoutine != null)
        {
            StopCoroutine(scheduledDropRoutine);
            scheduledDropRoutine = null;
        }
    }

    private void SubscribeToHealthDeath()
    {
        if (dropTriggerMode != LootDropTriggerMode.OnDeath)
        {
            return;
        }

        if (health == null)
        {
            Debug.LogWarning($"{name} is configured to drop loot OnDeath, but has no TopDownHealth assigned or found.", this);
            return;
        }

        health.OnDied += HandleDied;
    }

    private void UnsubscribeFromHealthDeath()
    {
        if (health == null)
        {
            return;
        }

        health.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        ScheduleDrop();
    }

    private void ScheduleDrop()
    {
        if (hasDropped)
        {
            return;
        }

        if (dropDelay <= 0f)
        {
            DropLoot();
            return;
        }

        if (scheduledDropRoutine != null)
        {
            return;
        }

        scheduledDropRoutine = StartCoroutine(DropAfterDelay());
    }

    private IEnumerator DropAfterDelay()
    {
        yield return new WaitForSeconds(dropDelay);
        scheduledDropRoutine = null;
        DropLoot();
    }

    public void DropLoot()
    {
        if (hasDropped)
        {
            return;
        }

        if (lootSpawner == null)
        {
            Debug.LogWarning($"{name} has no LootSpawner assigned.", this);
            return;
        }

        if (lootDropTable == null)
        {
            Debug.LogWarning($"{name} has no LootDropTable assigned.", this);
            return;
        }

        hasDropped = true;

        Vector3 dropPosition = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        lootSpawner.SpawnLootTable(lootDropTable, dropPosition);

        Debug.Log($"{name} dropped loot table: {lootDropTable.name}", this);
    }

    /// <summary>
    /// Permite que un futuro EnemySpawner inyecte el LootSpawner de escena
    /// cuando cree enemigos en runtime.
    /// </summary>
    public void SetLootSpawner(LootSpawner newLootSpawner)
    {
        lootSpawner = newLootSpawner;
    }

    /// <summary>
    /// Útil si en el futuro reciclamos enemigos con pooling.
    /// En instancias normales no debería llamarse.
    /// </summary>
    public void ResetDropState()
    {
        hasDropped = false;
    }
}
