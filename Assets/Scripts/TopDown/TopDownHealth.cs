using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class TopDownHealth : MonoBehaviour, ITopDownDamageable
{
    [SerializeField, Min(1f)]
    float maxHealth = 10f;

    [SerializeField]
    UnityEvent onDied = new UnityEvent();

    float currentHealth;

    public float CurrentHealth => currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || currentHealth <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (currentHealth <= 0f)
        {
            onDied.Invoke();
        }
    }
}
