using System;
using System.Collections;
using UnityEngine;

public class PlayerBandages : MonoBehaviour
{
    [SerializeField] private PlayerStatsSO stats;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerWeaponController weapons;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private Health health;

    private Coroutine useRoutine;

    public int Count { get; private set; }
    public int Max => stats.maxBandages;
    public bool IsUsing { get; private set; }
    public float Progress { get; private set; }

    public event Action<int> CountChanged;
    public event Action UseStarted;
    public event Action<bool> UseEnded;

    private void Awake()
    {
        Count = Mathf.Min(stats.startingBandages, stats.maxBandages);
    }

    private void Start()
    {
        CountChanged?.Invoke(Count);
    }

    private void OnEnable()
    {
        input.BandagePressed += TryUse;
        input.FirePressed += CancelUse;
    }

    private void OnDisable()
    {
        input.BandagePressed -= TryUse;
        input.FirePressed -= CancelUse;
        if (IsUsing) Finish(false);
    }

    public bool AddBandages(int amount)
    {
        if (Count >= stats.maxBandages || amount <= 0) return false;
        Count = Mathf.Min(stats.maxBandages, Count + amount);
        CountChanged?.Invoke(Count);
        return true;
    }

    private void TryUse()
    {
        if (IsUsing || Count <= 0 || health.IsDead || health.Current >= health.Max || weapons.IsBusy) return;
        useRoutine = StartCoroutine(UseRoutine());
    }

    private void CancelUse()
    {
        if (!IsUsing) return;
        if (useRoutine != null) StopCoroutine(useRoutine);
        Finish(false);
    }

    private IEnumerator UseRoutine()
    {
        IsUsing = true;
        Progress = 0f;
        weapons.SetHolstered(true);
        motor.SetSpeedModifier(this, stats.bandageMoveMultiplier);
        UseStarted?.Invoke();

        float elapsed = 0f;
        while (elapsed < stats.bandageUseTime)
        {
            if (health.IsDead)
            {
                Finish(false);
                yield break;
            }

            elapsed += Time.deltaTime;
            Progress = Mathf.Clamp01(elapsed / stats.bandageUseTime);
            yield return null;
        }

        Count--;
        CountChanged?.Invoke(Count);
        health.Heal(stats.bandageHealAmount);
        Finish(true);
    }

    private void Finish(bool completed)
    {
        IsUsing = false;
        Progress = 0f;
        useRoutine = null;
        weapons.SetHolstered(false);
        motor.ClearSpeedModifier(this);
        UseEnded?.Invoke(completed);
    }
}
