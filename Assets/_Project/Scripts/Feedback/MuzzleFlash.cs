using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    [SerializeField] private Weapon playerWeapon;
    [SerializeField] private EnemyWeapon enemyWeapon;
    [SerializeField] private SpriteRenderer flashSprite;
    [SerializeField] private Light flashLight;
    [SerializeField] private float duration = 0.05f;
    [SerializeField] private float minScale = 0.7f;
    [SerializeField] private float maxScale = 1.2f;
    [SerializeField] private float lightIntensity = 6f;

    private Vector3 baseScale;
    private float hideAt;

    private void Awake()
    {
        if (flashSprite != null)
        {
            baseScale = flashSprite.transform.localScale;
            flashSprite.enabled = false;
        }
        if (flashLight != null) flashLight.enabled = false;
    }

    private void OnEnable()
    {
        if (playerWeapon != null) playerWeapon.Fired += Flash;
        if (enemyWeapon != null) enemyWeapon.Fired += Flash;
    }

    private void OnDisable()
    {
        if (playerWeapon != null) playerWeapon.Fired -= Flash;
        if (enemyWeapon != null) enemyWeapon.Fired -= Flash;
        Hide();
    }

    public void Flash()
    {
        hideAt = Time.time + duration;
        if (flashSprite != null)
        {
            flashSprite.enabled = true;
            flashSprite.transform.localScale = baseScale * Random.Range(minScale, maxScale);
        }
        if (flashLight != null)
        {
            flashLight.enabled = true;
            flashLight.intensity = lightIntensity;
        }
    }

    private void LateUpdate()
    {
        if (flashSprite != null && flashSprite.enabled)
        {
            Camera cam = Camera.main;
            if (cam != null) flashSprite.transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }
        if (Time.time >= hideAt) Hide();
    }

    private void Hide()
    {
        if (flashSprite != null) flashSprite.enabled = false;
        if (flashLight != null) flashLight.enabled = false;
    }
}
