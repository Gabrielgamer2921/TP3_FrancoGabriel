using System;
using UnityEngine;

public class VisualBullet : MonoBehaviour
{
    [SerializeField] private float lingerTime = 0.06f;

    private Vector3 target;
    private float speed;
    private Action onArrive;
    private bool travelling;

    public void Launch(Vector3 from, Vector3 to, float speed, Action onArrive)
    {
        transform.position = from;
        Vector3 delta = to - from;
        if (delta.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(delta);

        target = to;
        this.speed = speed;
        this.onArrive = onArrive;
        travelling = true;
    }

    private void Update()
    {
        if (!travelling) return;

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if ((transform.position - target).sqrMagnitude <= 0.0001f) Arrive();
    }

    private void Arrive()
    {
        travelling = false;
        onArrive?.Invoke();

        foreach (Renderer meshRenderer in GetComponentsInChildren<MeshRenderer>()) meshRenderer.enabled = false;
        Destroy(gameObject, lingerTime);
    }
}
