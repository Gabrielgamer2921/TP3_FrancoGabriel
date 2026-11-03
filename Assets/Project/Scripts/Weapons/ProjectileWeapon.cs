using UnityEngine;

public class ProjectileWeapon : Weapon
{
    protected override void Shoot(Vector3 origin, Vector3 direction)
    {
        Vector3 spawnPoint = muzzle != null ? muzzle.position : origin;
        float muzzleDistance = Vector3.Distance(origin, spawnPoint);
        Vector3 target = origin + direction * data.range;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, data.range, data.hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.distance <= muzzleDistance + 0.5f)
            {
                ApplyHit(hit.collider, hit.point, hit.normal, direction, hit.distance);
                return;
            }
            target = hit.point;
        }

        Vector3 shotDirection = (target - spawnPoint).normalized;
        if (Vector3.Dot(shotDirection, direction) < 0.5f) shotDirection = direction;

        if (data.projectilePrefab == null)
        {
            Debug.LogWarning($"{name}: falta asignar el Projectile Prefab en {data.name}.");
            return;
        }

        Vector3 velocity = shotDirection * data.projectileSpeed;
        float zeroedDistance = Mathf.Min(Vector3.Distance(spawnPoint, target), data.zeroDistance);
        float flightTime = zeroedDistance / data.projectileSpeed;
        velocity -= Physics.gravity * data.projectileGravityScale * flightTime * 0.5f;

        Projectile projectile = Instantiate(data.projectilePrefab, spawnPoint, Quaternion.LookRotation(velocity));
        projectile.Launch(this, owner, velocity, data.projectileGravityScale, data.projectileLifetime);
    }
}
