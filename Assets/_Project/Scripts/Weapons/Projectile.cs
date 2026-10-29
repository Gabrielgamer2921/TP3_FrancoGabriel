using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    private Rigidbody body;
    private Weapon sourceWeapon;
    private Vector3 origin;
    private Vector3 lastVelocity;
    private float gravityScale;
    private bool consumed;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Launch(Weapon weapon, GameObject owner, Vector3 velocity, float gravityScale, float lifetime)
    {
        sourceWeapon = weapon;
        this.gravityScale = gravityScale;
        origin = transform.position;
        lastVelocity = velocity;
        body.linearVelocity = velocity;

        if (owner != null)
        {
            Collider[] ownColliders = GetComponentsInChildren<Collider>();
            foreach (Collider ownerCollider in owner.GetComponentsInChildren<Collider>())
                foreach (Collider ownCollider in ownColliders)
                    Physics.IgnoreCollision(ownCollider, ownerCollider);
        }

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        body.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
        lastVelocity = body.linearVelocity;
        if (lastVelocity.sqrMagnitude > 0.01f) body.MoveRotation(Quaternion.LookRotation(lastVelocity));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (consumed) return;
        consumed = true;

        ContactPoint contact = collision.GetContact(0);
        if (sourceWeapon != null)
        {
            float distance = Vector3.Distance(origin, contact.point);
            sourceWeapon.ReportProjectileHit(collision.collider, contact.point, contact.normal, lastVelocity.normalized, distance);
        }

        Destroy(gameObject);
    }
}
