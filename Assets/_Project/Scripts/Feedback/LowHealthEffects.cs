using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LowHealthEffects : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Volume volume;
    [SerializeField] private AudioSource heartbeat;
    [SerializeField, Range(0.05f, 1f)] private float threshold = 0.35f;
    [SerializeField] private float minSaturation = -60f;
    [SerializeField] private float extraVignette = 0.25f;
    [SerializeField] private float maxHeartbeatVolume = 0.8f;

    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private float baseSaturation;
    private float baseVignette;
    private float intensity;

    private void Start()
    {
        if (volume != null)
        {
            VolumeProfile profile = volume.profile;
            if (profile.TryGet(out colorAdjustments))
            {
                colorAdjustments.saturation.overrideState = true;
                baseSaturation = colorAdjustments.saturation.value;
            }
            if (profile.TryGet(out vignette))
            {
                vignette.intensity.overrideState = true;
                baseVignette = vignette.intensity.value;
            }
        }

        if (heartbeat != null)
        {
            heartbeat.loop = true;
            heartbeat.volume = 0f;
            heartbeat.spatialBlend = 0f;
            heartbeat.Play();
        }
    }

    private void Update()
    {
        float target = health.IsDead ? 1f : 1f - Mathf.Clamp01(health.Normalized / threshold);
        intensity = Mathf.MoveTowards(intensity, target, Time.unscaledDeltaTime * 1.5f);

        if (colorAdjustments != null) colorAdjustments.saturation.value = Mathf.Lerp(baseSaturation, minSaturation, intensity);
        if (vignette != null) vignette.intensity.value = Mathf.Lerp(baseVignette, baseVignette + extraVignette, intensity);

        if (heartbeat != null)
        {
            heartbeat.volume = health.IsDead ? 0f : intensity * maxHeartbeatVolume;
            heartbeat.pitch = Mathf.Lerp(1f, 1.35f, intensity);
        }
    }
}
