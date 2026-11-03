using UnityEngine;

public class Hitbox : MonoBehaviour, IDamageable
{
    [SerializeField] private Health owner;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private bool isCritical;

    public bool IsCritical => isCritical;

    private void Awake()
    {
        if (owner == null) owner = GetComponentInParent<Health>();
    }

    public void TakeDamage(DamageInfo info)
    {
        if (owner == null) return;
        info.amount *= damageMultiplier;
        info.isCritical = isCritical;
        owner.TakeDamage(info);
    }
}
