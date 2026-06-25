using System.Collections;
using UnityEngine;

/// <summary>
/// Define cuando una entidad debe soltar loot.
///
/// Manual  = otro sistema llama DropLoot(). Util para cofres, eventos o pruebas controladas.
/// OnDeath = escucha TopDownHealth.OnDied. Uso normal para enemigos vivos.
/// OnStart = dropea al iniciar la escena. Uso real para NPCs/cadaveres que ya empiezan muertos.
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
    [Tooltip("Tabla override. Si queda vacia, puede usar la tabla del EnemyDefinition.")]
    [SerializeField]
    private LootDropTable lootDropTable;

    [Tooltip("Si esta activo y no hay override local, intenta usar EnemyActor.Definition.LootDropTable.")]
    [SerializeField]
    private bool useEnemyDefinitionLootTable = true;

    [Tooltip("Spawner de escena responsable de instanciar los pickups. Evitamos singleton para que la referencia sea explicita.")]
    [SerializeField]
    private LootSpawner lootSpawner;

    [Tooltip("Fallback para pruebas o enemigos ya puestos en escena. Para spawners dinamicos, preferir SetLootSpawner().")]
    [SerializeField]
    private bool autoFindLootSpawner = true;

    [Header("Trigger")]
    [Tooltip("Define si el loot cae por muerte, al inicio de la escena, o manualmente.")]
    [SerializeField]
    private LootDropTriggerMode dropTriggerMode = LootDropTriggerMode.OnDeath;

    [Tooltip("Delay opcional antes de dropear. Util para esperar un poco la animacion de muerte.")]
    [SerializeField, Min(0f)]
    private float dropDelay = 0f;

    [Header("Death Source")]
    [Tooltip("Opcional. Solo se usa con OnDeath. Si queda vacio, busca TopDownHealth en este mismo GameObject.")]
    [SerializeField]
    private TopDownHealth health;

    [Tooltip("Opcional. Se usa para leer EnemyDefinition cuando la tabla viene del tipo de enemigo.")]
    [SerializeField]
    private EnemyActor enemyActor;

    [Header("Spawn Point")]
    [Tooltip("Opcional. Si queda vacio, el loot sale desde transform.position. Usar en cadaveres, bosses, cofres o visuales desplazados.")]
    [SerializeField]
    private Transform spawnPoint;

    private bool hasDropped;
    private Coroutine scheduledDropRoutine;

    public bool HasDropped => hasDropped;
    public LootDropTriggerMode DropTriggerMode => dropTriggerMode;

    private void Awake()
    {
        // LootDropper tambien debe servir para OnStart/Manual sin TopDownHealth.
        // Por eso no usamos RequireComponent(typeof(TopDownHealth)).
        if (enemyActor == null)
        {
            enemyActor = GetComponent<EnemyActor>();
        }

        if (health == null && dropTriggerMode == LootDropTriggerMode.OnDeath)
        {
            health = enemyActor != null && enemyActor.Health != null
                ? enemyActor.Health
                : GetComponent<TopDownHealth>();
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

        LootSpawner resolvedSpawner = ResolveLootSpawner();

        if (resolvedSpawner == null)
        {
            Debug.LogWarning($"{name} has no LootSpawner assigned.", this);
            return;
        }

        LootDropTable resolvedDropTable = ResolveLootDropTable();

        if (resolvedDropTable == null)
        {
            Debug.LogWarning($"{name} has no LootDropTable assigned or available from EnemyDefinition.", this);
            return;
        }

        hasDropped = true;

        Vector3 dropPosition = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        resolvedSpawner.SpawnLootTable(resolvedDropTable, dropPosition);

        Debug.Log($"{name} rolled loot table: {resolvedDropTable.name}", this);
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
    /// Util si en el futuro reciclamos enemigos con pooling.
    /// En instancias normales no deberia llamarse.
    /// </summary>
    public void ResetDropState()
    {
        hasDropped = false;
    }

    private LootDropTable ResolveLootDropTable()
    {
        if (lootDropTable != null)
        {
            return lootDropTable;
        }

        if (!useEnemyDefinitionLootTable)
        {
            return null;
        }

        if (enemyActor == null)
        {
            enemyActor = GetComponent<EnemyActor>();
        }

        if (enemyActor == null || enemyActor.Definition == null)
        {
            return null;
        }

        return enemyActor.Definition.LootDropTable;
    }

    private LootSpawner ResolveLootSpawner()
    {
        if (lootSpawner != null)
        {
            return lootSpawner;
        }

        if (!autoFindLootSpawner)
        {
            return null;
        }

        lootSpawner = FindFirstObjectByType<LootSpawner>();
        return lootSpawner;
    }
}
