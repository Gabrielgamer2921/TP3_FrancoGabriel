using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class ExplosiveBarrel : MonoBehaviour, IResettableProp
{
    [SerializeField] private ExplosionDataSO explosion;
    [SerializeField] private float chainDelay = 0.15f;
    [SerializeField] private Vector3 explosionOffset = new Vector3(0f, 0.5f, 0f);

    private Health health;
    private Rigidbody body;
    private Vector3 homePosition;
    private Quaternion homeRotation;

    public Vector3 HomePosition => homePosition;

    private void Awake()
    {
        health = GetComponent<Health>();
        body = GetComponent<Rigidbody>();
        homePosition = transform.position;
        homeRotation = transform.rotation;
        PropRegistry.Register(this);
    }

    private void OnDestroy() => PropRegistry.Unregister(this);

    private void OnEnable() => health.Died += OnDied;
    private void OnDisable() => health.Died -= OnDied;

    private void OnDied(DamageInfo info)
    {
        StartCoroutine(ExplodeAfterDelay(info.source));
    }

    private IEnumerator ExplodeAfterDelay(GameObject source)
    {
        yield return new WaitForSeconds(chainDelay);
        Explosion.Detonate(transform.position + explosionOffset, explosion, source != null ? source : gameObject);
        gameObject.SetActive(false);
    }

    public void ResetState()
    {
        StopAllCoroutines();
        gameObject.SetActive(true);
        transform.SetPositionAndRotation(homePosition, homeRotation);
        if (body != null && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        health.Revive(health.Max);
    }
}
