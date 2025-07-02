using System.Collections.Generic;
using UnityEngine;
public class LevelManager : MonoBehaviour, IStartable, IUpdatable
{
    [Header("Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private SpawnPointData[] spawnPointsData;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private LayerMask wallLayer;

    [Header("Enemy Stats")]
    [SerializeField] private float defaultHealth = 100f;
    [SerializeField] private float defaultSpeed = 2f;
    [SerializeField] private float defaultAttackDamage = 10f;
    [SerializeField] private float dashDamageToEnemy = 20f;

    [Header("Damage Settings")]
    [SerializeField] private float damageResetTime = 0.5f;

    [Header("Respawn Settings")]
    [SerializeField] private float globalRespawnDelay = 3f; // Delay global por defecto

    [Header("Win Trigger")]
    [SerializeField] private Collider2D winTrigger;
    [SerializeField] private bool winTriggered = false;

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

        // Crear pool basado en la cantidad de spawnpoints
        int poolSize = spawnPointsData.Length;
        for (int i = 0; i < poolSize; i++)
        {
            var obj = Instantiate(enemyPrefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }

        // Spawnear un enemigo por cada spawnpoint
        for (int i = 0; i < spawnPointsData.Length; i++)
        {
            SpawnEnemyAtPoint(i);
        }
    }

    public void Tick(float deltaTime)
    {
        // Manejar timers de respawn para spawn points desocupados
        HandleRespawnTimers(deltaTime);

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyData data = activeEnemies[i];
            Transform enemyTf = data.instance.transform;

            // Usar el flipTimer específico del spawnpoint
            data.flipTimer -= deltaTime;
            if (data.flipTimer <= 0f)
            {
                data.direction *= -1f;
                data.flipTimer = data.maxFlipTimer; // Resetear con el valor original
            }

            enemyTf.Translate(data.direction * data.speed * deltaTime);

            bool shouldDie = false;

            HandleEnemyDamageSystem(ref data, ref shouldDie, deltaTime);

            Vector2 checkPos = (Vector2)enemyTf.position + data.direction * 0.3f;
            Collider2D wallHit = Physics2D.OverlapCircle(checkPos, 0.05f, wallLayer);

            if (wallHit != null)
            {
                data.direction *= -1f;
                data.flipTimer = data.maxFlipTimer; // Resetear con el valor original
            }

            if (data.health <= 0 || shouldDie)
            {
                DespawnEnemy(data);
                activeEnemies.RemoveAt(i);

                // Iniciar timer de respawn en lugar de respawnear inmediatamente
                StartRespawnTimer(data.spawnPointIndex);
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

    private void HandleRespawnTimers(float deltaTime)
    {
        for (int i = 0; i < spawnPointsData.Length; i++)
        {
            var spawnData = spawnPointsData[i];

            // Si está desocupado y tiene timer activo
            if (!spawnData.isOccupied && spawnData.respawnTimer > 0f)
            {
                spawnData.respawnTimer -= deltaTime;

                // Si el timer llegó a 0, respawnear
                if (spawnData.respawnTimer <= 0f)
                {
                    SpawnEnemyAtPoint(i);
                }

                spawnPointsData[i] = spawnData;
            }
        }
    }

    private void StartRespawnTimer(int spawnPointIndex)
    {
        if (spawnPointIndex < 0 || spawnPointIndex >= spawnPointsData.Length) return;

        var spawnData = spawnPointsData[spawnPointIndex];

        // Usar el delay específico del spawn point, o el global si es 0
        float delayToUse = spawnData.respawnDelay > 0f ? spawnData.respawnDelay : globalRespawnDelay;
        spawnData.respawnTimer = delayToUse;

        spawnPointsData[spawnPointIndex] = spawnData;
    }

    private void SpawnEnemyAtPoint(int spawnPointIndex)
    {
        if (spawnPointIndex < 0 || spawnPointIndex >= spawnPointsData.Length) return;

        var spawnData = spawnPointsData[spawnPointIndex];
        if (spawnData.isOccupied) return; // Ya hay un enemigo en este punto

        GameObject obj = pool.Count > 0 ? pool.Dequeue() : Instantiate(enemyPrefab);
        obj.transform.position = spawnData.spawnPoint.position;
        obj.SetActive(true);

        Vector2 dir = Random.value > 0.5f ? Vector2.left : Vector2.right;

        var data = new EnemyData(obj, defaultSpeed, dir, defaultHealth, defaultAttackDamage, spawnData.flipTimer, spawnPointIndex);
        activeEnemies.Add(data);

        // Marcar el spawnpoint como ocupado
        spawnData.isOccupied = true;
        spawnPointsData[spawnPointIndex] = spawnData;
    }

    // Método público para spawning manual (mantiene compatibilidad)
    public void SpawnEnemy(Vector3 position)
    {
        // Buscar el spawnpoint más cercano a la posición dada
        int closestIndex = GetClosestSpawnPointIndex(position);
        if (closestIndex >= 0)
        {
            SpawnEnemyAtPoint(closestIndex);
        }
    }

    private int GetClosestSpawnPointIndex(Vector3 position)
    {
        int closestIndex = -1;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < spawnPointsData.Length; i++)
        {
            if (spawnPointsData[i].isOccupied) continue;

            float distance = Vector3.Distance(position, spawnPointsData[i].spawnPoint.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
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

        // Liberar el spawnpoint
        if (data.spawnPointIndex >= 0 && data.spawnPointIndex < spawnPointsData.Length)
        {
            var spawnData = spawnPointsData[data.spawnPointIndex];
            spawnData.isOccupied = false;
            spawnPointsData[data.spawnPointIndex] = spawnData;
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (winTriggered) return;

        PlayerSetup playerSetup = other.GetComponent<PlayerSetup>();
        if (playerSetup != null)
        {
            winTriggered = true;
            UIManager uiManager = ServiceLocator.Instance.GetService<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowVictoryCanvas();
            }
        }
    }
    private void OnDestroy()
    {
        UpdateManager.Instance.Unregister(this);
        UpdateManager.Instance.UnregisterStartable(this);
        ServiceLocator.Instance.Unregister<LevelManager>();
    }
}
