using TMPro;
using UnityEngine;

public class ObjectiveHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0.35f);
    [SerializeField] private float highlightDuration = 2.5f;

    private float highlightUntil;
    private float hintUntil;

    public void Show(string objective)
    {
        objectiveText.text = objective;
        highlightUntil = Time.time + highlightDuration;
    }

    public void Hint(string message, float duration = 3f)
    {
        hintText.text = message;
        hintUntil = Time.time + duration;
    }

    private void Update()
    {
        float highlight = Mathf.Clamp01((highlightUntil - Time.time) / highlightDuration);
        objectiveText.color = Color.Lerp(normalColor, highlightColor, highlight);
        objectiveText.transform.localScale = Vector3.one * (1f + highlight * 0.08f * Mathf.Abs(Mathf.Sin(Time.time * 6f)));

        Color hintColor = hintText.color;
        hintColor.a = Mathf.Clamp01(hintUntil - Time.time);
        hintText.color = hintColor;
    }
}
