using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyWeapon : MonoBehaviour
{
    [SerializeField] private Transform muzzle;
    [SerializeField] private VisualBullet visualBulletPrefab;
    [SerializeField] private LayerMask hitMask = ~0;

    private EnemyDataSO data;
    private int ammo;
    private float nextFireTime;

    public bool IsReloading { get; private set; }
    public bool CanFire => !IsReloading && Time.time >= nextFireTime && ammo > 0;
    public bool NeedsReload => ammo <= 0 && !IsReloading;
    public bool MagazineLow => ammo <= Mathf.CeilToInt(data.magazineSize * 0.3f);
    public Transform Muzzle => muzzle;

    public event Action Fired;
    public event Action ReloadStarted;

    public void Initialize(EnemyDataSO data)
    {
        this.data = data;
        ammo = data.magazineSize;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        IsReloading = false;
    }

    public void StartReload()
    {
        if (IsReloading || ammo >= data.magazineSize || !isActiveAndEnabled) return;
        StartCoroutine(ReloadRoutine());
    }

    public bool TryFire(Vector3 target, float spread)
    {
        if (!CanFire) return false;

        ammo--;
        nextFireTime = Time.time + data.timeBetweenShots;

        Vector3 origin = muzzle.position;
        Vector3 aimDirection = (target - origin).normalized;
        int pellets = Mathf.Max(1, data.pelletsPerShot);

        for (int i = 0; i < pellets; i++)
        {
            float pelletSpread = spread + (pellets > 1 ? data.pelletSpread : 0f);
            FirePellet(origin, ApplySpread(aimDirection, pelletSpread));
        }

        NoiseSystem.Emit(origin, data.shotNoiseRadius, gameObject);
        Fired?.Invoke();

        if (ammo <= 0) StartReload();
        return true;
    }

    private void FirePellet(Vector3 origin, Vector3 direction)
    {
        float maxDistance = data.attackRange * 1.5f;
        bool hasHit = LineOfSight.TryGetFirstHit(origin, direction, maxDistance, transform, hitMask, out RaycastHit hit);
        Vector3 end = hasHit ? hit.point : origin + direction * maxDistance;

        if (visualBulletPrefab == null)
        {
            if (hasHit) ApplyHit(hit.collider, hit.point, direction, hit.distance);
            return;
        }

        VisualBullet bullet = Instantiate(visualBulletPrefab, origin, Quaternion.identity);
        if (hasHit)
        {
            Collider hitCollider = hit.collider;
            Vector3 point = hit.point;
            float distance = hit.distance;
            bullet.Launch(origin, end, data.bulletSpeed, () => ApplyHit(hitCollider, point, direction, distance));
        }
        else
        {
            bullet.Launch(origin, end, data.bulletSpeed, null);
        }
    }

    private void ApplyHit(Collider hitCollider, Vector3 point, Vector3 direction, float distance)
    {
        if (hitCollider == null) return;
        if (hitCollider.GetComponentInParent<EnemyAI>() != null) return;

        float falloff = 1f;
        if (distance > data.falloffStartDistance)
        {
            float range = Mathf.Max(0.01f, data.falloffEndDistance - data.falloffStartDistance);
            falloff = Mathf.Lerp(1f, data.minFalloffMultiplier, (distance - data.falloffStartDistance) / range);
        }

        IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(new DamageInfo(data.damage * falloff, point, direction, gameObject, DamageType.Bullet));

        Rigidbody body = hitCollider.attachedRigidbody;
        if (body != null && !body.isKinematic) body.AddForceAtPosition(direction * 3f, point, ForceMode.Impulse);
    }

    private static Vector3 ApplySpread(Vector3 direction, float spread)
    {
        if (spread <= 0f) return direction;
        Vector3 right = Vector3.Cross(direction, Vector3.up);
        if (right.sqrMagnitude < 0.001f) right = Vector3.right;
        right.Normalize();
        Vector3 up = Vector3.Cross(right, direction);
        Vector2 offset = Random.insideUnitCircle * Mathf.Tan(spread * Mathf.Deg2Rad);
        return (direction + right * offset.x + up * offset.y).normalized;
    }

    private IEnumerator ReloadRoutine()
    {
        IsReloading = true;
        ReloadStarted?.Invoke();
        yield return new WaitForSeconds(data.reloadTime);
        ammo = data.magazineSize;
        IsReloading = false;
    }
}
