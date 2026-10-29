using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Cowboy FPS/Player Stats")]
public class PlayerStatsSO : ScriptableObject
{
    [Header("Movimiento")]
    [Min(0f)] public float walkSpeed = 4.5f;
    [Min(0f)] public float sprintSpeed = 7f;
    [Min(0f)] public float crouchSpeed = 2.2f;
    [Range(0.1f, 1f)] public float backwardMultiplier = 0.75f;
    [Min(0f)] public float acceleration = 30f;
    [Min(0f)] public float deceleration = 40f;
    [Min(0f)] public float airAcceleration = 8f;

    [Header("Salto y gravedad")]
    [Min(0f)] public float jumpHeight = 1.1f;
    public float gravity = -22f;
    public float groundedStickForce = -2f;
    [Min(0f)] public float coyoteTime = 0.12f;

    [Header("Agacharse")]
    [Min(0.5f)] public float standingHeight = 1.8f;
    [Min(0.5f)] public float crouchHeight = 1.1f;
    [Min(0.1f)] public float crouchTransitionSpeed = 6f;
    [Min(0f)] public float eyeOffsetFromTop = 0.15f;
    public bool crouchIsToggle = true;

    [Header("Cámara")]
    [Min(0.01f)] public float mouseSensitivity = 0.12f;
    [Range(45f, 90f)] public float maxLookAngle = 85f;
    public bool invertY = false;
}
