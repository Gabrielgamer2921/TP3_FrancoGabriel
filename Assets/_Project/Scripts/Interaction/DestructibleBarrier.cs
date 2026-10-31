using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DestructibleBarrier : MonoBehaviour, IDamageable
{
    [SerializeField] private float hitPoints = 100f;
    [SerializeField] private bool explosionsOnly = true;
    [SerializeField] private Transform piecesRoot;
    [SerializeField] private NavMeshObstacle obstacle;
    [SerializeField] private float piecePushVelocity = 9f;
    [SerializeField] private float pieceMass = 25f;
    [SerializeField] private float pieceLifetime = 10f;

    public bool IsBroken { get; private set; }

    public event Action Broken;
    public event Action RejectedHit;

    public void TakeDamage(DamageInfo info)
    {
        if (IsBroken) return;
        if (explosionsOnly && info.type != DamageType.Explosion)
        {
            RejectedHit?.Invoke();
            return;
        }

        hitPoints -= info.amount;
        if (hitPoints <= 0f) Break(info.point);
    }

    public void Break(Vector3 origin)
    {
        if (IsBroken) return;
        IsBroken = true;
        if (obstacle != null) obstacle.enabled = false;

        Transform root = piecesRoot != null ? piecesRoot : transform;
        List<Transform> pieces = new List<Transform>();
        foreach (Transform piece in root) pieces.Add(piece);

        foreach (Transform piece in pieces)
        {
            piece.SetParent(null, true);
            if (piece.GetComponent<Collider>() == null) piece.gameObject.AddComponent<BoxCollider>();
            Rigidbody body = piece.gameObject.AddComponent<Rigidbody>();
            body.mass = pieceMass;
            body.AddExplosionForce(piecePushVelocity, origin, 8f, 0.6f, ForceMode.VelocityChange);
            Destroy(piece.gameObject, pieceLifetime);
        }

        Broken?.Invoke();
    }
}
