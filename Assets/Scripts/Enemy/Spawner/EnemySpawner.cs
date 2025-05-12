using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour, IUpdatable
{
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 5f;

    private float _timer;

    private void OnEnable()
    {
        UpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        UpdateManager.Instance.Unregister(this);
    }

    public void Tick(float deltaTime)
    {
        _timer += deltaTime;

        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (spawnPoints.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        EnemySetup enemy = enemyPool.GetFromPool();
        enemy.transform.position = spawnPoint.position;
    }
}