using System;
using UnityEngine;

public class EnemyHealthSystem : HealthSystem
{
    public EnemyHealthSystem(Transform transform, float maxHealth, Action onDeath)
        : base(transform, maxHealth, onDeath)
    {
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);
    }

    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);
    }
}

