using UnityEngine;

public enum DamageType
{
    Bullet,
    Melee,
    Explosion
}

public struct DamageInfo
{
    public float amount;
    public Vector3 point;
    public Vector3 direction;
    public GameObject source;
    public DamageType type;
    public bool isCritical;

    public DamageInfo(float amount, Vector3 point, Vector3 direction, GameObject source, DamageType type)
    {
        this.amount = amount;
        this.point = point;
        this.direction = direction;
        this.source = source;
        this.type = type;
        isCritical = false;
    }
}

public interface IDamageable
{
    void TakeDamage(DamageInfo info);
}
