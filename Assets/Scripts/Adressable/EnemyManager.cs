using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;

public struct EnemyData
{
    public float speed;
    public Vector2 direction;
    public float attackTimer;
    public int health;
}


public class EnemyManager : MonoBehaviour, IStartable, IUpdatable
{
    [Header("Enemy Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float enemyHealth = 100f;
    [SerializeField] private float enemyAttackDamage = 10f;
    [SerializeField] private float enemySpeed = 2f;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private Dictionary<GameObject, float> healthDict = new Dictionary<GameObject, float>();
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Awake()
    {
        ServiceLocator.Instance.Register<EnemyManager>(this);
        UpdateManager.Instance.RegisterStartable(this);
        UpdateManager.Instance.Register(this);
    }

    public void Initialize()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab);
            enemy.SetActive(false);
            pool.Enqueue(enemy);
        }

        // Spawn inicial de prueba
        foreach (var point in spawnPoints)
        {
            SpawnEnemy(point.position);
        }
    }

    public void Tick(float deltaTime)
    {
        foreach (var enemy in activeEnemies)
        {
            // Movimiento simple hacia la izquierda
            enemy.transform.Translate(Vector3.left * enemySpeed * deltaTime);

            // Ejemplo de vida y daño
            if (healthDict[enemy] <= 0f)
            {
                DespawnEnemy(enemy);
                break; // Prevenir modificación en enumeración
            }
        }
    }

    public void DamageEnemy(GameObject enemy, float damage)
    {
        if (healthDict.ContainsKey(enemy))
        {
            healthDict[enemy] -= damage;
        }
    }

    public void AttackPlayer(GameObject enemy, GameObject player)
    {
        // Simulación de daño al jugador
        Debug.Log($"Enemy attacked the player for {enemyAttackDamage} damage.");
        // player.GetComponent<PlayerHealth>().TakeDamage(enemyAttackDamage);
    }

    public void SpawnEnemy(Vector3 position)
    {
        GameObject enemy = pool.Count > 0 ? pool.Dequeue() : Instantiate(enemyPrefab);
        enemy.transform.position = position;
        enemy.SetActive(true);

        healthDict[enemy] = enemyHealth;
        activeEnemies.Add(enemy);
    }

    public void DespawnEnemy(GameObject enemy)
    {
        enemy.SetActive(false);
        activeEnemies.Remove(enemy);
        healthDict.Remove(enemy);
        pool.Enqueue(enemy);
    }

    private void OnDestroy()
    {
        ServiceLocator.Instance.Unregister<EnemyManager>();
        UpdateManager.Instance.Unregister(this);
        UpdateManager.Instance.UnregisterStartable(this);
    }
}