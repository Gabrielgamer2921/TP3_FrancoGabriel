using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlarmBell : MonoBehaviour, IResettable
{
    [SerializeField] private Transform ringPoint;
    [SerializeField] private Transform bellVisual;
    [SerializeField] private float swingAngle = 35f;
    [SerializeField] private float swingDuration = 4f;
    [SerializeField] private float noiseRadius = 80f;

    public static readonly List<AlarmBell> All = new List<AlarmBell>();

    public bool IsRung { get; private set; }
    public EnemyAI Claimant { get; private set; }
    public Vector3 RingPosition => ringPoint != null ? ringPoint.position : transform.position;

    public event Action Rung;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    public static AlarmBell FindAvailable(Vector3 position, float radius)
    {
        AlarmBell best = null;
        float bestDistance = radius;
        foreach (AlarmBell bell in All)
        {
            if (bell.IsRung || bell.Claimant != null) continue;
            float distance = Vector3.Distance(position, bell.RingPosition);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = bell;
            }
        }
        return best;
    }

    public bool TryClaim(EnemyAI enemy)
    {
        if (IsRung || (Claimant != null && Claimant != enemy)) return false;
        Claimant = enemy;
        return true;
    }

    public void Release(EnemyAI enemy)
    {
        if (Claimant == enemy) Claimant = null;
    }

    public void ResetState()
    {
        StopAllCoroutines();
        IsRung = false;
        Claimant = null;
    }

    public void Ring()
    {
        if (IsRung) return;
        IsRung = true;
        Claimant = null;
        NoiseSystem.Emit(transform.position, noiseRadius, gameObject);
        Rung?.Invoke();
        if (bellVisual != null) StartCoroutine(SwingRoutine());
    }

    private IEnumerator SwingRoutine()
    {
        Quaternion start = bellVisual.localRotation;
        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float damping = 1f - elapsed / swingDuration;
            bellVisual.localRotation = start * Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 12f) * swingAngle * damping);
            yield return null;
        }
        bellVisual.localRotation = start;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsRung ? Color.red : new Color(1f, 0.8f, 0.2f);
        Gizmos.DrawWireSphere(RingPosition, 0.4f);
    }
}
