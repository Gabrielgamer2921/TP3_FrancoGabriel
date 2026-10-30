using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthHUD : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private PlayerVitals vitals;
    [SerializeField] private PlayerBandages bandages;

    [Header("Barra de vida")]
    [SerializeField] private RectTransform healthFill;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private RectTransform healthTrail;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Color healthyColor = new Color(0.8f, 0.2f, 0.15f);
    [SerializeField] private Color regenColor = new Color(0.95f, 0.75f, 0.35f);
    [SerializeField] private float trailDelay = 0.4f;
    [SerializeField] private float trailSpeed = 0.6f;

    [Header("Vendajes")]
    [SerializeField] private TMP_Text bandageCountText;
    [SerializeField] private GameObject bandageProgressRoot;
    [SerializeField] private RectTransform bandageProgressFill;

    [Header("Viñeta de daño")]
    [SerializeField] private Image damageVignette;
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private float lowHealthAlpha = 0.45f;
    [SerializeField] private float hitFlashAlpha = 0.6f;
    [SerializeField] private float flashFadeSpeed = 2f;

    private float trailValue = 1f;
    private float lastDamageTime;
    private float flashAlpha;

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        bandages.CountChanged += OnBandagesChanged;
        OnBandagesChanged(bandages.Count);
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        bandages.CountChanged -= OnBandagesChanged;
    }

    private void OnDamaged(DamageInfo info)
    {
        lastDamageTime = Time.time;
        flashAlpha = hitFlashAlpha;
    }

    private void OnBandagesChanged(int count)
    {
        bandageCountText.text = $"Vendajes  {count}/{bandages.Max}";
    }

    private void Update()
    {
        float value = health.Normalized;
        SetFill(healthFill, value);

        if (value > trailValue) trailValue = value;
        else if (Time.time - lastDamageTime > trailDelay) trailValue = Mathf.MoveTowards(trailValue, value, trailSpeed * Time.deltaTime);
        SetFill(healthTrail, trailValue);

        healthText.text = Mathf.CeilToInt(health.Current).ToString();

        Color fillColor = healthyColor;
        if (vitals.IsRegenerating) fillColor = Color.Lerp(healthyColor, regenColor, (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f);
        healthFillImage.color = fillColor;

        bandageProgressRoot.SetActive(bandages.IsUsing);
        if (bandages.IsUsing) SetFill(bandageProgressFill, bandages.Progress);

        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, flashFadeSpeed * Time.deltaTime);
        float lowAlpha = value < lowHealthThreshold && !health.IsDead
            ? (1f - value / lowHealthThreshold) * lowHealthAlpha * (0.8f + 0.2f * Mathf.Sin(Time.time * 4f))
            : 0f;
        if (health.IsDead) lowAlpha = lowHealthAlpha;

        Color vignetteColor = damageVignette.color;
        vignetteColor.a = Mathf.Max(flashAlpha, lowAlpha);
        damageVignette.color = vignetteColor;
    }

    private static void SetFill(RectTransform fill, float value)
    {
        fill.localScale = new Vector3(Mathf.Clamp01(value), 1f, 1f);
    }
}
