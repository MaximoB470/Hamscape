using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : IUpdatable
{
    [Header("Movement & Jump Settings")]
    private Rigidbody2D _rb;
    private float _moveSpeed;
    private float _jumpForce;
    private float _gravity;
    private float _verticalVelocity = 0f;
    private bool _isGrounded;
    private Transform _groundCheck;
    private float _groundCheckRadius;
    private LayerMask _groundLayer;
    private bool _lastMovingState = false;

    [Header("Dash Settings")]
    private bool _isDashing = false;
    private float _dashSpeed = 15f;
    private float _dashDuration = 0.2f;
    private float _dashCooldown = 1f;
    private float _dashTimer = 0f;
    private float _dashCooldownTimer = 0f;
    private Vector2 _dashDirection;
    private List<IMovementStateObserver> _movementObservers = new List<IMovementStateObserver>();

    public PlayerMovement(
        Rigidbody2D rb,
        Transform groundCheck,
        float moveSpeed,
        float jumpForce,
        float gravity,
        float groundCheckRadius,
        LayerMask groundLayer
    )
    {
        _rb = rb;
        _groundCheck = groundCheck;
        _moveSpeed = moveSpeed;
        _jumpForce = jumpForce;
        _gravity = gravity;
        _groundCheckRadius = groundCheckRadius;
        _groundLayer = groundLayer;

        _rb.gravityScale = 0f;
    }
    public void RegisterMovementObserver(IMovementStateObserver observer)
    {
        if (!_movementObservers.Contains(observer))
        {
            _movementObservers.Add(observer);
        }
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
            return; 
        }

        if (_dashCooldownTimer <= 0 && Input.GetKeyDown(KeyCode.LeftShift))
        {
            StartDash();
            return;
        }
        HandleNormalMovement(deltaTime);
    }

    private void HandleNormalMovement(float deltaTime)
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float horizontalVelocity = horizontalInput * _moveSpeed;

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

        _rb.velocity = new Vector2(horizontalVelocity, _verticalVelocity);

        bool currentlyMoving = Mathf.Abs(horizontalInput) > 0.01f || Mathf.Abs(_rb.velocity.y) > 0.01f;

        if (currentlyMoving != _lastMovingState)
        {
            foreach (var observer in _movementObservers)
            {
                observer.OnMovementStateChanged(currentlyMoving);
            }

            _lastMovingState = currentlyMoving;
        }
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
            _dashDirection = new Vector2(_rb.velocity.x, 0).normalized;
            if (_dashDirection == Vector2.zero)
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
        }
        else
        {
            _isDashing = false;

            if (_isGrounded)
            {
                _verticalVelocity = 0f;
            }
        }
    }
}