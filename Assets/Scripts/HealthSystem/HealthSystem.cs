using System;
using UnityEngine;

public abstract class HealthSystem : IUpdatable
{
    protected Transform _transform;
    protected float _maxHealth;
    protected float _currentHealth;
    protected Action _onDeath;

    // Added event for health changes
    public event Action<float, float> OnHealthChanged;

    public HealthSystem(Transform transform, float maxHealth, Action onDeath)
    {
        _transform = transform;
        _maxHealth = maxHealth;
        _currentHealth = maxHealth;
        _onDeath = onDeath;
    }

    public virtual void Tick(float deltaTime)
    {
        if (_currentHealth <= 0f)
        {
            _onDeath?.Invoke();
        }
    }

    public virtual void TakeDamage(float amount)
    {
        float previousHealth = _currentHealth;
        _currentHealth -= amount;
        _currentHealth = Mathf.Max(0f, _currentHealth);

        // Notify subscribers if health changed
        if (previousHealth != _currentHealth)
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        if (_currentHealth <= 0f)
        {
            _onDeath?.Invoke();
        }
    }

    public virtual void Heal(float amount)
    {
        float previousHealth = _currentHealth;
        _currentHealth += amount;
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth);

        // Notify subscribers if health changed
        if (previousHealth != _currentHealth)
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }

    public float GetCurrentHealth() => _currentHealth;
    public float GetMaxHealth() => _maxHealth;
    public float GetHealthPercentage() => _currentHealth / _maxHealth;
}