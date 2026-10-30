using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class Pickup : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 2f;

    private Vector3 visualStart;

    public static event Action<Pickup> AnyCollected;

    protected virtual void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (visual != null) visualStart = visual.localPosition;
    }

    private void Update()
    {
        if (visual == null) return;
        visual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        visual.localPosition = visualStart + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!TryCollect(other.gameObject)) return;

        AnyCollected?.Invoke(this);
        Destroy(gameObject);
    }

    protected abstract bool TryCollect(GameObject player);
}
