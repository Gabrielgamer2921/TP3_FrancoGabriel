using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionAsset actions;
    [SerializeField] private string mapName = "Player";

    private InputActionMap map;
    private InputAction move, look, fire, aim, reload, jump, sprint, crouch, interact, melee, throwAction, bandage, weapon1, weapon2, weapon3, switchWeapon, pause;

    public Vector2 Move => move.ReadValue<Vector2>();
    public Vector2 Look => look.ReadValue<Vector2>();
    public bool FireHeld => fire.IsPressed();
    public bool AimHeld => aim.IsPressed();
    public bool SprintHeld => sprint.IsPressed();
    public bool CrouchHeld => crouch.IsPressed();

    public event Action FirePressed;
    public event Action ReloadPressed;
    public event Action JumpPressed;
    public event Action CrouchPressed;
    public event Action InteractPressed;
    public event Action MeleePressed;
    public event Action ThrowPressed;
    public event Action ThrowReleased;
    public event Action BandagePressed;
    public event Action PausePressed;
    public event Action<int> WeaponSlotPressed;
    public event Action<int> WeaponScrolled;

    private void Awake()
    {
        map = actions.FindActionMap(mapName, true);
        move = map.FindAction("Move", true);
        look = map.FindAction("Look", true);
        fire = map.FindAction("Fire", true);
        aim = map.FindAction("Aim", true);
        reload = map.FindAction("Reload", true);
        jump = map.FindAction("Jump", true);
        sprint = map.FindAction("Sprint", true);
        crouch = map.FindAction("Crouch", true);
        interact = map.FindAction("Interact", true);
        melee = map.FindAction("Melee", true);
        throwAction = map.FindAction("Throw", true);
        bandage = map.FindAction("Bandage", true);
        weapon1 = map.FindAction("Weapon1", true);
        weapon2 = map.FindAction("Weapon2", true);
        weapon3 = map.FindAction("Weapon3", false);
        switchWeapon = map.FindAction("SwitchWeapon", true);
        pause = map.FindAction("Pause", true);
    }

    private void OnEnable()
    {
        fire.performed += OnFire;
        reload.performed += OnReload;
        jump.performed += OnJump;
        crouch.performed += OnCrouch;
        interact.performed += OnInteract;
        melee.performed += OnMelee;
        throwAction.started += OnThrowStarted;
        throwAction.canceled += OnThrowCanceled;
        bandage.performed += OnBandage;
        weapon1.performed += OnWeapon1;
        weapon2.performed += OnWeapon2;
        if (weapon3 != null) weapon3.performed += OnWeapon3;
        switchWeapon.performed += OnSwitchWeapon;
        pause.performed += OnPause;
        map.Enable();
    }

    private void OnDisable()
    {
        fire.performed -= OnFire;
        reload.performed -= OnReload;
        jump.performed -= OnJump;
        crouch.performed -= OnCrouch;
        interact.performed -= OnInteract;
        melee.performed -= OnMelee;
        throwAction.started -= OnThrowStarted;
        throwAction.canceled -= OnThrowCanceled;
        bandage.performed -= OnBandage;
        weapon1.performed -= OnWeapon1;
        weapon2.performed -= OnWeapon2;
        if (weapon3 != null) weapon3.performed -= OnWeapon3;
        switchWeapon.performed -= OnSwitchWeapon;
        pause.performed -= OnPause;
        map.Disable();
    }

    public void SetGameplayEnabled(bool enabled)
    {
        if (enabled) map.Enable();
        else map.Disable();
        pause.Enable();
    }

    private void OnFire(InputAction.CallbackContext ctx) => FirePressed?.Invoke();
    private void OnReload(InputAction.CallbackContext ctx) => ReloadPressed?.Invoke();
    private void OnJump(InputAction.CallbackContext ctx) => JumpPressed?.Invoke();
    private void OnCrouch(InputAction.CallbackContext ctx) => CrouchPressed?.Invoke();
    private void OnInteract(InputAction.CallbackContext ctx) => InteractPressed?.Invoke();
    private void OnMelee(InputAction.CallbackContext ctx) => MeleePressed?.Invoke();
    private void OnThrowStarted(InputAction.CallbackContext ctx) => ThrowPressed?.Invoke();
    private void OnThrowCanceled(InputAction.CallbackContext ctx) => ThrowReleased?.Invoke();
    private void OnBandage(InputAction.CallbackContext ctx) => BandagePressed?.Invoke();
    private void OnWeapon1(InputAction.CallbackContext ctx) => WeaponSlotPressed?.Invoke(0);
    private void OnWeapon2(InputAction.CallbackContext ctx) => WeaponSlotPressed?.Invoke(1);
    private void OnWeapon3(InputAction.CallbackContext ctx) => WeaponSlotPressed?.Invoke(2);
    private void OnPause(InputAction.CallbackContext ctx) => PausePressed?.Invoke();

    private void OnSwitchWeapon(InputAction.CallbackContext ctx)
    {
        float value = ctx.ReadValue<float>();
        if (Mathf.Abs(value) > 0.01f) WeaponScrolled?.Invoke(value > 0f ? 1 : -1);
    }
}
