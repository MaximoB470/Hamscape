using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;

    private HealthSystem _healthSystem;

    private void Start()
    {
        _healthSystem = ServiceLocator.Instance.GetService<HealthSystem>();

        if (_healthSystem != null)
        {
            _healthSystem.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(_healthSystem.GetCurrentHealth(), _healthSystem.GetMaxHealth());
        }
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        healthText.text = $"HP: {Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
    }

    private void OnDestroy()
    {
        if (_healthSystem != null)
        {
            _healthSystem.OnHealthChanged -= UpdateHealthUI;
        }
    }
}