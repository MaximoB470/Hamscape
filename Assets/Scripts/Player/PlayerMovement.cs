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

    // acceleration
    private float _acceleration = 10f;
    private float _currentHorizontalSpeed = 0f;
    private float _deceleration = 15f;

    // Dash variables
    private bool _isDashing = false;
    private float _dashSpeed = 15f;
    private float _dashDuration = 0.2f;
    private float _dashCooldown = 1f;
    private float _dashTimer = 0f;
    private float _dashCooldownTimer = 0f;
    private Vector2 _dashDirection;

    // Movement detection
    private Vector2 _lastPosition;
    private bool _wasMoving;
    private List<IMovementStateObserver> _movementObservers = new List<IMovementStateObserver>();

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
        _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);

        if (_dashCooldownTimer > 0)
        {
            _dashCooldownTimer -= deltaTime;
        }

        if (_isDashing)
        {
            HandleDash(deltaTime);
        }
        else
        {
            if (_dashCooldownTimer <= 0 && Input.GetKeyDown(KeyCode.LeftShift))
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
        }
        _wasMoving = isMoving;
        _lastPosition = _rb.position;
    }

    private bool CheckIfMoving()
    {
        bool hasHorizontalMovement = Mathf.Abs(_currentHorizontalSpeed) > 0.1f;
        bool hasVerticalMovement = !_isGrounded || Mathf.Abs(_verticalVelocity) > 0.1f;
        return hasHorizontalMovement || hasVerticalMovement || _isDashing;
    }

    private void NotifyMovementStateChange(bool isMoving)
    {
        foreach (var observer in _movementObservers)
        {
            observer.OnMovementStateChanged(isMoving);
        }
    }

    private void HandleSmoothMovement(float deltaTime)
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        float targetSpeed = horizontalInput * _moveSpeed;

        float accelerationRate = horizontalInput != 0 ? _acceleration : _deceleration;

        _currentHorizontalSpeed = Mathf.MoveTowards(
            _currentHorizontalSpeed,
            targetSpeed,
            accelerationRate * deltaTime
        );

        if (_isGrounded && Input.GetKeyDown(KeyCode.Space))
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

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        _dashDirection = new Vector2(horizontalInput, verticalInput).normalized;

        if (_dashDirection == Vector2.zero)
        {
            if (Mathf.Abs(_currentHorizontalSpeed) > 0.1f)
            {
                _dashDirection = new Vector2(Mathf.Sign(_currentHorizontalSpeed), 0).normalized;
            }
            else
            {
                _dashDirection = Vector2.right; 
            }
        }
    }

    private void HandleDash(float deltaTime)
    {
        _dashTimer -= deltaTime;

        if (_dashTimer > 0)
        {
            _rb.velocity = _dashDirection * _dashSpeed;
            if (Mathf.Abs(_dashDirection.x) > 0.1f)
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