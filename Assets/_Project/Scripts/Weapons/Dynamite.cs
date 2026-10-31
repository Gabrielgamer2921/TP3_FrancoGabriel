using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Dynamite : MonoBehaviour, IDamageable
{
    private Rigidbody body;
    private ExplosionDataSO explosion;
    private GameObject source;
    private float explodeAt;
    private bool armed;
    private bool exploded;

    public float RemainingFuse => armed ? explodeAt - Time.time : 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Arm(ExplosionDataSO explosion, GameObject source, float fuse, Vector3 velocity)
    {
        this.explosion = explosion;
        this.source = source;
        explodeAt = Time.time + fuse;
        armed = true;

        body.linearVelocity = velocity;
        body.angularVelocity = Random.insideUnitSphere * 10f;

        if (source == null) return;
        Collider[] own = GetComponentsInChildren<Collider>();
        foreach (Collider other in source.GetComponentsInChildren<Collider>())
            foreach (Collider mine in own)
                Physics.IgnoreCollision(mine, other);
    }

    private void Update()
    {
        if (armed && Time.time >= explodeAt) Explode();
    }

    public void TakeDamage(DamageInfo info)
    {
        if (!armed) return;
        if (info.source != null) source = info.source;
        Explode();
    }

    public void Explode()
    {
        if (exploded) return;
        exploded = true;
        Explosion.Detonate(transform.position, explosion, source);
        Destroy(gameObject);
    }
}
