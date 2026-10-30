using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class CombatTracker : MonoBehaviour
{
    [SerializeField] private PlayerStatsSO stats;
    [SerializeField] private Health health;

    private readonly HashSet<Object> engagedEnemies = new HashSet<Object>();
    private float lastHostileTime = float.NegativeInfinity;
    private float combatEndedTime;

    public bool IsInCombat { get; private set; }
    public int EngagedCount => engagedEnemies.Count;
    public float TimeOutOfCombat => IsInCombat ? 0f : Time.time - combatEndedTime;

    public event Action<bool> CombatStateChanged;

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
    }

    public void RegisterEngagement(Object enemy)
    {
        if (enemy == null) return;
        engagedEnemies.Add(enemy);
        MarkHostile();
    }

    public void UnregisterEngagement(Object enemy)
    {
        engagedEnemies.Remove(enemy);
    }

    public void MarkHostile()
    {
        lastHostileTime = Time.time;
    }

    private void OnDamaged(DamageInfo info) => MarkHostile();

    private void Update()
    {
        engagedEnemies.RemoveWhere(enemy => enemy == null);

        bool inCombat = engagedEnemies.Count > 0 || Time.time - lastHostileTime < stats.combatGraceTime;
        if (inCombat == IsInCombat) return;

        IsInCombat = inCombat;
        if (!inCombat) combatEndedTime = Time.time;
        CombatStateChanged?.Invoke(inCombat);
    }
}
