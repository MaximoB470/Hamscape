using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : IUpdatable
{
    private Rigidbody2D _rb;
    private float _moveSpeed;
    private float _jumpForce;
    private float _gravity;
    private float _verticalVelocity = 0f;
    private bool _isGrounded;
    private Transform _groundCheck;
    private float _groundCheckRadius;
    private LayerMask _groundLayer;

    // Pre-allocated array for collision checks
    private readonly Collider2D[] _groundHitResults = new Collider2D[1];

    // Acceleration
    private float _acceleration = 10f;
    private float _currentHorizontalSpeed = 0f;
    private float _deceleration = 15f;

    // Dash variables
    public bool _isDashing = false;
    private float _dashSpeed = 15f;
    private float _dashDuration = 0.2f;
    private float _dashCooldown = 1f;
    private float _dashTimer = 0f;
    private float _dashCooldownTimer = 0f;
    private Vector2 _dashDirection;

    // Input caching
    private float _horizontalInput;
    private bool _jumpPressed;
    private bool _dashPressed;

    // Movement detection
    private Vector2 _lastPosition;
    private bool _wasMoving;
    private readonly List<IMovementStateObserver> _movementObservers = new List<IMovementStateObserver>();

    private const float MOVEMENT_THRESHOLD = 0.1f;

    public PlayerMovement(
        Rigidbody2D rb,
        Transform groundCheck,
        float moveSpeed,
        float jumpForce,
        float gravity,
        float groundCheckRadius,
        LayerMask groundLayer,
        float acceleration = 10f,
        float deceleration = 15f
    )
    {
        _rb = rb;
        _groundCheck = groundCheck;
        _moveSpeed = moveSpeed;
        _jumpForce = jumpForce;
        _gravity = gravity;
        _groundCheckRadius = groundCheckRadius;
        _groundLayer = groundLayer;
        _acceleration = acceleration;
        _deceleration = deceleration;
        _rb.gravityScale = 0f;
        _lastPosition = rb.position;
        ServiceLocator.Instance.Register<PlayerMovement>(this);
    }

    public void RegisterMovementObserver(IMovementStateObserver observer)
    {
        if (!_movementObservers.Contains(observer))
        {
            _movementObservers.Add(observer);
        }
    }

    public void SetAcceleration(float acceleration)
    {
        _acceleration = Mathf.Max(0.1f, acceleration);
    }

    public void SetDeceleration(float deceleration)
    {
        _deceleration = Mathf.Max(0.1f, deceleration);
    }

    public void Tick(float deltaTime)
    {
        GatherInput();
        CheckGroundState();
        // Handle dash cooldown
        if (_dashCooldownTimer > 0)
        {
            _dashCooldownTimer -= deltaTime;
        }
        // Handle movement based on state
        if (_isDashing)
        {
            HandleDash(deltaTime);
        }
        else
        {
            if (_dashCooldownTimer <= 0 && _dashPressed)
            {
                StartDash();
            }
            else
            {
                HandleSmoothMovement(deltaTime);
            }
        }
        bool isMoving = CheckIfMoving();
        if (isMoving != _wasMoving)
        {
            NotifyMovementStateChange(isMoving);
            _wasMoving = isMoving;
        }

        _lastPosition = _rb.position;
    }

    private void GatherInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _jumpPressed = Input.GetKeyDown(KeyCode.Space);
        _dashPressed = Input.GetKeyDown(KeyCode.LeftShift);
    }
    private void CheckGroundState()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            _groundCheck.position,
            _groundCheckRadius,
            _groundHitResults,
            _groundLayer
        );

        _isGrounded = hitCount > 0;
    }
    private bool CheckIfMoving()
    {
        bool hasHorizontalMovement = Mathf.Abs(_currentHorizontalSpeed) > MOVEMENT_THRESHOLD;
        bool hasVerticalMovement = !_isGrounded || Mathf.Abs(_verticalVelocity) > MOVEMENT_THRESHOLD;
        return hasHorizontalMovement || hasVerticalMovement || _isDashing;
    }
    private void NotifyMovementStateChange(bool isMoving)
    {
        for (int i = 0; i < _movementObservers.Count; i++)
        {
            _movementObservers[i].OnMovementStateChanged(isMoving);
        }
    }
    private void HandleSmoothMovement(float deltaTime)
    {
        float targetSpeed = _horizontalInput * _moveSpeed;
        float accelerationRate = _horizontalInput != 0 ? _acceleration : _deceleration;

        float speedDifference = targetSpeed - _currentHorizontalSpeed;
        float maxChange = accelerationRate * deltaTime;

        if (Mathf.Abs(speedDifference) <= maxChange)
        {
            _currentHorizontalSpeed = targetSpeed;
        }
        else
        {
            _currentHorizontalSpeed += maxChange * Mathf.Sign(speedDifference);
        }

        if (_isGrounded && _jumpPressed)
        {
            _verticalVelocity = _jumpForce;
        }
        else if (!_isGrounded)
        {
            _verticalVelocity -= _gravity * deltaTime;
        }
        else if (_verticalVelocity < 0)
        {
            _verticalVelocity = 0f;
        }

        _rb.velocity = new Vector2(_currentHorizontalSpeed, _verticalVelocity);
    }

    private void StartDash()
    {
        _isDashing = true;
        _dashTimer = _dashDuration;
        _dashCooldownTimer = _dashCooldown;

        float verticalInput = Input.GetAxis("Vertical");

        _dashDirection.x = _horizontalInput;
        _dashDirection.y = verticalInput;

        if (_dashDirection.sqrMagnitude < 0.01f)
        {
            if (Mathf.Abs(_currentHorizontalSpeed) > MOVEMENT_THRESHOLD)
            {
                _dashDirection.x = Mathf.Sign(_currentHorizontalSpeed);
                _dashDirection.y = 0;
                _dashDirection = _dashDirection.normalized;
            }
            else
            {
                _dashDirection = Vector2.right;
            }
        }
        else
        {
            _dashDirection.Normalize();
        }
    }

    private void HandleDash(float deltaTime)
    {
        _dashTimer -= deltaTime;

        if (_dashTimer > 0)
        {
            _rb.velocity = _dashDirection * _dashSpeed;

            if (Mathf.Abs(_dashDirection.x) > MOVEMENT_THRESHOLD)
            {
                _currentHorizontalSpeed = _dashDirection.x * _moveSpeed * 0.8f;
            }
        }
        else
        {
            _isDashing = false;

            if (_isGrounded)
            {
                _verticalVelocity = 0f;
            }
            _rb.velocity = new Vector2(_currentHorizontalSpeed, _verticalVelocity);
        }
    }
}