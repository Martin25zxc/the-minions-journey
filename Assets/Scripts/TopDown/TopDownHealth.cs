using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class TopDownHealth : MonoBehaviour, ITopDownDamageable
{
    [Header("Health")]
    [SerializeField, Min(1f)]
    float maxHealth = 10f;

    [Header("Shield opcional")]
    [SerializeField, Min(0f)]
    float maxShield = 0f;

    [Header("Unity Events")]
    [SerializeField]
    UnityEvent onDied = new UnityEvent();

    float currentHealth;
    float currentShield;
    bool hasDied;

    public event Action<TMJ_DamageInfo> OnDamaged;
    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnShieldChanged;
    public event Action OnDied;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float MaxShield => maxShield;
    public float CurrentShield => currentShield;
    public bool IsAlive => !hasDied && currentHealth > 0f;

    void Awake()
    {
        ResetHealthState(restoreShield: true);
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        maxShield = Mathf.Max(0f, maxShield);
    }

    public void Initialize(float newMaxHealth)
    {
        Initialize(newMaxHealth, maxShield);
    }

    public void Initialize(float newMaxHealth, float newMaxShield)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        maxShield = Mathf.Max(0f, newMaxShield);

        ResetHealthState(restoreShield: true);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnShieldChanged?.Invoke(currentShield, maxShield);
    }

    public void TakeDamage(TMJ_DamageInfo damageInfo)
    {
        if (damageInfo.Damage <= 0f || !IsAlive)
        {
            return;
        }

        float remainingDamage = damageInfo.Damage;
        float previousHealth = currentHealth;
        float previousShield = currentShield;

        if (currentShield > 0f)
        {
            float absorbed = Mathf.Min(currentShield, remainingDamage);
            currentShield -= absorbed;
            remainingDamage -= absorbed;
        }

        if (remainingDamage > 0f)
        {
            currentHealth = Mathf.Max(0f, currentHealth - remainingDamage);
        }

        bool shieldChanged = !Mathf.Approximately(previousShield, currentShield);
        bool healthChanged = !Mathf.Approximately(previousHealth, currentHealth);

        if (!shieldChanged && !healthChanged)
        {
            return;
        }

        OnDamaged?.Invoke(damageInfo);

        if (shieldChanged)
        {
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }

        if (healthChanged)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        if (currentHealth <= 0f && !hasDied)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || !IsAlive)
        {
            return;
        }

        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (!Mathf.Approximately(previousHealth, currentHealth))
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    public void ReviveFull(bool restoreShield = false)
    {
        bool wasDead = hasDied;
        float previousHealth = currentHealth;
        float previousShield = currentShield;

        currentHealth = maxHealth;

        if (restoreShield)
        {
            currentShield = maxShield;
        }
        else
        {
            currentShield = Mathf.Min(currentShield, maxShield);
        }

        hasDied = false;

        if (wasDead || !Mathf.Approximately(previousHealth, currentHealth))
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        if (!Mathf.Approximately(previousShield, currentShield))
        {
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }
    }

    public void RestoreShieldFull()
    {
        if (maxShield <= 0f)
        {
            return;
        }

        float previousShield = currentShield;
        currentShield = maxShield;

        if (!Mathf.Approximately(previousShield, currentShield))
        {
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }
    }

    void Die()
    {
        hasDied = true;
        currentHealth = 0f;

        OnDied?.Invoke();
        onDied.Invoke();
    }

    void ResetHealthState(bool restoreShield)
    {
        currentHealth = maxHealth;

        if (restoreShield)
        {
            currentShield = maxShield;
        }
        else
        {
            currentShield = Mathf.Min(currentShield, maxShield);
        }

        hasDied = false;
    }
}
