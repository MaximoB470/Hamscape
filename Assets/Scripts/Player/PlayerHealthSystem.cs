using System;
using UnityEngine;
public class PlayerHealthSystem : HealthSystem, IDamageable, IMovementStateObserver
{
    private float _regenPerSecond;
    private float _damagePerSecond;
    private float _damageTimer;

    private float _standingStillDamage;
    private float _timeBeforeDamagingWhenStill;
    private bool _isMoving;
    private float _lastMovementTime;
    private bool _hasAppliedStillDamage = false;

    public PlayerHealthSystem(
       Transform transform,
       float maxHealth,
       float regenPerSecond,
       float damagePerSecond,
       float standingStillDamage,
       float timeBeforeDamagingWhenStill,
       Action onDeath
   ) : base(transform, maxHealth, onDeath)
    {
        _regenPerSecond = regenPerSecond;
        _damagePerSecond = damagePerSecond;
        _damageTimer = 0f;
        _standingStillDamage = standingStillDamage;
        _timeBeforeDamagingWhenStill = timeBeforeDamagingWhenStill;
        _isMoving = false;
        _lastMovementTime = Time.time;
        _hasAppliedStillDamage = false;
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        if (_damageTimer > 0)
        {
            _damageTimer -= deltaTime;
            float damageThisFrame = _damagePerSecond * deltaTime;
            TakeDamage(damageThisFrame);
        }
        HandleMoveOrDieMechanic(deltaTime);
    }

    private void HandleMoveOrDieMechanic(float deltaTime)
    {
        if (_isMoving)
        {
            _lastMovementTime = Time.time;
            _hasAppliedStillDamage = false;
        }
        else
        {
            float timeSinceLastMovement = Time.time - _lastMovementTime;

            if (timeSinceLastMovement > _timeBeforeDamagingWhenStill && !_hasAppliedStillDamage)
            {
                TakeDamage(_standingStillDamage);
                _hasAppliedStillDamage = true;
            }
        }
    }

    public override void TakeDamage(float amount)
    {
        // Mostrar texto de daño antes de aplicar el daño
        if (amount > 0)
        {
            UIManager uiManager = ServiceLocator.Instance.GetService<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowDamageText(_transform.position, amount, Color.red);
            }
            else
            {
                Debug.LogWarning("UIManager no encontrado en ServiceLocator");
            }
        }

        base.TakeDamage(amount);

        // Mostrar defeat canvas solo cuando el jugador muere
        if (_currentHealth <= 0f)
        {
            UIManager uiManager = ServiceLocator.Instance.GetService<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowDefeatCanvas();
            }
        }
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