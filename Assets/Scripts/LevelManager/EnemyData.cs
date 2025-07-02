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
    public float maxHealth;
    public bool hasDealtDamage;
    public Collider2D triggerCollider;
    public Collider2D physicsCollider;
    public float flipTimer;
    public float maxFlipTimer; // Timer máximo para resetear
    public int spawnPointIndex; // Índice del spawnpoint asociado

    public EnemyData(GameObject instance, float speed, Vector2 direction, float health, float attackDamage, float flipTimer, int spawnPointIndex)
    {
        this.instance = instance;
        this.speed = speed;
        this.direction = direction;
        this.attackTimer = 0f;
        this.health = health;
        this.maxHealth = health;
        this.attackDamage = attackDamage;
        this.hasDealtDamage = false;
        this.flipTimer = flipTimer;
        this.maxFlipTimer = flipTimer;
        this.spawnPointIndex = spawnPointIndex;

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
