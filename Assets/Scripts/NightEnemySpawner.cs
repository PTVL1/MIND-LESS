using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NightEnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Night Spawning")]
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private float minimumSpawnDistance = 12f;
    [SerializeField] private float maximumSpawnDistance = 24f;
    [SerializeField] private float navMeshSampleRadius = 4f;
    [SerializeField] private int maximumAliveEnemies = 12;
    [SerializeField] private int spawnAttempts = 10;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private Coroutine spawnRoutine;
    private bool isSubscribed;

    private void Awake()
    {
        if (dayNightCycle == null)
        {
            dayNightCycle = DayNightCycle.Instance;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void OnEnable()
    {
        BindToDayNightCycle();
    }

    private void Start()
    {
        BindToDayNightCycle();
        if (dayNightCycle != null && dayNightCycle.IsNight)
        {
            HandleNightStarted(dayNightCycle.CurrentDay);
        }
    }

    private void OnDisable()
    {
        if (dayNightCycle != null)
        {
            dayNightCycle.NightStarted -= HandleNightStarted;
            dayNightCycle.DayStarted -= HandleDayStarted;
        }
        isSubscribed = false;

        StopSpawning();
    }

    private void BindToDayNightCycle()
    {
        if (isSubscribed)
        {
            return;
        }

        if (dayNightCycle == null)
        {
            dayNightCycle = DayNightCycle.Instance;
        }

        if (dayNightCycle != null)
        {
            dayNightCycle.NightStarted += HandleNightStarted;
            dayNightCycle.DayStarted += HandleDayStarted;
            isSubscribed = true;
        }
    }

    private void HandleNightStarted(int day)
    {
        StopSpawning();
        spawnRoutine = StartCoroutine(SpawnDuringNight());
    }

    private void HandleDayStarted(int day)
    {
        StopSpawning();
    }

    private IEnumerator SpawnDuringNight()
    {
        while (dayNightCycle != null && dayNightCycle.IsNight)
        {
            RemoveDestroyedEnemies();
            if (spawnedEnemies.Count < maximumAliveEnemies)
            {
                TrySpawnEnemy();
            }

            yield return new WaitForSeconds(Mathf.Max(0.1f, spawnInterval));
        }

        spawnRoutine = null;
    }

    private void TrySpawnEnemy()
    {
        if (player == null || enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            return;
        }

        for (int attempt = 0; attempt < spawnAttempts; attempt++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minimumSpawnDistance, maximumSpawnDistance);
            Vector3 candidate = player.position + new Vector3(direction.x, 0f, direction.y) * distance;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                continue;
            }

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (prefab != null)
            {
                spawnedEnemies.Add(Instantiate(prefab, hit.position, Quaternion.identity));
            }
            return;
        }
    }

    private void RemoveDestroyedEnemies()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
    }

    private void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }
}
