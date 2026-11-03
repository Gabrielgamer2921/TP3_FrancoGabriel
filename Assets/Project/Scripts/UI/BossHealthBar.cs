using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private BossController boss;
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private RectTransform fill;
    [SerializeField] private Image fillImage;
    [SerializeField] private Color duelColor = new Color(0.85f, 0.65f, 0.3f);
    [SerializeField] private Color diabloColor = new Color(0.9f, 0.15f, 0.1f);
    [SerializeField] private Color invulnerableColor = new Color(0.5f, 0.5f, 0.5f);

    private void Update()
    {
        bool visible = boss != null && boss.FightStarted && boss.Phase != BossPhase.Defeated;
        if (root.activeSelf != visible) root.SetActive(visible);
        if (!visible) return;

        fill.localScale = new Vector3(boss.Health.Normalized, 1f, 1f);

        switch (boss.Phase)
        {
            case BossPhase.Retreat:
                fillImage.color = invulnerableColor;
                nameText.text = boss.BossName;
                break;
            case BossPhase.Diablo:
                fillImage.color = Color.Lerp(diabloColor, Color.black, (Mathf.Sin(Time.time * 5f) + 1f) * 0.12f);
                nameText.text = boss.BossName + " — Diablo";
                break;
            default:
                fillImage.color = duelColor;
                nameText.text = boss.BossName;
                break;
        }
    }
}
