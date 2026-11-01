using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerKeyring : MonoBehaviour
{
    private readonly Dictionary<string, string> keys = new Dictionary<string, string>();

    public event Action<string, string> KeyAdded;

    public bool Has(string keyId) => !string.IsNullOrEmpty(keyId) && keys.ContainsKey(keyId);

    public void Add(string keyId, string displayName)
    {
        if (string.IsNullOrEmpty(keyId) || keys.ContainsKey(keyId)) return;
        keys[keyId] = displayName;
        KeyAdded?.Invoke(keyId, displayName);
    }
}
