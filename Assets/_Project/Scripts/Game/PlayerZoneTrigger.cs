using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PlayerZoneTrigger : MonoBehaviour
{
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 0.4f, 0.25f);

    private bool triggered;

    public bool PlayerInside { get; private set; }

    public event Action Entered;
    public event Action Exited;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerInside = true;
        if (triggerOnce && triggered) return;
        triggered = true;
        Entered?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerInside = false;
        Exited?.Invoke();
    }

    public void ResetTrigger()
    {
        triggered = false;
        if (PlayerInside) Entered?.Invoke();
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
    }
}
