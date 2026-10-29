using System;
using System.Collections;
using UnityEngine;

public class PlayerMelee : MonoBehaviour
{
    [SerializeField] private PlayerStatsSO stats;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerWeaponController weapons;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private LayerMask hitMask = ~0;

    private float nextMeleeTime;

    public event Action Swung;
    public event Action<Vector3> HitLanded;

    private void OnEnable()
    {
        input.MeleePressed += TryMelee;
    }

    private void OnDisable()
    {
        input.MeleePressed -= TryMelee;
    }

    private void TryMelee()
    {
        if (Time.time < nextMeleeTime || weapons.IsBusy) return;

        nextMeleeTime = Time.time + stats.meleeCooldown;
        weapons.InterruptForAction(stats.meleeCooldown * 0.6f);
        Swung?.Invoke();
        StartCoroutine(HitAfterDelay());
    }

    private IEnumerator HitAfterDelay()
    {
        yield return new WaitForSeconds(stats.meleeHitDelay);

        Vector3 origin = attackOrigin.position;
        Vector3 direction = attackOrigin.forward;

        if (!Physics.SphereCast(origin, stats.meleeRadius, direction, out RaycastHit hit, stats.meleeRange, hitMask, QueryTriggerInteraction.Ignore))
            yield break;

        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(new DamageInfo(stats.meleeDamage, hit.point, direction, gameObject, DamageType.Melee));

        Rigidbody body = hit.collider.attachedRigidbody;
        if (body != null && !body.isKinematic) body.AddForceAtPosition(direction * stats.meleeForce, hit.point, ForceMode.Impulse);

        HitLanded?.Invoke(hit.point);
    }
}
