using UnityEngine;

public struct WeaponHit
{
    public Vector3 point;
    public Vector3 normal;
    public Collider collider;
    public bool damaged;
    public bool critical;
}
