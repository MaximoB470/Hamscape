using System;
using UnityEngine;

public class lifeSystem : IUpdatable
{
    private Transform _transform;
    private float _maxLife = 100f;
    private float _currentLife;
    private float _regenPerSecond = 5f;
    private float _damagePerSecond = 10f;
    private Vector3 _lastPosition;
    private bool _isMoving;
    private Action _onDeath;

    public lifeSystem(Transform transform, float maxLife, float regenPerSecond, float damagePerSecond, Action onDeath)
    {
        _transform = transform;
        _maxLife = maxLife;
        _regenPerSecond = regenPerSecond;
        _damagePerSecond = damagePerSecond;
        _currentLife = maxLife;
        _lastPosition = transform.position;
        _onDeath = onDeath;
    }

    public void Tick(float deltaTime)
    {
        DetectMovement();

        if (_isMoving)
        {
            _currentLife += _regenPerSecond * deltaTime;
        }
        else
        {
            _currentLife -= _damagePerSecond * deltaTime;
        }

        _currentLife = Mathf.Clamp(_currentLife, 0f, _maxLife);

        if (_currentLife <= 0f)
        {
            _onDeath?.Invoke();
        }
    }

    private void DetectMovement()
    {
        _isMoving = _transform.position != _lastPosition;
        _lastPosition = _transform.position;
    }

    public float GetCurrentLife()
    {
        return _currentLife;
    }

    public float GetMaxLife()
    {
        return _maxLife;
    }
}