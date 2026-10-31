using UnityEngine;

public class PatrolRoute : MonoBehaviour
{
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f);

    public int Count => transform.childCount;

    public Vector3 GetPoint(int index)
    {
        return transform.GetChild(Mathf.Clamp(index, 0, Count - 1)).position;
    }

    public int GetClosestIndex(Vector3 position)
    {
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < Count; i++)
        {
            float distance = (GetPoint(i) - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }
        return best;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        for (int i = 0; i < Count; i++)
        {
            Vector3 point = GetPoint(i);
            Gizmos.DrawWireSphere(point, 0.3f);
            Gizmos.DrawLine(point, GetPoint((i + 1) % Count));
        }
    }
}
