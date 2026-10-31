using System.Collections.Generic;
using UnityEngine;

public class CoverPoint : MonoBehaviour
{
    [SerializeField] private float protectionHeight = 0.9f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    public static readonly List<CoverPoint> All = new List<CoverPoint>();

    public EnemyAI Occupant { get; private set; }
    public Vector3 Position => transform.position;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    public bool IsAvailableFor(EnemyAI enemy) => Occupant == null || Occupant == enemy;

    public bool TryOccupy(EnemyAI enemy)
    {
        if (!IsAvailableFor(enemy)) return false;
        Occupant = enemy;
        return true;
    }

    public void Release(EnemyAI enemy)
    {
        if (Occupant == enemy) Occupant = null;
    }

    public bool ProtectsFrom(Vector3 threat)
    {
        return LineOfSight.IsBlocked(transform.position + Vector3.up * protectionHeight, threat, null, obstacleMask);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Occupant != null ? new Color(1f, 0.4f, 0.2f) : new Color(0.3f, 1f, 0.4f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.45f, new Vector3(0.4f, 0.9f, 0.4f));
    }
}
