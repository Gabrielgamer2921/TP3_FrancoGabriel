using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAmmoInventory : MonoBehaviour
{
    private readonly Dictionary<AmmoTypeSO, int> reserves = new Dictionary<AmmoTypeSO, int>();

    public event Action<AmmoTypeSO, int> Changed;

    public void Register(AmmoTypeSO type)
    {
        if (type == null || reserves.ContainsKey(type)) return;
        reserves[type] = Mathf.Min(type.startingReserve, type.maxReserve);
        Changed?.Invoke(type, reserves[type]);
    }

    public int GetReserve(AmmoTypeSO type)
    {
        return type != null && reserves.TryGetValue(type, out int amount) ? amount : 0;
    }

    public bool IsFull(AmmoTypeSO type) => GetReserve(type) >= type.maxReserve;

    public int Take(AmmoTypeSO type, int amount)
    {
        int current = GetReserve(type);
        int taken = Mathf.Min(current, amount);
        if (taken <= 0) return 0;

        reserves[type] = current - taken;
        Changed?.Invoke(type, reserves[type]);
        return taken;
    }

    public int Add(AmmoTypeSO type, int amount)
    {
        if (type == null) return 0;
        int current = GetReserve(type);
        int added = Mathf.Clamp(type.maxReserve - current, 0, amount);
        if (added <= 0) return 0;

        reserves[type] = current + added;
        Changed?.Invoke(type, reserves[type]);
        return added;
    }
}
