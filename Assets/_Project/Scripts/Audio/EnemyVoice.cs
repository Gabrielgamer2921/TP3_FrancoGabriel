using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyVoice : MonoBehaviour
{
    [SerializeField] private EnemyVoiceSO voice;
    [SerializeField] private EnemyAI ai;
    [SerializeField] private EnemyWeapon weapon;
    [SerializeField] private Health health;

    private static float lastAnyVoiceTime = float.NegativeInfinity;

    private AudioSource source;
    private BossController boss;
    private PlayerTarget subscribedPlayer;
    private float pitch;
    private float lastVoiceTime = float.NegativeInfinity;
    private float nextChaseTime;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 3f;
        boss = GetComponent<BossController>();
        if (voice != null)
        {
            pitch = voice.basePitch + Random.Range(-voice.pitchVariationPerEnemy, voice.pitchVariationPerEnemy);
            source.maxDistance = voice.maxDistance;
        }
    }

    private void OnEnable()
    {
        if (voice == null) return;
        ai.StateChanged += OnStateChanged;
        ai.DynamiteThrown += OnDynamiteThrown;
        weapon.Fired += OnFired;
        weapon.ReloadStarted += OnReloadStarted;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
        if (boss != null) boss.PhaseChanged += OnBossPhaseChanged;
    }

    private void OnDisable()
    {
        if (voice == null) return;
        ai.StateChanged -= OnStateChanged;
        ai.DynamiteThrown -= OnDynamiteThrown;
        weapon.Fired -= OnFired;
        weapon.ReloadStarted -= OnReloadStarted;
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
        if (boss != null) boss.PhaseChanged -= OnBossPhaseChanged;
        if (subscribedPlayer != null) subscribedPlayer.Health.Died -= OnPlayerDied;
        subscribedPlayer = null;
    }

    private void Update()
    {
        if (voice == null || ai.IsDead) return;

        if (subscribedPlayer == null && PlayerTarget.Instance != null)
        {
            subscribedPlayer = PlayerTarget.Instance;
            subscribedPlayer.Health.Died += OnPlayerDied;
        }

        if (ai.State != EnemyState.Combat || Time.time < nextChaseTime) return;
        nextChaseTime = Time.time + Random.Range(voice.chasingIntervalMin, voice.chasingIntervalMax);
        Say(voice.chasing);
    }

    private void OnStateChanged(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Combat:
                if (Say(voice.spotted, true)) nextChaseTime = Time.time + Random.Range(voice.chasingIntervalMin, voice.chasingIntervalMax);
                break;
            case EnemyState.Search:
                Say(voice.searching);
                break;
            case EnemyState.RingAlarm:
                Say(voice.ringingAlarm, true);
                break;
        }
    }

    private void OnFired()
    {
        if (Random.value < voice.attackingChance) Say(voice.attacking);
    }

    private void OnReloadStarted()
    {
        if (Random.value < voice.reloadingChance) Say(voice.reloading);
    }

    private void OnDynamiteThrown() => Say(voice.throwingDynamite, true);

    private void OnDamaged(DamageInfo info)
    {
        if (!health.IsDead && Random.value < voice.hurtChance) Say(voice.hurt, true);
    }

    private void OnDied(DamageInfo info) => Say(voice.death, true, true);

    private void OnPlayerDied(DamageInfo info)
    {
        if (ai.IsDead || ai.State != EnemyState.Combat) return;
        if (Random.value < voice.playerKilledChance) Invoke(nameof(SayPlayerKilled), Random.Range(0.4f, 1.5f));
    }

    private void SayPlayerKilled() => Say(voice.playerKilled);

    private void OnBossPhaseChanged(BossPhase phase)
    {
        if (phase == BossPhase.Duel) Say(voice.bossFightStart, true, true);
        else if (phase == BossPhase.Retreat) Say(voice.bossRetreat, true, true);
        else if (phase == BossPhase.Diablo) Say(voice.bossDiablo, true, true);
    }

    private bool Say(AudioClip[] clips, bool important = false, bool interrupt = false)
    {
        if (clips == null || clips.Length == 0) return false;

        if (!interrupt)
        {
            if (source.isPlaying) return false;
            if (Time.time - lastVoiceTime < voice.cooldownPerEnemy) return false;
            if (!important && Time.time - lastAnyVoiceTime < voice.minGapBetweenAnyEnemies) return false;
        }

        source.Stop();
        source.clip = clips[Random.Range(0, clips.Length)];
        source.pitch = pitch;
        source.volume = voice.volume;
        source.Play();

        lastVoiceTime = Time.time;
        lastAnyVoiceTime = Time.time;
        return true;
    }
}
