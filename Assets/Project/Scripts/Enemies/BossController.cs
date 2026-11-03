using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyAI), typeof(Health))]
public class BossController : MonoBehaviour
{
    [SerializeField] private string bossName = "Dirty Holliday";
    [SerializeField] private EnemyDataSO diabloData;
    [SerializeField] private Transform retreatPoint;
    [SerializeField] private Transform returnPoint;
    [SerializeField] private LockedDoor hideoutDoor;
    [SerializeField] private EnemySpawnGroup reinforcementWave;
    [SerializeField, Range(0.05f, 0.95f)] private float retreatThreshold = 0.5f;
    [SerializeField, Range(0.1f, 1f)] private float diabloDamageTaken = 0.65f;
    [SerializeField] private float diabloHealthBonus;
    [SerializeField] private Color diabloTint = new Color(0.85f, 0.12f, 0.08f);
    [SerializeField, Range(0f, 1f)] private float diabloTintStrength = 0.55f;
    [SerializeField] private float retreatSpeed = 7f;
    [SerializeField] private float returnDelay = 1.5f;
    [SerializeField] private GameObject rewardPickup;

    private EnemyAI ai;
    private Health health;
    private NavMeshAgent agent;
    private EnemyHitFlash hitFlash;
    private float diabloStartHealth;
    private EnemyDataSO duelData;
    private Vector3 startPosition;
    private Quaternion startRotation;

    public BossPhase Phase { get; private set; } = BossPhase.Waiting;
    public string BossName => bossName;
    public Health Health => health;
    public bool FightStarted => Phase != BossPhase.Waiting;

    public event Action<BossPhase> PhaseChanged;
    public event Action Defeated;

    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        hitFlash = GetComponent<EnemyHitFlash>();
        duelData = ai.Data;
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void Start()
    {
        health.Invulnerable = true;
        ai.enabled = false;
    }

    public void StartFight()
    {
        if (Phase != BossPhase.Waiting) return;
        health.Invulnerable = false;
        SetPhase(BossPhase.Duel);
        WakeUp();
    }

    private void WakeUp()
    {
        ai.enabled = true;
        PlayerTarget player = PlayerTarget.Instance;
        if (player != null) ai.AlertAsReinforcement(player.transform.position);
    }

    private void OnDamaged(DamageInfo info)
    {
        if (Phase == BossPhase.Duel && health.Normalized <= retreatThreshold)
            StartCoroutine(RetreatRoutine());
    }

    private IEnumerator RetreatRoutine()
    {
        SetPhase(BossPhase.Retreat);
        health.Invulnerable = true;
        ai.enabled = false;

        if (hideoutDoor != null) hideoutDoor.Open();

        if (agent.isOnNavMesh && retreatPoint != null)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = retreatSpeed;
            agent.SetDestination(retreatPoint.position);

            float timeout = Time.time + 8f;
            while (Time.time < timeout && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.4f))
                yield return null;
        }

        SetPresence(false);
        if (hideoutDoor != null) hideoutDoor.CloseAndSeal();

        if (reinforcementWave != null)
        {
            reinforcementWave.Activate();
            yield return new WaitForSeconds(1f);
            while (!reinforcementWave.IsCleared) yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(returnDelay);

        if (hideoutDoor != null) hideoutDoor.Open();
        if (returnPoint != null && agent.isOnNavMesh) agent.Warp(returnPoint.position);
        SetPresence(true);

        if (diabloData != null) ai.ApplyData(diabloData);
        if (hitFlash != null) hitFlash.SetTint(diabloTint, diabloTintStrength);
        health.DamageTakenMultiplier = diabloDamageTaken;
        if (diabloHealthBonus > 0f) health.Heal(diabloHealthBonus);
        health.Invulnerable = false;

        diabloStartHealth = health.Current;
        SetPhase(BossPhase.Diablo);
        WakeUp();
    }

    public void ResetToWaiting()
    {
        if (health.IsDead) return;
        StopAllCoroutines();

        if (reinforcementWave != null) reinforcementWave.ResetState();
        if (hideoutDoor != null) hideoutDoor.CloseAndSeal();
        SetPresence(true);

        ai.ForgetPlayer();
        ai.enabled = false;
        if (ai.Data != duelData) ai.ApplyData(duelData);
        if (hitFlash != null) hitFlash.SetTint(diabloTint, 0f);

        health.DamageTakenMultiplier = 1f;
        health.Heal(health.Max - health.Current);
        health.Invulnerable = true;

        if (agent.isOnNavMesh) agent.Warp(startPosition);
        else transform.position = startPosition;
        transform.rotation = startRotation;
        SetPhase(BossPhase.Waiting);
    }

    public void ResetDiablo()
    {
        if (health.IsDead) return;
        StopAllCoroutines();

        if (health.Current < diabloStartHealth) health.Heal(diabloStartHealth - health.Current);
        ai.ForgetPlayer();
        if (returnPoint != null && agent.isOnNavMesh) agent.Warp(returnPoint.position);
        StartCoroutine(WakeAfter(1.5f));
    }

    private IEnumerator WakeAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (Phase == BossPhase.Diablo && !health.IsDead) WakeUp();
    }

    private void SetPresence(bool present)
    {
        foreach (Renderer meshRenderer in GetComponentsInChildren<Renderer>(true)) meshRenderer.enabled = present;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true)) collider.enabled = present;
    }

    private void OnDied(DamageInfo info)
    {
        StopAllCoroutines();
        SetPhase(BossPhase.Defeated);
        if (rewardPickup != null)
            Instantiate(rewardPickup, transform.position + transform.forward * 0.8f + Vector3.up * 0.1f, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        Defeated?.Invoke();
    }

    private void SetPhase(BossPhase phase)
    {
        if (Phase == phase) return;
        Phase = phase;
        PhaseChanged?.Invoke(phase);
    }
}
