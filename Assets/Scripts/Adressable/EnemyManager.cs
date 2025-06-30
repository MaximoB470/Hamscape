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
    public float maxHealth;
    public bool hasDealtDamage;
    public Collider2D triggerCollider;
    public Collider2D physicsCollider;

    public EnemyData(GameObject instance, float speed, Vector2 direction, float health, float attackDamage)
    {
        this.instance = instance;
        this.speed = speed;
        this.direction = direction;
        this.attackTimer = 0f;
        this.health = health;
        this.maxHealth = health;
        this.attackDamage = attackDamage;
        this.hasDealtDamage = false;

        Collider2D[] colliders = instance.GetComponents<Collider2D>();
        this.triggerCollider = null;
        this.physicsCollider = null;

        foreach (var col in colliders)
        {
            if (col.isTrigger)
                this.triggerCollider = col;
            else
                this.physicsCollider = col;
        }
    }
}

public class EnemyManager : MonoBehaviour, IStartable, IUpdatable
{
    [Header("Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int spawnAmount = 1;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private LayerMask wallLayer;

    [Header("Enemy Stats")]
    [SerializeField] private float defaultHealth = 100f;
    [SerializeField] private float defaultSpeed = 2f;
    [SerializeField] private float defaultAttackDamage = 10f;
    [SerializeField] private float dashDamageToEnemy = 20f;

    [Header("Damage Settings")]
    [SerializeField] private float damageResetTime = 0.5f;

    private Queue<GameObject> pool = new();
    private List<EnemyData> activeEnemies = new();

    private PlayerMovement playerMovement;
    private IDamageable playerDamageable;

    private void Awake()
    {
        ServiceLocator.Instance.Register(this);
        UpdateManager.Instance.RegisterStartable(this);
        UpdateManager.Instance.Register(this);
    }
    public void Initialize()
    {
        playerMovement = ServiceLocator.Instance.GetService<PlayerMovement>();
        playerDamageable = playerCollider.GetComponent<IDamageable>();

        for (int i = 0; i < spawnAmount; i++)
        {
            var obj = Instantiate(enemyPrefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }

        for (int i = 0; i < spawnAmount; i++)
        {
            SpawnEnemy(GetRandomSpawnPoint());
        }
    }
    public void Tick(float deltaTime)
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyData data = activeEnemies[i];
            Transform enemyTf = data.instance.transform;

            enemyTf.Translate(data.direction * data.speed * deltaTime);

            bool shouldDie = false;

            HandleEnemyDamageSystem(ref data, ref shouldDie, deltaTime);

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
    private void HandleEnemyDamageSystem(ref EnemyData data, ref bool shouldDie, float deltaTime)
    {
        if (data.triggerCollider == null || playerCollider == null) return;

        if (data.hasDealtDamage)
        {
            data.attackTimer += deltaTime;
            if (data.attackTimer >= damageResetTime)
            {
                data.hasDealtDamage = false;
                data.attackTimer = 0f;
            }
        }

        if (data.triggerCollider.IsTouching(playerCollider))
        {
            Rigidbody2D playerRb = playerCollider.GetComponent<Rigidbody2D>();

            if (IsJumpKill(playerRb, data.physicsCollider ?? data.triggerCollider, playerCollider))
            {
                playerRb.velocity = new Vector2(playerRb.velocity.x, 8f);
                shouldDie = true;
                return;
            }

            if (playerMovement != null && playerMovement._isDashing)
            {
                data.health -= dashDamageToEnemy;
                if (data.health <= 0)
                {
                    shouldDie = true;
                }
                return;
            }

            if (!data.hasDealtDamage && playerDamageable != null)
            {
                playerDamageable.TakeDamage(data.attackDamage);
                data.hasDealtDamage = true;
                data.attackTimer = 0f;
            }
        }
        else
        {
            if (data.hasDealtDamage && data.attackTimer > 0.1f)
            {
                data.hasDealtDamage = false;
                data.attackTimer = 0f;
            }
        }
    }
    private bool IsJumpKill(Rigidbody2D playerRb, Collider2D enemyCol, Collider2D playerCol)
    {
        if (playerRb == null || enemyCol == null) return false;

        float playerBottom = playerCol.bounds.min.y;
        float enemyTop = enemyCol.bounds.max.y;

        return playerBottom > enemyTop - 0.1f && playerRb.velocity.y < 0f;
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
                data.health = Mathf.Max(0f, data.health);
                activeEnemies[i] = data;
                break;
            }
        }
    }
    public float GetEnemyHealthPercentage(GameObject enemy)
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i].instance == enemy)
            {
                var data = activeEnemies[i];
                return data.health / data.maxHealth;
            }
        }
        return 0f;
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
        ServiceLocator.Instance.Unregister<EnemyManager>();
    }
}