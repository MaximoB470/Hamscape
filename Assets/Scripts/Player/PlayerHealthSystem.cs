using System;
using UnityEngine;
public class PlayerHealthSystem : HealthSystem, IDamageable, IMovementStateObserver
{
    private float _regenPerSecond;
    private float _damagePerSecond;
    private float _damageTimer;

    // "Move or Die" mecánica
    private float _standingStillDamagePerSecond;
    private float _timeBeforeDamagingWhenStill;
    private bool _isMoving;
    private float _lastMovementTime;

    public PlayerHealthSystem(
        Transform transform,
        float maxHealth,
        float regenPerSecond,
        float damagePerSecond,
        float standingStillDamagePerSecond,
        float timeBeforeDamagingWhenStill,
        Action onDeath
    ) : base(transform, maxHealth, onDeath)
    {
        _regenPerSecond = regenPerSecond;
        _damagePerSecond = damagePerSecond;
        _damageTimer = 0f;

        // Valores para la mecánica "Move or Die"
        _standingStillDamagePerSecond = standingStillDamagePerSecond;
        _timeBeforeDamagingWhenStill = timeBeforeDamagingWhenStill;
        _isMoving = false;
        _lastMovementTime = Time.time;
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        Heal(_regenPerSecond * deltaTime);

        if (_damageTimer > 0)
        {
            _damageTimer -= deltaTime;
            TakeDamage(_damagePerSecond * deltaTime);
        }
        HandleMoveOrDieMechanic(deltaTime);
    }

    private void HandleMoveOrDieMechanic(float deltaTime)
    {
        if (_isMoving)
        {
            _lastMovementTime = Time.time;
        }
        else
        {
            float timeSinceLastMovement = Time.time - _lastMovementTime;

            if (timeSinceLastMovement > _timeBeforeDamagingWhenStill)
            {
                TakeDamage(_standingStillDamagePerSecond * deltaTime);
            }
        }
    }

    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);
    }
    public void StartTimedDamage(float duration)
    {
        _damageTimer = Mathf.Max(_damageTimer, duration);
    }

    public void OnMovementStateChanged(bool isMoving)
    {
        _isMoving = isMoving;

        if (isMoving)
        {
            _lastMovementTime = Time.time;
        }
    }
}