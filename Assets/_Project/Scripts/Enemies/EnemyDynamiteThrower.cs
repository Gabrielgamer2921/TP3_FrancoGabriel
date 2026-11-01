using System.Collections.Generic;
using UnityEngine;

public class EnemyDynamiteThrower : MonoBehaviour
{
    private EnemyDataSO data;
    private Transform hand;
    private int remaining;
    private float nextThrowTime;
    private float nextRollTime;

    public int Remaining => remaining;
    public int Level { get; private set; }
    public int LastRoll { get; private set; }

    public void Initialize(EnemyDataSO data, Transform hand)
    {
        this.data = data;
        this.hand = hand;
        remaining = data.dynamiteCount;
        Level = Random.Range(Mathf.Min(data.dynamiteLevelMin, data.dynamiteLevelMax), Mathf.Max(data.dynamiteLevelMin, data.dynamiteLevelMax) + 1);
        nextRollTime = Time.time + data.dynamiteRollInterval;
    }

    public bool TryThrow(Vector3 target, bool playerHidden, float hiddenTime, IReadOnlyList<EnemyAI> allies)
    {
        if (data == null || data.throwable == null || remaining <= 0) return false;
        if (Time.time < nextRollTime || Time.time < nextThrowTime) return false;

        Vector3 flat = target - transform.position;
        flat.y = 0f;
        float distance = flat.magnitude;
        if (distance < data.dynamiteMinDistance || distance > data.dynamiteMaxDistance) return false;

        nextRollTime = Time.time + data.dynamiteRollInterval * Random.Range(0.8f, 1.2f);

        bool hiding = playerHidden && hiddenTime >= data.hiddenTimeBeforeThrow;
        int effectiveLevel = Mathf.Min(20, Level + (hiding ? data.hiddenLevelBonus : 0));
        LastRoll = Random.Range(1, 21);
        bool success = LastRoll <= effectiveLevel;

        if (data.logDynamiteRolls)
        {
            string result = success ? "¡LANZA DINAMITA!" : "no lanza";
            Debug.Log($"{name} tiró d20: {LastRoll} (necesita {effectiveLevel} o menos{(hiding ? ", bonus por escondida" : "")}) → {result}");
        }

        if (!success) return false;

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
