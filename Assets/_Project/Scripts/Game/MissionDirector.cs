using System.Collections.Generic;
using UnityEngine;

public class MissionDirector : MonoBehaviour
{
    private enum MissionPhase
    {
        BreachGate,
        FindKey,
        OpenCourtyard,
        BossFight,
        Escape,
        Finished
    }

    [Header("Referencias")]
    [SerializeField] private ObjectiveHUD objectives;
    [SerializeField] private GameStateManager gameState;
    [SerializeField] private PlayerKeyring keyring;
    [SerializeField] private DestructibleBarrier mainGate;
    [SerializeField] private LockedDoor courtyardDoor;
    [SerializeField] private PlayerZoneTrigger courtyardZone;
    [SerializeField] private BossController boss;
    [SerializeField] private PlayerZoneTrigger exitZone;
    [SerializeField] private List<EnemySpawnGroup> escapeGroups = new List<EnemySpawnGroup>();
    [SerializeField] private string courtyardKeyId = "llave_patio";
    [SerializeField] private Checkpoint bossCheckpoint;
    [SerializeField] private Checkpoint diabloCheckpoint;
    [SerializeField] private Checkpoint escapeCheckpoint;
    [SerializeField] private ZoneEncounter roadAmbush;

    [Header("Textos")]
    [SerializeField] private string breachText = "Volá el portón de Fort Mercer con dinamita";
    [SerializeField] private string findKeyText = "Encontrá la llave del patio: la lleva el Capataz del cuartel oeste";
    [SerializeField] private string openDoorText = "Abrí la puerta del patio en las galerías este";
    [SerializeField] private string bossText = "Matá a Dirty Holliday";
    [SerializeField] private string waveText = "Eliminá a los hombres de Holliday";
    [SerializeField] private string diabloText = "¡Holliday volvió! Terminá con él";
    [SerializeField] private string escapeText = "Agarrá la escopeta de Holliday y escapá del fuerte por el portón";
    [SerializeField] private string retryBossText = "Volvé a abrir la puerta del patio y enfrentá a Holliday";

    private MissionPhase phase;

    public bool IsBossFightActive => phase == MissionPhase.BossFight;
    public Checkpoint BossEntranceCheckpoint => bossCheckpoint;
    public Checkpoint DiabloCheckpoint => diabloCheckpoint;

    public void ResetBossFight()
    {
        SetPhase(MissionPhase.OpenCourtyard, retryBossText);
        if (courtyardDoor != null) courtyardDoor.CloseUnlocked();
        if (courtyardZone != null) courtyardZone.ResetTrigger();
    }
    private float nextGateHintTime;

    private void Start()
    {
        SetPhase(MissionPhase.BreachGate, breachText);
    }

    private void OnEnable()
    {
        if (mainGate != null)
        {
            mainGate.Broken += OnGateBroken;
            mainGate.RejectedHit += OnGateRejected;
        }
        if (keyring != null) keyring.KeyAdded += OnKeyAdded;
        if (courtyardDoor != null) courtyardDoor.Denied += OnDoorDenied;
        if (courtyardZone != null) courtyardZone.Entered += OnCourtyardEntered;
        if (boss != null)
        {
            boss.PhaseChanged += OnBossPhaseChanged;
            boss.Defeated += OnBossDefeated;
        }
        if (exitZone != null) exitZone.Entered += OnExitReached;
    }

    private void OnDisable()
    {
        if (mainGate != null)
        {
            mainGate.Broken -= OnGateBroken;
            mainGate.RejectedHit -= OnGateRejected;
        }
        if (keyring != null) keyring.KeyAdded -= OnKeyAdded;
        if (courtyardDoor != null) courtyardDoor.Denied -= OnDoorDenied;
        if (courtyardZone != null) courtyardZone.Entered -= OnCourtyardEntered;
        if (boss != null)
        {
            boss.PhaseChanged -= OnBossPhaseChanged;
            boss.Defeated -= OnBossDefeated;
        }
        if (exitZone != null) exitZone.Entered -= OnExitReached;
    }

    private void OnGateBroken()
    {
        if (phase != MissionPhase.BreachGate) return;
        SetPhase(keyring != null && keyring.Has(courtyardKeyId) ? MissionPhase.OpenCourtyard : MissionPhase.FindKey,
                 keyring != null && keyring.Has(courtyardKeyId) ? openDoorText : findKeyText);
    }

    private void OnGateRejected()
    {
        if (Time.time < nextGateHintTime) return;
        nextGateHintTime = Time.time + 4f;
        objectives.Hint("Las balas no le hacen nada al portón. Probá con dinamita [G]");
    }

    private void OnKeyAdded(string keyId, string displayName)
    {
        if (keyId != courtyardKeyId) return;
        objectives.Hint($"Conseguiste {displayName}");
        if (phase == MissionPhase.FindKey || phase == MissionPhase.BreachGate) SetPhase(MissionPhase.OpenCourtyard, openDoorText);
    }

    private void OnDoorDenied()
    {
        objectives.Hint("Está cerrada con llave. La tiene el Capataz del cuartel oeste");
    }

    private void OnCourtyardEntered()
    {
        if (phase == MissionPhase.BossFight || phase == MissionPhase.Escape || phase == MissionPhase.Finished) return;
        if (courtyardDoor != null && !courtyardDoor.IsOpen) return;

        SetPhase(MissionPhase.BossFight, bossText);
        if (courtyardDoor != null) courtyardDoor.CloseAndSeal();
        objectives.Hint("¡La puerta se cerró detrás tuyo!");
        if (CheckpointManager.Instance != null && bossCheckpoint != null) CheckpointManager.Instance.Activate(bossCheckpoint, false);
        if (boss != null) boss.StartFight();
    }

    private void OnBossPhaseChanged(BossPhase bossPhase)
    {
        if (phase != MissionPhase.BossFight) return;
        if (bossPhase == BossPhase.Retreat) objectives.Show(waveText);
        else if (bossPhase == BossPhase.Diablo)
        {
            objectives.Show(diabloText);
            if (CheckpointManager.Instance != null && diabloCheckpoint != null) CheckpointManager.Instance.Activate(diabloCheckpoint, false);
        }
    }

    private void OnBossDefeated()
    {
        SetPhase(MissionPhase.Escape, escapeText);
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.IsEscaping = true;
            if (escapeCheckpoint != null) CheckpointManager.Instance.Activate(escapeCheckpoint, false);
        }
        if (roadAmbush != null) roadAmbush.Activate();
        objectives.Hint("Dirty Holliday está muerto. Llegan refuerzos de la banda...", 4f);

        if (courtyardDoor != null)
        {
            courtyardDoor.Unseal();
            courtyardDoor.Open();
        }

        foreach (EnemySpawnGroup group in escapeGroups)
            if (group != null) group.Arm();

        if (exitZone != null) exitZone.ResetTrigger();
    }

    private void OnExitReached()
    {
        if (phase != MissionPhase.Escape) return;
        SetPhase(MissionPhase.Finished, "Escapaste de Fort Mercer");
        if (gameState != null) gameState.Win();
    }

    private void SetPhase(MissionPhase newPhase, string text)
    {
        phase = newPhase;
        objectives.Show(text);
    }
}
