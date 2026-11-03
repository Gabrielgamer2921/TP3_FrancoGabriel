using UnityEngine;

public class WeaponPickup : Pickup
{
    [SerializeField] private WeaponDataSO weapon;

    protected override bool TryCollect(GameObject player)
    {
        PlayerWeaponController controller = player.GetComponent<PlayerWeaponController>();
        return controller != null && controller.Unlock(weapon);
    }
}
