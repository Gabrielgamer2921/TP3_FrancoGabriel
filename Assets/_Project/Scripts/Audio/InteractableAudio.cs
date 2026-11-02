using UnityEngine;

public class InteractableAudio : MonoBehaviour
{
    [SerializeField] private LockedDoor door;
    [SerializeField] private AlarmBell bell;
    [SerializeField] private DestructibleBarrier barrier;
    [SerializeField] private SoundSO doorOpen;
    [SerializeField] private SoundSO doorClose;
    [SerializeField] private SoundSO doorLocked;
    [SerializeField] private SoundSO bellRing;
    [SerializeField] private SoundSO barrierBreak;

    private void OnEnable()
    {
        if (door != null)
        {
            door.Opened += OnOpened;
            door.Closed += OnClosed;
            door.Denied += OnDenied;
        }
        if (bell != null) bell.Rung += OnBell;
        if (barrier != null) barrier.Broken += OnBroken;
    }

    private void OnDisable()
    {
        if (door != null)
        {
            door.Opened -= OnOpened;
            door.Closed -= OnClosed;
            door.Denied -= OnDenied;
        }
        if (bell != null) bell.Rung -= OnBell;
        if (barrier != null) barrier.Broken -= OnBroken;
    }

    private void OnOpened() => AudioManager.Play(doorOpen, transform.position);
    private void OnClosed() => AudioManager.Play(doorClose, transform.position);
    private void OnDenied() => AudioManager.Play(doorLocked, transform.position);
    private void OnBell() => AudioManager.Play(bellRing, transform.position);
    private void OnBroken() => AudioManager.Play(barrierBreak, transform.position + Vector3.up);
}
