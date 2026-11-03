using UnityEngine;

public class WeaponViewAnimator : MonoBehaviour
{
    [SerializeField] private PlayerWeaponController weapons;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerMelee melee;
    [SerializeField] private Camera playerCamera;

    [Header("Campo de visión")]
    [SerializeField] private float baseFov = 70f;
    [SerializeField] private float sprintFovBonus = 5f;
    [SerializeField] private float fovLerpSpeed = 10f;

    [Header("Pose de correr")]
    [SerializeField] private Vector3 sprintPositionOffset = new Vector3(-0.05f, -0.06f, 0f);
    [SerializeField] private Vector3 sprintRotation = new Vector3(10f, -30f, 10f);
    [SerializeField] private float sprintBlendSpeed = 6f;

    [Header("Balanceo")]
    [SerializeField] private float swayAmount = 0.002f;
    [SerializeField] private float swayMax = 0.05f;
    [SerializeField] private float swaySmooth = 8f;
    [SerializeField] private float bobFrequency = 9f;
    [SerializeField] private float bobAmplitude = 0.012f;

    [Header("Retroceso visual y equipar")]
    [SerializeField] private float recoilReturnSpeed = 12f;
    [SerializeField] private float equipDropDistance = 0.25f;
    [SerializeField] private float equipDropAngle = 35f;

    [Header("Arma guardada")]
    [SerializeField] private float holsterDropDistance = 0.4f;
    [SerializeField] private float holsterAngle = 50f;
    [SerializeField] private float holsterSpeed = 5f;

    [Header("Culatazo")]
    [SerializeField] private float meleeDuration = 0.35f;
    [SerializeField] private Vector3 meleePositionOffset = new Vector3(0.05f, 0.02f, 0.25f);
    [SerializeField] private Vector3 meleeRotation = new Vector3(-20f, 0f, -35f);

    private Weapon current;
    private float aimBlend;
    private float sprintBlend;
    private float equipBlend;
    private float holsterBlend;
    private float bobTimer;
    private float meleeTimer;
    private Vector3 swayOffset;
    private Vector3 recoilOffset;
    private Vector3 recoilRotation;

    public float AimBlend => aimBlend;

    private void OnEnable()
    {
        weapons.WeaponChanged += OnWeaponChanged;
        if (melee != null) melee.Swung += OnMeleeSwung;
        if (weapons.CurrentWeapon != null) OnWeaponChanged(weapons.CurrentWeapon);
    }

    private void OnDisable()
    {
        weapons.WeaponChanged -= OnWeaponChanged;
        if (melee != null) melee.Swung -= OnMeleeSwung;
        if (current != null) current.Fired -= OnFired;
    }

    private void OnWeaponChanged(Weapon weapon)
    {
        if (current != null) current.Fired -= OnFired;
        current = weapon;
        if (current != null) current.Fired += OnFired;
        equipBlend = 0f;
        recoilOffset = Vector3.zero;
        recoilRotation = Vector3.zero;
    }

    private void OnFired()
    {
        recoilOffset += Vector3.back * current.Data.visualKickBack;
        recoilRotation += new Vector3(-current.Data.visualKickRotation, Random.Range(-2f, 2f), 0f);
    }

    private void OnMeleeSwung()
    {
        meleeTimer = meleeDuration;
    }

    private void LateUpdate()
    {
        if (current == null) return;

        WeaponDataSO data = current.Data;
        float dt = Time.deltaTime;

        aimBlend = Mathf.MoveTowards(aimBlend, weapons.IsAiming ? 1f : 0f, data.aimSpeed * dt);
        sprintBlend = Mathf.MoveTowards(sprintBlend, motor.IsSprinting ? 1f : 0f, sprintBlendSpeed * dt);
        equipBlend = Mathf.MoveTowards(equipBlend, 1f, dt / Mathf.Max(0.01f, data.equipTime));
        meleeTimer = Mathf.Max(0f, meleeTimer - dt);
        holsterBlend = Mathf.MoveTowards(holsterBlend, weapons.IsHolstered ? 1f : 0f, holsterSpeed * dt);
        float holsterSmooth = Mathf.SmoothStep(0f, 1f, holsterBlend);

        float aimSmooth = Mathf.SmoothStep(0f, 1f, aimBlend);
        float equipSmooth = Mathf.SmoothStep(0f, 1f, equipBlend);
        float meleeWeight = meleeTimer > 0f ? Mathf.Sin((1f - meleeTimer / meleeDuration) * Mathf.PI) : 0f;

        Vector2 lookDelta = input.Look;
        Vector3 swayTarget = new Vector3(-lookDelta.x, -lookDelta.y, 0f) * swayAmount * (1f - aimSmooth * 0.8f);
        swayTarget = Vector3.ClampMagnitude(swayTarget, swayMax);
        swayOffset = Vector3.Lerp(swayOffset, swayTarget, swaySmooth * dt);

        Vector3 bob = Vector3.zero;
        if (motor.IsGrounded && motor.IsMoving)
        {
            bobTimer += dt * bobFrequency * (1f + sprintBlend * 0.4f);
            float amplitude = bobAmplitude * (1f + sprintBlend) * (1f - aimSmooth * 0.85f);
            bob = new Vector3(Mathf.Cos(bobTimer) * amplitude, Mathf.Abs(Mathf.Sin(bobTimer)) * amplitude, 0f);
        }

        recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, recoilReturnSpeed * dt);
        recoilRotation = Vector3.Lerp(recoilRotation, Vector3.zero, recoilReturnSpeed * dt);

        Vector3 position = Vector3.Lerp(current.HipLocalPosition, current.AimLocalPosition, aimSmooth)
            + sprintPositionOffset * sprintBlend
            + swayOffset + bob + recoilOffset
            + meleePositionOffset * meleeWeight
            + Vector3.down * equipDropDistance * (1f - equipSmooth)
            + Vector3.down * holsterDropDistance * holsterSmooth;

        Vector3 rotation = sprintRotation * sprintBlend
            + recoilRotation
            + meleeRotation * meleeWeight
            + new Vector3(equipDropAngle * (1f - equipSmooth) + holsterAngle * holsterSmooth, 0f, 0f);

        current.transform.localPosition = position;
        current.transform.localRotation = Quaternion.Euler(rotation);

        float targetFov = Mathf.Lerp(baseFov, data.aimFov, aimSmooth) + sprintFovBonus * sprintBlend;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, fovLerpSpeed * dt);
    }
}
