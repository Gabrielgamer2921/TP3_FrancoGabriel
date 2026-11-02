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

    private readonly Dictionary<AmmoTypeSO, int> savedAmmo = new Dictionary<AmmoTypeSO, int>();
    private Checkpoint current;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private int savedBandages;
    private int savedDynamite;

    public static CheckpointManager Instance { get; private set; }
    public Checkpoint Current => current;

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

    public void Activate(Checkpoint checkpoint, bool announce)
    {
        if (checkpoint == null || checkpoint == current) return;
        current = checkpoint;
        TakeSnapshot();
        if (announce && objectives != null) objectives.Hint($"Punto de control: {checkpoint.DisplayName}", 2.5f);
    }

    public void Respawn()
    {
        foreach (EnemyAI enemy in new List<EnemyAI>(EnemyAI.All)) enemy.ForgetPlayer();
        if (boss != null) boss.OnPlayerRespawned();

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
