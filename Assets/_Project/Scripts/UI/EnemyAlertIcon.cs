using TMPro;
using UnityEngine;

public class EnemyAlertIcon : MonoBehaviour
{
    [SerializeField] private EnemyAI enemy;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color suspiciousColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color combatColor = new Color(0.95f, 0.2f, 0.15f);
    [SerializeField] private Color searchColor = new Color(1f, 0.55f, 0.2f);
    [SerializeField] private float combatIconDuration = 2f;

    private float combatShownUntil;

    private void OnEnable() => enemy.StateChanged += OnStateChanged;
    private void OnDisable() => enemy.StateChanged -= OnStateChanged;

    private void OnStateChanged(EnemyState state)
    {
        if (state == EnemyState.Combat || state == EnemyState.RingAlarm) combatShownUntil = Time.time + combatIconDuration;
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;

        if (enemy.IsDead)
        {
            label.text = string.Empty;
            return;
        }

        label.transform.localScale = Vector3.one;

        switch (enemy.State)
        {
            case EnemyState.Combat:
            case EnemyState.RingAlarm:
                label.text = Time.time < combatShownUntil || enemy.State == EnemyState.RingAlarm ? "!" : string.Empty;
                label.color = combatColor;
                break;
            case EnemyState.Search:
                label.text = "?";
                label.color = searchColor;
                break;
            default:
                float awareness = enemy.Awareness;
                label.text = awareness > 0.05f ? "?" : string.Empty;
                Color color = suspiciousColor;
                color.a = Mathf.Lerp(0.3f, 1f, awareness);
                label.color = color;
                label.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.2f, awareness);
                break;
        }
    }
}
