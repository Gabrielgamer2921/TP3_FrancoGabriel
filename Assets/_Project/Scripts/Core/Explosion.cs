using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Explosion
{
    private static readonly Collider[] Buffer = new Collider[128];
    private static readonly HashSet<IDamageable> Damaged = new HashSet<IDamageable>();
    private static readonly HashSet<Rigidbody> Pushed = new HashSet<Rigidbody>();

    public static event Action<Vector3, float, float> Exploded;

    public static void Detonate(Vector3 position, ExplosionDataSO data, GameObject source)
    {
        if (data == null) return;

        if (data.effectPrefab != null)
            Object.Instantiate(data.effectPrefab, position, Quaternion.identity).Play(data.radius);

        Damaged.Clear();
        Pushed.Clear();

        int count = Physics.OverlapSphereNonAlloc(position, data.radius, Buffer, data.affectedLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider target = Buffer[i];
            if (target == null) continue;

            Vector3 closest = target.ClosestPoint(position);
            float falloff = Mathf.Clamp01(Vector3.Distance(position, closest) / data.radius);

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable is Hitbox) damageable = null;

            if (damageable != null && Damaged.Add(damageable))
            {
                Vector3 center = target.bounds.center;
                float amount = Mathf.Lerp(data.maxDamage, data.minDamage, falloff);
                Component owner = damageable as Component;
                Transform ignore = owner != null ? owner.transform : target.transform;
                if (LineOfSight.IsBlocked(position + Vector3.up * 0.2f, center, ignore, data.affectedLayers))
                    amount *= data.coverDamageMultiplier;

                Vector3 direction = (center - position).normalized;
                damageable.TakeDamage(new DamageInfo(amount, closest, direction, source, DamageType.Explosion));
            }

            Rigidbody body = target.attachedRigidbody;
            if (body != null && !body.isKinematic && Pushed.Add(body))
                body.AddExplosionForce(data.pushVelocity * (1f - falloff * 0.5f), position, data.radius, data.upwardsModifier, ForceMode.VelocityChange);
        }

        NoiseSystem.Emit(position, data.noiseRadius, source);
        Exploded?.Invoke(position, data.radius, data.cameraShake);
    }
}
