using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemySpawnTrigger : MonoBehaviour
{
    [SerializeField] private List<Transform> spawnPoints;

    private bool _alreadyTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_alreadyTriggered) return;

        IDamageable damageable = other.GetComponent<PlayerSetup>();
        if (damageable != null)
        {
            {
                _alreadyTriggered = true;

                List<Vector2> positions = new List<Vector2>();
                foreach (Transform point in spawnPoints)
                {
                    positions.Add(point.position);
                }

                EnemyLocationSpawner spawner = ServiceLocator.Instance.GetService<EnemyLocationSpawner>();
                if (spawner != null)
                {
                    spawner.SpawnEnemiesAtLocations(positions);
                }
                else
                {
                    Debug.LogWarning("No se encontró el EnemyLocationSpawner en el ServiceLocator");
                }
            }
        }
    }
}