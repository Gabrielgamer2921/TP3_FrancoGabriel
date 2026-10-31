using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private bool keepConstantScreenSize;
    [SerializeField] private float screenSize = 0.03f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        transform.rotation = cam.transform.rotation;
        if (keepConstantScreenSize)
        {
            float distance = Vector3.Distance(cam.transform.position, transform.position);
            transform.localScale = baseScale * distance * screenSize;
        }
    }
}
