using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class CowboyStage3Tools
{
    private const string Root = "Assets/_Project";
    private const string PrefabsFolder = Root + "/Prefabs";
    private const string PickupsFolder = PrefabsFolder + "/Pickups";
    private const string MaterialsFolder = Root + "/Materials";
    private const string UIFolder = MaterialsFolder + "/UI";

    [MenuItem("Cowboy FPS/Configurar Etapa 3 (bala visual, vida y vendajes)")]
    public static void SetupStage3()
    {
        EnsureFolder(PrefabsFolder);
        EnsureFolder(PickupsFolder);
        EnsureFolder(MaterialsFolder);
        EnsureFolder(UIFolder);

        RemoveMissingScripts();

        Material tracerMaterial = GetOrCreateMaterial("Mat_Tracer", "Universal Render Pipeline/Unlit", new Color(1f, 0.85f, 0.55f));
        VisualBullet visualBullet = CreateVisualBulletPrefab(tracerMaterial);
        AssignVisualBulletToHitscanWeapons(visualBullet);

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            PlayerMotor motorInScene = Object.FindFirstObjectByType<PlayerMotor>();
            if (motorInScene != null)
            {
                player = motorInScene.gameObject;
                player.tag = "Player";
            }
        }

        if (player == null)
        {
            EditorUtility.DisplayDialog("Falta el Player", "No encontré al Player en la escena.", "OK");
            return;
        }

        SetupPlayer(player);
        CreateHealthHud(player);
        CreatePickupPrefabsAndPlace(player.transform);
        CreateTestDamageZone(player.transform);
        AddSceneToBuildSettings();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Cowboy FPS: Etapa 3 configurada. Guardá la escena con Ctrl+S.");
    }

    [MenuItem("Cowboy FPS/Aplicar bala visual al Winchester")]
    public static void ApplyVisualToRifleBullet()
    {
        const string bulletPath = PrefabsFolder + "/Bullet.prefab";
        Material material = GetOrCreateMaterial("Mat_Tracer", "Universal Render Pipeline/Unlit", new Color(1f, 0.85f, 0.55f));

        GameObject bullet = PrefabUtility.LoadPrefabContents(bulletPath);

        MeshRenderer sphereRenderer = bullet.GetComponent<MeshRenderer>();
        if (sphereRenderer != null) sphereRenderer.enabled = false;

        if (bullet.transform.Find("Bala") == null)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Bala";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(bullet.transform, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Vector3 parentScale = bullet.transform.localScale;
            body.transform.localScale = new Vector3(0.035f / parentScale.x, 0.08f / parentScale.y, 0.035f / parentScale.z);
            MeshRenderer bodyRenderer = body.GetComponent<MeshRenderer>();
            bodyRenderer.sharedMaterial = material;
            bodyRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        TrailRenderer trail = bullet.GetComponent<TrailRenderer>();
        if (trail == null) trail = bullet.AddComponent<TrailRenderer>();
        trail.sharedMaterial = material;
        trail.time = 0.06f;
        trail.startWidth = 0.035f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.05f;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;

        PrefabUtility.SaveAsPrefabAsset(bullet, bulletPath);
        PrefabUtility.UnloadPrefabContents(bullet);

        WeaponDataSO rifle = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(Root + "/Data/Rifle_Winchester1873.asset");
        if (rifle != null)
        {
            rifle.projectileSpeed = 140f;
            EditorUtility.SetDirty(rifle);
            AssetDatabase.SaveAssets();
        }

        Debug.Log("Cowboy FPS: la bala del Winchester ahora usa la bala visual.");
    }

    private static void SetupPlayer(GameObject player)
    {
        PlayerStatsSO stats = FindAsset<PlayerStatsSO>();
        PlayerInputReader input = player.GetComponent<PlayerInputReader>();
        PlayerMotor motor = player.GetComponent<PlayerMotor>();
        PlayerLook look = player.GetComponent<PlayerLook>();
        PlayerWeaponController weapons = player.GetComponent<PlayerWeaponController>();

        Health health = GetOrAdd<Health>(player);
        CombatTracker combat = GetOrAdd<CombatTracker>(player);
        PlayerVitals vitals = GetOrAdd<PlayerVitals>(player);
        PlayerBandages bandages = GetOrAdd<PlayerBandages>(player);
        PlayerDeath death = GetOrAdd<PlayerDeath>(player);

        Transform cameraPivot = player.transform.Find("CameraPivot");

        Wire(combat, ("stats", stats), ("health", health));
        Wire(vitals, ("stats", stats), ("combat", combat));
        Wire(bandages, ("stats", stats), ("input", input), ("weapons", weapons), ("motor", motor), ("health", health));
        Wire(death, ("health", health), ("input", input), ("motor", motor), ("look", look), ("weapons", weapons), ("cameraPivot", cameraPivot));
    }

    private static void CreateHealthHud(GameObject player)
    {
        if (Object.FindFirstObjectByType<HealthHUD>(FindObjectsInactive.Include) != null) return;

        AmmoHUD ammoHud = Object.FindFirstObjectByType<AmmoHUD>(FindObjectsInactive.Include);
        if (ammoHud == null)
        {
            EditorUtility.DisplayDialog("Falta el HUD", "Primero ejecutá Cowboy FPS > Crear HUD.", "OK");
            return;
        }

        Transform hud = ammoHud.transform;
        Sprite vignetteSprite = GetOrCreateVignetteSprite();

        RectTransform vignette = CreateRect("ViñetaDaño", hud, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        vignette.SetAsFirstSibling();
        Image vignetteImage = vignette.gameObject.AddComponent<Image>();
        vignetteImage.sprite = vignetteSprite;
        vignetteImage.color = new Color(0.6f, 0f, 0f, 0f);
        vignetteImage.raycastTarget = false;

        RectTransform panel = CreateRect("Vida", hud, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(60f, 50f), new Vector2(420f, 110f));

        RectTransform barBackground = CreateRect("FondoBarra", panel, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0f, 0f), new Vector2(340f, 22f));
        AddImage(barBackground, new Color(0f, 0f, 0f, 0.55f));
        RectTransform trail = CreateRect("Rastro", barBackground, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        AddImage(trail, new Color(1f, 0.95f, 0.85f, 0.8f));
        RectTransform fill = CreateRect("Relleno", barBackground, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        Image fillImage = AddImage(fill, new Color(0.8f, 0.2f, 0.15f));

        TMP_Text healthText = CreateText("ValorVida", panel, "100", 30, FontStyles.Bold, TextAlignmentOptions.BottomLeft, new Vector2(350f, -4f), new Vector2(70f, 34f));
        TMP_Text bandageText = CreateText("Vendajes", panel, "Vendajes  2/4", 24, FontStyles.Normal, TextAlignmentOptions.BottomLeft, new Vector2(0f, 32f), new Vector2(340f, 30f));

        RectTransform progressRoot = CreateRect("ProgresoVendaje", hud, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(300f, 50f));
        CreateText("TextoVendando", progressRoot, "VENDANDO...", 22, FontStyles.Bold, TextAlignmentOptions.Bottom, new Vector2(0f, 18f), new Vector2(300f, 30f), new Vector2(0.5f, 0f));
        RectTransform progressBackground = CreateRect("FondoProgreso", progressRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(300f, 10f));
        AddImage(progressBackground, new Color(0f, 0f, 0f, 0.55f));
        RectTransform progressFill = CreateRect("RellenoProgreso", progressBackground, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        AddImage(progressFill, new Color(0.95f, 0.9f, 0.8f));
        progressRoot.gameObject.SetActive(false);

        HealthHUD healthHud = hud.gameObject.AddComponent<HealthHUD>();
        Wire(healthHud,
            ("health", player.GetComponent<Health>()),
            ("vitals", player.GetComponent<PlayerVitals>()),
            ("bandages", player.GetComponent<PlayerBandages>()),
            ("healthFill", fill),
            ("healthFillImage", fillImage),
            ("healthTrail", trail),
            ("healthText", healthText),
            ("bandageCountText", bandageText),
            ("bandageProgressRoot", progressRoot.gameObject),
            ("bandageProgressFill", progressFill),
            ("damageVignette", vignetteImage));
    }

    private static VisualBullet CreateVisualBulletPrefab(Material material)
    {
        string path = PrefabsFolder + "/VisualBullet.prefab";
        VisualBullet existing = AssetDatabase.LoadAssetAtPath<VisualBullet>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("VisualBullet");
        root.AddComponent<VisualBullet>();

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Bala";
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(root.transform, false);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.03f, 0.07f, 0.03f);
        MeshRenderer bodyRenderer = body.GetComponent<MeshRenderer>();
        bodyRenderer.sharedMaterial = material;
        bodyRenderer.shadowCastingMode = ShadowCastingMode.Off;

        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.sharedMaterial = material;
        trail.time = 0.05f;
        trail.startWidth = 0.03f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.05f;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<VisualBullet>();
    }

    private static void AssignVisualBulletToHitscanWeapons(VisualBullet visualBullet)
    {
        foreach (HitscanWeapon weapon in Object.FindObjectsByType<HitscanWeapon>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (weapon.Data == null || weapon.Data.visualBulletPrefab != null) continue;
            weapon.Data.visualBulletPrefab = visualBullet;
            EditorUtility.SetDirty(weapon.Data);
        }
    }

    private static void CreatePickupPrefabsAndPlace(Transform player)
    {
        if (Object.FindFirstObjectByType<Pickup>(FindObjectsInactive.Include) != null) return;

        AmmoTypeSO colt = AssetDatabase.LoadAssetAtPath<AmmoTypeSO>(Root + "/Data/Ammo_45Colt.asset");
        AmmoTypeSO winchester = AssetDatabase.LoadAssetAtPath<AmmoTypeSO>(Root + "/Data/Ammo_4440.asset");

        Material brass = GetOrCreateMaterial("Mat_Pickup_Municion", "Universal Render Pipeline/Lit", new Color(0.75f, 0.55f, 0.25f));
        Material green = GetOrCreateMaterial("Mat_Pickup_Rifle", "Universal Render Pipeline/Lit", new Color(0.35f, 0.45f, 0.25f));
        Material cloth = GetOrCreateMaterial("Mat_Pickup_Vendaje", "Universal Render Pipeline/Lit", new Color(0.92f, 0.9f, 0.82f));

        GameObject coltPrefab = CreatePickupPrefab<AmmoPickup>("Pickup_Municion_45Colt", brass, new Vector3(0.3f, 0.18f, 0.2f), pickup => Wire(pickup, ("ammoType", colt)));
        GameObject winchesterPrefab = CreatePickupPrefab<AmmoPickup>("Pickup_Municion_4440", green, new Vector3(0.4f, 0.18f, 0.22f), pickup => Wire(pickup, ("ammoType", winchester)));
        GameObject bandagePrefab = CreatePickupPrefab<BandagePickup>("Pickup_Vendaje", cloth, new Vector3(0.22f, 0.12f, 0.22f), null);

        Vector3 basePosition = player.position + player.forward * 4f;
        Place(coltPrefab, basePosition - player.right * 1.5f);
        Place(winchesterPrefab, basePosition);
        Place(bandagePrefab, basePosition + player.right * 1.5f);
    }

    private static GameObject CreatePickupPrefab<T>(string name, Material material, Vector3 size, System.Action<T> configure) where T : Pickup
    {
        string path = PickupsFolder + "/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject(name);
        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.5f, 0f);
        trigger.size = new Vector3(1f, 1f, 1f);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        visual.transform.localScale = size;
        visual.GetComponent<MeshRenderer>().sharedMaterial = material;

        T pickup = root.AddComponent<T>();
        Wire(pickup, ("visual", visual.transform));
        configure?.Invoke(pickup);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void CreateTestDamageZone(Transform player)
    {
        if (Object.FindFirstObjectByType<DamageZone>(FindObjectsInactive.Include) != null) return;

        Material danger = GetOrCreateMaterial("Mat_Peligro", "Universal Render Pipeline/Lit", new Color(0.85f, 0.3f, 0.1f));

        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "Fogata_Prueba";
        Undo.RegisterCreatedObjectUndo(zone, "Crear zona de daño");
        zone.transform.position = player.position + player.forward * 8f + player.right * 4f + Vector3.up * 0.05f;
        zone.transform.localScale = new Vector3(2f, 0.1f, 2f);
        zone.GetComponent<MeshRenderer>().sharedMaterial = danger;

        BoxCollider box = zone.GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, 5f, 0f);
        box.size = new Vector3(1f, 10f, 1f);
        zone.AddComponent<DamageZone>();
    }

    private static void Place(GameObject prefab, Vector3 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = new Vector3(position.x, 0f, position.z);
        Undo.RegisterCreatedObjectUndo(instance, "Colocar pickup");
    }

    private static void RemoveMissingScripts()
    {
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
    }

    private static void AddSceneToBuildSettings()
    {
        string scenePath = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(scenePath)) return;

        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == scenePath)) return;

        scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Sprite GetOrCreateVignetteSprite()
    {
        string path = UIFolder + "/Vignette.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x / (size - 1f)) * 2f - 1f;
                float dy = (y / (size - 1f)) * 2f - 1f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, distance));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Material GetOrCreateMaterial(string name, string shaderName, Color color)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        material = new Material(Shader.Find(shaderName));
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static T FindAsset<T>() where T : Object
    {
        string guid = AssetDatabase.FindAssets("t:" + typeof(T).Name).FirstOrDefault();
        return guid == null ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static void Wire(Object target, params (string property, Object value)[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        foreach ((string property, Object value) in values)
        {
            SerializedProperty prop = serialized.FindProperty(property);
            if (prop == null)
            {
                Debug.LogWarning($"No encontré el campo '{property}' en {target.GetType().Name}.");
                continue;
            }
            prop.objectReferenceValue = value;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image AddImage(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string content, float fontSize, FontStyles style, TextAlignmentOptions alignment, Vector2 position, Vector2 size)
    {
        return CreateText(name, parent, content, fontSize, style, alignment, position, size, Vector2.zero);
    }

    private static TMP_Text CreateText(string name, Transform parent, string content, float fontSize, FontStyles style, TextAlignmentOptions alignment, Vector2 position, Vector2 size, Vector2 anchor)
    {
        RectTransform rect = CreateRect(name, parent, anchor, anchor, anchor, position, size);
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
}
