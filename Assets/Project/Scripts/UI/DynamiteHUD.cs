using TMPro;
using UnityEngine;

public class DynamiteHUD : MonoBehaviour
{
    [SerializeField] private PlayerThrowController thrower;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private GameObject fuseRoot;
    [SerializeField] private RectTransform fuseFill;
    [SerializeField] private UnityEngine.UI.Image fuseImage;
    [SerializeField] private Color safeColor = new Color(1f, 0.8f, 0.3f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.2f, 0.1f);

    private void OnEnable()
    {
        thrower.CountChanged += OnCountChanged;
        OnCountChanged(thrower.Count);
    }

    private void OnDisable()
    {
        thrower.CountChanged -= OnCountChanged;
    }

    private void OnCountChanged(int count)
    {
        countText.text = $"Dinamita  {count}/{thrower.Max}";
    }

    private void Update()
    {
        bool cooking = thrower.IsCooking;
        if (fuseRoot.activeSelf != cooking) fuseRoot.SetActive(cooking);
        if (!cooking) return;

        float progress = thrower.CookProgress;
        fuseFill.localScale = new Vector3(1f - progress, 1f, 1f);
        fuseImage.color = Color.Lerp(safeColor, dangerColor, progress);
    }
}
