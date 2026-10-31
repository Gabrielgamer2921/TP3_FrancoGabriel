using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class ExplosiveBarrel : MonoBehaviour
{
    [SerializeField] private ExplosionDataSO explosion;
    [SerializeField] private float chainDelay = 0.15f;
    [SerializeField] private Vector3 explosionOffset = new Vector3(0f, 0.5f, 0f);

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

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
        Destroy(gameObject);
    }
}
