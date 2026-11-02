using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private ObjectiveHUD objectives;
    [SerializeField] private PlayerDeath playerDeath;
    [SerializeField] private PlayerAmmoInventory ammo;
    [SerializeField] private PlayerWeaponController weapons;
    [SerializeField] private PlayerBandages bandages;
    [SerializeField] private PlayerThrowController thrower;
    [SerializeField] private BossController boss;
    [SerializeField] private MissionDirector director;
    [SerializeField] private List<MonoBehaviour> resetOnStartRespawn = new List<MonoBehaviour>();

    private readonly Dictionary<AmmoTypeSO, int> savedAmmo = new Dictionary<AmmoTypeSO, int>();
    private Checkpoint current;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private int savedBandages;
    private int savedDynamite;

    public static CheckpointManager Instance { get; private set; }
    public Checkpoint Current => current;
    public bool IsEscaping { get; set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        startPosition = playerDeath.transform.position;
        startRotation = playerDeath.transform.rotation;
        TakeSnapshot();
    }

    public void Activate(Checkpoint checkpoint, bool fromZone)
    {
        if (checkpoint == null || checkpoint == current) return;
        if (fromZone && checkpoint.ActiveDuring == Checkpoint.Phase.IngressOnly && IsEscaping) return;
        if (fromZone && checkpoint.ActiveDuring == Checkpoint.Phase.EscapeOnly && !IsEscaping) return;

        current = checkpoint;
        checkpoint.OnReached();
        TakeSnapshot();
        if (objectives != null) objectives.Hint($"Punto de control: {checkpoint.DisplayName}", 2.5f);
    }

    public void Respawn()
    {
        foreach (EnemyAI enemy in new List<EnemyAI>(EnemyAI.All)) enemy.ForgetPlayer();

        if (director != null && director.IsBossFightActive && boss != null)
        {
            if (boss.Phase == BossPhase.Diablo && director.DiabloCheckpoint != null)
            {
                boss.ResetDiablo();
                current = director.DiabloCheckpoint;
            }
            else
            {
                boss.ResetToWaiting();
                director.ResetBossFight();
                if (director.BossEntranceCheckpoint != null) current = director.BossEntranceCheckpoint;
            }
        }
        else if (current != null)
        {
            current.ResetEncounters();
        }
        else
        {
            foreach (MonoBehaviour target in resetOnStartRespawn)
                if (target is IResettable resettable) resettable.ResetState();
        }

        Vector3 position = current != null ? current.SpawnPosition : startPosition;
        Quaternion rotation = current != null ? current.SpawnRotation : startRotation;
        playerDeath.Revive(position, rotation);

        RestoreSnapshot();
        foreach (Weapon weapon in weapons.Weapons) weapon.RefillMagazine();
        if (objectives != null) objectives.Hint("Volviste al último punto de control", 2.5f);
    }

    private void TakeSnapshot()
    {
        savedAmmo.Clear();
        foreach (Weapon weapon in weapons.Weapons)
        {
            AmmoTypeSO type = weapon.Data.ammoType;
            if (type != null) savedAmmo[type] = ammo.GetReserve(type);
        }
        savedBandages = bandages.Count;
        savedDynamite = thrower.Count;
    }

    private void RestoreSnapshot()
    {
        foreach (KeyValuePair<AmmoTypeSO, int> entry in savedAmmo)
        {
            int missing = entry.Value - ammo.GetReserve(entry.Key);
            if (missing > 0) ammo.Add(entry.Key, missing);
        }

        if (savedBandages > bandages.Count) bandages.AddBandages(savedBandages - bandages.Count);
        if (savedDynamite > thrower.Count) thrower.AddDynamite(savedDynamite - thrower.Count);
    }
}
