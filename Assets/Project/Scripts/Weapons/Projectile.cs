using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private bool stickOnImpact;
    [SerializeField] private float stuckLifetime = 10f;
    [SerializeField] private float penetration = 0.12f;
    [SerializeField] private float trailStartDelay = 0.035f;

    private Rigidbody body;
    private float launchTime;
    private float gravityDelay;
    private Weapon sourceWeapon;
    private Vector3 origin;
    private Vector3 lastVelocity;
    private float gravityScale;
    private bool consumed;
    private bool trailStarted;
    private TrailRenderer[] trails;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        trails = GetComponentsInChildren<TrailRenderer>();
    }

    public void Launch(Weapon weapon, GameObject owner, Vector3 velocity, float gravityScale, float lifetime, float gravityDelay = 0f)
    {
        sourceWeapon = weapon;
        launchTime = Time.time;
        this.gravityDelay = gravityDelay;
        this.gravityScale = gravityScale;
        origin = transform.position;
        lastVelocity = velocity;
        body.linearVelocity = velocity;
        SetTrails(false);

        if (owner != null)
        {
            Collider[] ownColliders = GetComponentsInChildren<Collider>();
            foreach (Collider ownerCollider in owner.GetComponentsInChildren<Collider>())
                foreach (Collider ownCollider in ownColliders)
                    Physics.IgnoreCollision(ownCollider, ownerCollider);
        }

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (consumed || trailStarted || Time.time - launchTime < trailStartDelay) return;
        trailStarted = true;
        SetTrails(true);
    }

    private void SetTrails(bool emitting)
    {
        foreach (TrailRenderer trail in trails)
        {
            if (!emitting) trail.Clear();
            trail.emitting = emitting;
        }
    }

    private void FixedUpdate()
    {
        if (consumed) return;
        if (Time.time - launchTime >= gravityDelay) body.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
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

        if (stickOnImpact) Stick(collision, contact.point);
        else Destroy(gameObject);
    }

    private void Stick(Collision collision, Vector3 point)
    {
        Vector3 direction = lastVelocity.sqrMagnitude > 0.01f ? lastVelocity.normalized : transform.forward;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        foreach (Collider own in GetComponentsInChildren<Collider>()) own.enabled = false;
        trailStarted = true;
        foreach (TrailRenderer trail in trails) trail.emitting = false;

        transform.SetPositionAndRotation(point + direction * penetration - direction * 0.36f, Quaternion.LookRotation(direction));

        Transform anchor = collision.rigidbody != null ? collision.rigidbody.transform : null;
        if (anchor != null && Mathf.Abs(anchor.lossyScale.x - anchor.lossyScale.y) < 0.01f && Mathf.Abs(anchor.lossyScale.y - anchor.lossyScale.z) < 0.01f)
            transform.SetParent(anchor, true);

        Destroy(gameObject, stuckLifetime);
        StartCoroutine(Wobble());
    }

    private System.Collections.IEnumerator Wobble()
    {
        Quaternion baseRotation = transform.localRotation;
        Vector3 axis = Vector3.Cross(Vector3.forward, Random.onUnitSphere).normalized;
        const float duration = 0.45f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float angle = Mathf.Sin(t * Mathf.PI * 7f) * 5f * (1f - t);
            transform.localRotation = baseRotation * Quaternion.AngleAxis(angle, axis);
            yield return null;
        }
        transform.localRotation = baseRotation;
    }
}
