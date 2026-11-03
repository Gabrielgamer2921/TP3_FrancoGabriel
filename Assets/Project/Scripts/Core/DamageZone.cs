using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageZone : MonoBehaviour
{
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private float tickInterval = 0.5f;

    private readonly List<Collider> inside = new List<Collider>();
    private readonly HashSet<IDamageable> damagedThisTick = new HashSet<IDamageable>();
    private float nextTick;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!inside.Contains(other)) inside.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        inside.Remove(other);
    }

    private void Update()
    {
        if (Time.time < nextTick) return;
        nextTick = Time.time + tickInterval;

        inside.RemoveAll(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        damagedThisTick.Clear();

        foreach (Collider other in inside)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || !damagedThisTick.Add(damageable)) continue;

            Vector3 point = other.ClosestPoint(transform.position);
            damageable.TakeDamage(new DamageInfo(damagePerSecond * tickInterval, point, Vector3.up, gameObject, DamageType.Environment));
        }
    }
}
