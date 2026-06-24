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
    [Header("Max lives")]
    [SerializeField, Min(1)]
    int maxLives = 3;

    [Header("Unity Events")]
    [SerializeField]
    UnityEvent onDied = new UnityEvent();



    float currentHealth;
    float currentShield;
    bool hasDied;
    int currentLives;

    public event Action<TMJ_DamageInfo> OnDamaged;
    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnShieldChanged;
    public event Action OnDied;

    public event Action OnGameOver;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float MaxShield => maxShield;
    public float CurrentShield => currentShield;
    public bool IsAlive => !hasDied && currentHealth > 0f;
    public int CurrentLives => currentLives;

    void Awake()
    {
        ResetHealthState();
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
        ResetHealthState();
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
            hasDied = true;
            if (currentLives > 1)
            {
                currentLives--;
                ResetHealthState();
                OnDied?.Invoke();
                onDied.Invoke();
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                OnShieldChanged?.Invoke(currentShield, maxShield);
            }
            else
            {
                currentLives = 0;
                OnGameOver?.Invoke();
            }
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
        Debug.Log($"¡Curado por {amount} puntos de salud! Salud actual: {currentHealth}/{maxHealth}");
    }

    private void ResetHealthState()
    {
        currentHealth = maxHealth;
        currentShield = maxShield;
        hasDied = false;
    }
}
