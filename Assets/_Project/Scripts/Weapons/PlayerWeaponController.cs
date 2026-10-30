using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private PlayerLook look;
    [SerializeField] private PlayerAmmoInventory ammo;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private List<Weapon> weapons = new List<Weapon>();
    [SerializeField] private int startingWeaponIndex;
    [SerializeField] private float sprintBlockAfterFire = 0.4f;

    private int currentIndex = -1;
    private bool fireQueued;
    private float fireRequestTime;
    private float sprintEndedTime = float.NegativeInfinity;
    private bool wasSprinting;
    private float blockSprintUntil;
    private float busyUntil;

    public Weapon CurrentWeapon => currentIndex >= 0 && currentIndex < weapons.Count ? weapons[currentIndex] : null;
    public IReadOnlyList<Weapon> Weapons => weapons;
    public bool IsAiming { get; private set; }
    public bool IsHolstered { get; private set; }
    public bool IsBusy => IsHolstered || Time.time < busyUntil;
    public float CurrentSpread { get; private set; }

    public event Action<Weapon> WeaponChanged;

    private void Awake()
    {
        foreach (Weapon weapon in weapons)
        {
            weapon.Initialize(gameObject, playerCamera, ammo, look);
            ammo.Register(weapon.Data.ammoType);
            weapon.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        Equip(startingWeaponIndex);
    }

    private void OnEnable()
    {
        input.FirePressed += RequestFire;
        input.ReloadPressed += RequestReload;
        input.WeaponSlotPressed += Equip;
        input.WeaponScrolled += Cycle;
    }

    private void OnDisable()
    {
        input.FirePressed -= RequestFire;
        input.ReloadPressed -= RequestReload;
        input.WeaponSlotPressed -= Equip;
        input.WeaponScrolled -= Cycle;
        motor.ClearSpeedModifier(this);
        motor.SetSprintBlocked(this, false);
        look.SetSensitivityMultiplier(1f);
        IsAiming = false;
    }

    private void Update()
    {
        Weapon weapon = CurrentWeapon;
        if (weapon == null) return;

        UpdateSprintState();
        UpdateAim(weapon.Data);
        UpdateSpread(weapon.Data);
        ProcessQueuedFire(weapon);
    }

    public void Equip(int index)
    {
        if (index < 0 || index >= weapons.Count || index == currentIndex || IsBusy) return;

        if (CurrentWeapon != null) CurrentWeapon.gameObject.SetActive(false);

        currentIndex = index;
        fireQueued = false;
        CurrentWeapon.gameObject.SetActive(true);
        CurrentWeapon.OnEquipped();
        WeaponChanged?.Invoke(CurrentWeapon);
    }

    public void InterruptForAction(float duration)
    {
        if (CurrentWeapon != null) CurrentWeapon.StopReloadImmediate();
        fireQueued = false;
        busyUntil = Mathf.Max(busyUntil, Time.time + duration);
    }

    public void SetHolstered(bool holstered)
    {
        IsHolstered = holstered;
        if (!holstered) return;

        if (CurrentWeapon != null) CurrentWeapon.StopReloadImmediate();
        fireQueued = false;
        IsAiming = false;
    }

    private void Cycle(int direction)
    {
        if (weapons.Count < 2) return;
        Equip((currentIndex + direction + weapons.Count) % weapons.Count);
    }

    private void RequestFire()
    {
        if (IsBusy) return;
        fireQueued = true;
        fireRequestTime = Time.time;
        if (motor.IsSprinting) blockSprintUntil = Time.time + sprintBlockAfterFire;
    }

    private void RequestReload()
    {
        if (IsBusy || CurrentWeapon == null) return;
        CurrentWeapon.TryStartReload();
    }

    private void UpdateSprintState()
    {
        bool sprinting = motor.IsSprinting;
        if (wasSprinting && !sprinting) sprintEndedTime = Time.time;
        wasSprinting = sprinting;

        motor.SetSprintBlocked(this, IsAiming || IsBusy || Time.time < blockSprintUntil);
    }

    private void UpdateAim(WeaponDataSO data)
    {
        IsAiming = input.AimHeld && !IsBusy;
        motor.SetSpeedModifier(this, IsAiming ? data.aimMoveMultiplier : 1f);
        look.SetSensitivityMultiplier(IsAiming ? data.aimSensitivityMultiplier : 1f);
    }

    private void UpdateSpread(WeaponDataSO data)
    {
        float target = IsAiming ? data.aimSpread : data.hipSpread;
        if (motor.IsMoving) target += data.moveSpreadBonus * (IsAiming ? 0.3f : 1f);
        if (!motor.IsGrounded) target += data.airSpreadBonus;
        CurrentSpread = Mathf.Lerp(CurrentSpread, target, 10f * Time.deltaTime);
    }

    private void ProcessQueuedFire(Weapon weapon)
    {
        if (!fireQueued) return;

        if (IsBusy || (weapon.IsReloading && weapon.CurrentMagazine == 0))
        {
            fireQueued = false;
            return;
        }

        if (!weapon.IsReloading && Time.time - fireRequestTime > weapon.Data.shotBufferTime + weapon.Data.sprintToFireTime)
        {
            fireQueued = false;
            return;
        }

        if (motor.IsSprinting || Time.time - sprintEndedTime < weapon.Data.sprintToFireTime) return;

        if (weapon.TryFire(CurrentSpread) || weapon.CurrentMagazine == 0) fireQueued = false;
    }
}
