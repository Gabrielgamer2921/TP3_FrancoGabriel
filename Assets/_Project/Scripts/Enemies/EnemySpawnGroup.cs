using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnGroup : MonoBehaviour, IResettable
{
    [Serializable]
    public class SpawnEntry
    {
        public EnemyAI prefab;
        public Transform point;
    }

    [SerializeField] private List<SpawnEntry> entries = new List<SpawnEntry>();
    [SerializeField] private PlayerZoneTrigger activationZone;
    [SerializeField] private bool armedOnStart;
    [SerializeField] private bool alertOnSpawn = true;
    [SerializeField] private float delayBetweenSpawns = 0.4f;
    [SerializeField] private ZoneEncounter territoryZone;

    private readonly List<EnemyAI> spawned = new List<EnemyAI>();
    private bool armed;
    private bool activated;
    private bool finishedSpawning;

    public bool IsActivated => activated;
    public bool IsCleared => finishedSpawning && CountAlive() == 0;

    public event Action Activated;
    public event Action Cleared;

    private void Start()
    {
        if (armedOnStart) Arm();
    }

    private void OnEnable()
    {
        if (activationZone != null) activationZone.Entered += OnZoneEntered;
    }

    private void OnDisable()
    {
        if (activationZone != null) activationZone.Entered -= OnZoneEntered;
    }

    public void Arm()
    {
        armed = true;
        if (activationZone == null) return;
        if (activationZone.PlayerInside) Activate();
        else activationZone.ResetTrigger();
    }

    public void Activate()
    {
        if (activated) return;
        activated = true;
        Activated?.Invoke();
        StartCoroutine(SpawnRoutine());
    }

    public void ResetState()
    {
        StopAllCoroutines();
        foreach (EnemyAI enemy in spawned)
            if (enemy != null) Destroy(enemy.gameObject);
        spawned.Clear();
        activated = false;
        finishedSpawning = false;
        if (armed || armedOnStart)
        {
            armed = false;
            Arm();
        }
    }

    private void OnZoneEntered()
    {
        if (armed) Activate();
    }

    private IEnumerator SpawnRoutine()
    {
        foreach (SpawnEntry entry in entries)
        {
            if (entry.prefab == null || entry.point == null) continue;

            Vector3 position = entry.point.position;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas)) position = hit.position;

            EnemyAI enemy = Instantiate(entry.prefab, position, entry.point.rotation);
            if (territoryZone != null) enemy.SetTerritory(territoryZone.Territory, territoryZone.ZoneId);
            spawned.Add(enemy);

            PlayerTarget player = PlayerTarget.Instance;
            if (alertOnSpawn && player != null) enemy.AlertAsReinforcement(player.transform.position);

            yield return new WaitForSeconds(delayBetweenSpawns);
        }

        finishedSpawning = true;
        while (CountAlive() > 0) yield return new WaitForSeconds(0.5f);
        Cleared?.Invoke();
    }

    private int CountAlive()
    {
        spawned.RemoveAll(enemy => enemy == null || enemy.IsDead);
        return spawned.Count;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.1f);
        foreach (SpawnEntry entry in entries)
            if (entry.point != null) Gizmos.DrawWireCube(entry.point.position + Vector3.up, new Vector3(0.6f, 2f, 0.6f));
    }
}
