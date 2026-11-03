using UnityEngine;

public class BandagePickup : Pickup
{
    [SerializeField] private int amount = 1;

    protected override bool TryCollect(GameObject player)
    {
        PlayerBandages bandages = player.GetComponent<PlayerBandages>();
        return bandages != null && bandages.AddBandages(amount);
    }
}
