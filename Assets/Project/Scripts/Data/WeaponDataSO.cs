using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Cowboy FPS/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [Header("General")]
    public string displayName = "Revólver";
    public AmmoTypeSO ammoType;

    [Header("Daño")]
    [Min(0f)] public float damage = 40f;
    [Min(1f)] public float range = 60f;
    public AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);
    [Min(0f)] public float impactForce = 4f;
    public LayerMask hitMask = ~0;

    [Header("Perdigones (escopetas)")]
    [Min(1)] public int pelletsPerShot = 1;
    [Min(0f)] public float pelletSpread;

    [Header("Ruido")]
    [Min(0f)] public float noiseRadius = 40f;

    [Header("Cadencia")]
    [Min(0.05f)] public float timeBetweenShots = 0.35f;
    [Min(0f)] public float shotBufferTime = 0.15f;

    [Header("Cargador")]
    [Min(1)] public int magazineSize = 6;
    [Min(0)] public int startingMagazine = 6;

    [Header("Recarga bala por bala")]
    [Min(0f)] public float reloadStartTime = 0.35f;
    [Min(0.05f)] public float reloadPerRoundTime = 0.45f;
    [Min(0f)] public float reloadEndTime = 0.3f;
    public bool canInterruptReload = true;

    [Header("Precisión (grados)")]
    [Min(0f)] public float hipSpread = 1.5f;
    [Min(0f)] public float aimSpread = 0.25f;
    [Min(0f)] public float moveSpreadBonus = 1.5f;
    [Min(0f)] public float airSpreadBonus = 4f;

    [Header("Retroceso")]
    [Min(0f)] public float recoilVertical = 3f;
    [Min(0f)] public float recoilHorizontal = 1f;
    [Min(0f)] public float visualKickBack = 0.06f;
    [Min(0f)] public float visualKickRotation = 10f;

    [Header("Apuntar")]
    [Range(20f, 90f)] public float aimFov = 60f;
    [Min(0.1f)] public float aimSpeed = 7f;
    [Range(0.1f, 1f)] public float aimMoveMultiplier = 0.6f;
    [Range(0.1f, 1f)] public float aimSensitivityMultiplier = 0.75f;

    [Header("Manejo")]
    [Min(0f)] public float equipTime = 0.35f;
    [Min(0f)] public float sprintToFireTime = 0.2f;

    [Header("Bala visual (solo armas hitscan)")]
    public VisualBullet visualBulletPrefab;
    [Min(0f)] public float visualBulletSpeed = 120f;
    public bool delayHitUntilBulletArrives = true;

    [Header("Proyectil (solo armas de proyectil)")]
    public Projectile projectilePrefab;
    [Min(1f)] public float projectileSpeed = 150f;
    [Min(0f)] public float projectileGravityScale = 0.4f;
    [Min(0.1f)] public float projectileLifetime = 4f;
    [Min(0f)] public float zeroDistance = 60f;
}
