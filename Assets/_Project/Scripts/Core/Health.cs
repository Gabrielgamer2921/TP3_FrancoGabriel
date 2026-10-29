using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath;
    [SerializeField] private float destroyDelay;

    public float Current { get; private set; }
    public float Max => maxHealth;
    public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;
    public bool IsDead { get; private set; }

    public event Action<DamageInfo> Damaged;
    public event Action<float> Healed;
    public event Action<float, float> Changed;
    public event Action<DamageInfo> Died;

    protected virtual void Awake()
    {
        Current = maxHealth;
    }

    public void SetMaxHealth(float value, bool refill = true)
    {
        maxHealth = Mathf.Max(1f, value);
        Current = refill ? maxHealth : Mathf.Min(Current, maxHealth);
        Changed?.Invoke(Current, maxHealth);
    }

    public virtual void TakeDamage(DamageInfo info)
    {
        if (IsDead || info.amount <= 0f) return;

        Current = Mathf.Max(0f, Current - info.amount);
        Damaged?.Invoke(info);
        Changed?.Invoke(Current, maxHealth);

        if (Current <= 0f) Die(info);
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f || Current >= maxHealth) return;

        float previous = Current;
        Current = Mathf.Min(maxHealth, Current + amount);
        Healed?.Invoke(Current - previous);
        Changed?.Invoke(Current, maxHealth);
    }

    private void Die(DamageInfo info)
    {
        IsDead = true;
        Died?.Invoke(info);
        if (destroyOnDeath) Destroy(gameObject, destroyDelay);
    }
}
