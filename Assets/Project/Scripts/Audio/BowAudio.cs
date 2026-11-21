using UnityEngine;

public class BowAudio : MonoBehaviour
{
    [SerializeField] private BowWeapon bow;
    [SerializeField] private SoundSO draw;
    [SerializeField] private SoundSO nock;

    private void OnEnable()
    {
        bow.DrawStarted += OnDraw;
        bow.NockStarted += OnNock;
    }

    private void OnDisable()
    {
        bow.DrawStarted -= OnDraw;
        bow.NockStarted -= OnNock;
    }

    private void OnDraw() => AudioManager.Play2D(draw);
    private void OnNock() => AudioManager.Play2D(nock);
}
