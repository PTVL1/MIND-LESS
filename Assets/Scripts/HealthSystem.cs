using System;
using UnityEngine;
using UnityEngine.Events;

namespace MindLess.Gameplay
{

public class HealthSystem : MonoBehaviour
{
    [System.Serializable]
    public class HealthChangedEvent : UnityEvent<int, int> { }

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private bool destroyOnDeath;

    [Header("Events")]
    [SerializeField] private HealthChangedEvent onHealthChanged;
    [SerializeField] private UnityEvent onDeath;
    [SerializeField] private UnityEvent onRevive;

    public event Action Died;
    public event Action<int, int> HealthChanged;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = maxHealth;
        NotifyHealthChanged();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead)
        {
            return;
        }

        int damage = Mathf.Max(0, amount);
        if (damage == 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        NotifyHealthChanged();

        if (CurrentHealth > 0)
        {
            return;
        }

        Died?.Invoke();
        onDeath?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    public void Heal(int amount)
    {
        if (IsDead)
        {
            return;
        }

        int healAmount = Mathf.Max(0, amount);
        if (healAmount == 0)
        {
            return;
        }

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + healAmount);
        NotifyHealthChanged();
    }

    public void SetMaxHealth(int newMaxHealth, bool healToFull = true)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);

        if (healToFull)
        {
            CurrentHealth = maxHealth;
        }
        else
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
        }

        NotifyHealthChanged();
    }

    public void SetDestroyOnDeath(bool shouldDestroy)
    {
        destroyOnDeath = shouldDestroy;
    }

    public void Revive(int healthAfterRevive = -1)
    {
        if (!IsDead)
        {
            return;
        }

        int restoredHealth = healthAfterRevive <= 0 ? maxHealth : Mathf.Clamp(healthAfterRevive, 1, maxHealth);
        CurrentHealth = restoredHealth;

        onRevive?.Invoke();
        NotifyHealthChanged();
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        onHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
}
