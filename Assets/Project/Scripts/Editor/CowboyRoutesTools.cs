using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class CowboyRoutesTools
{
    private const string Root = "Assets/Project";

    [MenuItem("Cowboy FPS/Agregar rutas alternativas")]
    public static void AddRoutes()
    {
        MissionDirector director = Object.FindFirstObjectByType<MissionDirector>();
        GameObject level = GameObject.Find("FortMercer");
        GameObject gameplay = GameObject.Find("Gameplay");
        if (director == null || level == null || gameplay == null)
        {
            EditorUtility.DisplayDialog("Abrí FortMercer", "Este menú se aplica sobre la escena FortMercer.", "OK");
            return;
        }
        if (GameObject.Find("Rutas_Alternativas") != null)
        {
            EditorUtility.DisplayDialog("Ya aplicado", "Las rutas alternativas ya existen.", "OK");
            return;
        }

        Material wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/FortMercer/Mat_Madera_Clara.mat");
        Material adobe = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/FortMercer/Mat_Adobe.mat");
        Material adobeDark = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/FortMercer/Mat_Adobe_Oscuro.mat");
        Material rock = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/FortMercer/Mat_Roca.mat");
        Material crack = GetOrCreateMaterial(Root + "/Materials/FortMercer/Mat_Grieta.mat", new Color(0.25f, 0.18f, 0.12f));

        Transform routes = new GameObject("Rutas_Alternativas").transform;
        routes.SetParent(level.transform);

        Transform rooftop = new GameObject("Ruta_Techos_Galeria").transform;
        rooftop.SetParent(routes);
        BuildStairs(rooftop, new Vector3(26.5f, 0f, -5.8f), Vector3.left, 1.4f, 3.28f, wood);

        DestructibleBarrier breach = BuildBreach(routes, level.transform, adobe, adobeDark, crack);
        BuildEastExterior(routes, gameplay.transform, director, breach, rock);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Rutas alternativas listas",
            "Se agregaron la escalera a los techos de las galerías este y el muro agrietado de huida.\n\nÚltimo paso: seleccioná 'NavMesh', apretá 'Bake' y guardá con Ctrl+S.", "OK");
    }

    private static DestructibleBarrier BuildBreach(Transform routes, Transform level, Material adobe, Material adobeDark, Material crack)
    {
        Transform eastWall = level.Find("Murallas/Muralla_Este");
        const float gapStart = 9.2f;
        const float gapEnd = 11.8f;
        float x = 45f;
        float thickness = 1.2f;
        float height = 5f;
        Material wallMaterial = adobe;

        if (eastWall != null)
        {
            Transform segment = null;
            foreach (Transform child in eastWall)
            {
                Bounds bounds = child.GetComponent<Renderer>().bounds;
                if (bounds.min.z <= gapStart && bounds.max.z >= gapEnd)
                {
                    segment = child;
                    break;
                }
            }

            if (segment != null)
            {
                Bounds bounds = segment.GetComponent<Renderer>().bounds;
                x = bounds.center.x;
                thickness = bounds.size.x;
                height = bounds.size.y;
                wallMaterial = segment.GetComponent<Renderer>().sharedMaterial;
                float zMin = bounds.min.z;
                float zMax = bounds.max.z;
                Object.DestroyImmediate(segment.gameObject);

                Box("Tramo", eastWall, new Vector3(x, height * 0.5f, (zMin + gapStart) * 0.5f), new Vector3(thickness, height, gapStart - zMin), wallMaterial, true);
                Box("Tramo", eastWall, new Vector3(x, height * 0.5f, (gapEnd + zMax) * 0.5f), new Vector3(thickness, height, zMax - gapEnd), wallMaterial, true);
            }
        }

        GameObject breachObject = new GameObject("Muro_Agrietado");
        breachObject.transform.SetParent(routes);
        breachObject.transform.position = new Vector3(x, 0f, (gapStart + gapEnd) * 0.5f);

        Transform pieces = new GameObject("Bloques").transform;
        pieces.SetParent(breachObject.transform, false);

        float width = gapEnd - gapStart;
        const int columns = 3;
        const int rows = 4;
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                float z = -width * 0.5f + width / columns * (column + 0.5f);
                float y = height / rows * (row + 0.5f);
                Material material = (row + column) % 2 == 0 ? adobe : adobeDark;
                GameObject block = Box("Bloque_" + row + "_" + column, pieces, Vector3.zero, new Vector3(thickness * 0.95f, height / rows - 0.02f, width / columns - 0.02f), material, true);
                block.transform.localPosition = new Vector3(0f, y, z);
            }
        }

        Vector3[] cracks =
        {
            new Vector3(-thickness * 0.5f - 0.01f, 2.6f, 0.2f), new Vector3(-thickness * 0.5f - 0.01f, 1.4f, -0.5f),
            new Vector3(-thickness * 0.5f - 0.01f, 3.6f, -0.2f), new Vector3(thickness * 0.5f + 0.01f, 2.2f, 0.3f)
        };
        float[] angles = { 35f, -20f, 60f, -40f };
        for (int i = 0; i < cracks.Length; i++)
        {
            GameObject line = Box("Grieta_" + i, pieces, Vector3.zero, new Vector3(0.02f, 1.4f, 0.06f), crack, false);
            line.transform.localPosition = cracks[i];
            line.transform.localRotation = Quaternion.Euler(angles[i], 0f, 0f);
        }

        NavMeshObstacle obstacle = breachObject.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = new Vector3(0f, height * 0.5f, 0f);
        obstacle.size = new Vector3(thickness, height, width);
        obstacle.carving = true;
        breachObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        DestructibleBarrier barrier = breachObject.AddComponent<DestructibleBarrier>();
        SerializedObject serialized = new SerializedObject(barrier);
        serialized.FindProperty("hitPoints").floatValue = 60f;
        serialized.FindProperty("startsBreakable").boolValue = false;
        serialized.FindProperty("piecesRoot").objectReferenceValue = pieces;
        serialized.FindProperty("obstacle").objectReferenceValue = obstacle;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SoundSO breakSound = AssetDatabase.LoadAssetAtPath<SoundSO>(Root + "/Data/Sonidos/Madera_Rota.asset");
        if (breakSound != null)
        {
            InteractableAudio audio = breachObject.AddComponent<InteractableAudio>();
            SerializedObject audioSerialized = new SerializedObject(audio);
            audioSerialized.FindProperty("barrier").objectReferenceValue = barrier;
            audioSerialized.FindProperty("barrierBreak").objectReferenceValue = breakSound;
            audioSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        return barrier;
    }

    private static void BuildEastExterior(Transform routes, Transform gameplay, MissionDirector director, DestructibleBarrier breach, Material rock)
    {
        Transform props = new GameObject("Exterior_Este").transform;
        props.SetParent(routes);
        Rock(props, new Vector3(52f, 0f, 0f), 3f, rock);
        Rock(props, new Vector3(56f, 0f, -14f), 4f, rock);
        Rock(props, new Vector3(50.5f, 0f, -27f), 3f, rock);
        Rock(props, new Vector3(59f, 0f, -38f), 5f, rock);
        Rock(props, new Vector3(53f, 0f, -48f), 3.5f, rock);

        GameObject dynamitePickup = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Pickups/Pickup_Dinamita.prefab");
        if (dynamitePickup != null)
        {
            GameObject pickup = (GameObject)PrefabUtility.InstantiatePrefab(dynamitePickup, gameplay);
            pickup.transform.position = new Vector3(39f, 0f, 10.5f);
        }

        GameObject territoryObject = new GameObject("Zona_6_Exterior este");
        territoryObject.transform.SetParent(gameplay);
        ZoneEncounter territory = territoryObject.AddComponent<ZoneEncounter>();
        SerializedObject territorySerialized = new SerializedObject(territory);
        territorySerialized.FindProperty("zoneName").stringValue = "Exterior este";
        territorySerialized.FindProperty("zoneId").intValue = 6;
        territorySerialized.FindProperty("territoryCenter").vector3Value = new Vector3(57f, 5f, -18f);
        territorySerialized.FindProperty("territorySize").vector3Value = new Vector3(26f, 30f, 66f);
        territorySerialized.ApplyModifiedPropertiesWithoutUndo();

        EnemyAI pistolero = AssetDatabase.LoadAssetAtPath<EnemyAI>(Root + "/Prefabs/Enemies/Enemy_Pistolero.prefab");
        EnemyAI fusilero = AssetDatabase.LoadAssetAtPath<EnemyAI>(Root + "/Prefabs/Enemies/Enemy_Fusilero.prefab");

        GameObject groupObject = new GameObject("Escape_Brecha");
        groupObject.transform.SetParent(gameplay);
        PlayerZoneTrigger zone = Zone("Zona_Escape_Brecha", groupObject.transform, new Vector3(49.5f, 2f, 10.5f), new Vector3(6f, 4f, 8f));
        EnemySpawnGroup group = groupObject.AddComponent<EnemySpawnGroup>();
        SerializedObject groupSerialized = new SerializedObject(group);
        groupSerialized.FindProperty("activationZone").objectReferenceValue = zone;
        groupSerialized.FindProperty("territoryZone").objectReferenceValue = territory;
        SerializedProperty entries = groupSerialized.FindProperty("entries");
        (EnemyAI prefab, Vector3 position)[] spawns =
        {
            (pistolero, new Vector3(54f, 0f, -5f)),
            (pistolero, new Vector3(50f, 0f, -20f)),
            (fusilero, new Vector3(57f, 0f, -30f)),
            (fusilero, new Vector3(52f, 0f, -42f)),
            (pistolero, new Vector3(60f, 0f, 4f))
        };
        entries.arraySize = spawns.Length;
        for (int i = 0; i < spawns.Length; i++)
        {
            Transform point = new GameObject("Spawn_" + i).transform;
            point.SetParent(groupObject.transform);
            point.SetPositionAndRotation(spawns[i].position, Quaternion.Euler(0f, 0f, 0f));
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("prefab").objectReferenceValue = spawns[i].prefab;
            entry.FindPropertyRelative("point").objectReferenceValue = point;
        }
        groupSerialized.ApplyModifiedPropertiesWithoutUndo();

        Transform checkpointsRoot = GameObject.Find("Checkpoints")?.transform;
        if (checkpointsRoot != null)
        {
            GameObject checkpointObject = new GameObject("Checkpoint_Huida: brecha");
            checkpointObject.transform.SetParent(checkpointsRoot);
            checkpointObject.transform.position = new Vector3(49f, 0f, 10.5f);
            Transform spawn = new GameObject("Spawn").transform;
            spawn.SetParent(checkpointObject.transform);
            spawn.SetPositionAndRotation(new Vector3(49f, 0f, 10.5f), Quaternion.Euler(0f, 180f, 0f));
            PlayerZoneTrigger checkpointZone = Zone("Zona", checkpointObject.transform, new Vector3(49.5f, 2f, 10.5f), new Vector3(6f, 4f, 8f));

            Checkpoint checkpoint = checkpointObject.AddComponent<Checkpoint>();
            SerializedObject checkpointSerialized = new SerializedObject(checkpoint);
            checkpointSerialized.FindProperty("displayName").stringValue = "Huida: brecha";
            checkpointSerialized.FindProperty("spawnPoint").objectReferenceValue = spawn;
            checkpointSerialized.FindProperty("zone").objectReferenceValue = checkpointZone;
            checkpointSerialized.FindProperty("activeDuring").enumValueIndex = (int)Checkpoint.Phase.EscapeOnly;
            SerializedProperty reset = checkpointSerialized.FindProperty("resetOnRespawn");
            reset.arraySize = 1;
            reset.GetArrayElementAtIndex(0).objectReferenceValue = group;
            checkpointSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        SerializedObject directorSerialized = new SerializedObject(director);
        directorSerialized.FindProperty("escapeBreach").objectReferenceValue = breach;
        SerializedProperty escapeGroups = directorSerialized.FindProperty("escapeGroups");
        escapeGroups.arraySize++;
        escapeGroups.GetArrayElementAtIndex(escapeGroups.arraySize - 1).objectReferenceValue = group;
        directorSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildStairs(Transform parent, Vector3 bottom, Vector3 direction, float width, float rise, Material material)
    {
        int steps = Mathf.CeilToInt(rise / 0.25f);
        float stepHeight = rise / steps;
        const float run = 0.35f;
        Quaternion rotation = Quaternion.LookRotation(direction);
        for (int i = 0; i < steps; i++)
        {
            float h = stepHeight * (i + 1);
            GameObject step = Box("Escalon_" + i, parent, Vector3.zero, new Vector3(width, h, run), material, true);
            step.transform.position = bottom + direction * (run * i + run * 0.5f) + Vector3.up * (h * 0.5f);
            step.transform.rotation = rotation;
        }
    }

    private static void Rock(Transform parent, Vector3 position, float size, Material material)
    {
        GameObject rockObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rockObject.name = "Roca";
        rockObject.transform.SetParent(parent);
        rockObject.transform.position = position + Vector3.up * size * 0.25f;
        rockObject.transform.localScale = new Vector3(size, size * 0.7f, size * 0.85f);
        rockObject.transform.rotation = Quaternion.Euler(0f, size * 37f, 0f);
        if (material != null) rockObject.GetComponent<MeshRenderer>().sharedMaterial = material;
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

    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collider)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        if (!collider) Object.DestroyImmediate(box.GetComponent<Collider>());
        box.transform.SetParent(parent, false);
        box.transform.position = position;
        box.transform.localScale = size;
        if (material != null) box.GetComponent<MeshRenderer>().sharedMaterial = material;
        return box;
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
