using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SpawnPointData
{
    public Transform spawnPoint;
    public float flipTimer;
    public float respawnDelay;
    public bool isOccupied;
    public float respawnTimer; // Timer interno para el respawn

    public SpawnPointData(Transform point, float timer, float delay)
    {
        spawnPoint = point;
        flipTimer = timer;
        respawnDelay = delay;
        isOccupied = false;
        respawnTimer = 0f;
    }
}