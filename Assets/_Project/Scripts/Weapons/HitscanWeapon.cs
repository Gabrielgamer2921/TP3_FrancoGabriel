using UnityEngine;

public class HitscanWeapon : Weapon
{
    protected override void Shoot(Vector3 origin, Vector3 direction)
    {
        int pellets = Mathf.Max(1, data.pelletsPerShot);
        for (int i = 0; i < pellets; i++)
        {
            Vector3 pelletDirection = pellets > 1 ? ApplySpread(direction, data.pelletSpread) : direction;
            FirePellet(origin, pelletDirection);
        }
    }

    private void FirePellet(Vector3 origin, Vector3 direction)
    {
        Vector3 start = muzzle != null ? muzzle.position : origin;
        bool hasHit = Physics.Raycast(origin, direction, out RaycastHit hit, data.range, data.hitMask, QueryTriggerInteraction.Ignore);
        Vector3 end = hasHit ? hit.point : origin + direction * data.range;

        RaiseShotTraced(start, end);

        bool useVisualBullet = data.visualBulletPrefab != null && data.visualBulletSpeed > 0f;
        bool delayHit = useVisualBullet && data.delayHitUntilBulletArrives;

        if (hasHit && !delayHit) ApplyHit(hit.collider, hit.point, hit.normal, direction, hit.distance);

        if (!useVisualBullet) return;

        VisualBullet bullet = Instantiate(data.visualBulletPrefab, start, Quaternion.identity);
        if (hasHit && delayHit)
        {
            Collider hitCollider = hit.collider;
            Vector3 point = hit.point;
            Vector3 normal = hit.normal;
            float distance = hit.distance;
            bullet.Launch(start, end, data.visualBulletSpeed, () => ApplyHit(hitCollider, point, normal, direction, distance));
        }
        else
        {
            bullet.Launch(start, end, data.visualBulletSpeed, null);
        }
    }
}
