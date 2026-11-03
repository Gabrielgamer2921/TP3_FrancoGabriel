using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyCombatStyle
{
    Assault,
    Ranged,
    Sniper
}

[Serializable]
public class LootEntry
{
    public GameObject prefab;
    [Range(0f, 1f)] public float chance = 0.5f;
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Cowboy FPS/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    [Header("General")]
    public string displayName = "Pistolero";
    public EnemyCombatStyle combatStyle = EnemyCombatStyle.Assault;
    [Min(1f)] public float maxHealth = 80f;
    [Min(0f)] public float staggerTime = 0.25f;
    [Min(1f)] public float corpseLifetime = 12f;
    [Min(0f)] public float deathImpulse = 4f;

    [Header("Movimiento")]
    [Min(0f)] public float patrolSpeed = 2f;
    [Min(0f)] public float chaseSpeed = 4.8f;
    [Min(0f)] public float turnSpeed = 360f;
    [Min(0f)] public float patrolWaitTime = 2f;

    [Header("Percepción")]
    [Min(1f)] public float viewDistance = 28f;
    [Range(10f, 360f)] public float viewAngle = 110f;
    [Min(0f)] public float closeAwarenessRange = 2.5f;
    [Min(0.05f)] public float detectionTime = 0.7f;
    [Range(0f, 1f)] public float suspicionThreshold = 0.35f;
    [Min(0f)] public float awarenessDecay = 0.25f;
    [Min(0f)] public float alertAllyRadius = 20f;
    [Min(0f)] public float memoryTime = 5f;
    [Min(0f)] public float searchDuration = 10f;
    [Min(0f)] public float investigateDuration = 5f;

    [Header("Disparo")]
    [Min(0f)] public float damage = 8f;
    [Min(0.05f)] public float timeBetweenShots = 0.55f;
    [Min(1)] public int magazineSize = 6;
    [Min(0f)] public float reloadTime = 2.2f;
    [Min(1f)] public float attackRange = 22f;
    [Min(1f)] public float bulletSpeed = 110f;
    [Min(0f)] public float shotNoiseRadius = 30f;

    [Header("Perdigones y caída de daño")]
    [Min(1)] public int pelletsPerShot = 1;
    [Min(0f)] public float pelletSpread;
    [Min(0f)] public float falloffStartDistance = 1000f;
    [Min(0f)] public float falloffEndDistance = 1000f;
    [Range(0f, 1f)] public float minFalloffMultiplier = 1f;

    [Header("Puntería (grados)")]
    [Min(0f)] public float baseSpread = 3.5f;
    [Min(0f)] public float spreadPerMeter = 0.08f;
    [Min(0f)] public float movingTargetSpread = 2.5f;
    [Min(0f)] public float movingShooterSpread = 1.5f;
    [Min(0.01f)] public float aimSettleTime = 1.2f;

    [Header("Posicionamiento")]
    [Min(0f)] public float preferredMinDistance = 5f;
    [Min(0f)] public float preferredMaxDistance = 11f;
    [Min(0.1f)] public float repositionIntervalMin = 2f;
    [Min(0.1f)] public float repositionIntervalMax = 3.5f;

    [Header("Cobertura")]
    public bool usesCover = true;
    [Range(0f, 1f)] public float coverChance = 0.4f;
    [Min(0f)] public float coverSearchRadius = 12f;
    [Min(0f)] public float coverStayTime = 1.2f;
    [Min(0f)] public float peekTime = 2f;

    [Header("Alarma")]
    public bool canRingAlarm = true;
    [Min(0f)] public float alarmSearchRadius = 40f;

    [Header("Dinamita")]
    public ThrowableDataSO throwable;
    [Min(0)] public int dynamiteCount;
    [Min(0f)] public float dynamiteCooldown = 6f;
    [Min(0f)] public float hiddenTimeBeforeThrow = 3f;

    [Header("Dinamita: nivel de inteligencia (d20)")]
    [Range(0, 20)] public int dynamiteLevelMin = 5;
    [Range(0, 20)] public int dynamiteLevelMax = 7;
    [Min(0.5f)] public float dynamiteRollInterval = 5f;
    [Range(0, 20)] public int hiddenLevelBonus = 4;
    public bool logDynamiteRolls;
    [Min(0f)] public float dynamiteMinDistance = 6f;
    [Min(0f)] public float dynamiteMaxDistance = 22f;
    [Min(0f)] public float fuseAfterLanding = 0.8f;

    [Header("Francotirador")]
    [Min(0f)] public float telegraphTime = 1.5f;
    [Min(0f)] public float aimTrackingSpeed = 5f;
    [Range(0f, 90f)] public float idleSweepAngle = 40f;

    [Header("Botín")]
    public List<LootEntry> loot = new List<LootEntry>();
}
