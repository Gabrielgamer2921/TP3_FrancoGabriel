using UnityEngine;

public class FuseSpark : MonoBehaviour
{
    [SerializeField] private Transform spark;
    [SerializeField] private Light sparkLight;
    [SerializeField] private float minScale = 0.6f;
    [SerializeField] private float maxScale = 1.3f;
    [SerializeField] private float maxIntensity = 3f;

    private Vector3 baseScale;

    private void Awake()
    {
        if (spark != null) baseScale = spark.localScale;
    }

    private void Update()
    {
        float flicker = Random.Range(minScale, maxScale);
        if (spark != null) spark.localScale = baseScale * flicker;
        if (sparkLight != null) sparkLight.intensity = maxIntensity * flicker;
    }
}
