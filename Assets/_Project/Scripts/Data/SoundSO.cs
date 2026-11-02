using UnityEngine;

[CreateAssetMenu(fileName = "Sound", menuName = "Cowboy FPS/Sound")]
public class SoundSO : ScriptableObject
{
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitchMin = 0.95f;
    [Range(0.5f, 2f)] public float pitchMax = 1.05f;
    [Range(0f, 1f)] public float spatialBlend = 1f;
    [Min(0.1f)] public float minDistance = 2f;
    [Min(1f)] public float maxDistance = 60f;
    [Min(0f)] public float cooldown;

    public bool HasClips => clips != null && clips.Length > 0;
}
