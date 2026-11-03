using System.IO;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class CowboyStage5Tools
{
    private const string Root = "Assets/Project";
    private const string DataFolder = Root + "/Data/Explosivos";
    private const string PrefabFolder = Root + "/Prefabs/Explosivos";
    private const string MaterialFolder = Root + "/Materials/Explosivos";
    private const string WeaponMaterialFolder = Root + "/Materials/Armas";

    [MenuItem("Cowboy FPS/Rehacer modelo del Winchester")]
    public static void RebuildWinchester()
    {
        EnsureFolder(WeaponMaterialFolder);

        ProjectileWeapon rifle = Object.FindFirstObjectByType<ProjectileWeapon>(FindObjectsInactive.Include);
        if (rifle == null)
        {
            EditorUtility.DisplayDialog("Falta el Winchester", "No encontré un arma con ProjectileWeapon en la escena.", "OK");
            return;
        }

        Transform root = rifle.transform;
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Rehacer Winchester");

        Transform model = root.Find("Modelo");
        if (model == null)
        {
            model = new GameObject("Modelo").transform;
            model.SetParent(root, false);
        }
        for (int i = model.childCount - 1; i >= 0; i--) Object.DestroyImmediate(model.GetChild(i).gameObject);

        Material metal = GetOrCreateMaterial(WeaponMaterialFolder, "Mat_Arma_Metal", new Color(0.12f, 0.12f, 0.13f));
        Material wood = GetOrCreateMaterial(WeaponMaterialFolder, "Mat_Arma_Madera", new Color(0.42f, 0.26f, 0.14f));

        ViewPart("Cano", model, new Vector3(0f, 0.03f, 0.36f), new Vector3(0.02f, 0.02f, 0.6f), metal);
        ViewPart("Tubo_Cargador", model, new Vector3(0f, 0.008f, 0.31f), new Vector3(0.017f, 0.017f, 0.48f), metal);
        ViewPart("Guardamano", model, new Vector3(0f, 0.012f, 0.17f), new Vector3(0.038f, 0.036f, 0.2f), wood);
        ViewPart("Cajon", model, new Vector3(0f, 0f, 0f), new Vector3(0.042f, 0.07f, 0.17f), metal);
        ViewPart("Palanca", model, new Vector3(0f, -0.05f, -0.01f), new Vector3(0.012f, 0.03f, 0.12f), metal);
        ViewPart("Palanca_Aro", model, new Vector3(0f, -0.072f, -0.05f), new Vector3(0.012f, 0.012f, 0.06f), metal);
        ViewPart("Culata", model, new Vector3(0f, -0.04f, -0.25f), new Vector3(0.04f, 0.08f, 0.34f), wood, -8f);
        ViewPart("Alza_Base", model, new Vector3(0f, 0.045f, 0.18f), new Vector3(0.02f, 0.006f, 0.012f), metal);
        ViewPart("Alza_Izquierda", model, new Vector3(-0.0055f, 0.059f, 0.18f), new Vector3(0.005f, 0.022f, 0.006f), metal);
        ViewPart("Alza_Derecha", model, new Vector3(0.0055f, 0.059f, 0.18f), new Vector3(0.005f, 0.022f, 0.006f), metal);
        ViewPart("Punto_Base", model, new Vector3(0f, 0.043f, 0.62f), new Vector3(0.012f, 0.006f, 0.016f), metal);
        ViewPart("Punto_Mira", model, new Vector3(0f, 0.0515f, 0.62f), new Vector3(0.004f, 0.021f, 0.008f), metal);

        SetChildPosition(root, "Muzzle", new Vector3(0f, 0.03f, 0.67f));
        SetChildPosition(root, "AimPoint", new Vector3(0f, 0.062f, 0.18f));
        root.localPosition = new Vector3(0.16f, -0.19f, 0.3f);
        root.localRotation = Quaternion.identity;
        if (root.parent != null) root.parent.localPosition = Vector3.zero;

        SerializedObject serialized = new SerializedObject(rifle);
        serialized.FindProperty("aimDistanceFromCamera").floatValue = 0.17f;
        serialized.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Cowboy FPS: Winchester rehecho con alza y punto de mira.");
    }

    [MenuItem("Cowboy FPS/Configurar Etapa 5 (dinamita)")]
    public static void SetupStage5()
    {
        EnsureFolder(DataFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Falta el Player", "No encontré un objeto con tag Player en la escena.", "OK");
            return;
        }

        Material dynamiteMat = GetOrCreateMaterial(MaterialFolder, "Mat_Dinamita", new Color(0.75f, 0.12f, 0.1f));
        Material fuseMat = GetOrCreateMaterial(MaterialFolder, "Mat_Mecha", new Color(0.15f, 0.12f, 0.1f));
        Material sparkMat = GetOrCreateMaterial(MaterialFolder, "Mat_Chispa", new Color(1f, 0.8f, 0.3f), true);
        Material fireMat = GetOrCreateMaterial(MaterialFolder, "Mat_Fuego", new Color(1f, 0.55f, 0.15f), true);
        Material smokeMat = GetOrCreateMaterial(MaterialFolder, "Mat_Humo", new Color(0.25f, 0.23f, 0.22f));
        Material barrelMat = GetOrCreateMaterial(MaterialFolder, "Mat_Barril_Explosivo", new Color(0.6f, 0.12f, 0.08f));
        Material ringMat = GetOrCreateMaterial(MaterialFolder, "Mat_Aro", new Color(0.18f, 0.18f, 0.18f));
        Material gateMat = GetOrCreateMaterial(MaterialFolder, "Mat_Porton", new Color(0.35f, 0.22f, 0.12f));

        ExplosionEffect effect = CreateExplosionEffect(fireMat, smokeMat);
        ExplosionDataSO dynamiteExplosion = GetOrCreateAsset<ExplosionDataSO>(DataFolder + "/Explosion_Dinamita.asset", e =>
        {
            e.radius = 6f;
            e.maxDamage = 150f;
            e.minDamage = 15f;
            e.effectPrefab = effect;
        });
        ExplosionDataSO barrelExplosion = GetOrCreateAsset<ExplosionDataSO>(DataFolder + "/Explosion_Barril.asset", e =>
        {
            e.radius = 7f;
            e.maxDamage = 180f;
            e.minDamage = 20f;
            e.pushVelocity = 12f;
            e.cameraShake = 1.2f;
            e.effectPrefab = effect;
        });

        Dynamite dynamitePrefab = CreateDynamitePrefab(dynamiteMat, fuseMat, sparkMat);
        ThrowableDataSO throwable = GetOrCreateAsset<ThrowableDataSO>(DataFolder + "/Dinamita.asset", t =>
        {
            t.prefab = dynamitePrefab;
            t.explosion = dynamiteExplosion;
        });

        GameObject pickup = CreateDynamitePickup(dynamiteMat);
        GameObject barrel = CreateBarrelPrefab(barrelMat, ringMat, barrelExplosion);
        GameObject gate = CreateGatePrefab(gateMat);

        SetupPlayer(player, throwable, dynamiteMat, fuseMat, sparkMat);
        SetupHud(player);
        SetupFusilero(throwable);
        PlaceTestObjects(player.transform, pickup, barrel, gate);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Cowboy FPS: Etapa 5 configurada. Guardá la escena con Ctrl+S.");
    }

    private static void SetupPlayer(GameObject player, ThrowableDataSO throwable, Material dynamiteMat, Material fuseMat, Material sparkMat)
    {
        PlayerWeaponController weapons = player.GetComponent<PlayerWeaponController>();
        Camera camera = player.GetComponentInChildren<Camera>(true);
        Transform holder = camera.transform.Find("WeaponHolder");

        Transform hand = holder != null ? holder.Find("DinamitaMano") : null;
        if (hand == null && holder != null)
        {
            GameObject handObject = BuildDynamiteVisual("DinamitaMano", dynamiteMat, fuseMat, sparkMat, false);
            hand = handObject.transform;
            hand.SetParent(holder, false);
            hand.localPosition = new Vector3(0.17f, -0.17f, 0.38f);
            hand.localRotation = Quaternion.Euler(-20f, 10f, -15f);
            Undo.RegisterCreatedObjectUndo(handObject, "Dinamita en mano");
        }

        PlayerThrowController thrower = player.GetComponent<PlayerThrowController>();
        if (thrower == null) thrower = Undo.AddComponent<PlayerThrowController>(player);
        Wire(thrower,
            ("data", throwable),
            ("input", player.GetComponent<PlayerInputReader>()),
            ("weapons", weapons),
            ("motor", player.GetComponent<PlayerMotor>()),
            ("playerCamera", camera),
            ("health", player.GetComponent<Health>()),
            ("handModel", hand != null ? hand.gameObject : null));

        CameraShake shake = camera.GetComponent<CameraShake>();
        if (shake == null) shake = Undo.AddComponent<CameraShake>(camera.gameObject);
        Wire(shake, ("playerHealth", player.GetComponent<Health>()));
    }

    private static void SetupHud(GameObject player)
    {
        if (Object.FindFirstObjectByType<DynamiteHUD>(FindObjectsInactive.Include) != null) return;

        HealthHUD healthHud = Object.FindFirstObjectByType<HealthHUD>(FindObjectsInactive.Include);
        if (healthHud == null) return;

        Transform hud = healthHud.transform;
        Transform lifePanel = hud.Find("Vida");

        RectTransform countRect = CreateRect("Dinamita", lifePanel != null ? lifePanel : hud, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0f, 64f), new Vector2(340f, 30f));
        TextMeshProUGUI countText = countRect.gameObject.AddComponent<TextMeshProUGUI>();
        countText.text = "Dinamita  3/5";
        countText.fontSize = 24f;
        countText.alignment = TextAlignmentOptions.BottomLeft;
        countText.color = new Color(1f, 0.75f, 0.55f);
        countText.raycastTarget = false;
        countText.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform fuseRoot = CreateRect("Mecha", hud, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(160f, 8f));
        Image background = fuseRoot.gameObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.55f);
        background.raycastTarget = false;
        RectTransform fill = CreateRect("RellenoMecha", fuseRoot, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = new Color(1f, 0.8f, 0.3f);
        fillImage.raycastTarget = false;
        fuseRoot.gameObject.SetActive(false);

        DynamiteHUD dynamiteHud = hud.gameObject.AddComponent<DynamiteHUD>();
        Wire(dynamiteHud,
            ("thrower", player.GetComponent<PlayerThrowController>()),
            ("countText", countText),
            ("fuseRoot", fuseRoot.gameObject),
            ("fuseFill", fill),
            ("fuseImage", fillImage));
    }

    private static void SetupFusilero(ThrowableDataSO throwable)
    {
        EnemyDataSO fusilero = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(Root + "/Data/Enemies/Enemy_Fusilero.asset");
        if (fusilero == null || fusilero.throwable != null) return;
        fusilero.throwable = throwable;
        fusilero.dynamiteCount = 2;
        EditorUtility.SetDirty(fusilero);
    }

    private static void PlaceTestObjects(Transform player, GameObject pickup, GameObject barrel, GameObject gate)
    {
        if (Object.FindFirstObjectByType<ExplosiveBarrel>(FindObjectsInactive.Include) != null) return;

        Vector3 origin = new Vector3(player.position.x, 0f, player.position.z);
        Vector3 f = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
        if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
        Vector3 r = Vector3.Cross(Vector3.up, f);

        GameObject group = new GameObject("Prueba_Explosivos");
        Undo.RegisterCreatedObjectUndo(group, "Prueba explosivos");

        Place(pickup, group.transform, origin + f * 3f - r * 3f, Quaternion.identity);
        Place(pickup, group.transform, origin + f * 3f - r * 4f, Quaternion.identity);
        Place(barrel, group.transform, origin + f * 17f - r * 1.5f + Vector3.up * 0.5f, Quaternion.identity);
        Place(barrel, group.transform, origin + f * 18f + r * 1.2f + Vector3.up * 0.5f, Quaternion.identity);
        Place(gate, group.transform, origin + f * 7f - r * 9f, Quaternion.LookRotation(-f));
    }

    private static ExplosionEffect CreateExplosionEffect(Material fire, Material smoke)
    {
        string path = PrefabFolder + "/Explosion_VFX.prefab";
        ExplosionEffect existing = AssetDatabase.LoadAssetAtPath<ExplosionEffect>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("Explosion_VFX");
        Transform fireball = Sphere("Fuego", root.transform, fire).transform;
        Transform smokeBall = Sphere("Humo", root.transform, smoke).transform;
        fireball.localScale = Vector3.zero;
        smokeBall.localScale = Vector3.zero;

        GameObject lightObject = new GameObject("Destello");
        lightObject.transform.SetParent(root.transform, false);
        Light flash = lightObject.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(1f, 0.6f, 0.25f);
        flash.intensity = 0f;
        flash.range = 15f;
        flash.shadows = LightShadows.None;

        ExplosionEffect effect = root.AddComponent<ExplosionEffect>();
        Wire(effect, ("fireball", fireball), ("smoke", smokeBall), ("flash", flash));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<ExplosionEffect>();
    }

    private static Dynamite CreateDynamitePrefab(Material dynamiteMat, Material fuseMat, Material sparkMat)
    {
        string path = PrefabFolder + "/Dinamita.prefab";
        Dynamite existing = AssetDatabase.LoadAssetAtPath<Dynamite>(path);
        if (existing != null) return existing;

        GameObject root = BuildDynamiteVisual("Dinamita", dynamiteMat, fuseMat, sparkMat, true);
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.mass = 0.4f;
        body.linearDamping = 0.2f;
        body.angularDamping = 0.5f;

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.direction = 2;
        collider.radius = 0.035f;
        collider.height = 0.22f;

        root.AddComponent<Dynamite>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<Dynamite>();
    }

    private static GameObject BuildDynamiteVisual(string name, Material dynamiteMat, Material fuseMat, Material sparkMat, bool castShadows)
    {
        GameObject root = new GameObject(name);

        GameObject stick = Primitive(PrimitiveType.Cylinder, "Cartucho", root.transform, Vector3.zero, new Vector3(0.07f, 0.1f, 0.07f), dynamiteMat, castShadows);
        stick.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        GameObject fuse = Primitive(PrimitiveType.Cylinder, "Mecha", root.transform, new Vector3(0f, 0f, 0.115f), new Vector3(0.008f, 0.02f, 0.008f), fuseMat, false);
        fuse.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        GameObject spark = Primitive(PrimitiveType.Sphere, "Chispa", root.transform, new Vector3(0f, 0f, 0.14f), Vector3.one * 0.03f, sparkMat, false);

        GameObject lightObject = new GameObject("LuzChispa");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0f, 0.14f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.7f, 0.3f);
        light.range = 2.5f;
        light.intensity = 2f;
        light.shadows = LightShadows.None;

        FuseSpark fuseSpark = root.AddComponent<FuseSpark>();
        Wire(fuseSpark, ("spark", spark.transform), ("sparkLight", light));
        return root;
    }

    private static GameObject CreateDynamitePickup(Material dynamiteMat)
    {
        string path = Root + "/Prefabs/Pickups/Pickup_Dinamita.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;
        EnsureFolder(Root + "/Prefabs/Pickups");

        GameObject root = new GameObject("Pickup_Dinamita");
        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.5f, 0f);

        Transform visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);
        visual.localPosition = new Vector3(0f, 0.35f, 0f);
        for (int i = 0; i < 3; i++)
        {
            GameObject stick = Primitive(PrimitiveType.Cylinder, "Cartucho_" + i, visual, new Vector3((i - 1) * 0.075f, 0f, 0f), new Vector3(0.07f, 0.12f, 0.07f), dynamiteMat, true);
            stick.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        DynamitePickup pickup = root.AddComponent<DynamitePickup>();
        Wire(pickup, ("visual", visual));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateBarrelPrefab(Material barrelMat, Material ringMat, ExplosionDataSO explosion)
    {
        string path = PrefabFolder + "/Barril_Explosivo.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        root.name = "Barril_Explosivo";
        root.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
        root.GetComponent<MeshRenderer>().sharedMaterial = barrelMat;

        Primitive(PrimitiveType.Cylinder, "Aro_Superior", root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(1.03f, 0.05f, 1.03f), ringMat, true);
        Primitive(PrimitiveType.Cylinder, "Aro_Inferior", root.transform, new Vector3(0f, -0.6f, 0f), new Vector3(1.03f, 0.05f, 1.03f), ringMat, true);

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.mass = 40f;

        Health health = root.AddComponent<Health>();
        SerializedObject healthSerialized = new SerializedObject(health);
        healthSerialized.FindProperty("maxHealth").floatValue = 25f;
        healthSerialized.ApplyModifiedPropertiesWithoutUndo();

        ExplosiveBarrel barrel = root.AddComponent<ExplosiveBarrel>();
        Wire(barrel, ("explosion", explosion));
        SerializedObject barrelSerialized = new SerializedObject(barrel);
        barrelSerialized.FindProperty("explosionOffset").vector3Value = Vector3.zero;
        barrelSerialized.ApplyModifiedPropertiesWithoutUndo();

        NavMeshObstacle obstacle = root.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Capsule;
        obstacle.carving = true;
        root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateGatePrefab(Material gateMat)
    {
        string path = PrefabFolder + "/Porton_Destructible.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("Porton_Destructible");
        Transform pieces = new GameObject("Tablas").transform;
        pieces.SetParent(root.transform, false);

        for (int i = 0; i < 8; i++)
        {
            float x = -1.75f + i * 0.5f;
            Primitive(PrimitiveType.Cube, "Tabla_" + i, pieces, new Vector3(x, 1.5f, 0f), new Vector3(0.48f, 3f, 0.12f), gateMat, true, true);
        }
        Primitive(PrimitiveType.Cube, "Travesaño_Alto", pieces, new Vector3(0f, 2.4f, -0.1f), new Vector3(4f, 0.25f, 0.1f), gateMat, true, true);
        Primitive(PrimitiveType.Cube, "Travesaño_Bajo", pieces, new Vector3(0f, 0.6f, -0.1f), new Vector3(4f, 0.25f, 0.1f), gateMat, true, true);

        NavMeshObstacle obstacle = root.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = new Vector3(0f, 1.5f, 0f);
        obstacle.size = new Vector3(4f, 3f, 0.4f);
        obstacle.carving = true;
        root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        DestructibleBarrier barrier = root.AddComponent<DestructibleBarrier>();
        Wire(barrier, ("piecesRoot", pieces), ("obstacle", obstacle));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void Place(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.SetPositionAndRotation(position, rotation);
    }

    private static GameObject Sphere(string name, Transform parent, Material material)
    {
        return Primitive(PrimitiveType.Sphere, name, parent, Vector3.zero, Vector3.one, material, false);
    }

    private static void ViewPart(string name, Transform parent, Vector3 position, Vector3 scale, Material material, float pitch = 0f)
    {
        GameObject part = Primitive(PrimitiveType.Cube, name, parent, position, scale, material, false);
        part.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool castShadows, bool keepCollider = false)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        if (!keepCollider) Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        MeshRenderer meshRenderer = part.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        return part;
    }

    private static void SetChildPosition(Transform root, string childName, Vector3 position)
    {
        Transform child = root.Find(childName);
        if (child == null)
        {
            child = new GameObject(childName).transform;
            child.SetParent(root, false);
        }
        child.localPosition = position;
        child.localRotation = Quaternion.identity;
    }

    private static T GetOrCreateAsset<T>(string path, System.Action<T> configure) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        configure(asset);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static Material GetOrCreateMaterial(string folder, string name, Color color, bool unlit = false)
    {
        string path = folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "HUD");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
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
}
