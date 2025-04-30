using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSetup : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("Life Settings")]
    [SerializeField] private float maxLife = 100f;
    [SerializeField] private float regenPerSecond = 5f;
    [SerializeField] private float damagePerSecond = 10f;

    private PlayerMovement _playerMovement;
    private lifeSystem _lifeSystem;

    private void Awake()
    {
        // Inicializar sistemas
        _playerMovement = new PlayerMovement(
            transform,
            groundCheck,
            moveSpeed,
            jumpForce,
            gravity,
            groundCheckRadius,
            groundLayer
        );

        _lifeSystem = new lifeSystem(
            transform,
            maxLife,
            regenPerSecond,
            damagePerSecond,
            OnPlayerDeath
        );

        // Registrar sistemas en el UpdateManager
        UpdateManager.Instance.Register(_playerMovement);
        UpdateManager.Instance.Register(_lifeSystem);
    }

    private void OnPlayerDeath()
    {
        Debug.Log("Player Died");

        // Desregistrar sistemas antes de destruir el objeto
        UpdateManager.Instance.Unregister(_playerMovement);
        UpdateManager.Instance.Unregister(_lifeSystem);

        // Destruir el objeto del jugador
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Asegurarse de desregistrar en caso de destrucción inesperada
        UpdateManager.Instance.Unregister(_playerMovement);
        UpdateManager.Instance.Unregister(_lifeSystem);
    }
}