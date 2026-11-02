using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class ReinforcementSpawner : MonoBehaviour, IResettable
{
    [Serializable]
    public class Wave
    {
        public List<EnemyAI> enemies = new List<EnemyAI>();
        [Min(0f)] public float delayBefore = 3f;
        [Min(0f)] public float delayBetweenSpawns = 0.6f;
    }

    [SerializeField] private AlarmBell triggerBell;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private List<Wave> waves = new List<Wave>();
    [SerializeField] private int maxAlive = 6;
    [SerializeField] private ZoneEncounter territoryZone;

    private readonly List<EnemyAI> spawned = new List<EnemyAI>();
    private bool started;

    public event Action<EnemyAI> EnemySpawned;
    public event Action AllWavesSpawned;

    private void OnEnable()
    {
        if (triggerBell != null) triggerBell.Rung += StartReinforcements;
    }

    private void OnDisable()
    {
        if (triggerBell != null) triggerBell.Rung -= StartReinforcements;
    }

    public void StartReinforcements()
    {
        if (started || spawnPoints == null || spawnPoints.Length == 0) return;
        started = true;
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        foreach (Wave wave in waves)
        {
            yield return new WaitForSeconds(wave.delayBefore);

            foreach (EnemyAI prefab in wave.enemies)
            {
                while (CountAlive() >= maxAlive) yield return new WaitForSeconds(1f);
                Spawn(prefab);
                yield return new WaitForSeconds(wave.delayBetweenSpawns);
            }
        }
        AllWavesSpawned?.Invoke();
    }

    private void Spawn(EnemyAI prefab)
    {
        if (prefab == null) return;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 position = point.position;
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas)) position = hit.position;

        EnemyAI enemy = Instantiate(prefab, position, point.rotation);
        if (territoryZone != null) enemy.SetTerritory(territoryZone.Territory, territoryZone.ZoneId);
        spawned.Add(enemy);

        PlayerTarget player = PlayerTarget.Instance;
        if (player != null) enemy.AlertAsReinforcement(player.transform.position);
        EnemySpawned?.Invoke(enemy);
    }

    public void ResetState()
    {
        StopAllCoroutines();
        foreach (EnemyAI enemy in spawned)
            if (enemy != null) Destroy(enemy.gameObject);
        spawned.Clear();
        started = false;
    }

    private int CountAlive()
    {
        spawned.RemoveAll(e => e == null || e.IsDead);
        return spawned.Count;
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.3f);
        foreach (Transform point in spawnPoints)
            if (point != null) Gizmos.DrawWireSphere(point.position, 0.6f);
    }
}
