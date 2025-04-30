using System;
using UnityEngine;

public class EnemyHealthSystem : HealthSystem
{
    private float _decayRate; // por si enemigos pierden vida con tiempo
    private bool _isDecaying;

    public EnemyHealthSystem(
        Transform transform,
        float maxHealth,
        float decayRate,
        bool isDecaying,
        Action onDeath)
        : base(transform, maxHealth, onDeath)
    {
        _decayRate = decayRate;
        _isDecaying = isDecaying;
    }

    public override void Tick(float deltaTime)
    {
        if (_isDecaying)
        {
            TakeDamage(_decayRate * deltaTime);
        }

        base.Tick(deltaTime);
    }


    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);

    }
}