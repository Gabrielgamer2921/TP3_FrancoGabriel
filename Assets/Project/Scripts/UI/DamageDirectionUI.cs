using UnityEngine;
using UnityEngine.UI;

public class DamageDirectionUI : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Transform player;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Image[] indicators;
    [SerializeField] private float duration = 1.6f;

    private Vector3[] sources;
    private float[] shownAt;
    private int next;

    private void Awake()
    {
        sources = new Vector3[indicators.Length];
        shownAt = new float[indicators.Length];
        for (int i = 0; i < indicators.Length; i++)
        {
            shownAt[i] = float.NegativeInfinity;
            SetAlpha(indicators[i], 0f);
        }
    }

    private void OnEnable() => health.Damaged += OnDamaged;
    private void OnDisable() => health.Damaged -= OnDamaged;

    private void OnDamaged(DamageInfo info)
    {
        Vector3 source = info.source != null && info.source.transform != player
            ? info.source.transform.position
            : info.point - info.direction * 5f;

        sources[next] = source;
        shownAt[next] = Time.time;
        next = (next + 1) % indicators.Length;
    }

    private void LateUpdate()
    {
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
        for (int i = 0; i < indicators.Length; i++)
        {
            float t = (Time.time - shownAt[i]) / duration;
            if (t >= 1f)
            {
                SetAlpha(indicators[i], 0f);
                continue;
            }

            Vector3 toSource = Vector3.ProjectOnPlane(sources[i] - player.position, Vector3.up);
            float angle = Vector3.SignedAngle(forward, toSource, Vector3.up);
            indicators[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
            SetAlpha(indicators[i], 1f - t * t);
        }
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
