using UnityEngine;

[CreateAssetMenu(fileName = "AmmoType", menuName = "Cowboy FPS/Ammo Type")]
public class AmmoTypeSO : ScriptableObject
{
    public string displayName = ".45 Colt";
    [Min(0)] public int startingReserve = 24;
    [Min(1)] public int maxReserve = 48;
    [Min(1)] public int pickupAmount = 12;
}
