using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct EnemyData
{
    public GameObject instance;
    public float speed;
    public Vector2 direction;
    public float attackTimer;
    public float health;
    public float attackDamage;

    public EnemyData(GameObject instance, float speed, Vector2 direction, float health, float attackDamage)
    {
        this.instance = instance;
        this.speed = speed;
        this.direction = direction;
        this.attackTimer = 0f;
        this.health = health;
        this.attackDamage = attackDamage;
    }
}

public class EnemyManager : MonoBehaviour, IStartable, IUpdatable
{
    [Header("Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private LayerMask wallLayer;

    [Header("Enemy Stats")]
    [SerializeField] private float defaultHealth = 100f;
    [SerializeField] private float defaultSpeed = 2f;
    [SerializeField] private float defaultAttackDamage = 10f;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;
    private float spawnTimer;

    private Queue<GameObject> pool = new();
    private List<EnemyData> activeEnemies = new();

    private void Awake()
    {
        ServiceLocator.Instance.Register(this);
        UpdateManager.Instance.RegisterStartable(this);
        UpdateManager.Instance.Register(this);
    }
    public void Initialize()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var obj = Instantiate(enemyPrefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
        SpawnEnemy(GetRandomSpawnPoint());
        spawnTimer = spawnInterval;
    }

    public void Tick(float deltaTime)
    {

        spawnTimer -= deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnEnemy(GetRandomSpawnPoint());
            spawnTimer = spawnInterval;
        }


        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyData data = activeEnemies[i];
            Transform enemyTf = data.instance.transform;

            enemyTf.Translate(data.direction * data.speed * deltaTime);

            bool shouldDie = false;

            Collider2D enemyCol = data.instance.GetComponent<Collider2D>();
            if (enemyCol != null && playerCollider != null && enemyCol.IsTouching(playerCollider))
            {
                Rigidbody2D playerRb = playerCollider.GetComponent<Rigidbody2D>();
                float playerBottom = playerCollider.bounds.min.y;
                float enemyTop = enemyCol.bounds.max.y;

                bool isJumpKill = playerBottom > enemyTop - 0.1f && playerRb.velocity.y < 0f;

                if (isJumpKill)
                {
                    playerRb.velocity = new Vector2(playerRb.velocity.x, 8f); // Rebote
                    shouldDie = true;
                }
            }

            RaycastHit2D hit = Physics2D.Raycast(enemyTf.position, data.direction, 0.1f, wallLayer);
            if (hit.collider != null)
            {
                data.direction *= -1f;
            }

            if (data.health <= 0 || shouldDie)
            {
                DespawnEnemy(data);
                activeEnemies.RemoveAt(i);
            }
            else
            {
                activeEnemies[i] = data;
            }
        }
    }
    private Vector3 GetRandomSpawnPoint()
    {
        if (spawnPoints.Length == 0) return Vector3.zero;
        return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
    }
    public void SpawnEnemy(Vector3 position)
    {
        GameObject obj = pool.Count > 0 ? pool.Dequeue() : Instantiate(enemyPrefab);
        obj.transform.position = position;
        obj.SetActive(true);

        Vector2 dir = Random.value > 0.5f ? Vector2.left : Vector2.right;

        var data = new EnemyData(obj, defaultSpeed, dir, defaultHealth, defaultAttackDamage);
        activeEnemies.Add(data);
    }
    public void DamageEnemy(GameObject enemy, float amount)
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i].instance == enemy)
            {
                var data = activeEnemies[i];
                data.health -= amount;
                activeEnemies[i] = data;
                break;
            }
        }
    }
    private void DespawnEnemy(EnemyData data)
    {
        data.instance.SetActive(false);
        pool.Enqueue(data.instance);
    }
    private void OnDestroy()
    {
        UpdateManager.Instance.Unregister(this);
        UpdateManager.Instance.UnregisterStartable(this);
    }
}