using UnityEngine;

public class PlayerMovement : IUpdatable
{
    [Header("Movement Settings")]
    private Transform _transform;
    private float _moveSpeed = 5f;

    [Header("Jump Settings")]
    private float _jumpForce = 5f;
    private float _gravity = 9.8f;
    private float _verticalVelocity = 0f;
    private bool _isGrounded;
    private Transform _groundCheck;
    private float _groundCheckRadius = 0.2f;
    private LayerMask _groundLayer;

    [Header("Acceleration settings")]
    private float _acceleration = 10f; // aceleración
    private float _currentHorizontalSpeed = 0f;

    public PlayerMovement(Transform transform, Transform groundCheck, float moveSpeed, float jumpForce, float gravity, float groundCheckRadius, LayerMask groundLayer)
    {
        _transform = transform;
        _groundCheck = groundCheck;
        _moveSpeed = moveSpeed;
        _jumpForce = jumpForce;
        _gravity = gravity;
        _groundCheckRadius = groundCheckRadius;
        _groundLayer = groundLayer;
    }

    public void Tick(float deltaTime)
    {
        // Comprobar si está en el suelo
        _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);

        // Entrada horizontal
        float input = Input.GetAxisRaw("Horizontal"); 
        float targetSpeed = input * _moveSpeed;

        // Aceleración hacia la velocidad objetivo
        _currentHorizontalSpeed = Mathf.MoveTowards(_currentHorizontalSpeed, targetSpeed, _acceleration * deltaTime);

        // Movimiento horizontal
        Vector2 movement = new Vector2(_currentHorizontalSpeed, 0f) * deltaTime;
        _transform.Translate(movement);

        // Saltar
        if (_isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            _verticalVelocity = _jumpForce;
        }

        // Simular gravedad
        if (!_isGrounded)
        {
            _verticalVelocity -= _gravity * deltaTime;
        }
        else if (_verticalVelocity < 0)
        {
            _verticalVelocity = 0f;
        }

        // Movimiento vertical
        _transform.Translate(new Vector3(0f, _verticalVelocity * deltaTime, 0f));
    }
}