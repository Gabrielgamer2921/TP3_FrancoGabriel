using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerVitals : MonoBehaviour
{
    [SerializeField] private PlayerStatsSO stats;
    [SerializeField] private CombatTracker combat;

    private Health health;

    public Health Health => health;
    public bool IsRegenerating { get; private set; }

    private void Awake()
    {
        health = GetComponent<Health>();
        health.SetMaxHealth(stats.maxHealth);
    }

    private void Update()
    {
        IsRegenerating = false;
        if (health.IsDead || combat.IsInCombat || combat.TimeOutOfCombat < stats.regenDelay) return;

        float cap = Mathf.Min(stats.regenCap, health.Max);
        if (health.Current >= cap) return;

        float amount = Mathf.Min(stats.regenPerSecond * Time.deltaTime, cap - health.Current);
        health.Heal(amount);
        IsRegenerating = true;
    }
}
