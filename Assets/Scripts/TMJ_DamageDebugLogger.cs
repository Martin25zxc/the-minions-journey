using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TopDownHealth))]
public sealed class TopDownDamageDebugLogger : MonoBehaviour
{
    [SerializeField]
    bool logDamage = true;

    [SerializeField]
    bool logHealthChanges = true;

    [SerializeField]
    bool logShieldChanges;

    [SerializeField]
    bool logDeath = true;

    TopDownHealth health;

    void Awake()
    {
        EnsureHealthReference();
    }

    void OnEnable()
    {
        EnsureHealthReference();

        if (health == null)
        {
            return;
        }

        health.OnDamaged += HandleDamaged;
        health.OnHealthChanged += HandleHealthChanged;
        health.OnShieldChanged += HandleShieldChanged;
        health.OnDied += HandleDied;
    }

    void OnDisable()
    {
        if (health == null)
        {
            return;
        }

        health.OnDamaged -= HandleDamaged;
        health.OnHealthChanged -= HandleHealthChanged;
        health.OnShieldChanged -= HandleShieldChanged;
        health.OnDied -= HandleDied;
    }

    void EnsureHealthReference()
    {
        if (health == null)
        {
            health = GetComponent<TopDownHealth>();
        }
    }

    void HandleDamaged(TMJ_DamageInfo damageInfo)
    {
        if (!logDamage)
        {
            return;
        }

        string sourceName = damageInfo.Source != null ? damageInfo.Source.name : "Unknown Source";
        Debug.Log($"[Damage] {name} received {damageInfo.Damage:0.##} damage from {sourceName} at {damageInfo.SourcePosition}.", this);
    }

    void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        if (!logHealthChanges)
        {
            return;
        }

        Debug.Log($"[Health] {name}: {currentHealth:0.##}/{maxHealth:0.##}.", this);
    }

    void HandleShieldChanged(float currentShield, float maxShield)
    {
        if (!logShieldChanges)
        {
            return;
        }

        Debug.Log($"[Shield] {name}: {currentShield:0.##}/{maxShield:0.##}.", this);
    }

    void HandleDied()
    {
        if (!logDeath)
        {
            return;
        }

        Debug.Log($"[Death] {name} died.", this);
    }
}
