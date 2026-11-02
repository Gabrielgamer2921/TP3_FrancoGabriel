using UnityEngine;

public class WorldAudio : MonoBehaviour
{
    [Header("Explosiones")]
    [SerializeField] private SoundSO explosion;

    [Header("Pickups")]
    [SerializeField] private SoundSO ammoPickup;
    [SerializeField] private SoundSO itemPickup;
    [SerializeField] private SoundSO keyPickup;

    private void OnEnable()
    {
        Explosion.Exploded += OnExploded;
        Pickup.AnyCollected += OnPickup;
    }

    private void OnDisable()
    {
        Explosion.Exploded -= OnExploded;
        Pickup.AnyCollected -= OnPickup;
    }

    private void OnExploded(Vector3 position, float radius, float shake) => AudioManager.Play(explosion, position);

    private void OnPickup(Pickup pickup)
    {
        SoundSO sound = pickup is AmmoPickup ? ammoPickup : pickup is KeyPickup ? keyPickup : itemPickup;
        AudioManager.Play2D(sound);
    }
}
