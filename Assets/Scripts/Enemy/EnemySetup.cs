using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySetup : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float decayRate = 0f;
    [SerializeField] private bool isDecaying = false;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 5f;

    private EnemyHealthSystem _healthSystem;
    private EnemyMovement _enemyMovement;
    private ObjectPool<EnemySetup> _pool;

    public void Initialize(ObjectPool<EnemySetup> pool, Vector3 position)
    {
        _pool = pool;
        transform.position = position;

        // Crear sistemas solo si no existen
        if (_healthSystem == null)
        {
            _healthSystem = new EnemyHealthSystem(
                transform,
                maxHealth,
                decayRate,
                isDecaying,
                OnEnemyDeath
            );
        }
        else
        {
            _healthSystem.Heal(maxHealth);
        }

        if (_enemyMovement == null)
        {
            _enemyMovement = new EnemyMovement(transform, moveSpeed, detectionRange);
        }


        UpdateManager.Instance.Register(_healthSystem);
        UpdateManager.Instance.Register(_enemyMovement);

        gameObject.SetActive(true);
    }

    private void OnEnemyDeath()
    {
        UpdateManager.Instance.Unregister(_healthSystem);
        UpdateManager.Instance.Unregister(_enemyMovement);

        if (_pool != null)
        {
            _pool.ReturnObject(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        if (_healthSystem != null)
            UpdateManager.Instance.Unregister(_healthSystem);

        if (_enemyMovement != null)
            UpdateManager.Instance.Unregister(_enemyMovement);
    }

    public void ApplyDamage(float amount)
    {
        _healthSystem.TakeDamage(amount);
    }

    public float GetHealthPercentage()
    {
        return _healthSystem.GetHealthPercentage();
    }
}
