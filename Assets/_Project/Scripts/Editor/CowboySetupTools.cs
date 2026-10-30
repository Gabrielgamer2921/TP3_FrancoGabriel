using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class CowboySetupTools
{
    private const string MaterialsFolder = "Assets/_Project/Materials";
    private const string TracerMaterialPath = MaterialsFolder + "/Mat_Tracer.mat";
    private const string BulletPrefabPath = "Assets/_Project/Prefabs/Bullet.prefab";

    [MenuItem("Cowboy FPS/Configurar trazadoras")]
    public static void SetupTracers()
    {
        Material material = GetOrCreateTracerMaterial();

        GameObject bullet = PrefabUtility.LoadPrefabContents(BulletPrefabPath);
        TrailRenderer trail = bullet.GetComponentInChildren<TrailRenderer>();
        if (trail == null) trail = bullet.AddComponent<TrailRenderer>();
        trail.sharedMaterial = material;
        trail.time = 0.06f;
        trail.startWidth = 0.02f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.1f;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        PrefabUtility.SaveAsPrefabAsset(bullet, BulletPrefabPath);
        PrefabUtility.UnloadPrefabContents(bullet);


        MarkSceneDirty();
        Debug.Log("Cowboy FPS: trazadoras configuradas.");
    }

    [MenuItem("Cowboy FPS/Crear HUD")]
    public static void CreateHud()
    {
        if (TMP_Settings.defaultFontAsset == null)
        {
            EditorUtility.DisplayDialog("Falta TextMeshPro", "Primero importá Window > TextMeshPro > Import TMP Essential Resources y después volvé a ejecutar este menú.", "OK");
            return;
        }

        if (Object.FindFirstObjectByType<AmmoHUD>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("HUD existente", "Ya hay un HUD en la escena. Borralo si querés crearlo de nuevo.", "OK");
            return;
        }

        PlayerWeaponController weapons = Object.FindFirstObjectByType<PlayerWeaponController>();
        PlayerMotor motor = Object.FindFirstObjectByType<PlayerMotor>();
        PlayerAmmoInventory ammo = Object.FindFirstObjectByType<PlayerAmmoInventory>();
        if (weapons == null || motor == null || ammo == null)
        {
            EditorUtility.DisplayDialog("Falta el Player", "No encontré al Player con sus componentes en la escena.", "OK");
            return;
        }

        GameObject hud = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(hud, "Crear HUD");
        hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform ammoPanel = CreateRect("Municion", hud.transform, new Vector2(1f, 0f), new Vector2(-60f, 50f), new Vector2(360f, 150f));
        TMP_Text reserve = CreateText("Reserva", ammoPanel, "/ 24", 34, FontStyles.Normal, TextAlignmentOptions.BottomLeft, new Vector2(0f, 10f), new Vector2(110f, 50f));
        TMP_Text magazine = CreateText("Cargador", ammoPanel, "6", 76, FontStyles.Bold, TextAlignmentOptions.BottomRight, new Vector2(-115f, 0f), new Vector2(150f, 90f));
        TMP_Text weaponName = CreateText("NombreArma", ammoPanel, "Revólver", 28, FontStyles.Normal, TextAlignmentOptions.BottomRight, new Vector2(0f, 90f), new Vector2(360f, 36f));
        TMP_Text reloading = CreateText("Recargando", ammoPanel, "RECARGANDO...", 24, FontStyles.Bold, TextAlignmentOptions.BottomRight, new Vector2(0f, 126f), new Vector2(360f, 30f));
        reloading.color = new Color(1f, 0.8f, 0.3f);
        reloading.gameObject.SetActive(false);

        RectTransform crosshair = CreateRect("Mira", hud.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));
        crosshair.pivot = new Vector2(0.5f, 0.5f);
        CanvasGroup crosshairGroup = crosshair.gameObject.AddComponent<CanvasGroup>();
        crosshairGroup.interactable = false;
        crosshairGroup.blocksRaycasts = false;
        RectTransform top = CreateLine("Arriba", crosshair, new Vector2(2f, 10f));
        RectTransform bottom = CreateLine("Abajo", crosshair, new Vector2(2f, 10f));
        RectTransform left = CreateLine("Izquierda", crosshair, new Vector2(10f, 2f));
        RectTransform right = CreateLine("Derecha", crosshair, new Vector2(10f, 2f));

        AmmoHUD ammoHud = hud.AddComponent<AmmoHUD>();
        SerializedObject ammoSerialized = new SerializedObject(ammoHud);
        ammoSerialized.FindProperty("weapons").objectReferenceValue = weapons;
        ammoSerialized.FindProperty("ammo").objectReferenceValue = ammo;
        ammoSerialized.FindProperty("weaponNameText").objectReferenceValue = weaponName;
        ammoSerialized.FindProperty("magazineText").objectReferenceValue = magazine;
        ammoSerialized.FindProperty("reserveText").objectReferenceValue = reserve;
        ammoSerialized.FindProperty("reloadingIndicator").objectReferenceValue = reloading.gameObject;
        ammoSerialized.ApplyModifiedPropertiesWithoutUndo();

        CrosshairUI crosshairUI = crosshair.gameObject.AddComponent<CrosshairUI>();
        SerializedObject crossSerialized = new SerializedObject(crosshairUI);
        crossSerialized.FindProperty("weapons").objectReferenceValue = weapons;
        crossSerialized.FindProperty("motor").objectReferenceValue = motor;
        crossSerialized.FindProperty("canvasGroup").objectReferenceValue = crosshairGroup;
        crossSerialized.FindProperty("top").objectReferenceValue = top;
        crossSerialized.FindProperty("bottom").objectReferenceValue = bottom;
        crossSerialized.FindProperty("left").objectReferenceValue = left;
        crossSerialized.FindProperty("right").objectReferenceValue = right;
        crossSerialized.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = hud;
        MarkSceneDirty();
        Debug.Log("Cowboy FPS: HUD creado.");
    }

    private static Material GetOrCreateTracerMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(TracerMaterialPath);
        if (material != null) return material;

        if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets/_Project", "Materials");

        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", new Color(1f, 0.85f, 0.55f));
        AssetDatabase.CreateAsset(material, TracerMaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, string content, float fontSize, FontStyles style, TextAlignmentOptions alignment, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(1f, 0f), position, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static RectTransform CreateLine(string name, Transform parent, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        rect.pivot = new Vector2(0.5f, 0.5f);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);
        image.raycastTarget = false;
        return rect;
    }

    private static void MarkSceneDirty()
    {
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
