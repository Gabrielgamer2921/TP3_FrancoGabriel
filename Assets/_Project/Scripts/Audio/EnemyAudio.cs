using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private EnemyWeapon weapon;
    [SerializeField] private SoundSO fire;
    [SerializeField] private SoundSO reload;

    private void OnEnable()
    {
        weapon.Fired += OnFired;
        weapon.ReloadStarted += OnReload;
    }

    private void OnDisable()
    {
        weapon.Fired -= OnFired;
        weapon.ReloadStarted -= OnReload;
    }

    private void OnFired() => AudioManager.Play(fire, weapon.Muzzle.position);
    private void OnReload() => AudioManager.Play(reload, transform.position + Vector3.up * 1.2f);
}
