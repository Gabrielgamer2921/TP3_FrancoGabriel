using UnityEngine;

public class KeyPickup : Pickup
{
    [SerializeField] private string keyId = "llave_patio";
    [SerializeField] private string displayName = "la llave del patio";

    protected override bool TryCollect(GameObject player)
    {
        PlayerKeyring keyring = player.GetComponent<PlayerKeyring>();
        if (keyring == null || keyring.Has(keyId)) return false;
        keyring.Add(keyId, displayName);
        return true;
    }
}
