using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public enum Phase
    {
        Always,
        IngressOnly,
        EscapeOnly
    }

    [SerializeField] private string displayName = "Punto de control";
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PlayerZoneTrigger zone;
    [SerializeField] private Phase activeDuring = Phase.Always;
    [SerializeField] private List<MonoBehaviour> resetOnRespawn = new List<MonoBehaviour>();
    [SerializeField] private List<ZoneEncounter> activateOnReach = new List<ZoneEncounter>();

    public string DisplayName => displayName;
    public Phase ActiveDuring => activeDuring;
    public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
    public Quaternion SpawnRotation => spawnPoint != null ? spawnPoint.rotation : transform.rotation;

    private void Awake()
    {
        if (zone != null) zone.TriggerOnce = false;
    }

    private void OnEnable()
    {
        if (zone != null) zone.Entered += OnEntered;
    }

    private void OnDisable()
    {
        if (zone != null) zone.Entered -= OnEntered;
    }

    public void OnReached()
    {
        foreach (ZoneEncounter encounter in activateOnReach)
            if (encounter != null) encounter.Activate();
    }

    public void ResetEncounters()
    {
        foreach (MonoBehaviour target in resetOnRespawn)
            if (target is IResettable resettable) resettable.ResetState();
    }

    private void OnEntered()
    {
        if (CheckpointManager.Instance != null) CheckpointManager.Instance.Activate(this, true);
    }

    private void OnDrawGizmos()
    {
        Vector3 position = SpawnPosition;
        Gizmos.color = activeDuring == Phase.EscapeOnly ? new Color(1f, 0.4f, 0.3f) : new Color(0.3f, 0.6f, 1f);
        Gizmos.DrawLine(position, position + Vector3.up * 2.2f);
        Gizmos.DrawCube(position + Vector3.up * 1.95f + SpawnRotation * Vector3.forward * 0.3f, new Vector3(0.05f, 0.4f, 0.6f));
        Gizmos.DrawWireSphere(position, 0.4f);
    }
}
