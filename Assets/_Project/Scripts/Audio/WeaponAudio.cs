using System.Collections;
using UnityEngine;

public class WeaponAudio : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private SoundSO fire;
    [SerializeField] private SoundSO cycle;
    [SerializeField] private float cycleDelay = 0.25f;
    [SerializeField] private SoundSO dryFire;
    [SerializeField] private SoundSO reloadStart;
    [SerializeField] private SoundSO roundInserted;
    [SerializeField] private SoundSO reloadEnd;

    private void OnEnable()
    {
        weapon.Fired += OnFired;
        weapon.DryFired += OnDryFired;
        weapon.ReloadStarted += OnReloadStarted;
        weapon.RoundInserted += OnRoundInserted;
        weapon.ReloadFinished += OnReloadFinished;
    }

    private void OnDisable()
    {
        weapon.Fired -= OnFired;
        weapon.DryFired -= OnDryFired;
        weapon.ReloadStarted -= OnReloadStarted;
        weapon.RoundInserted -= OnRoundInserted;
        weapon.ReloadFinished -= OnReloadFinished;
    }

    private void OnFired()
    {
        AudioManager.Play2D(fire);
        if (cycle != null && weapon.CurrentMagazine > 0) StartCoroutine(PlayDelayed(cycle, cycleDelay));
    }

    private void OnDryFired() => AudioManager.Play2D(dryFire);
    private void OnReloadStarted() => AudioManager.Play2D(reloadStart);
    private void OnRoundInserted() => AudioManager.Play2D(roundInserted);
    private void OnReloadFinished() => AudioManager.Play2D(reloadEnd);

    private IEnumerator PlayDelayed(SoundSO sound, float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioManager.Play2D(sound);
    }
}
