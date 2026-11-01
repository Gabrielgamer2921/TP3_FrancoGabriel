using UnityEngine;

[RequireComponent(typeof(Health))]
public class KeyCarrier : MonoBehaviour
{
    [SerializeField] private GameObject keyPrefab;
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.2f, 0f);

    private Health health;

    private void Awake() => health = GetComponent<Health>();
    private void OnEnable() => health.Died += OnDied;
    private void OnDisable() => health.Died -= OnDied;

    private void OnDied(DamageInfo info)
    {
        if (keyPrefab == null) return;
        Vector3 position = transform.position + dropOffset;
        position.y = Mathf.Max(position.y, transform.position.y);
        Instantiate(keyPrefab, position, Quaternion.identity);
    }
}
