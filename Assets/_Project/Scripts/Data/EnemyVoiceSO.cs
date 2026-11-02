using UnityEngine;

[CreateAssetMenu(fileName = "EnemyVoice", menuName = "Cowboy FPS/Enemy Voice")]
public class EnemyVoiceSO : ScriptableObject
{
    [Header("Frases (cargá tus clips)")]
    public AudioClip[] spotted;
    public AudioClip[] chasing;
    public AudioClip[] attacking;
    public AudioClip[] reloading;
    public AudioClip[] throwingDynamite;
    public AudioClip[] hurt;
    public AudioClip[] death;
    public AudioClip[] searching;
    public AudioClip[] ringingAlarm;
    public AudioClip[] playerKilled;

    [Header("Solo jefes")]
    public AudioClip[] bossFightStart;
    public AudioClip[] bossRetreat;
    public AudioClip[] bossDiablo;

    [Header("Voz")]
    [Range(0f, 1f)] public float volume = 0.9f;
    [Range(0.5f, 2f)] public float basePitch = 1f;
    [Range(0f, 0.3f)] public float pitchVariationPerEnemy = 0.08f;
    [Min(1f)] public float maxDistance = 40f;

    [Header("Frecuencia")]
    [Range(0f, 1f)] public float attackingChance = 0.2f;
    [Range(0f, 1f)] public float hurtChance = 0.5f;
    [Range(0f, 1f)] public float reloadingChance = 0.7f;
    [Range(0f, 1f)] public float playerKilledChance = 0.6f;
    [Min(1f)] public float chasingIntervalMin = 6f;
    [Min(1f)] public float chasingIntervalMax = 11f;
    [Min(0f)] public float cooldownPerEnemy = 1.8f;
    [Min(0f)] public float minGapBetweenAnyEnemies = 0.5f;
}
