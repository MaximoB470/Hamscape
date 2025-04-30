using System;
using UnityEngine;
public class PlayerHealthSystem : HealthSystem
{
    private float _regenPerSecond;
    private float _damagePerSecond;
    private Vector3 _lastPosition;
    private bool _isMoving;

    public PlayerHealthSystem(
        Transform transform,
        float maxHealth,
        float regenPerSecond,
        float damagePerSecond,
        Action onDeath)
        : base(transform, maxHealth, onDeath)
    {
        _regenPerSecond = regenPerSecond;
        _damagePerSecond = damagePerSecond;
        _lastPosition = transform.position;
    }

    public override void Tick(float deltaTime)
    {
        DetectMovement();

        if (_isMoving)
        {
            Heal(_regenPerSecond * deltaTime);
        }
        else
        {
            TakeDamage(_damagePerSecond * deltaTime);
        }

        base.Tick(deltaTime);
    }

    private void DetectMovement()
    {
        _isMoving = _transform.position != _lastPosition;
        _lastPosition = _transform.position;
    }
}