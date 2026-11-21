using UnityEditor;
using UnityEngine;

public static class BowPreviewTools
{
    private static Vector3 savedPosition;
    private static bool previewing;

    [MenuItem("Cowboy FPS/Depuración/Previsualizar arco en reposo")]
    public static void PreviewRest() => Preview(0f, false);

    [MenuItem("Cowboy FPS/Depuración/Previsualizar arco tensado y apuntando")]
    public static void PreviewDrawn() => Preview(1f, true);

    [MenuItem("Cowboy FPS/Depuración/Terminar previsualización del arco")]
    public static void EndPreview()
    {
        Transform bow = FindBow();
        if (bow == null) return;
        if (previewing) bow.localPosition = savedPosition;
        previewing = false;
        bow.gameObject.SetActive(false);
        foreach (Weapon weapon in bow.parent.GetComponentsInChildren<Weapon>(true))
            if (weapon.transform != bow) weapon.gameObject.SetActive(weapon.GetComponentInParent<PlayerWeaponController>() != null && IsStarting(weapon));
        SceneView.RepaintAll();
    }

    private static bool IsStarting(Weapon weapon)
    {
        PlayerWeaponController controller = weapon.GetComponentInParent<PlayerWeaponController>();
        return controller != null && controller.Weapons.Count > 0 && controller.Weapons[0] == weapon;
    }

    private static void Preview(float draw, bool aim)
    {
        Transform bow = FindBow();
        if (bow == null) return;

        foreach (Weapon other in bow.parent.GetComponentsInChildren<Weapon>(true)) other.gameObject.SetActive(other.transform == bow);
        if (!previewing) savedPosition = bow.localPosition;
        previewing = true;
        bow.localPosition = savedPosition;
        bow.localRotation = Quaternion.identity;

        BowWeapon weapon = bow.GetComponent<BowWeapon>();
        SerializedObject serialized = new SerializedObject(weapon);
        Transform nock = serialized.FindProperty("nockPoint").objectReferenceValue as Transform;
        Transform upperTip = serialized.FindProperty("upperTip").objectReferenceValue as Transform;
        Transform lowerTip = serialized.FindProperty("lowerTip").objectReferenceValue as Transform;
        Transform upperString = serialized.FindProperty("upperString").objectReferenceValue as Transform;
        Transform lowerString = serialized.FindProperty("lowerString").objectReferenceValue as Transform;
        Vector3 rest = serialized.FindProperty("restNock").vector3Value;
        Vector3 drawn = serialized.FindProperty("drawnNock").vector3Value;
        Vector3 arrowRest = serialized.FindProperty("arrowRest").vector3Value;
        float thickness = serialized.FindProperty("stringThickness").floatValue;

        Vector3 nockPos = Vector3.Lerp(rest, drawn, draw);
        nock.localPosition = nockPos;
        Vector3 toRest = arrowRest - nockPos;
        if (toRest.sqrMagnitude > 0.0001f) nock.localRotation = Quaternion.LookRotation(toRest, Vector3.up);
        Place(upperString, upperTip.localPosition, nockPos, thickness);
        Place(lowerString, lowerTip.localPosition, nockPos, thickness);

        if (aim)
        {
            Transform aimPoint = bow.Find("AimPoint");
            float distance = serialized.FindProperty("aimDistanceFromCamera").floatValue;
            Vector3 aimInParent = bow.parent.InverseTransformPoint(aimPoint.position);
            Vector3 offset = aimInParent - bow.localPosition;
            bow.localPosition = new Vector3(0f, 0f, distance) - offset;
        }

        SceneView.RepaintAll();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    private static void Place(Transform segment, Vector3 from, Vector3 to, float thickness)
    {
        Vector3 delta = to - from;
        segment.localPosition = (from + to) * 0.5f;
        if (delta.sqrMagnitude > 0.000001f) segment.localRotation = Quaternion.LookRotation(delta, Vector3.forward);
        segment.localScale = new Vector3(thickness, thickness, delta.magnitude);
    }

    private static Transform FindBow()
    {
        BowWeapon bow = Object.FindFirstObjectByType<BowWeapon>(FindObjectsInactive.Include);
        if (bow == null) Debug.LogWarning("No encontré el arco en la escena.");
        return bow != null ? bow.transform : null;
    }

    [MenuItem("Cowboy FPS/Depuración/Alternar cámara lenta")]
    private static void ToggleSlowMotion()
    {
        Time.timeScale = Time.timeScale < 0.99f ? 1f : 0.12f;
        Debug.Log("Escala de tiempo: " + Time.timeScale);
    }
}
