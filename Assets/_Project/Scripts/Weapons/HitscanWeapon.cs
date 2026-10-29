using UnityEngine;

public class HitscanWeapon : Weapon
{
    protected override void Shoot(Vector3 origin, Vector3 direction)
    {
        Vector3 traceStart = muzzle != null ? muzzle.position : origin;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, data.range, data.hitMask, QueryTriggerInteraction.Ignore))
        {
            ApplyHit(hit.collider, hit.point, hit.normal, direction, hit.distance);
            RaiseShotTraced(traceStart, hit.point);
        }
        else
        {
            RaiseShotTraced(traceStart, origin + direction * data.range);
        }
    }
}
