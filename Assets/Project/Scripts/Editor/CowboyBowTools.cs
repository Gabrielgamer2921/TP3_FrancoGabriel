using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CowboyBowTools
{
    private const string Root = "Assets/Project";

    [MenuItem("Cowboy FPS/Agregar arco y reinicio de objetos rompibles")]
    public static void Apply()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null || Object.FindFirstObjectByType<MissionDirector>() == null)
        {
            EditorUtility.DisplayDialog("Abrí FortMercer", "Este menú se aplica sobre la escena FortMercer.", "OK");
            return;
        }

        Material wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Armas/Mat_Arma_Madera.mat");
        Material metal = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Armas/Mat_Arma_Metal.mat");
        Material cord = GetOrCreateMaterial(Root + "/Materials/Armas/Mat_Cuerda.mat", new Color(0.86f, 0.82f, 0.72f));
        Material feather = GetOrCreateMaterial(Root + "/Materials/Armas/Mat_Pluma.mat", new Color(0.75f, 0.18f, 0.12f));
        Material tracer = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Mat_Tracer.mat");

        Projectile arrow = CreateArrowPrefab(wood, metal, feather, tracer);
        WeaponDataSO data = CreateBowData(arrow);
        SoundSO fire = Sound("Jugador_Arco_Disparo", 0.9f, 0f, "Armas/Arco_Disparo_1", "Armas/Arco_Disparo_2", "Armas/Arco_Disparo_3");
        SoundSO draw = Sound("Arco_Tensar", 0.55f, 0f, "Armas/Arco_Tensar");
        SoundSO nock = Sound("Arco_Cargar", 0.6f, 0f, "Armas/Arco_Cargar");

        BuildBow(player, data, wood, metal, cord, feather, fire, draw, nock);
        ConfigureCheckpoints();
        UpdateIntroControls();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Arco listo", "Arco agregado en la tecla 3 (la escopeta pasa a la 4) y objetos rompibles reiniciables.\n\nGuardá la escena con Ctrl+S.", "OK");
    }

    [MenuItem("Cowboy FPS/Rehacer arco y estela de la flecha")]
    public static void RebuildBow()
    {
        GameObject player = GameObject.FindWithTag("Player");
        Camera camera = player != null ? player.GetComponentInChildren<Camera>(true) : null;
        Transform bowTransform = camera != null ? camera.transform.Find("WeaponHolder/Arco") : null;
        if (bowTransform == null)
        {
            EditorUtility.DisplayDialog("Falta el arco", "Primero ejecutá 'Agregar arco y reinicio de objetos rompibles'.", "OK");
            return;
        }

        Material wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Armas/Mat_Arma_Madera.mat");
        Material metal = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Armas/Mat_Arma_Metal.mat");
        Material cord = GetOrCreateMaterial(Root + "/Materials/Armas/Mat_Cuerda.mat", new Color(0.86f, 0.82f, 0.72f));
        Material feather = GetOrCreateMaterial(Root + "/Materials/Armas/Mat_Pluma.mat", new Color(0.75f, 0.18f, 0.12f));
        Material grip = GetOrCreateMaterial(Root + "/Materials/Armas/Mat_Cuero.mat", new Color(0.22f, 0.14f, 0.09f));

        Transform model = bowTransform.Find("Modelo");
        for (int i = model.childCount - 1; i >= 0; i--) Object.DestroyImmediate(model.GetChild(i).gameObject);
        model.localRotation = Quaternion.Euler(0f, 0f, -25f);
        bowTransform.localPosition = new Vector3(-0.04f, -0.16f, 0.36f);

        Part("Empunadura", model, Vector3.zero, new Vector3(0.028f, 0.12f, 0.036f), grip, 0f, false);
        float[] ys = { 0.1f, 0.185f, 0.265f, 0.34f };
        float[] zs = { -0.006f, -0.022f, -0.047f, -0.08f };
        float[] angles = { -6f, -14f, -22f, -32f };
        float[] widths = { 0.02f, 0.019f, 0.017f, 0.015f };
        for (int i = 0; i < 4; i++)
        {
            Part("Rama_Superior_" + i, model, new Vector3(0f, ys[i], zs[i]), new Vector3(widths[i], 0.09f, 0.016f), wood, angles[i], false);
            Part("Rama_Inferior_" + i, model, new Vector3(0f, -ys[i], zs[i]), new Vector3(widths[i], 0.09f, 0.016f), wood, -angles[i], false);
        }

        Transform upperTip = Marker("Punta_Superior", model, new Vector3(0f, 0.38f, -0.105f));
        Transform lowerTip = Marker("Punta_Inferior", model, new Vector3(0f, -0.38f, -0.105f));
        Transform nockPoint = Marker("Cuerda_Centro", model, new Vector3(0f, 0f, -0.105f));
        Transform upperString = Part("Cuerda_Superior", model, Vector3.zero, Vector3.one * 0.004f, cord, 0f, false).transform;
        Transform lowerString = Part("Cuerda_Inferior", model, Vector3.zero, Vector3.one * 0.004f, cord, 0f, false).transform;

        GameObject loaded = new GameObject("Flecha_Cargada");
        loaded.transform.SetParent(nockPoint, false);
        Part("Asta", loaded.transform, new Vector3(0f, 0f, 0.23f), new Vector3(0.007f, 0.007f, 0.46f), wood, 0f, false);
        Part("Punta", loaded.transform, new Vector3(0f, 0f, 0.475f), new Vector3(0.015f, 0.015f, 0.04f), metal, 0f, false);
        Part("Pluma_A", loaded.transform, new Vector3(0f, 0.01f, 0.05f), new Vector3(0.002f, 0.018f, 0.07f), feather, 0f, false);
        Part("Pluma_B", loaded.transform, new Vector3(0f, -0.01f, 0.05f), new Vector3(0.002f, 0.018f, 0.07f), feather, 0f, false);

        Transform aimPoint = bowTransform.Find("AimPoint");
        if (aimPoint != null) aimPoint.localPosition = new Vector3(-0.03f, 0.03f, 0f);
        Transform muzzle = bowTransform.Find("Muzzle");
        if (muzzle != null) muzzle.localPosition = new Vector3(0.02f, 0.01f, 0.45f);

        BowWeapon weapon = bowTransform.GetComponent<BowWeapon>();
        SerializedObject serialized = new SerializedObject(weapon);
        serialized.FindProperty("aimDistanceFromCamera").floatValue = 0.45f;
        serialized.FindProperty("nockPoint").objectReferenceValue = nockPoint;
        serialized.FindProperty("upperTip").objectReferenceValue = upperTip;
        serialized.FindProperty("lowerTip").objectReferenceValue = lowerTip;
        serialized.FindProperty("upperString").objectReferenceValue = upperString;
        serialized.FindProperty("lowerString").objectReferenceValue = lowerString;
        serialized.FindProperty("loadedArrow").objectReferenceValue = loaded;
        serialized.FindProperty("restNock").vector3Value = new Vector3(0f, 0f, -0.105f);
        serialized.FindProperty("drawnNock").vector3Value = new Vector3(0.093f, -0.034f, -0.32f);
        serialized.FindProperty("arrowRest").vector3Value = new Vector3(0.022f, 0f, 0f);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        UpdateArrowTrail();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Cowboy FPS: arco rehecho y estela de la flecha actualizada.");
    }

    private static void UpdateArrowTrail()
    {
        string path = Root + "/Prefabs/Flecha.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        TrailRenderer trail = root.GetComponent<TrailRenderer>();
        if (trail == null) trail = root.AddComponent<TrailRenderer>();

        Material trailMaterial = CreateTrailMaterial();
        if (trailMaterial != null) trail.sharedMaterial = trailMaterial;
        trail.time = 0.8f;
        trail.minVertexDistance = 0.15f;
        trail.numCapVertices = 4;
        trail.textureMode = LineTextureMode.Stretch;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.13f), new Keyframe(0.35f, 0.09f), new Keyframe(1f, 0f));
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0.4f, 0.35f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static Material CreateTrailMaterial()
    {
        string path = Root + "/Materials/Estela_Flecha.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline == null || pipeline.defaultParticleMaterial == null) return null;

        material = new Material(pipeline.defaultParticleMaterial);
        material.name = "Estela_Flecha";
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", null);
        material.mainTexture = null;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static WeaponDataSO CreateBowData(Projectile arrow)
    {
        string path = Root + "/Data/Arco.asset";
        WeaponDataSO data = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(path);
        if (data != null) return data;

        data = ScriptableObject.CreateInstance<WeaponDataSO>();
        data.displayName = "Arco";
        data.ammoType = null;
        data.damage = 300f;
        data.range = 120f;
        data.damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        data.impactForce = 4f;
        data.noiseRadius = 6f;
        data.timeBetweenShots = 0.2f;
        data.magazineSize = 1;
        data.startingMagazine = 1;
        data.hipSpread = 4f;
        data.aimSpread = 0.15f;
        data.moveSpreadBonus = 1f;
        data.airSpreadBonus = 3f;
        data.recoilVertical = 1f;
        data.recoilHorizontal = 0.3f;
        data.visualKickBack = 0.03f;
        data.visualKickRotation = 3f;
        data.aimFov = 50f;
        data.aimSpeed = 5f;
        data.aimMoveMultiplier = 0.55f;
        data.aimSensitivityMultiplier = 0.7f;
        data.equipTime = 0.4f;
        data.sprintToFireTime = 0.2f;
        data.projectilePrefab = arrow;
        data.projectileSpeed = 55f;
        data.projectileGravityScale = 1f;
        data.projectileLifetime = 8f;
        data.zeroDistance = 0f;
        data.projectileGravityDelay = 0.4f;
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static Projectile CreateArrowPrefab(Material wood, Material metal, Material feather, Material tracer)
    {
        string path = Root + "/Prefabs/Flecha.prefab";
        Projectile existing = AssetDatabase.LoadAssetAtPath<Projectile>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("Flecha");
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.mass = 0.05f;
        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.direction = 2;
        capsule.radius = 0.02f;
        capsule.height = 0.75f;

        Part("Asta", root.transform, new Vector3(0f, 0f, 0f), new Vector3(0.01f, 0.01f, 0.72f), wood, 0f, true);
        Part("Punta", root.transform, new Vector3(0f, 0f, 0.37f), new Vector3(0.02f, 0.02f, 0.05f), metal, 0f, true);
        Part("Pluma_A", root.transform, new Vector3(0f, 0.012f, -0.3f), new Vector3(0.002f, 0.022f, 0.08f), feather, 0f, true);
        Part("Pluma_B", root.transform, new Vector3(0f, -0.012f, -0.3f), new Vector3(0.002f, 0.022f, 0.08f), feather, 0f, true);

        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.sharedMaterial = tracer;
        trail.time = 0.06f;
        trail.startWidth = 0.012f;
        trail.endWidth = 0f;
        trail.shadowCastingMode = ShadowCastingMode.Off;

        Projectile projectile = root.AddComponent<Projectile>();
        SerializedObject serialized = new SerializedObject(projectile);
        serialized.FindProperty("stickOnImpact").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<Projectile>();
    }

    private static void BuildBow(GameObject player, WeaponDataSO data, Material wood, Material metal, Material cord, Material feather, SoundSO fire, SoundSO draw, SoundSO nock)
    {
        PlayerWeaponController controller = player.GetComponent<PlayerWeaponController>();
        Camera camera = player.GetComponentInChildren<Camera>(true);
        Transform holder = camera.transform.Find("WeaponHolder");
        if (holder == null || holder.Find("Arco") != null) return;

        GameObject bow = new GameObject("Arco");
        bow.transform.SetParent(holder, false);
        bow.transform.localPosition = new Vector3(0.1f, -0.14f, 0.32f);

        Transform model = new GameObject("Modelo").transform;
        model.SetParent(bow.transform, false);
        model.localRotation = Quaternion.Euler(0f, 0f, -15f);

        Part("Empunadura", model, Vector3.zero, new Vector3(0.026f, 0.12f, 0.034f), wood, 0f, false);
        float[] ys = { 0.1f, 0.185f, 0.265f, 0.34f };
        float[] zs = { 0.006f, 0.022f, 0.047f, 0.08f };
        float[] angles = { 6f, 14f, 22f, 32f };
        float[] widths = { 0.02f, 0.019f, 0.017f, 0.015f };
        for (int i = 0; i < 4; i++)
        {
            Part("Rama_Superior_" + i, model, new Vector3(0f, ys[i], zs[i]), new Vector3(widths[i], 0.09f, 0.016f), wood, angles[i], false);
            Part("Rama_Inferior_" + i, model, new Vector3(0f, -ys[i], zs[i]), new Vector3(widths[i], 0.09f, 0.016f), wood, -angles[i], false);
        }

        Transform upperTip = Marker("Punta_Superior", model, new Vector3(0f, 0.38f, 0.105f));
        Transform lowerTip = Marker("Punta_Inferior", model, new Vector3(0f, -0.38f, 0.105f));
        Transform nockPoint = Marker("Cuerda_Centro", model, new Vector3(0f, 0f, 0.105f));
        Transform upperString = Part("Cuerda_Superior", model, Vector3.zero, Vector3.one * 0.004f, cord, 0f, false).transform;
        Transform lowerString = Part("Cuerda_Inferior", model, Vector3.zero, Vector3.one * 0.004f, cord, 0f, false).transform;

        GameObject loaded = new GameObject("Flecha_Cargada");
        loaded.transform.SetParent(nockPoint, false);
        Part("Asta", loaded.transform, new Vector3(0.025f, 0f, 0.36f), new Vector3(0.008f, 0.008f, 0.72f), wood, 0f, false);
        Part("Punta", loaded.transform, new Vector3(0.025f, 0f, 0.73f), new Vector3(0.018f, 0.018f, 0.05f), metal, 0f, false);
        Part("Pluma_A", loaded.transform, new Vector3(0.025f, 0.012f, 0.06f), new Vector3(0.002f, 0.022f, 0.08f), feather, 0f, false);
        Part("Pluma_B", loaded.transform, new Vector3(0.025f, -0.012f, 0.06f), new Vector3(0.002f, 0.022f, 0.08f), feather, 0f, false);

        Transform muzzle = Marker("Muzzle", bow.transform, new Vector3(0.02f, 0f, 0.5f));
        Transform aimPoint = Marker("AimPoint", bow.transform, new Vector3(0.02f, 0.02f, 0.05f));

        BowWeapon weapon = bow.AddComponent<BowWeapon>();
        SerializedObject serialized = new SerializedObject(weapon);
        serialized.FindProperty("data").objectReferenceValue = data;
        serialized.FindProperty("muzzle").objectReferenceValue = muzzle;
        serialized.FindProperty("aimPoint").objectReferenceValue = aimPoint;
        serialized.FindProperty("aimDistanceFromCamera").floatValue = 0.45f;
        serialized.FindProperty("nockPoint").objectReferenceValue = nockPoint;
        serialized.FindProperty("upperTip").objectReferenceValue = upperTip;
        serialized.FindProperty("lowerTip").objectReferenceValue = lowerTip;
        serialized.FindProperty("upperString").objectReferenceValue = upperString;
        serialized.FindProperty("lowerString").objectReferenceValue = lowerString;
        serialized.FindProperty("loadedArrow").objectReferenceValue = loaded;
        serialized.FindProperty("restNock").vector3Value = new Vector3(0f, 0f, 0.105f);
        serialized.FindProperty("drawnNock").vector3Value = new Vector3(0f, 0f, -0.3f);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        WeaponAudio audio = bow.AddComponent<WeaponAudio>();
        SerializedObject audioSerialized = new SerializedObject(audio);
        audioSerialized.FindProperty("weapon").objectReferenceValue = weapon;
        audioSerialized.FindProperty("fire").objectReferenceValue = fire;
        audioSerialized.ApplyModifiedPropertiesWithoutUndo();

        BowAudio bowAudio = bow.AddComponent<BowAudio>();
        SerializedObject bowSerialized = new SerializedObject(bowAudio);
        bowSerialized.FindProperty("bow").objectReferenceValue = weapon;
        bowSerialized.FindProperty("draw").objectReferenceValue = draw;
        bowSerialized.FindProperty("nock").objectReferenceValue = nock;
        bowSerialized.ApplyModifiedPropertiesWithoutUndo();

        bow.SetActive(false);

        SerializedObject controllerSerialized = new SerializedObject(controller);
        SerializedProperty weapons = controllerSerialized.FindProperty("weapons");
        weapons.arraySize++;
        weapons.GetArrayElementAtIndex(weapons.arraySize - 1).objectReferenceValue = weapon;
        controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCheckpoints()
    {
        Dictionary<int, ZoneEncounter> zones = Object.FindObjectsByType<ZoneEncounter>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .GroupBy(z => z.ZoneId).ToDictionary(g => g.Key, g => g.First());
        GameObject gateObject = GameObject.Find("Porton_Principal");
        DestructibleBarrier gate = gateObject != null ? gateObject.GetComponent<DestructibleBarrier>() : null;

        foreach (Checkpoint checkpoint in Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SerializedObject serialized = new SerializedObject(checkpoint);
            string displayName = serialized.FindProperty("displayName").stringValue;

            if (displayName == "Patio de entrada" && gate != null)
            {
                SerializedProperty locks = serialized.FindProperty("lockBarriersOnReach");
                locks.arraySize = 1;
                locks.GetArrayElementAtIndex(0).objectReferenceValue = gate;
            }

            int zoneId = displayName switch
            {
                "Huida" => 4,
                "Huida: callejón" => 3,
                "Huida: cuartel" => 2,
                "Huida: patio de entrada" => 1,
                "Huida: camino" => 0,
                _ => -1
            };

            SerializedProperty props = serialized.FindProperty("resetPropsOfZones");
            if (zoneId >= 0 && zones.TryGetValue(zoneId, out ZoneEncounter zone))
            {
                props.arraySize = 1;
                props.GetArrayElementAtIndex(0).objectReferenceValue = zone;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void UpdateIntroControls()
    {
        IntroSequence intro = Object.FindFirstObjectByType<IntroSequence>(FindObjectsInactive.Include);
        if (intro == null) return;
        Transform body = intro.transform.Find("Controles/Cuerpo");
        TMP_Text text = body != null ? body.GetComponent<TMP_Text>() : null;
        if (text == null || text.text.Contains("Arco")) return;

        string updated = text.text.Replace("1 / 2 / Rueda</color>  Cambiar arma", "1 / 2 / 3 / 4</color>  Revólver, Winchester, arco, escopeta");
        string bowLine = "<color=#E8B04A>Arco</color>: mantené clic derecho para tensar y clic izquierdo para soltar la flecha.\n";
        int noteIndex = updated.IndexOf("<size=80%>");
        updated = noteIndex >= 0 ? updated.Insert(noteIndex, bowLine) : updated + "\n" + bowLine;
        text.text = updated;
        EditorUtility.SetDirty(text);
    }

    private static SoundSO Sound(string name, float volume, float spatial, params string[] clips)
    {
        string path = Root + "/Data/Sonidos/" + name + ".asset";
        SoundSO sound = AssetDatabase.LoadAssetAtPath<SoundSO>(path);
        if (sound != null) return sound;
        sound = ScriptableObject.CreateInstance<SoundSO>();
        sound.clips = clips.Select(c => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Generados/" + c + ".wav")).Where(c => c != null).ToArray();
        sound.volume = volume;
        sound.spatialBlend = spatial;
        AssetDatabase.CreateAsset(sound, path);
        return sound;
    }

    private static GameObject Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material, float pitch, bool castShadows)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        part.transform.localScale = scale;
        MeshRenderer meshRenderer = part.GetComponent<MeshRenderer>();
        if (material != null) meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        return part;
    }

    private static Transform Marker(string name, Transform parent, Vector3 localPosition)
    {
        Transform marker = new GameObject(name).transform;
        marker.SetParent(parent, false);
        marker.localPosition = localPosition;
        return marker;
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
