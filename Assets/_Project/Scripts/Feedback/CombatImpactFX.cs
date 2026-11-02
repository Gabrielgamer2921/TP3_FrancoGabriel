using UnityEngine;

public class CombatImpactFX : MonoBehaviour
{
    [Header("Partículas")]
    [SerializeField] private ParticleSystem dustPrefab;
    [SerializeField] private ParticleSystem bloodPrefab;

    [Header("Sonidos")]
    [SerializeField] private SoundSO dirtImpact;
    [SerializeField] private SoundSO woodImpact;
    [SerializeField] private SoundSO fleshImpact;
    [SerializeField] private string woodMaterialKeyword = "Madera";

    private void OnEnable()
    {
        Weapon.AnyHit += OnHit;
        EnemyWeapon.AnyHit += OnHit;
    }

    private void OnDisable()
    {
        Weapon.AnyHit -= OnHit;
        EnemyWeapon.AnyHit -= OnHit;
    }

    private void OnHit(WeaponHit hit)
    {
        Quaternion rotation = hit.normal.sqrMagnitude > 0.01f ? Quaternion.LookRotation(hit.normal) : Quaternion.identity;
        bool flesh = hit.damaged && hit.collider != null && hit.collider.GetComponentInParent<Health>() != null;

        ParticleSystem prefab = flesh ? bloodPrefab : dustPrefab;
        if (prefab != null) Instantiate(prefab, hit.point + hit.normal * 0.02f, rotation);

        SoundSO sound = flesh ? fleshImpact : IsWood(hit.collider) ? woodImpact : dirtImpact;
        AudioManager.Play(sound, hit.point);
    }

    private bool IsWood(Collider collider)
    {
        if (collider == null) return false;
        Renderer hitRenderer = collider.GetComponent<Renderer>();
        return hitRenderer != null && hitRenderer.sharedMaterial != null && hitRenderer.sharedMaterial.name.Contains(woodMaterialKeyword);
    }
}
