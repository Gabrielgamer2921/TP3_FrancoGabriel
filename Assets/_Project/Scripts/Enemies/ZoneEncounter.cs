using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZoneEncounter : MonoBehaviour, IResettable
{
    [Serializable]
    public class Entry
    {
        public EnemyAI prefab;
        public Transform spawn;
        public PatrolRoute route;
        public GameObject keyDrop;
    }

    [SerializeField] private string zoneName = "Zona";
    [SerializeField] private int zoneId;
    [SerializeField] private Vector3 territoryCenter;
    [SerializeField] private Vector3 territorySize = new Vector3(20f, 30f, 20f);
    [SerializeField] private bool activeOnStart;
    [SerializeField] private bool alertOnSpawn;
    [SerializeField] private List<Entry> enemies = new List<Entry>();

    [Header("Refuerzos de la zona")]
    [SerializeField] private List<Entry> reinforcements = new List<Entry>();
    [SerializeField, Range(0f, 1f)] private float reinforceWhenAliveBelow = 0.4f;
    [SerializeField] private float delayBetweenReinforcements = 0.8f;

    [Header("Se reinician junto con la zona")]
    [SerializeField] private List<MonoBehaviour> linkedResettables = new List<MonoBehaviour>();

    private readonly List<EnemyAI> instances = new List<EnemyAI>();
    private bool spawned;
    private bool reinforced;
    private int initialCount;

    public int ZoneId => zoneId;
    public string ZoneName => zoneName;
    public Bounds Territory => new Bounds(territoryCenter, territorySize);
    public bool IsActive => spawned;

    private void Start()
    {
        if (activeOnStart) Activate();
    }

    public void Activate()
    {
        if (spawned) return;
        spawned = true;
        reinforced = false;
        foreach (Entry entry in enemies) Spawn(entry, alertOnSpawn);
        initialCount = Mathf.Max(1, instances.Count);
    }

    public void ResetState()
    {
        StopAllCoroutines();
        foreach (EnemyAI enemy in instances)
            if (enemy != null) Destroy(enemy.gameObject);
        instances.Clear();

        foreach (MonoBehaviour linked in linkedResettables)
            if (linked is IResettable resettable) resettable.ResetState();

        bool wasActive = spawned;
        spawned = false;
        if (wasActive || activeOnStart) Activate();
    }

    private void Update()
    {
        if (!spawned || reinforced || reinforcements.Count == 0) return;

        int alive = 0;
        foreach (EnemyAI enemy in instances)
            if (enemy != null && !enemy.IsDead) alive++;

        if (alive / (float)initialCount > reinforceWhenAliveBelow) return;
        reinforced = true;
        StartCoroutine(SpawnReinforcements());
    }

    private IEnumerator SpawnReinforcements()
    {
        foreach (Entry entry in reinforcements)
        {
            Spawn(entry, true);
            yield return new WaitForSeconds(delayBetweenReinforcements);
        }
    }

    private void Spawn(Entry entry, bool alert)
    {
        if (entry.prefab == null || entry.spawn == null) return;

        Vector3 position = entry.spawn.position;
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas)) position = hit.position;

        EnemyAI enemy = Instantiate(entry.prefab, position, entry.spawn.rotation);
        enemy.name = entry.spawn.name;
        enemy.PatrolRoute = entry.route;
        enemy.SetTerritory(Territory, zoneId);

        if (entry.keyDrop != null)
        {
            KeyCarrier carrier = enemy.GetComponent<KeyCarrier>();
            if (carrier == null) carrier = enemy.gameObject.AddComponent<KeyCarrier>();
            carrier.SetKey(entry.keyDrop);
        }

        instances.Add(enemy);

        PlayerTarget player = PlayerTarget.Instance;
        if (alert && player != null && !player.IsDead) enemy.AlertAsReinforcement(player.transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.15f);
        Gizmos.DrawCube(territoryCenter, territorySize);
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
        Gizmos.DrawWireCube(territoryCenter, territorySize);
        foreach (Entry entry in enemies)
            if (entry.spawn != null) Gizmos.DrawWireCube(entry.spawn.position + Vector3.up, new Vector3(0.6f, 2f, 0.6f));
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        foreach (Entry entry in reinforcements)
            if (entry.spawn != null) Gizmos.DrawWireCube(entry.spawn.position + Vector3.up, new Vector3(0.6f, 2f, 0.6f));
    }
}
