using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private PlayerBandages bandages;
    [SerializeField] private PlayerThrowController thrower;
    [SerializeField] private PlayerMelee melee;
    [SerializeField] private SoundSO hurt;
    [SerializeField] private SoundSO bandage;
    [SerializeField] private SoundSO lightFuse;
    [SerializeField] private SoundSO throwSound;
    [SerializeField] private SoundSO meleeSwing;
    [SerializeField] private SoundSO meleeHit;

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        bandages.UseStarted += OnBandage;
        thrower.CookStarted += OnCook;
        thrower.Thrown += OnThrow;
        melee.Swung += OnSwing;
        melee.HitLanded += OnMeleeHit;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        bandages.UseStarted -= OnBandage;
        thrower.CookStarted -= OnCook;
        thrower.Thrown -= OnThrow;
        melee.Swung -= OnSwing;
        melee.HitLanded -= OnMeleeHit;
    }

    private void OnDamaged(DamageInfo info) => AudioManager.Play2D(hurt);
    private void OnBandage() => AudioManager.Play2D(bandage);
    private void OnCook() => AudioManager.Play2D(lightFuse);
    private void OnThrow() => AudioManager.Play2D(throwSound);
    private void OnSwing() => AudioManager.Play2D(meleeSwing);
    private void OnMeleeHit(Vector3 point) => AudioManager.Play(meleeHit, point);
}
