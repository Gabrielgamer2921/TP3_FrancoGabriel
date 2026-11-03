using UnityEngine;

public class AmmoPickup : Pickup
{
    [SerializeField] private AmmoTypeSO ammoType;
    [SerializeField] private int amountOverride;

    protected override bool TryCollect(GameObject player)
    {
        PlayerAmmoInventory inventory = player.GetComponent<PlayerAmmoInventory>();
        if (inventory == null || ammoType == null || !inventory.IsRegistered(ammoType)) return false;

        int amount = amountOverride > 0 ? amountOverride : ammoType.pickupAmount;
        return inventory.Add(ammoType, amount) > 0;
    }
}
