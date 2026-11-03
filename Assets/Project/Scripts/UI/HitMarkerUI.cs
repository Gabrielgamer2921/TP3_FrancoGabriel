using UnityEngine;
using UnityEngine.UI;

public class HitMarkerUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Image[] lines;
    [SerializeField] private PlayerMelee melee;
    [SerializeField] private Color hitColor = Color.white;
    [SerializeField] private Color criticalColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color killColor = new Color(1f, 0.2f, 0.15f);
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private SoundSO hitSound;
    [SerializeField] private SoundSO criticalSound;
    [SerializeField] private SoundSO killSound;

    private float shownAt = float.NegativeInfinity;
    private float scale = 1f;

    private void Awake() => group.alpha = 0f;

    private void OnEnable()
    {
        Weapon.AnyHit += OnHit;
        if (melee != null) melee.HitLanded += OnMeleeHit;
        EnemyAI.AnyKilled += OnEnemyKilled;
    }

    private void OnDisable()
    {
        Weapon.AnyHit -= OnHit;
        if (melee != null) melee.HitLanded -= OnMeleeHit;
        EnemyAI.AnyKilled -= OnEnemyKilled;
    }

    private void OnHit(WeaponHit hit)
    {
        if (!hit.damaged) return;

        if (hit.killed) Show(killColor, 1.4f, killSound);
        else if (hit.critical) Show(criticalColor, 1.2f, criticalSound);
        else Show(hitColor, 1f, hitSound);
    }

    private void OnMeleeHit(Vector3 point) => Show(hitColor, 1.1f, hitSound);

    private void OnEnemyKilled(EnemyAI enemy)
    {
        if (Time.unscaledTime - shownAt > 0.05f) Show(killColor, 1.4f, null);
    }

    private void Show(Color color, float size, SoundSO sound)
    {
        foreach (Image line in lines) line.color = color;
        scale = size;
        shownAt = Time.unscaledTime;
        AudioManager.Play2D(sound);
    }

    private void LateUpdate()
    {
        float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / duration);
        group.alpha = 1f - t;
        transform.localScale = Vector3.one * Mathf.Lerp(scale * 1.25f, scale, t);
    }
}
