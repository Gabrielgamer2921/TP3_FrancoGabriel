using System.Collections.Generic;
using UnityEngine;

public class EnemyDynamiteThrower : MonoBehaviour
{
    private EnemyDataSO data;
    private Transform hand;
    private int remaining;
    private float nextThrowTime;

    public int Remaining => remaining;

    public void Initialize(EnemyDataSO data, Transform hand)
    {
        this.data = data;
        this.hand = hand;
        remaining = data.dynamiteCount;
        nextThrowTime = Time.time + data.dynamiteCooldown * 0.5f;
    }

    public bool TryThrow(Vector3 target, float hiddenTime, IReadOnlyList<EnemyAI> allies)
    {
        if (data == null || data.throwable == null || remaining <= 0) return false;
        if (Time.time < nextThrowTime || hiddenTime < data.hiddenTimeBeforeThrow) return false;

        Vector3 flat = target - transform.position;
        flat.y = 0f;
        float distance = flat.magnitude;
        if (distance < data.dynamiteMinDistance || distance > data.dynamiteMaxDistance) return false;

        foreach (EnemyAI ally in allies)
        {
            if (ally == null || ally.IsDead || ally.gameObject == gameObject) continue;
            if (Vector3.Distance(ally.transform.position, target) < data.throwable.explosion.radius) return false;
        }

        Throw(target, distance);
        return true;
    }

    private void Throw(Vector3 target, float distance)
    {
        remaining--;
        nextThrowTime = Time.time + data.dynamiteCooldown;

        Vector3 origin = hand != null ? hand.position : transform.position + Vector3.up * 1.6f;
        float flightTime = Mathf.Clamp(distance / 12f, 0.7f, 1.6f);
        Vector3 velocity = (target - origin - 0.5f * Physics.gravity * flightTime * flightTime) / flightTime;

        Dynamite dynamite = Instantiate(data.throwable.prefab, origin, Quaternion.LookRotation(velocity));
        dynamite.Arm(data.throwable.explosion, gameObject, flightTime + data.fuseAfterLanding, velocity);
    }
}
