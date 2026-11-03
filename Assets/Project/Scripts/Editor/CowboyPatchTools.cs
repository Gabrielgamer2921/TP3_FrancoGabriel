using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CowboyPatchTools
{
    private const string Root = "Assets/Project";

    private static EnemyAI pistolero;
    private static EnemyAI fusilero;
    private static EnemyAI sniper;

    [MenuItem("Cowboy FPS/Aplicar parche (checkpoints, enemigos y correcciones)")]
    public static void ApplyPatch()
    {
        GameObject player = GameObject.FindWithTag("Player");
        GameStateManager gameState = Object.FindFirstObjectByType<GameStateManager>();
        MissionDirector director = Object.FindFirstObjectByType<MissionDirector>();
        if (player == null || gameState == null || director == null)
        {
            EditorUtility.DisplayDialog("Abrí FortMercer", "Este parche se aplica sobre la escena FortMercer.", "OK");
            return;
        }

        AssetDatabase.Refresh();
        pistolero = AssetDatabase.LoadAssetAtPath<EnemyAI>(Root + "/Prefabs/Enemies/Enemy_Pistolero.prefab");
        fusilero = AssetDatabase.LoadAssetAtPath<EnemyAI>(Root + "/Prefabs/Enemies/Enemy_Fusilero.prefab");
        sniper = AssetDatabase.LoadAssetAtPath<EnemyAI>(Root + "/Prefabs/Enemies/Enemy_Francotirador.prefab");

        FixSprites(player);
        FixDamageFeedback();
        AddExtraEnemies();
        SetupCheckpoints(player, gameState, director);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Parche aplicado", "Checkpoints, enemigos extra y correcciones listos.\n\nGuardá la escena con Ctrl+S.", "OK");
    }

    private static void FixSprites(GameObject player)
    {
        Sprite glint = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Materials/UI/Glint.png");
        if (glint == null)
        {
            Debug.LogWarning("Cowboy FPS: el sprite Glint todavía no se reimportó. Volvé a ejecutar el parche en unos segundos.");
            return;
        }

        string[] prefabs =
        {
            Root + "/Prefabs/Enemies/Enemy_Pistolero.prefab",
            Root + "/Prefabs/Enemies/Enemy_Fusilero.prefab",
            Root + "/Prefabs/Enemies/Enemy_Francotirador.prefab",
            Root + "/Prefabs/FortMercer/Boss_DirtyHolliday.prefab"
        };

        foreach (string path in prefabs)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            foreach (SpriteRenderer sprite in root.GetComponentsInChildren<SpriteRenderer>(true)) sprite.sprite = glint;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        foreach (SpriteRenderer sprite in player.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sprite.sprite = glint;
            EditorUtility.SetDirty(sprite);
        }
    }

    private static void FixDamageFeedback()
    {
        DamageDirectionUI direction = Object.FindFirstObjectByType<DamageDirectionUI>(FindObjectsInactive.Include);
        if (direction != null) Object.DestroyImmediate(direction.gameObject);

        HealthHUD healthHud = Object.FindFirstObjectByType<HealthHUD>(FindObjectsInactive.Include);
        if (healthHud == null) return;

        SerializedObject serialized = new SerializedObject(healthHud);
        Image vignette = serialized.FindProperty("damageVignette").objectReferenceValue as Image;
        if (vignette != null)
        {
            vignette.sprite = null;
            vignette.color = new Color(0.75f, 0.05f, 0.02f, 0f);
            EditorUtility.SetDirty(vignette);
        }
        serialized.FindProperty("hitFlashAlpha").floatValue = 0.22f;
        serialized.FindProperty("lowHealthAlpha").floatValue = 0f;
        serialized.FindProperty("flashFadeSpeed").floatValue = 1.1f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddExtraEnemies()
    {
        if (GameObject.Find("Refuerzos_Extra") != null) return;

        Transform group = new GameObject("Refuerzos_Extra").transform;
        GameObject gameplay = GameObject.Find("Gameplay");
        if (gameplay != null) group.SetParent(gameplay.transform);

        Transform front = Group("1_PatioEntrada", group);
        Place(pistolero, "Pistolero_F4", front, new Vector3(-20f, 0f, -26f), 0f, new Vector3(8f, 0f, 0f));
        Place(pistolero, "Pistolero_F5", front, new Vector3(28f, 0f, -12f), 180f, new Vector3(0f, 0f, -8f));
        Place(fusilero, "Fusilero_F3", front, new Vector3(-38f, 0f, -12f), 90f);

        Transform west = Group("2_CuartelOeste", group);
        Place(fusilero, "Fusilero_Techo_Armeria", west, new Vector3(-23.5f, 4.3f, 2f), 270f, new Vector3(0f, 0f, 8f));
        Place(pistolero, "Guardia_Cuartel_Sur", west, new Vector3(-40f, 0f, -3f), 90f, new Vector3(0f, 0f, 5f));
        Place(pistolero, "Pistolero_Calle_Norte", west, new Vector3(-31f, 0f, 15f), 180f, new Vector3(0f, 0f, -6f));
        Place(pistolero, "Guardia_Entrada_Oeste", west, new Vector3(-30f, 0f, -4f), 180f);
        Place(sniper, "Francotirador_Cuartel", west, new Vector3(-42f, 4.3f, 16f), 135f);

        Transform north = Group("3_CallejonNorte", group);
        Place(pistolero, "Pistolero_Callejon", north, new Vector3(-20f, 0f, 24.5f), 270f);
        Place(pistolero, "Pistolero_Callejon_2", north, new Vector3(15f, 0f, 27f), 270f);
        Place(fusilero, "Fusilero_Techo_Oficina", north, new Vector3(37f, 4.3f, 17f), 315f);

        Transform east = Group("4_GaleriasEste", group);
        Place(fusilero, "Fusilero_Techo_Almacen", east, new Vector3(41f, 4.8f, -4f), 270f);
        Place(fusilero, "Fusilero_Galeria", east, new Vector3(20.1f, 3.28f, 0f), 0f);
        Place(pistolero, "Guardia_Puerta_A", east, new Vector3(22f, 0f, 3.5f), 0f);
        Place(pistolero, "Guardia_Puerta_B", east, new Vector3(23f, 0f, 9.5f), 0f);
        Place(pistolero, "Pistolero_E3", east, new Vector3(30f, 0f, -6f), 0f, new Vector3(0f, 0f, 6f));
        Place(pistolero, "Pistolero_Oficina", east, new Vector3(35f, 0f, 11f), 0f);
        Place(sniper, "Francotirador_Oficina", east, new Vector3(42f, 4.3f, 13f), 315f);

        PlayerZoneTrigger zone = Zone("Zona_Emboscada_Norte_2", group, new Vector3(25f, 2f, 25f), new Vector3(14f, 4f, 9f));
        EnemySpawnGroup ambush = new GameObject("Emboscada_Norte_2").AddComponent<EnemySpawnGroup>();
        ambush.transform.SetParent(group);
        SerializedObject serialized = new SerializedObject(ambush);
        serialized.FindProperty("activationZone").objectReferenceValue = zone;
        serialized.FindProperty("armedOnStart").boolValue = true;
        SerializedProperty entries = serialized.FindProperty("entries");
        Vector3[] points = { new Vector3(-12f, 0f, 25f), new Vector3(-4f, 0f, 27.5f) };
        entries.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++)
        {
            Transform point = Marker("Spawn_" + i, ambush.transform, points[i]);
            point.rotation = Quaternion.Euler(0f, 90f, 0f);
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("prefab").objectReferenceValue = pistolero;
            entry.FindPropertyRelative("point").objectReferenceValue = point;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetupCheckpoints(GameObject player, GameStateManager gameState, MissionDirector director)
    {
        CheckpointManager manager = Object.FindFirstObjectByType<CheckpointManager>();
        if (manager == null) manager = gameState.gameObject.AddComponent<CheckpointManager>();

        Wire(manager,
            ("objectives", Object.FindFirstObjectByType<ObjectiveHUD>(FindObjectsInactive.Include)),
            ("playerDeath", player.GetComponent<PlayerDeath>()),
            ("ammo", player.GetComponent<PlayerAmmoInventory>()),
            ("weapons", player.GetComponent<PlayerWeaponController>()),
            ("bandages", player.GetComponent<PlayerBandages>()),
            ("thrower", player.GetComponent<PlayerThrowController>()),
            ("boss", Object.FindFirstObjectByType<BossController>(FindObjectsInactive.Include)));
        Wire(gameState, ("checkpoints", manager));

        if (GameObject.Find("Checkpoints") == null)
        {
            Transform root = new GameObject("Checkpoints").transform;
            GameObject gameplay = GameObject.Find("Gameplay");
            if (gameplay != null) root.SetParent(gameplay.transform);

            CreateCheckpoint(root, "Patio de entrada", new Vector3(0f, 2f, -25.5f), new Vector3(8f, 4f, 3f), new Vector3(0f, 0f, -26f), 0f);
            CreateCheckpoint(root, "Cuartel oeste", new Vector3(-33.5f, 2f, -5.5f), new Vector3(6f, 4f, 3f), new Vector3(-33.5f, 0f, -5f), 0f);
            CreateCheckpoint(root, "Callejón norte", new Vector3(-31f, 2f, 21.5f), new Vector3(26f, 4f, 2f), new Vector3(-34f, 0f, 22.5f), 90f);
            CreateCheckpoint(root, "Galerías este", new Vector3(24f, 2f, 21f), new Vector3(12f, 4f, 2f), new Vector3(24f, 0f, 20f), 180f);
            Checkpoint boss = CreateCheckpoint(root, "Patio final", Vector3.zero, Vector3.zero, new Vector3(14f, 0f, 6.5f), 270f);
            Checkpoint escape = CreateCheckpoint(root, "Huida", Vector3.zero, Vector3.zero, new Vector3(14f, 0f, 6.5f), 90f);
            Wire(director, ("bossCheckpoint", boss), ("escapeCheckpoint", escape));
        }

        AddRespawnButton(gameState);
    }

    private static Checkpoint CreateCheckpoint(Transform parent, string name, Vector3 zoneCenter, Vector3 zoneSize, Vector3 spawn, float yaw)
    {
        GameObject checkpointObject = new GameObject("Checkpoint_" + name);
        checkpointObject.transform.SetParent(parent);
        checkpointObject.transform.position = spawn;

        Transform spawnPoint = Marker("Spawn", checkpointObject.transform, spawn);
        spawnPoint.rotation = Quaternion.Euler(0f, yaw, 0f);

        PlayerZoneTrigger zone = null;
        if (zoneSize != Vector3.zero) zone = Zone("Zona", checkpointObject.transform, zoneCenter, zoneSize);

        Checkpoint checkpoint = checkpointObject.AddComponent<Checkpoint>();
        SerializedObject serialized = new SerializedObject(checkpoint);
        serialized.FindProperty("displayName").stringValue = name;
        serialized.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
        serialized.FindProperty("zone").objectReferenceValue = zone;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return checkpoint;
    }

    private static void AddRespawnButton(GameStateManager gameState)
    {
        SerializedObject serialized = new SerializedObject(gameState);
        GameObject defeat = serialized.FindProperty("defeatPanel").objectReferenceValue as GameObject;
        if (defeat == null || defeat.transform.Find("Boton_Checkpoint") != null) return;

        foreach (Transform child in defeat.transform)
            if (child.name.StartsWith("Boton_")) ((RectTransform)child).anchoredPosition += new Vector2(0f, -80f);

        GameObject buttonObject = new GameObject("Boton_Checkpoint", typeof(RectTransform));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(defeat.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -10f);
        rect.sizeDelta = new Vector2(440f, 62f);

        Image background = buttonObject.AddComponent<Image>();
        background.color = new Color(0.55f, 0.32f, 0.12f, 0.95f);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
        colors.pressedColor = new Color(0.8f, 0.6f, 0.4f);
        button.colors = colors;
        UnityEventTools.AddPersistentListener(button.onClick, gameState.RespawnAtCheckpoint);

        GameObject labelObject = new GameObject("Texto", typeof(RectTransform));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "Volver al punto de control";
        label.fontSize = 30f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        Transform retry = defeat.transform.Find("Boton_Reintentar");
        if (retry != null)
        {
            TMP_Text retryText = retry.GetComponentInChildren<TMP_Text>();
            if (retryText != null) retryText.text = "Reiniciar misión";
        }
    }

    private static EnemyAI Place(EnemyAI prefab, string name, Transform parent, Vector3 position, float yaw, params Vector3[] routeOffsets)
    {
        EnemyAI enemy = (EnemyAI)PrefabUtility.InstantiatePrefab(prefab, parent);
        enemy.name = name;
        enemy.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        if (routeOffsets == null || routeOffsets.Length == 0) return enemy;

        GameObject route = new GameObject("Ruta_" + name);
        route.transform.SetParent(parent);
        route.transform.position = position;
        Marker("Punto_0", route.transform, position);
        for (int i = 0; i < routeOffsets.Length; i++) Marker("Punto_" + (i + 1), route.transform, position + routeOffsets[i]);
        PatrolRoute patrol = route.AddComponent<PatrolRoute>();
        Wire(enemy, ("patrolRoute", patrol));
        return enemy;
    }

    private static PlayerZoneTrigger Zone(string name, Transform parent, Vector3 center, Vector3 size)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent);
        zone.transform.position = center;
        BoxCollider box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        return zone.AddComponent<PlayerZoneTrigger>();
    }

    private static Transform Group(string name, Transform parent)
    {
        Transform group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    private static Transform Marker(string name, Transform parent, Vector3 position)
    {
        Transform marker = new GameObject(name).transform;
        marker.SetParent(parent);
        marker.position = position;
        return marker;
    }

    private static void Wire(Object target, params (string property, Object value)[] values)
    {
        if (target == null) return;
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
}
