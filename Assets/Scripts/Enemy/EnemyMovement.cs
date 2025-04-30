using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : IUpdatable
{
    private Transform _transform;
    private float _moveSpeed;
    private float _detectionRange;
    private Transform _playerTransform;

    public EnemyMovement(Transform transform, float moveSpeed, float detectionRange)
    {
        _transform = transform;
        _moveSpeed = moveSpeed;
        _detectionRange = detectionRange;

        // Encontrar el jugador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
    
        }
    }

    public void Tick(float deltaTime)
    {
        if (_playerTransform == null) return;

        // Calcular distancia al jugador
        float distanceToPlayer = Vector3.Distance(_transform.position, _playerTransform.position);

        // Si está dentro del rango de detección, perseguir al jugador
        if (distanceToPlayer <= _detectionRange)
        {
            Vector3 direction = (_playerTransform.position - _transform.position).normalized;
            _transform.Translate(direction * _moveSpeed * deltaTime);

            if (direction.x > 0)
            {
                _transform.localScale = new Vector3(1, 1, 1);
            }
            else if (direction.x < 0)
            {
                _transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }
}