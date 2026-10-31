using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SniperGlint : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 14f;
    [SerializeField] private float minScale = 0.6f;
    [SerializeField] private float maxScale = 1.4f;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;
    private float shownAt;
    private float duration = 1f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        spriteRenderer.enabled = false;
    }

    public void Show(float telegraphDuration)
    {
        duration = Mathf.Max(0.01f, telegraphDuration);
        shownAt = Time.time;
        spriteRenderer.enabled = true;
    }

    public void Hide()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (!spriteRenderer.enabled) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        float progress = Mathf.Clamp01((Time.time - shownAt) / duration);
        float pulse = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed * (0.5f + progress)) + 1f) * 0.5f);
        float distance = Vector3.Distance(cam.transform.position, transform.position);

        transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, Time.time * 90f);
        transform.localScale = baseScale * pulse * (0.4f + progress) * Mathf.Max(1f, distance * 0.08f);
    }
}
