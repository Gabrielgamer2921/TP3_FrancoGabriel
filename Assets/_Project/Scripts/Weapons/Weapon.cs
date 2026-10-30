using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class Weapon : MonoBehaviour
{
    [SerializeField] protected WeaponDataSO data;
    [SerializeField] protected Transform muzzle;
    [SerializeField] private Transform aimPoint;
    [SerializeField] private float aimDistanceFromCamera = 0.3f;

    protected GameObject owner;
    protected Camera aimCamera;
    protected PlayerAmmoInventory ammo;
    private PlayerLook look;

    private float nextFireTime;
    private Coroutine reloadRoutine;
    private bool reloadCancelRequested;

    public WeaponDataSO Data => data;
    public Transform Muzzle => muzzle;
    public int CurrentMagazine { get; private set; }
    public int Reserve => ammo != null ? ammo.GetReserve(data.ammoType) : 0;
    public bool IsReloading { get; private set; }
    public Vector3 HipLocalPosition { get; private set; }
    public Vector3 AimLocalPosition { get; private set; }

    public event Action Fired;
    public event Action DryFired;
    public event Action AmmoChanged;
    public event Action ReloadStarted;
    public event Action RoundInserted;
    public event Action ReloadFinished;
    public event Action<WeaponHit> Hit;
    public event Action<Vector3, Vector3> ShotTraced;

    public void Initialize(GameObject owner, Camera aimCamera, PlayerAmmoInventory ammo, PlayerLook look)
    {
        this.owner = owner;
        this.aimCamera = aimCamera;
        this.ammo = ammo;
        this.look = look;

        transform.localRotation = Quaternion.identity;
        HipLocalPosition = transform.localPosition;
        CurrentMagazine = Mathf.Min(data.startingMagazine, data.magazineSize);

        if (aimPoint != null && transform.parent != null)
        {
            Vector3 aimPointInParent = transform.parent.InverseTransformPoint(aimPoint.position);
            Vector3 offset = aimPointInParent - transform.localPosition;
            AimLocalPosition = new Vector3(0f, 0f, aimDistanceFromCamera) - offset;
        }
        else
        {
            AimLocalPosition = new Vector3(0f, HipLocalPosition.y * 0.5f, HipLocalPosition.z);
        }
    }

    private void OnDisable()
    {
        StopReloadImmediate();
    }

    public void OnEquipped()
    {
        nextFireTime = Mathf.Max(nextFireTime, Time.time + data.equipTime);
        AmmoChanged?.Invoke();
    }

    public bool TryFire(float spread)
    {
        if (IsReloading)
        {
            if (data.canInterruptReload && CurrentMagazine > 0) reloadCancelRequested = true;
            return false;
        }

        if (Time.time < nextFireTime) return false;

        if (CurrentMagazine <= 0)
        {
            nextFireTime = Time.time + data.timeBetweenShots * 0.5f;
            DryFired?.Invoke();
            TryStartReload();
            return false;
        }

        CurrentMagazine--;
        nextFireTime = Time.time + data.timeBetweenShots;

        Vector3 origin = aimCamera.transform.position;
        Vector3 direction = ApplySpread(aimCamera.transform.forward, spread);
        Shoot(origin, direction);

        look.AddRecoil(data.recoilVertical, Random.Range(-data.recoilHorizontal, data.recoilHorizontal));
        Fired?.Invoke();
        AmmoChanged?.Invoke();
        return true;
    }

    public bool TryStartReload()
    {
        if (IsReloading || !isActiveAndEnabled) return false;
        if (CurrentMagazine >= data.magazineSize || Reserve <= 0) return false;

        reloadRoutine = StartCoroutine(ReloadRoutine());
        return true;
    }

    public void StopReloadImmediate()
    {
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = null;
        if (IsReloading)
        {
            IsReloading = false;
            ReloadFinished?.Invoke();
        }
    }

    public void AddToMagazine(int amount)
    {
        CurrentMagazine = Mathf.Clamp(CurrentMagazine + amount, 0, data.magazineSize);
        AmmoChanged?.Invoke();
    }

    public void ReportProjectileHit(Collider hitCollider, Vector3 point, Vector3 normal, Vector3 direction, float distance)
    {
        ApplyHit(hitCollider, point, normal, direction, distance);
    }

    protected abstract void Shoot(Vector3 origin, Vector3 direction);

    protected void ApplyHit(Collider hitCollider, Vector3 point, Vector3 normal, Vector3 direction, float distance)
    {
        if (hitCollider == null) return;
        if (owner != null && hitCollider.transform.IsChildOf(owner.transform)) return;

        float falloff = data.damageFalloff.Evaluate(Mathf.Clamp01(distance / data.range));
        WeaponHit result = new WeaponHit { point = point, normal = normal, collider = hitCollider };

        IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(new DamageInfo(data.damage * falloff, point, direction, owner, DamageType.Bullet));
            result.damaged = true;
            Hitbox hitbox = hitCollider.GetComponent<Hitbox>();
            result.critical = hitbox != null && hitbox.IsCritical;
        }

        Rigidbody body = hitCollider.attachedRigidbody;
        if (body != null && !body.isKinematic) body.AddForceAtPosition(direction * data.impactForce, point, ForceMode.Impulse);

        Hit?.Invoke(result);
    }

    protected void RaiseShotTraced(Vector3 from, Vector3 to) => ShotTraced?.Invoke(from, to);

    private Vector3 ApplySpread(Vector3 forward, float spreadAngle)
    {
        if (spreadAngle <= 0f) return forward;
        Vector2 offset = Random.insideUnitCircle * Mathf.Tan(spreadAngle * Mathf.Deg2Rad);
        Transform cam = aimCamera.transform;
        return (forward + cam.right * offset.x + cam.up * offset.y).normalized;
    }

    private IEnumerator ReloadRoutine()
    {
        IsReloading = true;
        reloadCancelRequested = false;
        ReloadStarted?.Invoke();

        yield return new WaitForSeconds(data.reloadStartTime);

        while (CurrentMagazine < data.magazineSize && Reserve > 0 && !reloadCancelRequested)
        {
            yield return new WaitForSeconds(data.reloadPerRoundTime);
            if (ammo.Take(data.ammoType, 1) > 0)
            {
                CurrentMagazine++;
                RoundInserted?.Invoke();
                AmmoChanged?.Invoke();
            }
        }

        if (!reloadCancelRequested) yield return new WaitForSeconds(data.reloadEndTime);

        IsReloading = false;
        reloadCancelRequested = false;
        reloadRoutine = null;
        ReloadFinished?.Invoke();
    }
}
