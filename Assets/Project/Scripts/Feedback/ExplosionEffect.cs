using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [SerializeField] private Transform fireball;
    [SerializeField] private Transform smoke;
    [SerializeField] private Light flash;
    [SerializeField] private float fireballDuration = 0.45f;
    [SerializeField] private float smokeDuration = 1.8f;
    [SerializeField] private float flashIntensity = 12f;

    private float radius = 5f;
    private float elapsed;

    public void Play(float explosionRadius)
    {
        radius = explosionRadius;
        elapsed = 0f;
        Update();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float fire = Mathf.Clamp01(elapsed / fireballDuration);
        if (fireball != null) fireball.localScale = Vector3.one * radius * 0.75f * Mathf.Sin(fire * Mathf.PI);

        float smokeProgress = Mathf.Clamp01(elapsed / smokeDuration);
        if (smoke != null)
        {
            smoke.localScale = Vector3.one * radius * Mathf.Lerp(0.3f, 1.1f, Mathf.Sqrt(smokeProgress)) * (1f - Mathf.Pow(smokeProgress, 3f));
            smoke.localPosition = Vector3.up * smokeProgress * radius * 0.4f;
        }

        if (flash != null)
        {
            flash.intensity = flashIntensity * (1f - fire);
            flash.range = radius * 3f;
        }

        if (elapsed >= Mathf.Max(fireballDuration, smokeDuration)) Destroy(gameObject);
    }
}
