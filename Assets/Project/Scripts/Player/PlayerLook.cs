using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private PlayerStatsSO stats;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private Transform cameraPivot;

    private float pitch;
    private float sensitivityMultiplier = 1f;

    public float Pitch => pitch;

    private void Start()
    {
        SetCursorLocked(true);
    }

    private void Update()
    {
        Vector2 delta = input.Look * stats.mouseSensitivity * sensitivityMultiplier;
        if (stats.invertY) delta.y = -delta.y;

        transform.Rotate(Vector3.up, delta.x, Space.Self);
        pitch = Mathf.Clamp(pitch - delta.y, -stats.maxLookAngle, stats.maxLookAngle);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void SetSensitivityMultiplier(float multiplier)
    {
        sensitivityMultiplier = Mathf.Max(0.01f, multiplier);
    }

    public void AddRecoil(float vertical, float horizontal)
    {
        pitch = Mathf.Clamp(pitch - vertical, -stats.maxLookAngle, stats.maxLookAngle);
        transform.Rotate(Vector3.up, horizontal, Space.Self);
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
