using UnityEngine;

public static class LineOfSight
{
    private static readonly RaycastHit[] Buffer = new RaycastHit[16];

    public static bool IsBlocked(Vector3 from, Vector3 to, Transform ignore, LayerMask mask)
    {
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.01f) return false;

        int count = Physics.RaycastNonAlloc(from, delta / distance, Buffer, distance, mask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (IsObstacle(Buffer[i].collider, ignore)) return true;
        }
        return false;
    }

    public static bool TryGetFirstHit(Vector3 origin, Vector3 direction, float maxDistance, Transform ignore, LayerMask mask, out RaycastHit hit)
    {
        int count = Physics.RaycastNonAlloc(origin, direction, Buffer, maxDistance, mask, QueryTriggerInteraction.Ignore);
        hit = default;
        float best = float.MaxValue;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            Collider c = Buffer[i].collider;
            if (ignore != null && c.transform.IsChildOf(ignore)) continue;
            if (Buffer[i].distance < best)
            {
                best = Buffer[i].distance;
                hit = Buffer[i];
                found = true;
            }
        }
        return found;
    }

    private static bool IsObstacle(Collider collider, Transform ignore)
    {
        if (ignore != null && collider.transform.IsChildOf(ignore)) return false;
        if (collider.GetComponentInParent<PlayerTarget>() != null) return false;
        if (collider.GetComponentInParent<EnemyAI>() != null) return false;
        return true;
    }
}
