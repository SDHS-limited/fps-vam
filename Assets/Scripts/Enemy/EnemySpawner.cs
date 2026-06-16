using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
 public GameObject enemyPrefab;

    public Transform[] spawnPoints;

    public int enemyCount = 5;

    public float spawnDelay = 1f;
    public int aliveEnemies;
    public PhaseManager phaseManager;
    public static EnemySpawner Instance;
    public bool canSpawn = true;

    private void Awake()
    {
        Instance = this;
    }
    
    public void SpawnWave()
    {
        if (canSpawn)
        {
            StartCoroutine(SpawnWaveRoutine());
            aliveEnemies++;
        }
        else
        {
            
        }

    }

    IEnumerator SpawnWaveRoutine()
    {
        for(int i = 0; i < enemyCount; i++)
        {
            Transform point =
                spawnPoints[Random.Range(0, spawnPoints.Length)];

            Instantiate(
                enemyPrefab,
                point.position,
                point.rotation);

            yield return new WaitForSeconds(spawnDelay);
        }
    }
}