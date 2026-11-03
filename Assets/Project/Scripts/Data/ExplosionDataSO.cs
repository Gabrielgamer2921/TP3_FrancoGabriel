using UnityEngine;

[CreateAssetMenu(fileName = "ExplosionData", menuName = "Cowboy FPS/Explosion Data")]
public class ExplosionDataSO : ScriptableObject
{
    [Header("Daño")]
    [Min(0.5f)] public float radius = 6f;
    [Min(0f)] public float maxDamage = 150f;
    [Min(0f)] public float minDamage = 15f;
    [Range(0f, 1f)] public float coverDamageMultiplier = 0.35f;
    public LayerMask affectedLayers = ~0;

    [Header("Física")]
    [Min(0f)] public float pushVelocity = 10f;
    [Min(0f)] public float upwardsModifier = 1.5f;

    [Header("Feedback")]
    public ExplosionEffect effectPrefab;
    [Min(0f)] public float noiseRadius = 80f;
    [Min(0f)] public float cameraShake = 1f;
}
