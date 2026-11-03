using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private int poolSize = 32;

    private static AudioManager instance;
    private readonly List<AudioSource> pool = new List<AudioSource>();
    private readonly Dictionary<SoundSO, float> lastPlayed = new Dictionary<SoundSO, float>();
    private readonly Dictionary<SoundSO, int> lastIndex = new Dictionary<SoundSO, int>();
    private int next;

    private static AudioManager Instance
    {
        get
        {
            if (instance == null) instance = new GameObject("AudioManager").AddComponent<AudioManager>();
            return instance;
        }
    }

    private void Awake()
    {
        instance = this;

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = new GameObject("Voz_" + i).AddComponent<AudioSource>();
            source.transform.SetParent(transform);
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            pool.Add(source);
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static AudioSource Play(SoundSO sound, Vector3 position) => sound == null ? null : Instance.PlayInternal(sound, position, false);

    public static AudioSource Play2D(SoundSO sound) => sound == null ? null : Instance.PlayInternal(sound, Vector3.zero, true);

    public static AudioClip PickClip(SoundSO sound) => sound == null ? null : Instance.Pick(sound);

    private AudioSource PlayInternal(SoundSO sound, Vector3 position, bool force2D)
    {
        if (!sound.HasClips) return null;

        if (sound.cooldown > 0f && lastPlayed.TryGetValue(sound, out float last) && Time.unscaledTime - last < sound.cooldown) return null;
        lastPlayed[sound] = Time.unscaledTime;

        AudioSource source = pool[next];
        next = (next + 1) % pool.Count;

        source.Stop();
        source.transform.position = position;
        source.clip = Pick(sound);
        source.volume = sound.volume;
        source.pitch = Random.Range(sound.pitchMin, sound.pitchMax);
        source.spatialBlend = force2D ? 0f : sound.spatialBlend;
        source.minDistance = sound.minDistance;
        source.maxDistance = sound.maxDistance;
        source.Play();
        return source;
    }

    private AudioClip Pick(SoundSO sound)
    {
        if (sound.clips.Length == 1) return sound.clips[0];
        lastIndex.TryGetValue(sound, out int previous);
        int index = Random.Range(0, sound.clips.Length - 1);
        if (index >= previous) index++;
        lastIndex[sound] = index;
        return sound.clips[index];
    }
}
