using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private SoundSO footstep;
    [SerializeField] private SoundSO landing;
    [SerializeField] private float strideLength = 2.1f;
    [SerializeField] private float sprintStrideMultiplier = 1.35f;
    [SerializeField, Range(0f, 1f)] private float crouchVolume = 0.35f;

    private float distanceAccumulator;

    private void OnEnable() => motor.Landed += OnLanded;
    private void OnDisable() => motor.Landed -= OnLanded;

    private void Update()
    {
        if (!motor.IsGrounded || !motor.IsMoving) return;

        Vector3 velocity = motor.Velocity;
        velocity.y = 0f;
        distanceAccumulator += velocity.magnitude * Time.deltaTime;

        float stride = motor.IsSprinting ? strideLength * sprintStrideMultiplier : strideLength;
        if (distanceAccumulator < stride) return;
        distanceAccumulator = 0f;

        AudioSource source = AudioManager.Play2D(footstep);
        if (source != null && motor.IsCrouching) source.volume *= crouchVolume;
    }

    private void OnLanded() => AudioManager.Play2D(landing);
}
