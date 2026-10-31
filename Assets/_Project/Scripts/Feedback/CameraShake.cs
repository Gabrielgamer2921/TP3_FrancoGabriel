using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private float maxAngle = 4f;
    [SerializeField] private float maxOffset = 0.06f;
    [SerializeField] private float frequency = 22f;
    [SerializeField] private float decay = 1.6f;
    [SerializeField] private float damageTrauma = 0.25f;
    [SerializeField] private float explosionReachMultiplier = 4f;

    private float trauma;

    private void OnEnable()
    {
        Explosion.Exploded += OnExplosion;
        if (playerHealth != null) playerHealth.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        Explosion.Exploded -= OnExplosion;
        if (playerHealth != null) playerHealth.Damaged -= OnDamaged;
    }

    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    private void OnExplosion(Vector3 position, float radius, float intensity)
    {
        float distance = Vector3.Distance(transform.position, position);
        float proximity = 1f - Mathf.Clamp01(distance / (radius * explosionReachMultiplier));
        AddTrauma(proximity * intensity);
    }

    private void OnDamaged(DamageInfo info) => AddTrauma(damageTrauma);

    private void LateUpdate()
    {
        trauma = Mathf.Max(0f, trauma - decay * Time.deltaTime);
        float shake = trauma * trauma;
        float t = Time.time * frequency;

        float pitch = (Mathf.PerlinNoise(t, 0.1f) - 0.5f) * 2f * maxAngle * shake;
        float yaw = (Mathf.PerlinNoise(0.2f, t) - 0.5f) * 2f * maxAngle * shake;
        float roll = (Mathf.PerlinNoise(t, t * 0.5f) - 0.5f) * 2f * maxAngle * shake;

        transform.localRotation = Quaternion.Euler(pitch, yaw, roll);
        transform.localPosition = new Vector3(Mathf.PerlinNoise(t, 3f) - 0.5f, Mathf.PerlinNoise(4f, t) - 0.5f, 0f) * 2f * maxOffset * shake;
    }
}
