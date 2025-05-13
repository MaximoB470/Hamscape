using UnityEngine;
public class EnemySetup : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 50f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float horizontalDistance = 3f;

    [Header("Damage Settings")]
    [SerializeField] private float damageToPlayer = 10f;
    [SerializeField] private float damageFromDash = 20f;

    private EnemyHealthSystem _healthSystem;
    private EnemyMovement _enemyMovement;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 1f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        _healthSystem = new EnemyHealthSystem(
            transform,
            maxHealth,
            OnEnemyDeath
        );

        _enemyMovement = new EnemyMovement(
            transform,
            _rb,
            moveSpeed,
            horizontalDistance
        );

        UpdateManager.Instance.Register(_enemyMovement);
        UpdateManager.Instance.Register(_healthSystem);
    }

    private void OnEnemyDeath()
    {
        Debug.Log("Enemy Died");
        UpdateManager.Instance.Unregister(_enemyMovement);
        UpdateManager.Instance.Unregister(_healthSystem);

        gameObject.SetActive(false);
        EnemyPool enemyPool = FindObjectOfType<EnemyPool>();
        if (enemyPool != null)
            enemyPool.ReturnToPool(this);
    }

    private void OnDestroy()
    {
        UpdateManager.Instance.Unregister(_enemyMovement);
        UpdateManager.Instance.Unregister(_healthSystem);
    }

    public void TakeDamage(float amount)
    {
        _healthSystem.TakeDamage(amount);
    }

    public void ApplyDamage(float amount)
    {
        _healthSystem.TakeDamage(amount);
    }

    public float GetHealthPercentage()
    {
        return _healthSystem.GetHealthPercentage();
    }

    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        if (_enemyMovement != null)
        {
            _enemyMovement.SetMoveSpeed(moveSpeed);
        }
    }

    public void SetHorizontalDistance(float newDistance)
    {
        horizontalDistance = newDistance;
        if (_enemyMovement != null)
        {
            _enemyMovement.SetHorizontalDistance(horizontalDistance);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }

    public void ResetMovement()
    {
        _enemyMovement.ResetStartPosition();
    }
    private void HandleCollision(GameObject target)
    {
        IDamageable damageable = target.GetComponent<PlayerSetup>();
        if (damageable != null)
        {
            PlayerMovement playerMovement = ServiceLocator.Instance.GetService<PlayerMovement>();

            if (playerMovement != null && playerMovement._isDashing)
            {
                TakeDamage(damageFromDash);
            }
            else
            {
                damageable.TakeDamage(damageToPlayer);
            }
        }
    }
}