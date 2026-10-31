using UnityEngine;

public class EnemyPerception : MonoBehaviour
{
    [SerializeField] private Transform eyes;
    [SerializeField] private LayerMask obstacleMask = ~0;

    private EnemyDataSO data;

    public Transform Eyes => eyes;
    public bool CanSeePlayer { get; private set; }
    public float Awareness { get; private set; }
    public Vector3 LastSeenPosition { get; private set; }

    public void Initialize(EnemyDataSO data)
    {
        this.data = data;
    }

    public void Tick(bool alerted)
    {
        PlayerTarget player = PlayerTarget.Instance;
        if (player == null || player.IsDead)
        {
            CanSeePlayer = false;
            Awareness = Mathf.MoveTowards(Awareness, 0f, data.awarenessDecay * Time.deltaTime);
            return;
        }

        CanSeePlayer = CheckVision(player, alerted, out float distance);

        if (CanSeePlayer)
        {
            LastSeenPosition = player.transform.position;
            if (alerted)
            {
                Awareness = 1f;
            }
            else
            {
                float distanceFactor = Mathf.Lerp(0.35f, 1.6f, distance / data.viewDistance);
                if (player.IsCrouching) distanceFactor *= 1.5f;
                Awareness = Mathf.Min(1f, Awareness + Time.deltaTime / (data.detectionTime * distanceFactor));
            }
        }
        else if (!alerted)
        {
            Awareness = Mathf.MoveTowards(Awareness, 0f, data.awarenessDecay * Time.deltaTime);
        }
    }

    public void SetAwareness(float value)
    {
        Awareness = Mathf.Clamp01(value);
    }

    public bool HasLineOfSightTo(Vector3 point)
    {
        return !LineOfSight.IsBlocked(eyes.position, point, transform, obstacleMask);
    }

    private bool CheckVision(PlayerTarget player, bool alerted, out float distance)
    {
        Vector3 toPlayer = player.AimPoint - eyes.position;
        distance = toPlayer.magnitude;

        float range = alerted ? data.viewDistance * 1.5f : data.viewDistance;
        if (distance > range) return false;

        if (!alerted && distance > data.closeAwarenessRange)
        {
            Vector3 flat = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
            if (Vector3.Angle(transform.forward, flat) > data.viewAngle * 0.5f) return false;
        }

        return HasLineOfSightTo(player.AimPoint) || HasLineOfSightTo(player.HeadPoint);
    }

    private void OnDrawGizmosSelected()
    {
        if (eyes == null || data == null) return;
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
        Vector3 left = Quaternion.Euler(0f, -data.viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, data.viewAngle * 0.5f, 0f) * transform.forward;
        Gizmos.DrawLine(eyes.position, eyes.position + left * data.viewDistance);
        Gizmos.DrawLine(eyes.position, eyes.position + right * data.viewDistance);
    }
}
