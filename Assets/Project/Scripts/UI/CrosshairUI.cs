using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    [SerializeField] private PlayerWeaponController weapons;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform top;
    [SerializeField] private RectTransform bottom;
    [SerializeField] private RectTransform left;
    [SerializeField] private RectTransform right;
    [SerializeField] private float baseGap = 6f;
    [SerializeField] private float pixelsPerDegree = 12f;
    [SerializeField] private float fadeSpeed = 12f;

    private void LateUpdate()
    {
        bool visible = !weapons.IsAiming && !motor.IsSprinting;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, visible ? 1f : 0f, fadeSpeed * Time.deltaTime);

        float gap = baseGap + weapons.CurrentSpread * pixelsPerDegree;
        top.anchoredPosition = new Vector2(0f, gap);
        bottom.anchoredPosition = new Vector2(0f, -gap);
        left.anchoredPosition = new Vector2(-gap, 0f);
        right.anchoredPosition = new Vector2(gap, 0f);
    }
}
