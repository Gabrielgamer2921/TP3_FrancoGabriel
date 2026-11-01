using System.Collections.Generic;
using UnityEngine;

public class EnemyHitFlash : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.2f);
    [SerializeField] private Color criticalColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
    private readonly List<Color> originalColors = new List<Color>();
    private readonly List<Color> baseColors = new List<Color>();
    private MaterialPropertyBlock block;
    private float flashUntil;
    private bool flashing;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
        {
            Material material = meshRenderer.sharedMaterial;
            if (material == null || !material.HasProperty(BaseColorId)) continue;
            renderers.Add(meshRenderer);
            originalColors.Add(material.GetColor(BaseColorId));
            baseColors.Add(material.GetColor(BaseColorId));
        }
    }

    private void OnEnable() => health.Damaged += OnDamaged;
    private void OnDisable() => health.Damaged -= OnDamaged;

    private void OnDamaged(DamageInfo info)
    {
        SetColor(info.isCritical ? criticalColor : flashColor);
        flashUntil = Time.time + flashDuration;
        flashing = true;
    }

    private void Update()
    {
        if (!flashing || Time.time < flashUntil) return;
        flashing = false;
        for (int i = 0; i < renderers.Count; i++) Apply(renderers[i], originalColors[i]);
    }

    public void SetTint(Color tint, float strength)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            originalColors[i] = Color.Lerp(baseColors[i], tint, strength);
            Apply(renderers[i], originalColors[i]);
        }
    }

    private void SetColor(Color color)
    {
        foreach (MeshRenderer meshRenderer in renderers) Apply(meshRenderer, color);
    }

    private void Apply(MeshRenderer meshRenderer, Color color)
    {
        if (meshRenderer == null) return;
        meshRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        meshRenderer.SetPropertyBlock(block);
    }
}
