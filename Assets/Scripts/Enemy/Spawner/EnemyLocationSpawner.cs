using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLocationSpawner : MonoBehaviour
{
    [SerializeField] private EnemyPool enemyPool;

    private void Awake()
    {
        ServiceLocator.Instance.Register<EnemyLocationSpawner>(this);
    }

    public void SpawnEnemiesAtLocations(List<Vector2> spawnPositions)
    {
        foreach (Vector2 pos in spawnPositions)
        {
            EnemySetup enemy = enemyPool.GetFromPool();
            enemy.transform.position = pos;
            enemy.ResetMovement();
        }
    }
}