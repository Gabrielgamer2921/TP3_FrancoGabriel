using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LoopingSound : MonoBehaviour
{
    [SerializeField] private SoundSO sound;
    [SerializeField, Range(0f, 1f)] private float volumeMultiplier = 1f;

    private AudioSource source;

    public AudioSource Source => source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.dopplerLevel = 0f;
        if (sound == null || !sound.HasClips) return;

        source.clip = sound.clips[0];
        source.volume = sound.volume * volumeMultiplier;
        source.spatialBlend = sound.spatialBlend;
        source.minDistance = sound.minDistance;
        source.maxDistance = sound.maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    private void OnEnable()
    {
        if (source != null && source.clip != null)
        {
            source.time = Random.Range(0f, source.clip.length * 0.9f);
            source.Play();
        }
    }

    private void OnDisable()
    {
        if (source != null) source.Stop();
    }
}
