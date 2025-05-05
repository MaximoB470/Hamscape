
using UnityEngine;

public class Spike : MonoBehaviour
{
    [SerializeField] private float damageAmount = 10f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleDamage(other.gameObject);
    }


    private void HandleDamage(GameObject target)
    {

        IDamageable damageable = target.GetComponent<PlayerSetup>();
        if (damageable != null)
        {
            damageable.TakeDamage(damageAmount);
            return;
        }
    }
}
