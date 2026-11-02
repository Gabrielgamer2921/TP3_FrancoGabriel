using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class IntroSequence : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerLook look;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private GameObject[] cards;
    [SerializeField] private TMP_Text continueHint;
    [SerializeField] private bool showOncePerSession = true;
    [SerializeField] private float inputDelay = 0.8f;
    [SerializeField] private float fadeSpeed = 2.5f;

    private static bool shownThisSession;

    private int index;
    private float cardShownAt;
    private bool fadingOut;

    public static bool IsPlaying { get; private set; }

    private void Start()
    {
        if (showOncePerSession && shownThisSession)
        {
            group.gameObject.SetActive(false);
            return;
        }

        shownThisSession = true;
        IsPlaying = true;
        group.gameObject.SetActive(true);
        group.alpha = 1f;
        Time.timeScale = 0f;
        input.SetGameplayEnabled(false);
        ShowCard(0);
    }

    private void OnDestroy()
    {
        IsPlaying = false;
    }

    private void Update()
    {
        if (fadingOut)
        {
            group.alpha -= Time.unscaledDeltaTime * fadeSpeed;
            if (group.alpha <= 0f)
            {
                fadingOut = false;
                group.gameObject.SetActive(false);
            }
            return;
        }

        if (!IsPlaying) return;

        if (continueHint != null)
        {
            bool ready = Time.unscaledTime - cardShownAt >= inputDelay;
            Color color = continueHint.color;
            color.a = ready ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f) : 0f;
            continueHint.color = color;
        }

        if (Time.unscaledTime - cardShownAt < inputDelay) return;
        if (ContinuePressed()) Next();
    }

    private static bool ContinuePressed()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        return (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            || (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

    private void ShowCard(int cardIndex)
    {
        index = cardIndex;
        cardShownAt = Time.unscaledTime;
        for (int i = 0; i < cards.Length; i++) cards[i].SetActive(i == cardIndex);
    }

    private void Next()
    {
        if (index + 1 < cards.Length)
        {
            ShowCard(index + 1);
            return;
        }

        IsPlaying = false;
        fadingOut = true;
        Time.timeScale = 1f;
        input.SetGameplayEnabled(true);
        look.SetCursorLocked(true);
    }
}
