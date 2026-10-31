using UnityEngine;

public class PlayerTarget : MonoBehaviour
{
    [SerializeField] private Transform headPoint;
    [SerializeField] private float chestOffset = 0.45f;

    private Health health;
    private PlayerMotor motor;
    private CombatTracker combat;

    public static PlayerTarget Instance { get; private set; }

    public Vector3 HeadPoint => headPoint.position;
    public Vector3 AimPoint => headPoint.position + Vector3.down * chestOffset;
    public Vector3 Velocity => motor.Velocity;
    public bool IsCrouching => motor.IsCrouching;
    public bool IsDead => health.IsDead;
    public Health Health => health;
    public CombatTracker Combat => combat;

    private void Awake()
    {
        Instance = this;
        health = GetComponent<Health>();
        motor = GetComponent<PlayerMotor>();
        combat = GetComponent<CombatTracker>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
