using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : IUpdatable
{
    private Transform _transform;
    private float _moveSpeed = 5f;
    private float _jumpForce = 5f;
    private float _gravity = 9.8f;
    private float _verticalVelocity = 0f;
    private bool _isGrounded;
    private Transform _groundCheck;
    private float _groundCheckRadius = 0.2f;
    private LayerMask _groundLayer;

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
        // Comprobar si est? en el suelo
        _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);

        // Movimiento horizontal
        float h = Input.GetAxis("Horizontal");
        Vector2 movement = new Vector2(h, 0f) * _moveSpeed * deltaTime;
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