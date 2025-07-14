using UnityEngine;

public class PlayerSetup : MonoBehaviour, IDamageable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("Acceleration Settings")]
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;

    [Header("Life Settings")]
    [SerializeField] private float maxLife = 100f;
    [SerializeField] private float regenPerSecond = 5f;
    [SerializeField] private float damagePerSecond = 10f;

    [Header("Move or Die Settings")]
    [SerializeField] private float standingStillDamagePerSecond = 15f;
    [SerializeField] private float timeBeforeDamagingWhenStill = 1.5f;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite hamsterRightSprite;
    [SerializeField] private Sprite hamsterLeftSprite;

    private PlayerMovement _playerMovement;
    private PlayerHealthSystem _healthSystem;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _healthSystem = new PlayerHealthSystem(
            transform,
            maxLife,
            regenPerSecond,
            damagePerSecond,
            standingStillDamagePerSecond,
            timeBeforeDamagingWhenStill,
            OnPlayerDeath
        );

        _playerMovement = new PlayerMovement(
            GetComponent<Rigidbody2D>(),
            groundCheck,
            moveSpeed,
            jumpForce,
            gravity,
            groundCheckRadius,
            groundLayer,
            spriteRenderer,
            hamsterRightSprite,
            hamsterLeftSprite
        );
        _playerMovement.RegisterMovementObserver(_healthSystem);
        UpdateManager.Instance.Register(_playerMovement);
        UpdateManager.Instance.Register(_healthSystem);
        ServiceLocator.Instance.Register<HealthSystem>(_healthSystem);
    }

    private void OnPlayerDeath()
    {
        Debug.Log("Player Died");
        UpdateManager.Instance.Unregister(_playerMovement);
        UpdateManager.Instance.Unregister(_healthSystem);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        UpdateManager.Instance.Unregister(_playerMovement);
        UpdateManager.Instance.Unregister(_healthSystem);
        ServiceLocator.Instance.Unregister<HealthSystem>();
        Destroy(gameObject);
    }

    public void TakeDamage(float amount)
    {
        _healthSystem.TakeDamage(amount);
    }

    public void ApplyDamage(float amount)
    {
        _healthSystem.TakeDamage(amount);
    }

    public void StartTimedDamage(float duration)
    {
        _healthSystem.StartTimedDamage(duration);
    }

    public void ApplyHealing(float amount)
    {
        _healthSystem.Heal(amount);
    }

    public float GetHealthPercentage()
    {
        return _healthSystem.GetHealthPercentage();
    }
    public void SetAcceleration(float newAcceleration)
    {
        acceleration = newAcceleration;
        if (_playerMovement != null)
        {
            _playerMovement.SetAcceleration(acceleration);
        }
    }

    public void SetDeceleration(float newDeceleration)
    {
        deceleration = newDeceleration;
        if (_playerMovement != null)
        {
            _playerMovement.SetDeceleration(deceleration);
        }
    }
}
