using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DestructibleBarrier : MonoBehaviour, IDamageable, IResettableProp
{
    [SerializeField] private float hitPoints = 100f;
    [SerializeField] private bool explosionsOnly = true;
    [SerializeField] private bool startsBreakable = true;
    [SerializeField] private Transform piecesRoot;
    [SerializeField] private NavMeshObstacle obstacle;
    [SerializeField] private float piecePushVelocity = 9f;
    [SerializeField] private float pieceMass = 25f;
    [SerializeField] private float pieceLifetime = 10f;

    private bool breakable;
    private bool permanent;
    private float startHitPoints;
    private Transform pieceParent;
    private readonly System.Collections.Generic.List<Transform> pieces = new System.Collections.Generic.List<Transform>();
    private readonly System.Collections.Generic.List<Vector3> pieceLocalPositions = new System.Collections.Generic.List<Vector3>();
    private readonly System.Collections.Generic.List<Quaternion> pieceLocalRotations = new System.Collections.Generic.List<Quaternion>();
    private readonly System.Collections.Generic.List<Vector3> pieceLocalScales = new System.Collections.Generic.List<Vector3>();

    public Vector3 HomePosition => transform.position;

    public bool IsBroken { get; private set; }
    public bool IsBreakable => breakable;

    public event Action Broken;
    public event Action Restored;
    public event Action RejectedHit;

    private void Awake()
    {
        breakable = startsBreakable;
        startHitPoints = hitPoints;
        pieceParent = piecesRoot != null ? piecesRoot : transform;
        foreach (Transform piece in pieceParent)
        {
            pieces.Add(piece);
            pieceLocalPositions.Add(piece.localPosition);
            pieceLocalRotations.Add(piece.localRotation);
            pieceLocalScales.Add(piece.localScale);
        }
        PropRegistry.Register(this);
    }

    private void OnDestroy() => PropRegistry.Unregister(this);

    public void MakePermanent() => permanent = true;

    public void ResetState()
    {
        if (permanent || !IsBroken) return;
        StopAllCoroutines();

        for (int i = 0; i < pieces.Count; i++)
        {
            Transform piece = pieces[i];
            if (piece == null) continue;
            Rigidbody body = piece.GetComponent<Rigidbody>();
            if (body != null) Destroy(body);
            piece.SetParent(pieceParent, false);
            piece.localPosition = pieceLocalPositions[i];
            piece.localRotation = pieceLocalRotations[i];
            piece.localScale = pieceLocalScales[i];
            piece.gameObject.SetActive(true);
        }

        hitPoints = startHitPoints;
        IsBroken = false;
        if (obstacle != null) obstacle.enabled = true;
        Restored?.Invoke();
    }

    private System.Collections.IEnumerator HidePiecesLater()
    {
        yield return new WaitForSeconds(pieceLifetime);
        foreach (Transform piece in pieces)
            if (piece != null && piece.parent != pieceParent) piece.gameObject.SetActive(false);
    }

    public void SetBreakable(bool value) => breakable = value;

    public void TakeDamage(DamageInfo info)
    {
        if (IsBroken) return;
        if (!breakable)
        {
            RejectedHit?.Invoke();
            return;
        }
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

        foreach (Transform piece in pieces)
        {
            if (piece == null) continue;
            piece.SetParent(null, true);
            if (piece.GetComponent<Collider>() == null) piece.gameObject.AddComponent<BoxCollider>();
            Rigidbody body = piece.gameObject.AddComponent<Rigidbody>();
            body.mass = pieceMass;
            body.AddExplosionForce(piecePushVelocity, origin, 8f, 0.6f, ForceMode.VelocityChange);
        }
        StartCoroutine(HidePiecesLater());

        Broken?.Invoke();
    }
}
