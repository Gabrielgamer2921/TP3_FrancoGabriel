using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    public static readonly List<EnemyAI> All = new List<EnemyAI>();
    public static event Action<EnemyAI> AnyKilled;

    [SerializeField] private EnemyDataSO data;
    [SerializeField] private EnemyPerception perception;
    [SerializeField] private EnemyWeapon weapon;
    [SerializeField] private PatrolRoute patrolRoute;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SniperGlint glint;
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1.1f;

    private NavMeshAgent agent;
    private Health health;
    private CapsuleCollider body;
    private Rigidbody rb;
    private EnemyDynamiteThrower thrower;

    private Vector3 homePosition;
    private Quaternion homeRotation;
    private Vector3 lastKnownPosition;
    private Vector3 lastAimPoint;
    private float lastSeenTime = float.NegativeInfinity;
    private bool wasSeeing;
    private float aimStartTime;

    private int patrolIndex;
    private float waitUntil = -1f;
    private float stateEndTime;
    private float nextRepositionTime;
    private float nextSearchPointTime;
    private float nextCoverCheckTime;
    private float staggerUntil;

    private CoverPoint currentCover;
    private bool arrivedAtCover;
    private bool hidingInCover;
    private float coverPhaseEndTime;

    private bool telegraphing;
    private float telegraphEndTime;
    private Vector3 trackedAimPoint;

    private AlarmBell claimedBell;
    private bool engaged;
    private bool canRingAlarm;
    private bool isCrouched;

    public EnemyDataSO Data => data;
    public EnemyState State { get; private set; }
    public bool IsDead => State == EnemyState.Dead;
    public float Awareness => perception.Awareness;
    public PatrolRoute PatrolRoute { get => patrolRoute; set => patrolRoute = value; }

    public event Action<EnemyState> StateChanged;
    public event Action<EnemyAI> Killed;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        body = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();

        perception.Initialize(data);
        weapon.Initialize(data);
        health.SetMaxHealth(data.maxHealth);

        agent.angularSpeed = data.turnSpeed;
        agent.speed = data.patrolSpeed;
        canRingAlarm = data.canRingAlarm && data.combatStyle != EnemyCombatStyle.Sniper;

        if (rb != null) rb.isKinematic = true;

        if (data.throwable != null && data.dynamiteCount > 0)
        {
            thrower = GetComponent<EnemyDynamiteThrower>();
            if (thrower == null) thrower = gameObject.AddComponent<EnemyDynamiteThrower>();
            thrower.Initialize(data, weapon.Muzzle);
        }

        homePosition = transform.position;
        homeRotation = transform.rotation;
    }

    private void OnEnable()
    {
        All.Add(this);
        health.Damaged += OnDamaged;
        health.Died += OnDied;
        NoiseSystem.NoiseEmitted += OnNoise;
    }

    private void OnDisable()
    {
        All.Remove(this);
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
        NoiseSystem.NoiseEmitted -= OnNoise;
        SetEngaged(false);
        ReleaseCover();
        ReleaseBell();
    }

    private void Start()
    {
        if (State == EnemyState.Patrol) EnterPatrol();
    }

    private void Update()
    {
        if (IsDead) return;

        PlayerTarget player = PlayerTarget.Instance;
        bool alerted = State == EnemyState.Combat || State == EnemyState.RingAlarm || State == EnemyState.Search;
        perception.Tick(alerted);

        bool sees = perception.CanSeePlayer;
        if (sees && player != null)
        {
            lastKnownPosition = player.transform.position;
            lastAimPoint = player.AimPoint;
            lastSeenTime = Time.time;
            if (!wasSeeing) aimStartTime = Time.time;
        }
        wasSeeing = sees;

        if (player == null || player.IsDead)
        {
            if (State != EnemyState.Patrol) EnterPatrol();
            UpdatePatrol();
            return;
        }

        switch (State)
        {
            case EnemyState.Patrol: UpdatePatrol(); break;
            case EnemyState.Investigate: UpdateInvestigate(); break;
            case EnemyState.Combat: UpdateCombat(player); break;
            case EnemyState.Search: UpdateSearch(); break;
            case EnemyState.RingAlarm: UpdateRingAlarm(); break;
        }
    }

    public void AlertAsReinforcement(Vector3 playerPosition)
    {
        canRingAlarm = false;
        EnterCombat(playerPosition, false);
    }

    public void ApplyData(EnemyDataSO newData)
    {
        data = newData;
        perception.Initialize(data);
        weapon.Initialize(data);
        agent.angularSpeed = data.turnSpeed;
        canRingAlarm = false;

        if (data.throwable != null && data.dynamiteCount > 0)
        {
            thrower = GetComponent<EnemyDynamiteThrower>();
            if (thrower == null) thrower = gameObject.AddComponent<EnemyDynamiteThrower>();
            thrower.Initialize(data, weapon.Muzzle);
        }
        else
        {
            thrower = null;
        }
    }

    public void AlertFromAlly(Vector3 position, float delay)
    {
        if (IsDead || State == EnemyState.Combat || State == EnemyState.RingAlarm) return;
        StartCoroutine(DelayedAlert(position, delay));
    }

    private IEnumerator DelayedAlert(Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!IsDead) EnterCombat(position, false);
    }

    private void EnterPatrol()
    {
        SetEngaged(false);
        ReleaseCover();
        ReleaseBell();
        CancelTelegraph();
        SetState(EnemyState.Patrol);
        agent.speed = data.patrolSpeed;
        waitUntil = -1f;

        if (patrolRoute != null && patrolRoute.Count > 0)
        {
            patrolIndex = patrolRoute.GetClosestIndex(transform.position);
            MoveTo(patrolRoute.GetPoint(patrolIndex));
        }
        else
        {
            MoveTo(homePosition);
        }
    }

    private void StartInvestigate(Vector3 point)
    {
        SetState(EnemyState.Investigate);
        lastKnownPosition = point;
        stateEndTime = Time.time + data.investigateDuration;
        agent.speed = data.patrolSpeed;
        if (data.combatStyle == EnemyCombatStyle.Sniper) StopMoving();
        else MoveTo(point);
    }

    private void EnterCombat(Vector3 knownPosition, bool alertAllies)
    {
        lastKnownPosition = knownPosition;
        lastSeenTime = Time.time;
        perception.SetAwareness(1f);

        if (State == EnemyState.Combat || State == EnemyState.RingAlarm)
        {
            SetEngaged(true);
            return;
        }

        SetState(EnemyState.Combat);
        SetEngaged(true);
        aimStartTime = Time.time;
        nextRepositionTime = 0f;

        if (alertAllies)
        {
            foreach (EnemyAI ally in All)
            {
                if (ally == this || ally.IsDead) continue;
                if (Vector3.Distance(ally.transform.position, transform.position) <= data.alertAllyRadius)
                    ally.AlertFromAlly(knownPosition, Random.Range(0.3f, 0.9f));
            }
        }

        TryClaimAlarm();
    }

    private void StartSearch()
    {
        ReleaseCover();
        CancelTelegraph();
        SetState(EnemyState.Search);
        stateEndTime = Time.time + data.searchDuration;
        nextSearchPointTime = Time.time + 1f;
        agent.speed = Mathf.Lerp(data.patrolSpeed, data.chaseSpeed, 0.5f);
        if (data.combatStyle != EnemyCombatStyle.Sniper) MoveTo(lastKnownPosition);
    }

    private void TryClaimAlarm()
    {
        if (!canRingAlarm) return;
        AlarmBell bell = AlarmBell.FindAvailable(transform.position, data.alarmSearchRadius);
        if (bell == null || !bell.TryClaim(this)) return;

        claimedBell = bell;
        ReleaseCover();
        SetState(EnemyState.RingAlarm);
        agent.speed = data.chaseSpeed;
        MoveTo(bell.RingPosition);
    }

    private void UpdatePatrol()
    {
        PlayerTarget player = PlayerTarget.Instance;
        bool playerAlive = player != null && !player.IsDead;

        if (playerAlive && perception.Awareness >= 1f)
        {
            EnterCombat(perception.LastSeenPosition, true);
            return;
        }

        if (playerAlive && perception.Awareness >= data.suspicionThreshold)
        {
            StartInvestigate(perception.LastSeenPosition);
            return;
        }

        if (patrolRoute != null && patrolRoute.Count > 1)
        {
            if (!HasArrived()) return;
            if (waitUntil < 0f) waitUntil = Time.time + data.patrolWaitTime;
            else if (Time.time >= waitUntil)
            {
                patrolIndex = (patrolIndex + 1) % patrolRoute.Count;
                MoveTo(patrolRoute.GetPoint(patrolIndex));
                waitUntil = -1f;
            }
            return;
        }

        if (Vector3.Distance(transform.position, homePosition) > 1f)
        {
            MoveTo(homePosition);
            return;
        }

        StopMoving();
        float sweep = data.combatStyle == EnemyCombatStyle.Sniper ? Mathf.Sin(Time.time * 0.4f) * data.idleSweepAngle : 0f;
        Quaternion target = homeRotation * Quaternion.Euler(0f, sweep, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, data.turnSpeed * 0.25f * Time.deltaTime);
    }

    private void UpdateInvestigate()
    {
        if (perception.Awareness >= 1f)
        {
            EnterCombat(perception.LastSeenPosition, true);
            return;
        }

        if (data.combatStyle == EnemyCombatStyle.Sniper || HasArrived()) FaceTowards(lastKnownPosition, 0.5f);
        if (Time.time >= stateEndTime && perception.Awareness < data.suspicionThreshold) EnterPatrol();
    }

    private void UpdateSearch()
    {
        if (perception.CanSeePlayer)
        {
            EnterCombat(lastKnownPosition, true);
            return;
        }

        if (Time.time >= stateEndTime)
        {
            perception.SetAwareness(0f);
            EnterPatrol();
            return;
        }

        if (data.combatStyle == EnemyCombatStyle.Sniper)
        {
            FaceTowards(lastKnownPosition, 0.5f);
            return;
        }

        if (HasArrived() && Time.time >= nextSearchPointTime)
        {
            nextSearchPointTime = Time.time + Random.Range(2f, 4f);
            Vector3 random = lastKnownPosition + Random.insideUnitSphere * 8f;
            if (NavMesh.SamplePosition(random, out NavMeshHit hit, 4f, NavMesh.AllAreas)) MoveTo(hit.position);
        }
    }

    private void UpdateRingAlarm()
    {
        if (claimedBell == null || claimedBell.IsRung)
        {
            ReleaseBell();
            SetState(EnemyState.Combat);
            return;
        }

        agent.speed = data.chaseSpeed;
        if (Vector3.Distance(transform.position, claimedBell.RingPosition) <= agent.stoppingDistance + 0.6f)
        {
            claimedBell.Ring();
            claimedBell = null;
            SetState(EnemyState.Combat);
        }
    }

    private void UpdateCombat(PlayerTarget player)
    {
        bool sees = perception.CanSeePlayer;
        if (!sees && Time.time - lastSeenTime > data.memoryTime)
        {
            StartSearch();
            return;
        }

        FaceTowards(sees ? player.transform.position : lastKnownPosition, 1f);

        if (thrower != null && Time.time >= staggerUntil)
        {
            Vector3 target = lastKnownPosition;
            if (sees) target = player.transform.position + Vector3.ProjectOnPlane(player.Velocity, Vector3.up) * 0.6f;
            if (thrower.TryThrow(target, !sees, Time.time - lastSeenTime, All)) staggerUntil = Time.time + 0.7f;
        }

        switch (data.combatStyle)
        {
            case EnemyCombatStyle.Assault: UpdateAssault(player, sees); break;
            case EnemyCombatStyle.Ranged: UpdateRanged(player, sees); break;
            case EnemyCombatStyle.Sniper: UpdateSniper(player, sees); break;
        }
    }

    private void UpdateAssault(PlayerTarget player, bool sees)
    {
        agent.speed = data.chaseSpeed;

        if (Time.time >= nextRepositionTime)
        {
            nextRepositionTime = Time.time + Random.Range(data.repositionIntervalMin, data.repositionIntervalMax);
            ReleaseCover();

            if (!sees) MoveTo(lastKnownPosition);
            else if (data.usesCover && Random.value < data.coverChance && TryFindCover(player, true, out CoverPoint cover)) MoveToCover(cover);
            else MoveTo(PickCombatPosition(player.transform.position));
        }

        if (currentCover != null) UpdateCoverCycle(player, sees);
        else TryShoot(player, sees);
    }

    private void UpdateRanged(PlayerTarget player, bool sees)
    {
        agent.speed = data.chaseSpeed;
        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (currentCover != null && Time.time >= nextCoverCheckTime)
        {
            nextCoverCheckTime = Time.time + 0.5f;
            bool compromised = !currentCover.ProtectsFrom(player.HeadPoint) || distance < data.preferredMinDistance * 0.6f;
            if (compromised) ReleaseCover();
        }

        if (currentCover == null && Time.time >= nextRepositionTime)
        {
            nextRepositionTime = Time.time + Random.Range(data.repositionIntervalMin, data.repositionIntervalMax);

            if (!sees && Time.time - lastSeenTime > 2f) MoveTo(Vector3.Lerp(transform.position, lastKnownPosition, 0.5f));
            else if (data.usesCover && Random.value < data.coverChance && TryFindCover(player, false, out CoverPoint cover)) MoveToCover(cover);
            else MoveTo(PickCombatPosition(player.transform.position));
        }

        if (currentCover != null) UpdateCoverCycle(player, sees);
        else TryShoot(player, sees);
    }

    private void UpdateSniper(PlayerTarget player, bool sees)
    {
        StopMoving();

        if (!telegraphing)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (sees && distance <= data.attackRange && weapon.CanFire)
            {
                telegraphing = true;
                telegraphEndTime = Time.time + data.telegraphTime;
                trackedAimPoint = player.AimPoint;
                if (glint != null) glint.Show(data.telegraphTime);
            }
            else if (weapon.NeedsReload)
            {
                weapon.StartReload();
            }
            return;
        }

        if (sees) trackedAimPoint = Vector3.MoveTowards(trackedAimPoint, player.AimPoint, data.aimTrackingSpeed * Time.deltaTime);
        if (Time.time < telegraphEndTime) return;

        CancelTelegraph();
        weapon.TryFire(trackedAimPoint, data.baseSpread);
    }

    private void UpdateCoverCycle(PlayerTarget player, bool sees)
    {
        if (!arrivedAtCover)
        {
            if (Vector3.Distance(transform.position, currentCover.Position) > 0.7f)
            {
                TryShoot(player, sees);
                return;
            }

            arrivedAtCover = true;
            hidingInCover = true;
            coverPhaseEndTime = Time.time + data.coverStayTime * Random.Range(0.7f, 1.3f);
            StopMoving();
        }

        if (hidingInCover)
        {
            SetCrouched(true);
            if (weapon.MagazineLow) weapon.StartReload();
            if (Time.time >= coverPhaseEndTime && !weapon.IsReloading)
            {
                hidingInCover = false;
                coverPhaseEndTime = Time.time + data.peekTime * Random.Range(0.8f, 1.2f);
            }
            return;
        }

        SetCrouched(false);
        TryShoot(player, sees);
        if (Time.time >= coverPhaseEndTime || weapon.NeedsReload)
        {
            hidingInCover = true;
            coverPhaseEndTime = Time.time + data.coverStayTime * Random.Range(0.7f, 1.3f);
        }
    }

    private void TryShoot(PlayerTarget player, bool sees)
    {
        if (!sees || Time.time < staggerUntil) return;

        if (weapon.NeedsReload)
        {
            weapon.StartReload();
            return;
        }

        Vector3 toPlayer = player.AimPoint - weapon.Muzzle.position;
        float distance = toPlayer.magnitude;
        if (distance > data.attackRange) return;

        Vector3 flat = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        if (Vector3.Angle(transform.forward, flat) > 25f) return;

        float spread = data.baseSpread + distance * data.spreadPerMeter;
        spread += data.movingTargetSpread * Mathf.Clamp01(Vector3.ProjectOnPlane(player.Velocity, Vector3.up).magnitude / 7f);
        if (agent.velocity.sqrMagnitude > 0.5f) spread += data.movingShooterSpread;
        spread *= Mathf.Lerp(1.6f, 1f, (Time.time - aimStartTime) / data.aimSettleTime);

        weapon.TryFire(player.AimPoint, spread);
    }

    private bool TryFindCover(PlayerTarget player, bool advance, out CoverPoint best)
    {
        best = null;
        float bestScore = float.MaxValue;
        float myDistanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float idealDistance = (data.preferredMinDistance + data.preferredMaxDistance) * 0.5f;

        foreach (CoverPoint cover in CoverPoint.All)
        {
            if (!cover.IsAvailableFor(this)) continue;

            float distanceToCover = Vector3.Distance(transform.position, cover.Position);
            if (distanceToCover > data.coverSearchRadius) continue;

            float coverToPlayer = Vector3.Distance(cover.Position, player.transform.position);
            if (coverToPlayer < data.preferredMinDistance * 0.7f || coverToPlayer > data.attackRange) continue;
            if (advance && coverToPlayer > myDistanceToPlayer + 2f) continue;
            if (!cover.ProtectsFrom(player.HeadPoint)) continue;

            float score = distanceToCover + Mathf.Abs(coverToPlayer - idealDistance) * 0.5f;
            if (score < bestScore)
            {
                bestScore = score;
                best = cover;
            }
        }
        return best != null;
    }

    private void MoveToCover(CoverPoint cover)
    {
        ReleaseCover();
        if (!cover.TryOccupy(this)) return;
        currentCover = cover;
        arrivedAtCover = false;
        hidingInCover = false;
        MoveTo(cover.Position);
    }

    private void ReleaseCover()
    {
        if (currentCover != null) currentCover.Release(this);
        currentCover = null;
        arrivedAtCover = false;
        hidingInCover = false;
        SetCrouched(false);
    }

    private void ReleaseBell()
    {
        if (claimedBell != null) claimedBell.Release(this);
        claimedBell = null;
    }

    private Vector3 PickCombatPosition(Vector3 playerPosition)
    {
        Vector3 away = transform.position - playerPosition;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -transform.forward;
        away.Normalize();

        float distance = Random.Range(data.preferredMinDistance, data.preferredMaxDistance);
        Vector3 side = Vector3.Cross(Vector3.up, away) * Random.Range(-4f, 4f);
        Vector3 candidate = playerPosition + away * distance + side;

        return NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas) ? hit.position : transform.position;
    }

    private void FaceTowards(Vector3 point, float speedMultiplier)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return;
        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, data.turnSpeed * speedMultiplier * Time.deltaTime);
    }

    private void CancelTelegraph()
    {
        telegraphing = false;
        if (glint != null) glint.Hide();
    }

    private void SetCrouched(bool crouched)
    {
        if (isCrouched == crouched) return;
        isCrouched = crouched;

        float height = crouched ? crouchHeight : standingHeight;
        if (body != null)
        {
            body.height = height;
            body.center = new Vector3(0f, height * 0.5f, 0f);
        }
        if (visualRoot != null) visualRoot.localScale = new Vector3(1f, height / standingHeight, 1f);
    }

    private void SetEngaged(bool value)
    {
        if (engaged == value) return;
        engaged = value;

        PlayerTarget player = PlayerTarget.Instance;
        if (player == null || player.Combat == null) return;
        if (value) player.Combat.RegisterEngagement(this);
        else player.Combat.UnregisterEngagement(this);
    }

    private void SetState(EnemyState state)
    {
        if (State == state) return;
        State = state;
        agent.updateRotation = state != EnemyState.Combat && state != EnemyState.Investigate;
        StateChanged?.Invoke(state);
    }

    private bool HasArrived()
    {
        if (!agent.isOnNavMesh || agent.pathPending) return false;
        return agent.remainingDistance <= agent.stoppingDistance + 0.2f;
    }

    private void MoveTo(Vector3 destination)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    private void StopMoving()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    private void OnNoise(Vector3 position, float radius, GameObject source)
    {
        if (IsDead || source == gameObject) return;
        if (State == EnemyState.Combat || State == EnemyState.RingAlarm) return;
        if (Vector3.Distance(position, transform.position) > radius) return;

        PlayerTarget player = PlayerTarget.Instance;
        if (player == null || player.IsDead) return;

        bool fromPlayer = source != null && source.GetComponent<PlayerTarget>() != null;
        Vector3 known = fromPlayer ? position : player.transform.position;
        EnterCombat(known, false);
    }

    private void OnDamaged(DamageInfo info)
    {
        if (IsDead) return;
        staggerUntil = Time.time + data.staggerTime;

        Vector3 known = info.source != null ? info.source.transform.position : transform.position - info.direction * 10f;
        if (State != EnemyState.Combat && State != EnemyState.RingAlarm) EnterCombat(known, true);
        else lastKnownPosition = known;
    }

    private void OnDied(DamageInfo info)
    {
        SetEngaged(false);
        ReleaseCover();
        ReleaseBell();
        CancelTelegraph();
        StopAllCoroutines();
        SetState(EnemyState.Dead);

        weapon.enabled = false;
        perception.enabled = false;
        if (agent.isOnNavMesh) agent.ResetPath();
        agent.enabled = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            Vector3 push = (info.direction.sqrMagnitude > 0.01f ? info.direction.normalized : -transform.forward) + Vector3.up * 0.3f;
            rb.AddForceAtPosition(push * data.deathImpulse, info.point, ForceMode.Impulse);
        }

        DropLoot();
        Killed?.Invoke(this);
        AnyKilled?.Invoke(this);
        Destroy(gameObject, data.corpseLifetime);
    }

    private void DropLoot()
    {
        foreach (LootEntry entry in data.loot)
        {
            if (entry.prefab == null || Random.value > entry.chance) continue;
            Vector3 offset = Random.insideUnitSphere * 0.6f;
            offset.y = 0f;
            Instantiate(entry.prefab, transform.position + offset, Quaternion.identity);
        }
    }
}
