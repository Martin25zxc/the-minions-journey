using System;
using UnityEngine;

/// <summary>
/// Productor experimental de GameWorldEvent para pickeables de misión/evento.
///
/// Importante:
/// - No agrega items al inventario.
/// - No completa misiones directamente.
/// - No setea WorldFlags directamente.
/// - No entrega recompensas.
/// - Solo intenta reportar un GameWorldEvent al MissionManager.
///
/// Esta clase existe porque LootPickup nació para la casuística de loot/inventario/armas.
/// No asumimos que LootPickup sea el modelo universal de pickup.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldEventPickup : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] private WorldEventPickupDefinition definition;

    [Header("Scene References")]
    [Tooltip("Referencia explícita al MissionManager de la escena. Evitamos singleton para mantener dependencias visibles.")]
    [SerializeField] private MissionManager missionManager;

    [Header("Collection")]
    [SerializeField] private bool overrideCollectMode;
    [SerializeField] private WorldEventPickupCollectMode collectMode = WorldEventPickupCollectMode.Trigger;

    [Tooltip("Si está activo, solo se consume cuando MissionManager.TryReportWorldEvent devuelve true.")]
    [SerializeField] private bool requireSuccessfulMissionReport = true;

    [Tooltip("Evita que el mismo pickup reporte dos veces.")]
    [SerializeField] private bool reportOnce = true;

    [SerializeField] private WorldEventPickupConsumeMode consumeMode = WorldEventPickupConsumeMode.DisableGameObject;

    [Header("Mission Requirement")]
    [Tooltip("Útil incluso cuando responders activan/desactivan objetos. Evita que el jugador recoja objetivos antes de aceptar misión si alguien dejó el objeto activo por error.")]
    [SerializeField] private bool requireMissionActive;

    [SerializeField] private MissionDefinition requiredMission;

    [Header("Collector Filter")]
    [SerializeField] private string playerTag = "Player";

    [Header("Visual Source")]
    [Tooltip("InstantiateFromDefinition instancia el WorldModelPrefab de la Definition. UseExistingSceneVisual conserva el visual armado manualmente en escena/prefab.")]
    [SerializeField] private WorldEventPickupVisualSource visualSource = WorldEventPickupVisualSource.InstantiateFromDefinition;

    [Tooltip("Contenedor visual que recibe bob/rotación y puede apagarse al consumir. Normalmente se llama VisualRoot.")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("Anchor opcional para el modelo. Si está vacío, se usa VisualRoot. Recomendado cuando querés separar aura/partículas del modelo.")]
    [SerializeField] private Transform modelRoot;

    [Tooltip("Si está activo, aplica VisualScaleMultiplier del PickupVisualProfile al VisualRoot. Apagalo cuando quieras controlar la escala manualmente en la escena/prefab.")]
    [SerializeField] private bool applyProfileScaleToVisualRoot = true;

    [Tooltip("Solo para UseExistingSceneVisual. Si está activo, aplica offset/rotación/escala de la Definition al ModelRoot. Recomendado solo si asignaste un ModelRoot específico.")]
    [SerializeField] private bool applyDefinitionTransformToExistingVisual;

    [SerializeField] private Light auraLight;
    [SerializeField] private ParticleSystem auraParticles;

    [Header("Collider")]
    [SerializeField] private Collider pickupCollider;

    [Header("Debug")]
    [SerializeField] private bool logDebug;

    private GameObject currentVisualInstance;
    private Vector3 visualInitialLocalPosition;
    private bool hasInitialVisualPosition;
    private bool hasReported;

    public event Action<WorldEventPickup, WorldEventPickupDefinition> Collected;

    public WorldEventPickupDefinition Definition => definition;
    public bool HasReported => hasReported;

    public WorldEventPickupCollectMode ActiveCollectMode => overrideCollectMode
        ? collectMode
        : definition != null ? definition.DefaultCollectMode : collectMode;

    private void Reset()
    {
        pickupCollider = GetComponent<Collider>();

        if (visualRoot == null)
        {
            Transform foundVisualRoot = transform.Find("VisualRoot");
            if (foundVisualRoot != null)
            {
                visualRoot = foundVisualRoot;
            }
        }

        if (modelRoot == null && visualRoot != null)
        {
            Transform foundModelRoot = visualRoot.Find("ModelRoot");
            if (foundModelRoot != null)
            {
                modelRoot = foundModelRoot;
            }
        }
    }

    private void Awake()
    {
        CacheVisualInitialPosition();
    }

    private void Start()
    {
        RefreshVisual();
    }

    private void Update()
    {
        AnimateVisual(Time.time);
    }

    public void Initialize(WorldEventPickupDefinition newDefinition)
    {
        definition = newDefinition;
        RefreshVisual();
    }

    private void CacheVisualInitialPosition()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualInitialLocalPosition = visualRoot.localPosition;
        hasInitialVisualPosition = true;
    }

    private void RefreshVisual()
    {
        if (definition == null)
        {
            Debug.LogWarning($"{name} has no WorldEventPickupDefinition assigned.", this);
            return;
        }

        if (!hasInitialVisualPosition)
        {
            CacheVisualInitialPosition();
        }

        if (visualSource == WorldEventPickupVisualSource.InstantiateFromDefinition)
        {
            SpawnWorldModel();
        }
        else
        {
            PrepareExistingSceneVisual();
        }

        ApplyVisualProfile();
    }

    private void SpawnWorldModel()
    {
        Transform parent = GetModelParent();
        if (parent == null)
        {
            Debug.LogWarning($"{name} has no VisualRoot or ModelRoot assigned.", this);
            return;
        }

        ClearSpawnedVisualInstance();

        if (definition.WorldModelPrefab == null)
        {
            Debug.LogWarning($"{definition.name} has no WorldModelPrefab assigned, but {name} is using InstantiateFromDefinition.", this);
            return;
        }

        currentVisualInstance = Instantiate(definition.WorldModelPrefab, parent);
        ApplyDefinitionTransform(currentVisualInstance.transform);
    }

    private void PrepareExistingSceneVisual()
    {
        // Si el modo se cambió en runtime desde Instantiate hacia Existing, limpiamos solo la instancia creada por este script.
        ClearSpawnedVisualInstance();

        if (!applyDefinitionTransformToExistingVisual)
        {
            return;
        }

        if (modelRoot == null)
        {
            Debug.LogWarning(
                $"{name} is using UseExistingSceneVisual with Apply Definition Transform, but ModelRoot is not assigned. " +
                "Assign a ModelRoot to avoid moving/scaling the full VisualRoot with aura/particles.",
                this
            );
            return;
        }

        ApplyDefinitionTransform(modelRoot);
    }

    private Transform GetModelParent()
    {
        if (modelRoot != null)
        {
            return modelRoot;
        }

        return visualRoot;
    }

    private void ApplyDefinitionTransform(Transform target)
    {
        if (target == null || definition == null)
        {
            return;
        }

        target.localPosition = definition.ModelLocalPositionOffset;
        target.localRotation = Quaternion.Euler(definition.ModelLocalEulerAngles);
        target.localScale = definition.ModelLocalScale;
    }

    private void ClearSpawnedVisualInstance()
    {
        if (currentVisualInstance == null)
        {
            return;
        }

        Destroy(currentVisualInstance);
        currentVisualInstance = null;
    }

    private void ApplyVisualProfile()
    {
        PickupVisualProfile profile = definition.VisualProfile;
        if (profile == null)
        {
            Debug.LogWarning($"{definition.name} has no PickupVisualProfile assigned.", this);
            return;
        }

        if (visualRoot != null && applyProfileScaleToVisualRoot)
        {
            visualRoot.localScale = Vector3.one * profile.VisualScaleMultiplier;
        }

        if (auraLight != null)
        {
            auraLight.color = profile.LightColor;
            auraLight.intensity = profile.LightIntensity;
            auraLight.range = profile.LightRange;
        }

        if (auraParticles != null)
        {
            ParticleSystem.MainModule main = auraParticles.main;
            main.startColor = profile.ParticleColor;
            main.startSize = profile.ParticleStartSize;
            main.startSpeed = profile.ParticleStartSpeed;
            main.startLifetime = profile.ParticleStartLifetime;

            ParticleSystem.EmissionModule emission = auraParticles.emission;
            emission.rateOverTime = profile.ParticleRateOverTime;
        }
    }

    private void AnimateVisual(float time)
    {
        if (definition == null || definition.VisualProfile == null || visualRoot == null)
        {
            return;
        }

        PickupVisualProfile profile = definition.VisualProfile;

        if (profile.BobAmplitude > 0f && hasInitialVisualPosition)
        {
            float bob = Mathf.Sin(time * profile.BobFrequency * Mathf.PI * 2f) * profile.BobAmplitude;
            visualRoot.localPosition = visualInitialLocalPosition + Vector3.up * bob;
        }

        if (!Mathf.Approximately(profile.RotationSpeedY, 0f))
        {
            visualRoot.Rotate(Vector3.up, profile.RotationSpeedY * Time.deltaTime, Space.Self);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ActiveCollectMode != WorldEventPickupCollectMode.Trigger)
        {
            return;
        }

        if (!IsValidCollector(other.gameObject))
        {
            return;
        }

        TryCollect(other.gameObject);
    }

    /// <summary>
    /// Punto público para adapters de interacción, cinemática, pruebas o spawners.
    /// Para CollectMode.Interact, el adapter de IPlayerInteractable debe llamar a este método.
    /// </summary>
    public bool TryCollect(GameObject collector)
    {
        if (reportOnce && hasReported)
        {
            return false;
        }

        if (definition == null)
        {
            Debug.LogWarning($"{name} has no WorldEventPickupDefinition assigned.", this);
            return false;
        }

        if (collector != null && !IsValidCollector(collector))
        {
            return false;
        }

        if (!CanCollectByMissionState())
        {
            if (logDebug)
            {
                Debug.Log($"{name} cannot be collected because the required mission is not active.", this);
            }

            return false;
        }

        if (missionManager == null)
        {
            Debug.LogWarning($"{name} has no MissionManager assigned.", this);
            return false;
        }

        GameWorldEvent worldEvent = CreateWorldEvent();
        bool reportSucceeded = missionManager.TryReportWorldEvent(worldEvent);

        if (!reportSucceeded && requireSuccessfulMissionReport)
        {
            Debug.LogWarning(
                $"{name} reported {definition.EventType} / {definition.TargetId}, but MissionManager did not accept it. Pickup will not be consumed.",
                this
            );
            return false;
        }

        hasReported = true;
        Collected?.Invoke(this, definition);

        if (logDebug)
        {
            Debug.Log($"Collected WorldEventPickup: {definition.DisplayName} ({definition.EventType} / {definition.TargetId})", this);
        }

        Consume();
        return true;
    }

    private bool IsValidCollector(GameObject collector)
    {
        if (collector == null)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(playerTag))
        {
            return true;
        }

        return collector.CompareTag(playerTag);
    }

    private bool CanCollectByMissionState()
    {
        if (!requireMissionActive)
        {
            return true;
        }

        if (requiredMission == null)
        {
            Debug.LogWarning($"{name} requires an active mission, but RequiredMission is not assigned.", this);
            return false;
        }

        if (missionManager == null)
        {
            Debug.LogWarning($"{name} requires an active mission, but MissionManager is not assigned.", this);
            return false;
        }

        return missionManager.IsMissionActive(requiredMission.MissionId);
    }

    private GameWorldEvent CreateWorldEvent()
    {
        string sourceId = !string.IsNullOrWhiteSpace(definition.PickupId)
            ? definition.PickupId
            : name;

        // Este constructor fue el contrato previsto para GameWorldEvent en el scope de misiones.
        // Si tu implementación actual usa factories obligatorias, cambiar solo este método.
        return new GameWorldEvent(
            definition.EventType,
            definition.TargetId,
            definition.Amount,
            sourceId
        );
    }

    private void Consume()
    {
        switch (consumeMode)
        {
            case WorldEventPickupConsumeMode.None:
                return;

            case WorldEventPickupConsumeMode.DisableColliderAndVisual:
                if (pickupCollider != null)
                {
                    pickupCollider.enabled = false;
                }

                if (visualRoot != null)
                {
                    visualRoot.gameObject.SetActive(false);
                }
                return;

            case WorldEventPickupConsumeMode.DisableGameObject:
                gameObject.SetActive(false);
                return;

            case WorldEventPickupConsumeMode.DestroyGameObject:
                Destroy(gameObject);
                return;

            default:
                gameObject.SetActive(false);
                return;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (pickupCollider == null)
        {
            pickupCollider = GetComponent<Collider>();
        }

        if (pickupCollider != null && ActiveCollectMode == WorldEventPickupCollectMode.Trigger && !pickupCollider.isTrigger)
        {
            Debug.LogWarning($"{name} uses Trigger collect mode, but its Collider is not marked as Is Trigger.", this);
        }

        if (requireMissionActive && requiredMission == null)
        {
            Debug.LogWarning($"{name} requires mission active but has no RequiredMission assigned.", this);
        }

        if (visualSource == WorldEventPickupVisualSource.InstantiateFromDefinition &&
            definition != null &&
            definition.WorldModelPrefab == null)
        {
            Debug.LogWarning($"{name} uses InstantiateFromDefinition, but its Definition has no WorldModelPrefab assigned.", this);
        }

        if (visualSource == WorldEventPickupVisualSource.UseExistingSceneVisual &&
            applyDefinitionTransformToExistingVisual &&
            modelRoot == null)
        {
            Debug.LogWarning($"{name} wants to apply Definition transform to an existing visual, but ModelRoot is not assigned.", this);
        }
    }
#endif
}
