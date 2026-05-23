using System;
using UnityEngine;

/// <summary>
/// Componente mínimo de vida del jugador.
/// Los ataques del jefe buscan este componente para aplicar daño.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }

    public event Action<float> OnHealthChanged; // porcentaje 0-1
    public event Action        OnDeath;

    private void Awake() => CurrentHealth = maxHealth;

    public void TakeDamage(float amount)
    {
        if (CurrentHealth <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth / maxHealth);
        if (CurrentHealth <= 0f) OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth / maxHealth);
    }
}