using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [SerializeField] private PlayerStatsSO stats;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private LayerMask ceilingMask = ~0;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float currentHeight;
    private float lastGroundedTime = float.NegativeInfinity;
    private bool crouchToggled;
    private float speedMultiplier = 1f;
    private bool sprintBlocked;

    public bool IsGrounded => controller.isGrounded;
    public bool IsSprinting { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsMoving => horizontalVelocity.sqrMagnitude > 0.1f;
    public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;
    public float NormalizedSpeed => stats.sprintSpeed > 0f ? horizontalVelocity.magnitude / stats.sprintSpeed : 0f;

    public event Action Jumped;
    public event Action Landed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        currentHeight = stats.standingHeight;
        ApplyHeight(currentHeight);
    }

    private void OnEnable()
    {
        input.JumpPressed += TryJump;
        input.CrouchPressed += OnCrouchPressed;
    }

    private void OnDisable()
    {
        input.JumpPressed -= TryJump;
        input.CrouchPressed -= OnCrouchPressed;
    }

    private void Update()
    {
        UpdateCrouch();
        UpdateMovement();
    }

    public void SetSpeedMultiplier(float multiplier) => speedMultiplier = Mathf.Max(0f, multiplier);

    public void SetSprintBlocked(bool blocked) => sprintBlocked = blocked;

    private void UpdateMovement()
    {
        bool wasGrounded = controller.isGrounded;
        if (wasGrounded) lastGroundedTime = Time.time;

        Vector2 moveInput = input.Move;
        Vector3 wishDirection = Vector3.ClampMagnitude(transform.right * moveInput.x + transform.forward * moveInput.y, 1f);

        IsSprinting = CanSprint(moveInput);
        Vector3 targetVelocity = wishDirection * GetTargetSpeed(moveInput);

        float rate;
        if (!controller.isGrounded) rate = stats.airAcceleration;
        else rate = targetVelocity.sqrMagnitude >= horizontalVelocity.sqrMagnitude ? stats.acceleration : stats.deceleration;

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * Time.deltaTime);

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = stats.groundedStickForce;
        verticalVelocity += stats.gravity * Time.deltaTime;

        CollisionFlags flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
        if (!wasGrounded && controller.isGrounded) Landed?.Invoke();
    }

    private bool CanSprint(Vector2 moveInput)
    {
        if (!input.SprintHeld || sprintBlocked || moveInput.y < 0.1f) return false;

        if (IsCrouching)
        {
            if (!CanStandUp()) return false;
            IsCrouching = false;
            crouchToggled = false;
        }

        return controller.isGrounded || IsSprinting;
    }

    private float GetTargetSpeed(Vector2 moveInput)
    {
        float speed = IsCrouching ? stats.crouchSpeed : IsSprinting ? stats.sprintSpeed : stats.walkSpeed;
        if (moveInput.y < -0.1f) speed *= stats.backwardMultiplier;
        return speed * speedMultiplier;
    }

    private void TryJump()
    {
        if (!enabled) return;

        if (IsCrouching)
        {
            if (CanStandUp())
            {
                IsCrouching = false;
                crouchToggled = false;
            }
            return;
        }

        bool canJump = controller.isGrounded || Time.time - lastGroundedTime <= stats.coyoteTime;
        if (!canJump || verticalVelocity > 0f) return;

        verticalVelocity = Mathf.Sqrt(stats.jumpHeight * -2f * stats.gravity);
        lastGroundedTime = float.NegativeInfinity;
        Jumped?.Invoke();
    }

    private void OnCrouchPressed()
    {
        if (stats.crouchIsToggle) crouchToggled = !crouchToggled;
    }

    private void UpdateCrouch()
    {
        bool wantsCrouch = stats.crouchIsToggle ? crouchToggled : input.CrouchHeld;

        if (wantsCrouch) IsCrouching = true;
        else if (IsCrouching && CanStandUp()) IsCrouching = false;

        float targetHeight = IsCrouching ? stats.crouchHeight : stats.standingHeight;
        if (!Mathf.Approximately(currentHeight, targetHeight))
        {
            currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, stats.crouchTransitionSpeed * Time.deltaTime);
            ApplyHeight(currentHeight);
        }
    }

    private bool CanStandUp()
    {
        float radius = controller.radius * 0.9f;
        Vector3 origin = transform.position + Vector3.up * (currentHeight - controller.radius);
        float distance = stats.standingHeight - currentHeight + 0.05f;
        return !Physics.SphereCast(origin, radius, Vector3.up, out _, distance, ceilingMask, QueryTriggerInteraction.Ignore);
    }

    private void ApplyHeight(float height)
    {
        controller.height = height;
        controller.center = Vector3.up * (height * 0.5f);

        Vector3 pivotPosition = cameraPivot.localPosition;
        pivotPosition.y = height - stats.eyeOffsetFromTop;
        cameraPivot.localPosition = pivotPosition;
    }
}
