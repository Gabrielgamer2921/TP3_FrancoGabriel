using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private PlayerLook look;
    [SerializeField] private PlayerWeaponController weapons;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float fallDuration = 0.8f;
    [SerializeField] private float fallenEyeHeight = 0.3f;
    [SerializeField] private float fallenRoll = 70f;
    [SerializeField] private bool reloadSceneOnDeath = true;
    [SerializeField] private float reloadDelay = 4f;

    public event Action PlayerDied;

    private void OnEnable()
    {
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Died -= OnDied;
    }

    private void OnDied(DamageInfo info)
    {
        weapons.SetHolstered(true);
        input.SetGameplayEnabled(false);
        weapons.enabled = false;
        motor.enabled = false;
        look.enabled = false;

        PlayerDied?.Invoke();
        StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
        Vector3 startPosition = cameraPivot.localPosition;
        Quaternion startRotation = cameraPivot.localRotation;
        Vector3 endPosition = new Vector3(startPosition.x, fallenEyeHeight, startPosition.z);
        Quaternion endRotation = startRotation * Quaternion.Euler(0f, 0f, fallenRoll);

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fallDuration);
            cameraPivot.localPosition = Vector3.Lerp(startPosition, endPosition, t);
            cameraPivot.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }

        if (!reloadSceneOnDeath) yield break;
        yield return new WaitForSeconds(reloadDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
