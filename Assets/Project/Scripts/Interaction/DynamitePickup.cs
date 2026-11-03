using UnityEngine;

public class DynamitePickup : Pickup
{
    [SerializeField] private int amount = 1;

    protected override bool TryCollect(GameObject player)
    {
        PlayerThrowController thrower = player.GetComponent<PlayerThrowController>();
        return thrower != null && thrower.AddDynamite(amount);
    }
}
