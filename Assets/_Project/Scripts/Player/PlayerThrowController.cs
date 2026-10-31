using System;
using System.Collections;
using UnityEngine;

public class PlayerThrowController : MonoBehaviour
{
    [SerializeField] private ThrowableDataSO data;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerWeaponController weapons;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Health health;
    [SerializeField] private GameObject handModel;

    private float cookStartTime;
    private float nextThrowTime;

    public int Count { get; private set; }
    public int Max => data.maxCount;
    public bool IsCooking { get; private set; }
    public float CookProgress => IsCooking && data.canCook ? Mathf.Clamp01((Time.time - cookStartTime) / data.fuseTime) : 0f;

    public event Action<int> CountChanged;
    public event Action CookStarted;
    public event Action Thrown;

    private void Awake()
    {
        Count = Mathf.Min(data.startingCount, data.maxCount);
        if (handModel != null) handModel.SetActive(false);
    }

    private void Start()
    {
        CountChanged?.Invoke(Count);
    }

    private void OnEnable()
    {
        input.ThrowPressed += StartCooking;
        input.ThrowReleased += ReleaseThrow;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        input.ThrowPressed -= StartCooking;
        input.ThrowReleased -= ReleaseThrow;
        health.Died -= OnDied;
    }

    public bool AddDynamite(int amount)
    {
        if (Count >= data.maxCount || amount <= 0) return false;
        Count = Mathf.Min(data.maxCount, Count + amount);
        CountChanged?.Invoke(Count);
        return true;
    }

    private void StartCooking()
    {
        if (IsCooking || Count <= 0 || Time.time < nextThrowTime || weapons.IsBusy || health.IsDead) return;

        IsCooking = true;
        cookStartTime = Time.time;
        weapons.SetHolstered(true);
        if (handModel != null) handModel.SetActive(true);
        CookStarted?.Invoke();
    }

    private void Update()
    {
        if (!IsCooking || !data.canCook) return;
        if (Time.time - cookStartTime < data.fuseTime) return;

        IsCooking = false;
        ConsumeOne();
        Explosion.Detonate(playerCamera.transform.position + playerCamera.transform.forward * 0.4f, data.explosion, gameObject);
        FinishThrow();
    }

    private void ReleaseThrow()
    {
        if (!IsCooking) return;
        Throw(GetThrowVelocity());
    }

    private void OnDied(DamageInfo info)
    {
        if (IsCooking) Throw(Vector3.down);
    }

    private Vector3 GetThrowVelocity()
    {
        Transform cam = playerCamera.transform;
        return cam.forward * data.throwForce + Vector3.up * data.upwardForce + Vector3.ProjectOnPlane(motor.Velocity, Vector3.up) * 0.5f;
    }

    private void Throw(Vector3 velocity)
    {
        float cooked = data.canCook ? Time.time - cookStartTime : 0f;
        float remainingFuse = Mathf.Max(0.1f, data.fuseTime - cooked);

        Transform cam = playerCamera.transform;
        Vector3 origin = cam.position + cam.forward * 0.5f + cam.right * 0.15f - cam.up * 0.1f;

        ConsumeOne();
        Dynamite dynamite = Instantiate(data.prefab, origin, Quaternion.LookRotation(cam.forward));
        dynamite.Arm(data.explosion, gameObject, remainingFuse, velocity);

        Thrown?.Invoke();
        FinishThrow();
    }

    private void ConsumeOne()
    {
        Count = Mathf.Max(0, Count - 1);
        CountChanged?.Invoke(Count);
    }

    private void FinishThrow()
    {
        IsCooking = false;
        nextThrowTime = Time.time + data.throwCooldown;
        if (handModel != null) handModel.SetActive(false);
        StartCoroutine(UnholsterAfterDelay());
    }

    private IEnumerator UnholsterAfterDelay()
    {
        yield return new WaitForSeconds(data.holsterTime);
        if (!IsCooking && !health.IsDead) weapons.SetHolstered(false);
    }
}
