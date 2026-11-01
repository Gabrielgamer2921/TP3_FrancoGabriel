using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public static class CowboyStage4Tools
{
    private const string Root = "Assets/_Project";
    private const string EnemyDataFolder = Root + "/Data/Enemies";
    private const string EnemyPrefabFolder = Root + "/Prefabs/Enemies";
    private const string EnemyMaterialFolder = Root + "/Materials/Enemies";
    private const string UIFolder = Root + "/Materials/UI";

    [MenuItem("Cowboy FPS/Configurar Etapa 4 (enemigos)")]
    public static void SetupStage4()
    {
        EnsureFolder(EnemyDataFolder);
        EnsureFolder(EnemyPrefabFolder);
        EnsureFolder(EnemyMaterialFolder);
        EnsureFolder(UIFolder);

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Falta el Player", "No encontré un objeto con tag Player en la escena.", "OK");
            return;
        }

        SetupPlayer(player);

        EnemyDataSO pistoleroData = CreatePistoleroData();
        EnemyDataSO fusileroData = CreateFusileroData();
        EnemyDataSO sniperData = CreateSniperData();

        EnemyAI pistolero = CreateEnemyPrefab("Enemy_Pistolero", pistoleroData, new Color(0.55f, 0.25f, 0.18f), 0.3f, false);
        EnemyAI fusilero = CreateEnemyPrefab("Enemy_Fusilero", fusileroData, new Color(0.3f, 0.38f, 0.22f), 0.6f, false);
        EnemyAI sniper = CreateEnemyPrefab("Enemy_Francotirador", sniperData, new Color(0.32f, 0.34f, 0.42f), 0.8f, true);

        EnsureNavMeshSurface();

        if (Object.FindFirstObjectByType<EnemyAI>(FindObjectsInactive.Include) == null)
            PlaceDemoEncounter(player.transform, pistolero, fusilero, sniper);

        GenerateCoverPoints();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Etapa 4 lista",
            "Último paso: seleccioná el objeto 'NavMesh' en la jerarquía y apretá 'Bake' en su componente NavMesh Surface.\n\nDespués guardá la escena con Ctrl+S.", "OK");
    }

    [MenuItem("Cowboy FPS/Generar puntos de cobertura")]
    public static void GenerateCoverPoints()
    {
        GameObject existing = GameObject.Find("CoverPoints");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject parent = new GameObject("CoverPoints");
        Undo.RegisterCreatedObjectUndo(parent, "Generar coberturas");
        int created = 0;

        foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (!IsCoverObstacle(collider)) continue;

            Bounds bounds = collider.bounds;
            Vector3 center = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Vector3[] offsets =
            {
                new Vector3(bounds.extents.x + 0.55f, 0f, 0f),
                new Vector3(-bounds.extents.x - 0.55f, 0f, 0f),
                new Vector3(0f, 0f, bounds.extents.z + 0.55f),
                new Vector3(0f, 0f, -bounds.extents.z - 0.55f)
            };

            foreach (Vector3 offset in offsets)
            {
                Vector3 position = center + offset;
                if (Physics.CheckSphere(position + Vector3.up * 0.6f, 0.3f, ~0, QueryTriggerInteraction.Ignore)) continue;

                GameObject point = new GameObject("Cover_" + collider.name);
                point.transform.SetParent(parent.transform);
                point.transform.position = position;
                point.AddComponent<CoverPoint>();
                created++;
            }
        }

        Debug.Log($"Cowboy FPS: {created} puntos de cobertura generados.");
    }

    private static bool IsCoverObstacle(Collider collider)
    {
        if (collider.isTrigger || collider.attachedRigidbody != null) return false;
        if (collider.GetComponentInParent<PlayerTarget>() != null) return false;
        if (collider.GetComponentInParent<EnemyAI>() != null) return false;
        if (collider.GetComponentInParent<Pickup>() != null) return false;
        if (collider.GetComponentInParent<AlarmBell>() != null) return false;
        if (collider.GetComponentInParent<LockedDoor>() != null) return false;
        if (collider.GetComponentInParent<DestructibleBarrier>() != null) return false;
        if (collider.name.StartsWith("Torre")) return false;

        Bounds bounds = collider.bounds;
        if (bounds.size.y < 0.7f || bounds.size.y > 4.5f) return false;
        if (bounds.size.x > 10f || bounds.size.z > 10f) return false;
        return true;
    }

    private static void SetupPlayer(GameObject player)
    {
        PlayerTarget target = player.GetComponent<PlayerTarget>();
        if (target == null) target = Undo.AddComponent<PlayerTarget>(player);
        Wire(target, ("headPoint", player.transform.Find("CameraPivot")));

        NavMeshModifier modifier = player.GetComponent<NavMeshModifier>();
        if (modifier == null) modifier = Undo.AddComponent<NavMeshModifier>(player);
        modifier.ignoreFromBuild = true;
    }

    private static void EnsureNavMeshSurface()
    {
        if (Object.FindFirstObjectByType<NavMeshSurface>() != null) return;
        GameObject navMesh = new GameObject("NavMesh");
        Undo.RegisterCreatedObjectUndo(navMesh, "Crear NavMesh");
        NavMeshSurface surface = navMesh.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
    }

    private static EnemyDataSO CreatePistoleroData()
    {
        return GetOrCreateData("Enemy_Pistolero", d =>
        {
            d.displayName = "Pistolero";
            d.combatStyle = EnemyCombatStyle.Assault;
            d.maxHealth = 80f;
            d.chaseSpeed = 4.8f;
            d.viewDistance = 28f;
            d.detectionTime = 0.7f;
            d.damage = 8f;
            d.timeBetweenShots = 0.55f;
            d.magazineSize = 6;
            d.reloadTime = 2.2f;
            d.attackRange = 22f;
            d.bulletSpeed = 110f;
            d.baseSpread = 3.5f;
            d.preferredMinDistance = 5f;
            d.preferredMaxDistance = 11f;
            d.usesCover = true;
            d.coverChance = 0.4f;
            d.coverStayTime = 1.2f;
            d.peekTime = 2f;
            d.repositionIntervalMin = 2f;
            d.repositionIntervalMax = 3.5f;
            d.loot = new List<LootEntry> { Loot("Pickup_Municion_45Colt", 0.6f), Loot("Pickup_Vendaje", 0.1f) };
        });
    }

    private static EnemyDataSO CreateFusileroData()
    {
        return GetOrCreateData("Enemy_Fusilero", d =>
        {
            d.displayName = "Fusilero";
            d.combatStyle = EnemyCombatStyle.Ranged;
            d.maxHealth = 100f;
            d.chaseSpeed = 4f;
            d.viewDistance = 38f;
            d.detectionTime = 0.9f;
            d.damage = 14f;
            d.timeBetweenShots = 1.1f;
            d.magazineSize = 8;
            d.reloadTime = 3f;
            d.attackRange = 40f;
            d.bulletSpeed = 130f;
            d.baseSpread = 2f;
            d.spreadPerMeter = 0.05f;
            d.preferredMinDistance = 12f;
            d.preferredMaxDistance = 25f;
            d.usesCover = true;
            d.coverChance = 0.9f;
            d.coverSearchRadius = 16f;
            d.coverStayTime = 2f;
            d.peekTime = 2.5f;
            d.repositionIntervalMin = 4f;
            d.repositionIntervalMax = 6f;
            d.loot = new List<LootEntry> { Loot("Pickup_Municion_4440", 0.6f), Loot("Pickup_Vendaje", 0.15f) };
        });
    }

    private static EnemyDataSO CreateSniperData()
    {
        return GetOrCreateData("Enemy_Francotirador", d =>
        {
            d.displayName = "Francotirador";
            d.combatStyle = EnemyCombatStyle.Sniper;
            d.maxHealth = 70f;
            d.patrolSpeed = 0f;
            d.viewDistance = 60f;
            d.viewAngle = 90f;
            d.detectionTime = 1.2f;
            d.memoryTime = 8f;
            d.damage = 45f;
            d.timeBetweenShots = 3.5f;
            d.magazineSize = 5;
            d.reloadTime = 3.5f;
            d.attackRange = 70f;
            d.bulletSpeed = 200f;
            d.baseSpread = 0.4f;
            d.spreadPerMeter = 0.01f;
            d.usesCover = false;
            d.canRingAlarm = false;
            d.telegraphTime = 1.5f;
            d.loot = new List<LootEntry> { Loot("Pickup_Municion_4440", 0.7f) };
        });
    }

    private static EnemyAI CreateEnemyPrefab(string name, EnemyDataSO data, Color bodyColor, float gunLength, bool isSniper)
    {
        string path = EnemyPrefabFolder + "/" + name + ".prefab";
        EnemyAI existing = AssetDatabase.LoadAssetAtPath<EnemyAI>(path);
        if (existing != null) return existing;

        Material bodyMaterial = GetOrCreateMaterial(EnemyMaterialFolder, "Mat_" + name, bodyColor);
        Material skin = GetOrCreateMaterial(EnemyMaterialFolder, "Mat_Piel", new Color(0.85f, 0.65f, 0.5f));
        Material hat = GetOrCreateMaterial(EnemyMaterialFolder, "Mat_Sombrero", new Color(0.25f, 0.17f, 0.1f));
        Material metal = GetOrCreateMaterial(EnemyMaterialFolder, "Mat_Metal", new Color(0.15f, 0.15f, 0.15f));
        VisualBullet visualBullet = AssetDatabase.LoadAssetAtPath<VisualBullet>(Root + "/Prefabs/VisualBullet.prefab");

        GameObject root = new GameObject(name);
        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.9f, 0f);
        capsule.height = 1.8f;
        capsule.radius = 0.35f;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.mass = 70f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.35f;
        agent.height = 1.8f;
        agent.stoppingDistance = 0.3f;
        agent.acceleration = 16f;

        Health health = root.AddComponent<Health>();
        root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        Transform visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);

        Part(PrimitiveType.Capsule, "Cuerpo", visual, new Vector3(0f, 0.85f, 0f), new Vector3(0.65f, 0.75f, 0.5f), bodyMaterial);
        GameObject head = Part(PrimitiveType.Sphere, "Cabeza", visual, new Vector3(0f, 1.62f, 0f), Vector3.one * 0.3f, skin, true);
        Hitbox hitbox = head.AddComponent<Hitbox>();
        Wire(hitbox, ("owner", health));
        SetFloat(hitbox, "damageMultiplier", 2f);
        SetBool(hitbox, "isCritical", true);
        Part(PrimitiveType.Cylinder, "Sombrero_Ala", visual, new Vector3(0f, 1.76f, 0f), new Vector3(0.5f, 0.012f, 0.5f), hat);
        Part(PrimitiveType.Cylinder, "Sombrero_Copa", visual, new Vector3(0f, 1.84f, 0f), new Vector3(0.26f, 0.07f, 0.26f), hat);
        Part(PrimitiveType.Cube, "Arma", visual, new Vector3(0.2f, 1.2f, 0.25f + gunLength * 0.5f), new Vector3(0.06f, 0.08f, gunLength), metal);

        Transform muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(visual, false);
        muzzle.localPosition = new Vector3(0.2f, 1.22f, 0.3f + gunLength);

        Transform eyes = new GameObject("Ojos").transform;
        eyes.SetParent(visual, false);
        eyes.localPosition = new Vector3(0f, 1.62f, 0.12f);

        SniperGlint glint = null;
        if (isSniper)
        {
            Part(PrimitiveType.Cylinder, "Mira_Telescopica", visual, new Vector3(0.2f, 1.29f, 0.45f), new Vector3(0.05f, 0.12f, 0.05f), metal).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject glintObject = new GameObject("Brillo");
            glintObject.transform.SetParent(visual, false);
            glintObject.transform.localPosition = new Vector3(0.2f, 1.29f, 0.6f);
            glintObject.transform.localScale = Vector3.one * 0.5f;
            SpriteRenderer sprite = glintObject.AddComponent<SpriteRenderer>();
            sprite.sprite = GetOrCreateGlintSprite();
            sprite.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            sprite.color = new Color(1f, 0.97f, 0.85f);
            sprite.sortingOrder = 100;
            glint = glintObject.AddComponent<SniperGlint>();
        }

        GameObject iconObject = new GameObject("IconoAlerta");
        iconObject.transform.SetParent(root.transform, false);
        iconObject.transform.localPosition = new Vector3(0f, 2.25f, 0f);
        TextMeshPro label = iconObject.AddComponent<TextMeshPro>();
        label.text = string.Empty;
        label.fontSize = 6f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(1f, 1f);

        EnemyPerception perception = root.AddComponent<EnemyPerception>();
        Wire(perception, ("eyes", eyes));

        EnemyWeapon weapon = root.AddComponent<EnemyWeapon>();
        Wire(weapon, ("muzzle", muzzle), ("visualBulletPrefab", visualBullet));

        EnemyAI ai = root.AddComponent<EnemyAI>();
        Wire(ai, ("data", data), ("perception", perception), ("weapon", weapon), ("visualRoot", visual), ("glint", glint));

        EnemyHitFlash flash = root.AddComponent<EnemyHitFlash>();
        Wire(flash, ("health", health));

        EnemyAlertIcon icon = iconObject.AddComponent<EnemyAlertIcon>();
        Wire(icon, ("enemy", ai), ("label", label));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<EnemyAI>();
    }

    private static void PlaceDemoEncounter(Transform player, EnemyAI pistolero, EnemyAI fusilero, EnemyAI sniper)
    {
        Vector3 origin = new Vector3(player.position.x, 0f, player.position.z);
        Vector3 f = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
        if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
        Vector3 r = Vector3.Cross(Vector3.up, f);
        Quaternion facingPlayer = Quaternion.LookRotation(-f);

        GameObject encounter = new GameObject("Encuentro_Prueba");
        Undo.RegisterCreatedObjectUndo(encounter, "Crear encuentro");

        PlaceWithRoute(pistolero, "Pistolero_A", encounter.transform, origin + f * 13f - r * 5f, facingPlayer, new[] { -r * 3f, r * 3f, f * 3f });
        PlaceWithRoute(pistolero, "Pistolero_B", encounter.transform, origin + f * 15f + r * 6f, facingPlayer, new[] { f * 3f, -f * 3f });
        PlaceWithRoute(fusilero, "Fusilero", encounter.transform, origin + f * 20f, facingPlayer, new[] { r * 4f, -r * 4f });

        Material wood = GetOrCreateMaterial(EnemyMaterialFolder, "Mat_Madera", new Color(0.45f, 0.3f, 0.18f));
        GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tower.name = "Torre_Francotirador";
        tower.transform.SetParent(encounter.transform);
        tower.transform.position = origin + f * 20f + r * 13f + Vector3.up * 2f;
        tower.transform.localScale = new Vector3(3f, 4f, 3f);
        tower.GetComponent<MeshRenderer>().sharedMaterial = wood;

        EnemyAI sniperInstance = (EnemyAI)PrefabUtility.InstantiatePrefab(sniper, encounter.transform);
        sniperInstance.name = "Francotirador";
        sniperInstance.transform.position = tower.transform.position + Vector3.up * 2f;
        Vector3 toPlayer = origin - sniperInstance.transform.position;
        toPlayer.y = 0f;
        sniperInstance.transform.rotation = Quaternion.LookRotation(toPlayer);

        GameObject bellRoot = new GameObject("Campana_Alarma");
        bellRoot.transform.SetParent(encounter.transform);
        bellRoot.transform.position = origin + f * 18f - r * 12f;
        Material bellMaterial = GetOrCreateMaterial(EnemyMaterialFolder, "Mat_Campana", new Color(0.7f, 0.55f, 0.2f));
        Part(PrimitiveType.Cube, "Poste", bellRoot.transform, new Vector3(0f, 1.5f, 0f), new Vector3(0.15f, 3f, 0.15f), wood, true);
        Part(PrimitiveType.Cube, "Travesaño", bellRoot.transform, new Vector3(0.35f, 2.9f, 0f), new Vector3(0.8f, 0.12f, 0.12f), wood);
        Transform bellPivot = new GameObject("PivoteCampana").transform;
        bellPivot.SetParent(bellRoot.transform, false);
        bellPivot.localPosition = new Vector3(0.6f, 2.85f, 0f);
        Part(PrimitiveType.Cylinder, "Campana", bellPivot, new Vector3(0f, -0.2f, 0f), new Vector3(0.35f, 0.18f, 0.35f), bellMaterial);
        Transform ringPoint = new GameObject("PuntoToque").transform;
        ringPoint.SetParent(bellRoot.transform, false);
        ringPoint.localPosition = new Vector3(0.6f, 0f, -0.8f);
        AlarmBell bell = bellRoot.AddComponent<AlarmBell>();
        Wire(bell, ("ringPoint", ringPoint), ("bellVisual", bellPivot));

        GameObject spawnerObject = new GameObject("Refuerzos");
        spawnerObject.transform.SetParent(encounter.transform);
        spawnerObject.transform.position = origin + f * 24f;
        Transform spawnA = new GameObject("Spawn_A").transform;
        spawnA.SetParent(spawnerObject.transform, false);
        spawnA.position = origin + f * 24f - r * 18f;
        Transform spawnB = new GameObject("Spawn_B").transform;
        spawnB.SetParent(spawnerObject.transform, false);
        spawnB.position = origin + f * 24f + r * 18f;

        ReinforcementSpawner spawner = spawnerObject.AddComponent<ReinforcementSpawner>();
        SerializedObject serialized = new SerializedObject(spawner);
        serialized.FindProperty("triggerBell").objectReferenceValue = bell;
        SerializedProperty points = serialized.FindProperty("spawnPoints");
        points.arraySize = 2;
        points.GetArrayElementAtIndex(0).objectReferenceValue = spawnA;
        points.GetArrayElementAtIndex(1).objectReferenceValue = spawnB;
        SerializedProperty waves = serialized.FindProperty("waves");
        waves.arraySize = 2;
        FillWave(waves.GetArrayElementAtIndex(0), 3f, pistolero, pistolero);
        FillWave(waves.GetArrayElementAtIndex(1), 10f, pistolero, fusilero);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void FillWave(SerializedProperty wave, float delay, params EnemyAI[] enemies)
    {
        wave.FindPropertyRelative("delayBefore").floatValue = delay;
        wave.FindPropertyRelative("delayBetweenSpawns").floatValue = 0.6f;
        SerializedProperty list = wave.FindPropertyRelative("enemies");
        list.arraySize = enemies.Length;
        for (int i = 0; i < enemies.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
    }

    private static void PlaceWithRoute(EnemyAI prefab, string name, Transform parent, Vector3 position, Quaternion rotation, Vector3[] routeOffsets)
    {
        EnemyAI enemy = (EnemyAI)PrefabUtility.InstantiatePrefab(prefab, parent);
        enemy.name = name;
        enemy.transform.SetPositionAndRotation(position, rotation);

        GameObject route = new GameObject("Ruta_" + name);
        route.transform.SetParent(parent);
        route.transform.position = position;
        GameObject start = new GameObject("Punto_0");
        start.transform.SetParent(route.transform, false);
        for (int i = 0; i < routeOffsets.Length; i++)
        {
            GameObject point = new GameObject("Punto_" + (i + 1));
            point.transform.SetParent(route.transform, false);
            point.transform.position = position + routeOffsets[i];
        }

        PatrolRoute patrol = route.AddComponent<PatrolRoute>();
        Wire(enemy, ("patrolRoute", patrol));
    }

    private static EnemyDataSO GetOrCreateData(string name, System.Action<EnemyDataSO> configure)
    {
        string path = EnemyDataFolder + "/" + name + ".asset";
        EnemyDataSO data = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(path);
        if (data != null) return data;

        data = ScriptableObject.CreateInstance<EnemyDataSO>();
        configure(data);
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static LootEntry Loot(string pickupName, float chance)
    {
        return new LootEntry
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Pickups/" + pickupName + ".prefab"),
            chance = chance
        };
    }

    private static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider = false)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        if (!keepCollider) Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        MeshRenderer meshRenderer = part.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.On;
        return part;
    }

    private static Sprite GetOrCreateGlintSprite()
    {
        string path = UIFolder + "/Glint.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x / (size - 1f)) * 2f - 1f;
                float dy = (y / (size - 1f)) * 2f - 1f;
                float radial = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                float core = Mathf.Pow(radial, 3f);
                float rays = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(dy) * 18f), Mathf.Clamp01(1f - Mathf.Abs(dx) * 18f)) * radial;
                float alpha = Mathf.Clamp01(core * 1.5f + rays);
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

    private static Material GetOrCreateMaterial(string folder, string name, Color color)
    {
        string path = folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
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

    private static void SetFloat(Object target, string property, float value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(Object target, string property, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).boolValue = value;
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
