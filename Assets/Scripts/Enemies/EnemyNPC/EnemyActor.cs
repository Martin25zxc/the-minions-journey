using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TopDownHealth))]
public sealed class EnemyActor : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField]
    private EnemyDefinition definition;

    [Header("References")]
    [SerializeField]
    private TopDownHealth health;

    [Header("Death")]
    [Tooltip("En estas primeras etapas conviene dejarlo apagado para poder ver el cuerpo/estado al morir. Mas adelante se puede destruir despues de la animacion de muerte.")]
    [SerializeField]
    private bool destroyOnDeath;

    [SerializeField, Min(0f)]
    private float destroyDelay = 3f;

    private bool initialized;
    private bool hasDied;

    public event Action<EnemyActor> Died;
    public event Action<EnemyActor, float, float> HealthChanged;
    public event Action<EnemyActor, TMJ_DamageInfo> Damaged;

    public EnemyDefinition Definition => definition;
    public TopDownHealth Health => health;
    public bool IsAlive => health != null && health.IsAlive && !hasDied;
    public bool HasDied => hasDied;

    private void Awake()
    {
        if (health == null)
        {
            health = GetComponent<TopDownHealth>();
        }
    }

    private void OnEnable()
    {
        if (health == null)
        {
            health = GetComponent<TopDownHealth>();
        }

        if (health != null)
        {
            health.OnDied += HandleDied;
            health.OnHealthChanged += HandleHealthChanged;
            health.OnDamaged += HandleDamaged;
        }
    }

    private void Start()
    {
        InitializeFromDefinition();
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDied -= HandleDied;
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDamaged -= HandleDamaged;
        }
    }

    public void InitializeFromDefinition()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        hasDied = false;

        if (definition == null)
        {
            Debug.LogError($"[{nameof(EnemyActor)}] {name} has no EnemyDefinition assigned.", this);
            return;
        }

        if (health == null)
        {
            Debug.LogError($"[{nameof(EnemyActor)}] {name} has no TopDownHealth component.", this);
            return;
        }

        health.Initialize(definition.MaxHealth);
    }

    public void SetDefinition(EnemyDefinition newDefinition, bool reinitializeHealth = true)
    {
        definition = newDefinition;

        if (reinitializeHealth)
        {
            initialized = false;
            InitializeFromDefinition();
        }
    }

    private void HandleDamaged(TMJ_DamageInfo damageInfo)
    {
        // TopDownHealth dispara OnDamaged antes de OnDied.
        // EnemyAnimator ya evita reproducir HitFront/HitBack si la vida llego a 0.
        // Mantener este evento incluso en daño letal permite que sistemas de IA
        // como EnemyDamageResponder / alerta grupal futura reaccionen a un aliado one-shotteado.
        Damaged?.Invoke(this, damageInfo);
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        HealthChanged?.Invoke(this, currentHealth, maxHealth);
    }

    private void HandleDied()
    {
        if (hasDied)
        {
            return;
        }

        hasDied = true;
        Died?.Invoke(this);

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }
}
