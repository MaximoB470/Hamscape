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
    private PlayerHealthSystem _healthSystem;

    private void Awake()
    {
        _playerMovement = new PlayerMovement(
            transform,
            groundCheck,
            moveSpeed,
            jumpForce,
            gravity,
            groundCheckRadius,
            groundLayer
        );

        _healthSystem = new PlayerHealthSystem(
            transform,
            maxLife,
            regenPerSecond,
            damagePerSecond,
            OnPlayerDeath
        );

        // Registrar sistemas en el UpdateManager
        UpdateManager.Instance.Register(_playerMovement);
        UpdateManager.Instance.Register(_healthSystem);
    }

    private void OnPlayerDeath()
    {
        Debug.Log("Player Died");

        // Desregistrar sistemas antes de destruir el objeto
        UpdateManager.Instance.Unregister(_playerMovement);
        UpdateManager.Instance.Unregister(_healthSystem);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Asegurarse de desregistrar en caso de hacer cagada
        UpdateManager.Instance.Unregister(_playerMovement);
        UpdateManager.Instance.Unregister(_healthSystem);
    }
    public void ApplyDamage(float amount)
    {
        _healthSystem.TakeDamage(amount);
    }

    public void ApplyHealing(float amount)
    {
        _healthSystem.Heal(amount);
    }

    public float GetHealthPercentage()
    {
        return _healthSystem.GetHealthPercentage();
    }
}