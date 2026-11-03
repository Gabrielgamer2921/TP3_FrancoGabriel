using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CowboyZonesTools
{
    private const string Root = "Assets/Project";

    private static EnemyAI pistolero;
    private static EnemyAI fusilero;
    private static EnemyAI sniper;

    private class ZoneDef
    {
        public int id;
        public string name;
        public Vector3 center;
        public Vector3 size;
        public bool activeOnStart;
        public ZoneEncounter encounter;
        public Transform spawns;
    }

    [MenuItem("Cowboy FPS/Reestructurar Fort Mercer por zonas")]
    public static void Restructure()
    {
        GameObject player = GameObject.FindWithTag("Player");
        MissionDirector director = Object.FindFirstObjectByType<MissionDirector>();
        CheckpointManager manager = Object.FindFirstObjectByType<CheckpointManager>();
        GameObject gameplay = GameObject.Find("Gameplay");
        if (player == null || director == null || manager == null || gameplay == null)
        {
            EditorUtility.DisplayDialog("Falta algo", "Abrí FortMercer y aplicá antes el parche de checkpoints.", "OK");
            return;
        }
        if (GameObject.Find("Zonas_Enemigos") != null)
        {
            EditorUtility.DisplayDialog("Ya aplicado", "Fort Mercer ya está organizado por zonas.", "OK");
            return;
        }

        pistolero = AssetDatabase.LoadAssetAtPath<EnemyAI>(Root + "/Prefabs/Enemies/Enemy_Pistolero.prefab");
        fusilero = AssetDatabase.LoadAssetAtPath<EnemyAI>(Root + "/Prefabs/Enemies/Enemy_Fusilero.prefab");
        sniper = AssetDatabase.LoadAssetAtPath<EnemyAI>(Root + "/Prefabs/Enemies/Enemy_Francotirador.prefab");

        Transform zonesRoot = new GameObject("Zonas_Enemigos").transform;
        zonesRoot.SetParent(gameplay.transform);

        ZoneDef exterior = CreateZone(zonesRoot, 0, "Exterior", new Vector3(0f, 5f, -52f), new Vector3(120f, 30f, 44f), true);
        ZoneDef front = CreateZone(zonesRoot, 1, "Patio de entrada", new Vector3(0f, 5f, -19f), new Vector3(90f, 30f, 22f), true);
        ZoneDef west = CreateZone(zonesRoot, 2, "Cuartel oeste", new Vector3(-31.5f, 5f, 6f), new Vector3(27f, 30f, 28f), false);
        ZoneDef north = CreateZone(zonesRoot, 3, "Callejón norte", new Vector3(0f, 5f, 25f), new Vector3(90f, 30f, 10f), false);
        ZoneDef east = CreateZone(zonesRoot, 4, "Galerías este", new Vector3(31.5f, 5f, 6f), new Vector3(27f, 30f, 28f), false);
        ZoneDef road = CreateZone(zonesRoot, 5, "Camino (huida)", new Vector3(0f, 5f, -55f), new Vector3(80f, 30f, 46f), false);

        EnemyAI capatazPrefab = CreateCapatazPrefab();
        ConvertSceneEnemies(capatazPrefab, exterior, front, west, north, east);

        AddEntry(exterior, "enemies", fusilero, "Fusilero_Roca", new Vector3(-18f, 0f, -42f), 180f);
        AddEntry(exterior, "enemies", pistolero, "Pistolero_Camino", new Vector3(14f, 0f, -48f), 180f);
        AddEntry(north, "enemies", pistolero, "Pistolero_Callejon_Oeste", new Vector3(-38f, 0f, 27.5f), 90f);
        AddEntry(north, "enemies", fusilero, "Fusilero_Callejon", new Vector3(5f, 0f, 22.2f), 270f);
        AddEntry(north, "enemies", pistolero, "Pistolero_Callejon_Este", new Vector3(30f, 0f, 28.5f), 270f);

        AddEntry(front, "reinforcements", pistolero, "Refuerzo_F1", new Vector3(-33.5f, 0f, -6.5f), 180f);
        AddEntry(front, "reinforcements", pistolero, "Refuerzo_F2", new Vector3(40f, 0f, -12f), 270f);
        AddEntry(front, "reinforcements", fusilero, "Refuerzo_F3", new Vector3(-40f, 0f, -20f), 90f);
        AddEntry(west, "reinforcements", pistolero, "Refuerzo_O1", new Vector3(-40f, 0f, 18.5f), 180f);
        AddEntry(west, "reinforcements", pistolero, "Refuerzo_O2", new Vector3(-24f, 0f, 16f), 180f);
        AddEntry(west, "reinforcements", fusilero, "Refuerzo_O3", new Vector3(-31f, 0f, 18.5f), 180f);
        AddEntry(north, "reinforcements", pistolero, "Refuerzo_N1", new Vector3(42f, 0f, 25f), 270f);
        AddEntry(north, "reinforcements", pistolero, "Refuerzo_N2", new Vector3(-42f, 0f, 25f), 90f);
        AddEntry(east, "reinforcements", pistolero, "Refuerzo_E1", new Vector3(40f, 0f, 17.5f), 270f);
        AddEntry(east, "reinforcements", fusilero, "Refuerzo_E2", new Vector3(42f, 0f, -4f), 270f);
        AddEntry(east, "reinforcements", pistolero, "Refuerzo_E3", new Vector3(24f, 0f, -6.5f), 0f);

        AddEntry(road, "enemies", pistolero, "Camino_Pistolero_1", new Vector3(-6f, 0f, -40f), 0f);
        AddEntry(road, "enemies", pistolero, "Camino_Pistolero_2", new Vector3(6f, 0f, -42f), 0f);
        AddEntry(road, "enemies", pistolero, "Camino_Pistolero_3", new Vector3(-12f, 0f, -52f), 0f);
        AddEntry(road, "enemies", pistolero, "Camino_Pistolero_4", new Vector3(10f, 0f, -55f), 0f);
        AddEntry(road, "enemies", pistolero, "Camino_Pistolero_5", new Vector3(-5f, 0f, -48.5f), 0f);
        AddEntry(road, "enemies", fusilero, "Camino_Fusilero_1", new Vector3(-17f, 0f, -47f), 30f);
        AddEntry(road, "enemies", fusilero, "Camino_Fusilero_2", new Vector3(19f, 0f, -60f), -30f);
        AddEntry(road, "enemies", fusilero, "Camino_Fusilero_3", new Vector3(3f, 0f, -62f), 0f);
        AddEntry(road, "reinforcements", pistolero, "Camino_Refuerzo_1", new Vector3(-30f, 0f, -66f), 45f);
        AddEntry(road, "reinforcements", pistolero, "Camino_Refuerzo_2", new Vector3(30f, 0f, -66f), -45f);

        LinkResettables(front, "Campana_Alarma", "Refuerzos_Campana");
        LinkResettables(north, "Emboscada_Norte", "Emboscada_Norte_2");
        SetTerritoryOn("Refuerzos_Campana", front);
        SetTerritoryOn("Emboscada_Norte", north);
        SetTerritoryOn("Emboscada_Norte_2", north);

        EnemySpawnGroup escapeEast = ReinforceEscape("Escape_Este", east,
            (pistolero, new Vector3(36f, 0f, 10f), 270f), (fusilero, new Vector3(38f, 4.8f, 0f), 270f), (pistolero, new Vector3(28f, 0f, 2f), 0f));
        EnemySpawnGroup escapeNorth = ReinforceEscape("Escape_Norte", north,
            (pistolero, new Vector3(-20f, 0f, 27f), 90f), (fusilero, new Vector3(-10f, 0f, 22.3f), 90f), (pistolero, new Vector3(-36f, 0f, 24f), 90f));
        EnemySpawnGroup escapeWest = ReinforceEscape("Escape_Oeste", west,
            (pistolero, new Vector3(-40f, 0f, 14f), 180f), (fusilero, new Vector3(-23.5f, 4.3f, 6f), 270f), (pistolero, new Vector3(-31f, 0f, -2f), 0f));
        EnemySpawnGroup escapeFront = ReinforceEscape("Escape_Frente", front,
            (pistolero, new Vector3(-12f, 0f, -20f), 0f), (pistolero, new Vector3(12f, 0f, -20f), 0f), (fusilero, new Vector3(-40.5f, 5f, -25.5f), 45f));

        PlayerZoneTrigger exitZone = GameObject.Find("Zona_Salida")?.GetComponent<PlayerZoneTrigger>();
        if (exitZone != null)
        {
            exitZone.transform.position = new Vector3(0f, 2f, -74f);
            BoxCollider box = exitZone.GetComponent<BoxCollider>();
            box.size = new Vector3(40f, 4f, 6f);
        }

        ConfigureCheckpoints(director, manager, exterior, front, west, north, east, road, escapeEast, escapeNorth, escapeWest, escapeFront);
        SetupShotgun(player);
        UpdateLoot();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Fort Mercer por zonas", "Zonas, checkpoints por fase, escopeta de Holliday y huida reforzada listos.\n\nGuardá la escena con Ctrl+S.", "OK");
    }

    private static ZoneDef CreateZone(Transform parent, int id, string name, Vector3 center, Vector3 size, bool activeOnStart)
    {
        GameObject zoneObject = new GameObject("Zona_" + id + "_" + name);
        zoneObject.transform.SetParent(parent);
        Transform spawns = new GameObject("Spawns").transform;
        spawns.SetParent(zoneObject.transform);

        ZoneEncounter encounter = zoneObject.AddComponent<ZoneEncounter>();
        SerializedObject serialized = new SerializedObject(encounter);
        serialized.FindProperty("zoneName").stringValue = name;
        serialized.FindProperty("zoneId").intValue = id;
        serialized.FindProperty("territoryCenter").vector3Value = center;
        serialized.FindProperty("territorySize").vector3Value = size;
        serialized.FindProperty("activeOnStart").boolValue = activeOnStart;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return new ZoneDef { id = id, name = name, center = center, size = size, activeOnStart = activeOnStart, encounter = encounter, spawns = spawns };
    }

    private static EnemyAI CreateCapatazPrefab()
    {
        string path = Root + "/Prefabs/FortMercer/Enemy_Capataz.prefab";
        EnemyAI existing = AssetDatabase.LoadAssetAtPath<EnemyAI>(path);
        if (existing != null) return existing;

        EnemyAI capataz = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(e => e.name == "El_Capataz");
        if (capataz == null) return pistolero;

        GameObject copy = Object.Instantiate(capataz.gameObject);
        copy.name = "Enemy_Capataz";
        KeyCarrier carrier = copy.GetComponent<KeyCarrier>();
        if (carrier != null) Object.DestroyImmediate(carrier);
        SerializedObject serialized = new SerializedObject(copy.GetComponent<EnemyAI>());
        serialized.FindProperty("patrolRoute").objectReferenceValue = null;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(copy, path);
        Object.DestroyImmediate(copy);
        return prefab.GetComponent<EnemyAI>();
    }

    private static void ConvertSceneEnemies(EnemyAI capatazPrefab, ZoneDef exterior, ZoneDef front, ZoneDef west, ZoneDef north, ZoneDef east)
    {
        EnemyAI[] sceneEnemies = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (EnemyAI enemy in sceneEnemies)
        {
            if (enemy.GetComponent<BossController>() != null) continue;
            if (!PrefabUtility.IsPartOfPrefabInstance(enemy)) continue;

            Vector3 p = enemy.transform.position;
            ZoneDef zone = p.z < -30f ? exterior : p.z < -8f ? front : p.z >= 20f ? north : p.x < -18f ? west : p.x > 18f ? east : front;

            EnemyAI prefab = enemy.name == "El_Capataz" ? capatazPrefab : PrefabUtility.GetCorrespondingObjectFromSource(enemy);
            if (prefab == null) continue;

            GameObject keyDrop = null;
            KeyCarrier carrier = enemy.GetComponent<KeyCarrier>();
            if (carrier != null) keyDrop = new SerializedObject(carrier).FindProperty("keyPrefab").objectReferenceValue as GameObject;

            Transform spawn = new GameObject(enemy.name).transform;
            spawn.SetParent(zone.spawns);
            spawn.SetPositionAndRotation(p, enemy.transform.rotation);

            AppendEntry(zone.encounter, "enemies", prefab, spawn, enemy.PatrolRoute, keyDrop);
            Object.DestroyImmediate(enemy.gameObject);
        }
    }

    private static void AddEntry(ZoneDef zone, string list, EnemyAI prefab, string name, Vector3 position, float yaw)
    {
        Transform spawn = new GameObject(name).transform;
        spawn.SetParent(zone.spawns);
        spawn.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        AppendEntry(zone.encounter, list, prefab, spawn, null, null);
    }

    private static void AppendEntry(ZoneEncounter encounter, string list, EnemyAI prefab, Transform spawn, PatrolRoute route, GameObject keyDrop)
    {
        SerializedObject serialized = new SerializedObject(encounter);
        SerializedProperty array = serialized.FindProperty(list);
        int index = array.arraySize;
        array.arraySize++;
        SerializedProperty entry = array.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        entry.FindPropertyRelative("spawn").objectReferenceValue = spawn;
        entry.FindPropertyRelative("route").objectReferenceValue = route;
        entry.FindPropertyRelative("keyDrop").objectReferenceValue = keyDrop;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void LinkResettables(ZoneDef zone, params string[] objectNames)
    {
        SerializedObject serialized = new SerializedObject(zone.encounter);
        SerializedProperty array = serialized.FindProperty("linkedResettables");
        foreach (string objectName in objectNames)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null) continue;
            foreach (MonoBehaviour behaviour in target.GetComponents<MonoBehaviour>())
            {
                if (!(behaviour is IResettable)) continue;
                array.arraySize++;
                array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = behaviour;
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetTerritoryOn(string objectName, ZoneDef zone)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null) return;
        foreach (MonoBehaviour behaviour in target.GetComponents<MonoBehaviour>())
        {
            SerializedObject serialized = new SerializedObject(behaviour);
            SerializedProperty prop = serialized.FindProperty("territoryZone");
            if (prop == null) continue;
            prop.objectReferenceValue = zone.encounter;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static EnemySpawnGroup ReinforceEscape(string groupName, ZoneDef zone, params (EnemyAI prefab, Vector3 position, float yaw)[] extra)
    {
        GameObject groupObject = GameObject.Find(groupName);
        if (groupObject == null) return null;
        EnemySpawnGroup group = groupObject.GetComponent<EnemySpawnGroup>();

        SerializedObject serialized = new SerializedObject(group);
        serialized.FindProperty("territoryZone").objectReferenceValue = zone.encounter;
        SerializedProperty entries = serialized.FindProperty("entries");
        foreach ((EnemyAI prefab, Vector3 position, float yaw) in extra)
        {
            Transform point = new GameObject("Spawn_Extra_" + entries.arraySize).transform;
            point.SetParent(groupObject.transform);
            point.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            entries.arraySize++;
            SerializedProperty entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("point").objectReferenceValue = point;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return group;
    }

    private static void ConfigureCheckpoints(MissionDirector director, CheckpointManager manager, ZoneDef exterior, ZoneDef front, ZoneDef west, ZoneDef north, ZoneDef east, ZoneDef road,
        EnemySpawnGroup escapeEast, EnemySpawnGroup escapeNorth, EnemySpawnGroup escapeWest, EnemySpawnGroup escapeFront)
    {
        Transform root = GameObject.Find("Checkpoints")?.transform;
        if (root == null) return;

        SetupCheckpoint(Find<Checkpoint>(root, "Checkpoint_Patio de entrada"), Checkpoint.Phase.IngressOnly, new MonoBehaviour[] { front.encounter }, new[] { west.encounter });
        SetupCheckpoint(Find<Checkpoint>(root, "Checkpoint_Cuartel oeste"), Checkpoint.Phase.IngressOnly, new MonoBehaviour[] { west.encounter }, new[] { north.encounter });
        SetupCheckpoint(Find<Checkpoint>(root, "Checkpoint_Callejón norte"), Checkpoint.Phase.IngressOnly, new MonoBehaviour[] { north.encounter }, new[] { east.encounter });
        SetupCheckpoint(Find<Checkpoint>(root, "Checkpoint_Galerías este"), Checkpoint.Phase.IngressOnly, new MonoBehaviour[] { east.encounter }, new ZoneEncounter[0]);

        Checkpoint bossEntrance = Find<Checkpoint>(root, "Checkpoint_Patio final");
        if (bossEntrance != null)
        {
            Transform spawn = bossEntrance.transform.Find("Spawn");
            spawn.SetPositionAndRotation(new Vector3(21.8f, 0f, 6.5f), Quaternion.Euler(0f, 270f, 0f));
        }
        Checkpoint escapeStart = Find<Checkpoint>(root, "Checkpoint_Huida");
        SetupCheckpoint(escapeStart, Checkpoint.Phase.Always, new MonoBehaviour[] { escapeEast }, new ZoneEncounter[0]);

        Checkpoint diablo = NewCheckpoint(root, "Modo Diablo", Vector3.zero, Vector3.zero, new Vector3(14f, 0f, 6.5f), 270f, Checkpoint.Phase.Always, new MonoBehaviour[0]);
        NewCheckpoint(root, "Huida: callejón", new Vector3(24f, 2f, 21f), new Vector3(12f, 4f, 2f), new Vector3(26f, 0f, 22f), 270f, Checkpoint.Phase.EscapeOnly, new MonoBehaviour[] { escapeNorth });
        NewCheckpoint(root, "Huida: cuartel", new Vector3(-31f, 2f, 21.5f), new Vector3(26f, 4f, 2f), new Vector3(-34f, 0f, 19f), 180f, Checkpoint.Phase.EscapeOnly, new MonoBehaviour[] { escapeWest });
        NewCheckpoint(root, "Huida: patio de entrada", new Vector3(-33.5f, 2f, -5.5f), new Vector3(6f, 4f, 3f), new Vector3(-33.5f, 0f, -10.5f), 180f, Checkpoint.Phase.EscapeOnly, new MonoBehaviour[] { escapeFront });
        NewCheckpoint(root, "Huida: camino", new Vector3(0f, 2f, -34f), new Vector3(10f, 4f, 3f), new Vector3(0f, 0f, -35f), 180f, Checkpoint.Phase.EscapeOnly, new MonoBehaviour[] { road.encounter });

        SerializedObject directorSerialized = new SerializedObject(director);
        directorSerialized.FindProperty("diabloCheckpoint").objectReferenceValue = diablo;
        directorSerialized.FindProperty("roadAmbush").objectReferenceValue = road.encounter;
        directorSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject managerSerialized = new SerializedObject(manager);
        managerSerialized.FindProperty("director").objectReferenceValue = director;
        SerializedProperty start = managerSerialized.FindProperty("resetOnStartRespawn");
        start.arraySize = 2;
        start.GetArrayElementAtIndex(0).objectReferenceValue = exterior.encounter;
        start.GetArrayElementAtIndex(1).objectReferenceValue = front.encounter;
        managerSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetupCheckpoint(Checkpoint checkpoint, Checkpoint.Phase phase, MonoBehaviour[] reset, ZoneEncounter[] activate)
    {
        if (checkpoint == null) return;
        SerializedObject serialized = new SerializedObject(checkpoint);
        serialized.FindProperty("activeDuring").enumValueIndex = (int)phase;
        SerializedProperty resetList = serialized.FindProperty("resetOnRespawn");
        resetList.arraySize = reset.Length;
        for (int i = 0; i < reset.Length; i++) resetList.GetArrayElementAtIndex(i).objectReferenceValue = reset[i];
        SerializedProperty activateList = serialized.FindProperty("activateOnReach");
        activateList.arraySize = activate.Length;
        for (int i = 0; i < activate.Length; i++) activateList.GetArrayElementAtIndex(i).objectReferenceValue = activate[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Checkpoint NewCheckpoint(Transform parent, string name, Vector3 zoneCenter, Vector3 zoneSize, Vector3 spawn, float yaw, Checkpoint.Phase phase, MonoBehaviour[] reset)
    {
        GameObject checkpointObject = new GameObject("Checkpoint_" + name);
        checkpointObject.transform.SetParent(parent);
        checkpointObject.transform.position = spawn;

        Transform spawnPoint = new GameObject("Spawn").transform;
        spawnPoint.SetParent(checkpointObject.transform);
        spawnPoint.SetPositionAndRotation(spawn, Quaternion.Euler(0f, yaw, 0f));

        PlayerZoneTrigger zone = null;
        if (zoneSize != Vector3.zero)
        {
            GameObject zoneObject = new GameObject("Zona");
            zoneObject.transform.SetParent(checkpointObject.transform);
            zoneObject.transform.position = zoneCenter;
            BoxCollider box = zoneObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = zoneSize;
            zone = zoneObject.AddComponent<PlayerZoneTrigger>();
        }

        Checkpoint checkpoint = checkpointObject.AddComponent<Checkpoint>();
        SerializedObject serialized = new SerializedObject(checkpoint);
        serialized.FindProperty("displayName").stringValue = name;
        serialized.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
        serialized.FindProperty("zone").objectReferenceValue = zone;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SetupCheckpoint(checkpoint, phase, reset, new ZoneEncounter[0]);
        return checkpoint;
    }

    private static void SetupShotgun(GameObject player)
    {
        AmmoTypeSO shells = GetOrCreate<AmmoTypeSO>(Root + "/Data/Ammo_Cartuchos12.asset", a =>
        {
            a.displayName = "Cartuchos";
            a.startingReserve = 12;
            a.maxReserve = 24;
            a.pickupAmount = 6;
        });

        VisualBullet visualBullet = AssetDatabase.LoadAssetAtPath<VisualBullet>(Root + "/Prefabs/VisualBullet.prefab");
        WeaponDataSO data = GetOrCreate<WeaponDataSO>(Root + "/Data/Escopeta_Holliday.asset", d =>
        {
            d.displayName = "Escopeta de Holliday";
            d.ammoType = shells;
            d.damage = 14f;
            d.range = 30f;
            d.damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.15f);
            d.impactForce = 6f;
            d.timeBetweenShots = 0.35f;
            d.magazineSize = 2;
            d.startingMagazine = 2;
            d.reloadStartTime = 0.3f;
            d.reloadPerRoundTime = 0.5f;
            d.reloadEndTime = 0.3f;
            d.hipSpread = 2.5f;
            d.aimSpread = 1f;
            d.moveSpreadBonus = 1.5f;
            d.airSpreadBonus = 3f;
            d.recoilVertical = 7f;
            d.recoilHorizontal = 1.5f;
            d.visualKickBack = 0.14f;
            d.visualKickRotation = 18f;
            d.aimFov = 60f;
            d.aimSpeed = 6f;
            d.aimMoveMultiplier = 0.6f;
            d.equipTime = 0.5f;
            d.sprintToFireTime = 0.25f;
            d.visualBulletPrefab = visualBullet;
            d.visualBulletSpeed = 140f;
            d.pelletsPerShot = 8;
            d.pelletSpread = 4.5f;
            d.noiseRadius = 60f;
        });

        GameObject pickup = CreateWeaponPickup(data);
        CreateShellPickup(shells);

        BossController boss = Object.FindFirstObjectByType<BossController>(FindObjectsInactive.Include);
        if (boss != null)
        {
            SerializedObject serialized = new SerializedObject(boss);
            serialized.FindProperty("rewardPickup").objectReferenceValue = pickup;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        PlayerWeaponController controller = player.GetComponent<PlayerWeaponController>();
        Camera camera = player.GetComponentInChildren<Camera>(true);
        Transform holder = camera.transform.Find("WeaponHolder");
        if (holder == null || holder.Find("Escopeta_Holliday") != null) return;

        Material metal = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Armas/Mat_Arma_Metal.mat");
        Material wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Armas/Mat_Arma_Madera.mat");

        GameObject gun = new GameObject("Escopeta_Holliday");
        gun.transform.SetParent(holder, false);
        gun.transform.localPosition = new Vector3(0.17f, -0.2f, 0.32f);
        Transform model = new GameObject("Modelo").transform;
        model.SetParent(gun.transform, false);
        ViewPart("Cano_Izquierdo", model, new Vector3(-0.012f, 0.03f, 0.33f), new Vector3(0.022f, 0.022f, 0.56f), metal, 0f);
        ViewPart("Cano_Derecho", model, new Vector3(0.012f, 0.03f, 0.33f), new Vector3(0.022f, 0.022f, 0.56f), metal, 0f);
        ViewPart("Banda", model, new Vector3(0f, 0.043f, 0.33f), new Vector3(0.012f, 0.004f, 0.56f), metal, 0f);
        ViewPart("Cajon", model, new Vector3(0f, 0.01f, 0f), new Vector3(0.05f, 0.06f, 0.14f), metal, 0f);
        ViewPart("Guardamano", model, new Vector3(0f, 0.004f, 0.19f), new Vector3(0.05f, 0.03f, 0.18f), wood, 0f);
        ViewPart("Culata", model, new Vector3(0f, -0.035f, -0.22f), new Vector3(0.045f, 0.08f, 0.32f), wood, -8f);
        ViewPart("Gatillos", model, new Vector3(0f, -0.035f, -0.01f), new Vector3(0.01f, 0.03f, 0.05f), metal, 0f);
        ViewPart("Punto_Mira", model, new Vector3(0f, 0.049f, 0.6f), new Vector3(0.006f, 0.008f, 0.006f), metal, 0f);

        Transform muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(gun.transform, false);
        muzzle.localPosition = new Vector3(0f, 0.03f, 0.62f);
        Transform aimPoint = new GameObject("AimPoint").transform;
        aimPoint.SetParent(gun.transform, false);
        aimPoint.localPosition = new Vector3(0f, 0.053f, 0.1f);

        HitscanWeapon weapon = gun.AddComponent<HitscanWeapon>();
        SerializedObject weaponSerialized = new SerializedObject(weapon);
        weaponSerialized.FindProperty("data").objectReferenceValue = data;
        weaponSerialized.FindProperty("muzzle").objectReferenceValue = muzzle;
        weaponSerialized.FindProperty("aimPoint").objectReferenceValue = aimPoint;
        weaponSerialized.FindProperty("aimDistanceFromCamera").floatValue = 0.2f;
        weaponSerialized.ApplyModifiedPropertiesWithoutUndo();

        SoundSO fire = GetOrCreate<SoundSO>(Root + "/Data/Sonidos/Jugador_Escopeta_Disparo.asset", s =>
        {
            s.clips = new[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Generados/Armas/Escopeta_Disparo_1.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Generados/Armas/Escopeta_Disparo_2.wav")
            };
            s.volume = 1f;
            s.spatialBlend = 0f;
        });

        WeaponAudio audio = gun.AddComponent<WeaponAudio>();
        SerializedObject audioSerialized = new SerializedObject(audio);
        audioSerialized.FindProperty("weapon").objectReferenceValue = weapon;
        audioSerialized.FindProperty("fire").objectReferenceValue = fire;
        audioSerialized.FindProperty("dryFire").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SoundSO>(Root + "/Data/Sonidos/Arma_Vacia.asset");
        audioSerialized.FindProperty("reloadStart").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SoundSO>(Root + "/Data/Sonidos/Recarga_Abrir.asset");
        audioSerialized.FindProperty("roundInserted").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SoundSO>(Root + "/Data/Sonidos/Recarga_Bala.asset");
        audioSerialized.FindProperty("reloadEnd").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SoundSO>(Root + "/Data/Sonidos/Recarga_Cerrar.asset");
        audioSerialized.ApplyModifiedPropertiesWithoutUndo();

        Sprite glint = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Materials/UI/Glint.png");
        GameObject flash = new GameObject("Fogonazo");
        flash.transform.SetParent(muzzle, false);
        flash.transform.localScale = Vector3.one * 0.16f;
        SpriteRenderer sprite = flash.AddComponent<SpriteRenderer>();
        sprite.sprite = glint;
        sprite.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        sprite.color = new Color(1f, 0.75f, 0.35f);
        sprite.sortingOrder = 50;
        Light light = flash.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.7f, 0.35f);
        light.range = 7f;
        light.shadows = LightShadows.None;
        MuzzleFlash muzzleFlash = flash.AddComponent<MuzzleFlash>();
        SerializedObject flashSerialized = new SerializedObject(muzzleFlash);
        flashSerialized.FindProperty("playerWeapon").objectReferenceValue = weapon;
        flashSerialized.FindProperty("flashSprite").objectReferenceValue = sprite;
        flashSerialized.FindProperty("flashLight").objectReferenceValue = light;
        flashSerialized.ApplyModifiedPropertiesWithoutUndo();

        gun.SetActive(false);

        SerializedObject controllerSerialized = new SerializedObject(controller);
        SerializedProperty locked = controllerSerialized.FindProperty("lockedWeapons");
        locked.arraySize++;
        locked.GetArrayElementAtIndex(locked.arraySize - 1).objectReferenceValue = weapon;
        controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateWeaponPickup(WeaponDataSO data)
    {
        string path = Root + "/Prefabs/FortMercer/Pickup_Escopeta_Holliday.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        Material metal = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Armas/Mat_Arma_Metal.mat");
        Material wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Armas/Mat_Arma_Madera.mat");

        GameObject root = new GameObject("Pickup_Escopeta_Holliday");
        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.6f, 0f);
        trigger.size = new Vector3(1.6f, 1.2f, 1.6f);

        Transform visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);
        visual.localPosition = new Vector3(0f, 0.6f, 0f);
        visual.localScale = Vector3.one * 1.6f;
        ViewPart("Cano_Izquierdo", visual, new Vector3(-0.012f, 0.03f, 0.33f), new Vector3(0.022f, 0.022f, 0.56f), metal, 0f);
        ViewPart("Cano_Derecho", visual, new Vector3(0.012f, 0.03f, 0.33f), new Vector3(0.022f, 0.022f, 0.56f), metal, 0f);
        ViewPart("Cajon", visual, new Vector3(0f, 0.01f, 0f), new Vector3(0.05f, 0.06f, 0.14f), metal, 0f);
        ViewPart("Guardamano", visual, new Vector3(0f, 0.004f, 0.19f), new Vector3(0.05f, 0.03f, 0.18f), wood, 0f);
        ViewPart("Culata", visual, new Vector3(0f, -0.035f, -0.22f), new Vector3(0.045f, 0.08f, 0.32f), wood, -8f);

        GameObject lightObject = new GameObject("Brillo");
        lightObject.transform.SetParent(visual, false);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.4f);
        light.range = 3f;
        light.intensity = 2.5f;

        WeaponPickup pickup = root.AddComponent<WeaponPickup>();
        SerializedObject serialized = new SerializedObject(pickup);
        serialized.FindProperty("weapon").objectReferenceValue = data;
        serialized.FindProperty("visual").objectReferenceValue = visual;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateShellPickup(AmmoTypeSO shells)
    {
        string path = Root + "/Prefabs/Pickups/Pickup_Cartuchos.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        Material red = GetOrCreate<Material>(Root + "/Materials/Mat_Cartucho.mat", null, () =>
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.7f, 0.12f, 0.1f));
            return material;
        });
        Material brass = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Mat_Pickup_Municion.mat");

        GameObject root = new GameObject("Pickup_Cartuchos");
        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.5f, 0f);

        Transform visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);
        visual.localPosition = new Vector3(0f, 0.35f, 0f);
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * 0.06f;
            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shell.name = "Cartucho_" + i;
            Object.DestroyImmediate(shell.GetComponent<Collider>());
            shell.transform.SetParent(visual, false);
            shell.transform.localPosition = new Vector3(x, 0f, 0f);
            shell.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            shell.GetComponent<MeshRenderer>().sharedMaterial = red;
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(cap.GetComponent<Collider>());
            cap.transform.SetParent(visual, false);
            cap.transform.localPosition = new Vector3(x, -0.05f, 0f);
            cap.transform.localScale = new Vector3(0.052f, 0.012f, 0.052f);
            if (brass != null) cap.GetComponent<MeshRenderer>().sharedMaterial = brass;
        }

        AmmoPickup pickup = root.AddComponent<AmmoPickup>();
        SerializedObject serialized = new SerializedObject(pickup);
        serialized.FindProperty("ammoType").objectReferenceValue = shells;
        serialized.FindProperty("visual").objectReferenceValue = visual;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void UpdateLoot()
    {
        GameObject bandage = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Pickups/Pickup_Vendaje.prefab");
        GameObject shells = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Pickups/Pickup_Cartuchos.prefab");

        SetLoot("Enemy_Pistolero", bandage, 0.3f, shells, 0.25f);
        SetLoot("Enemy_Fusilero", bandage, 0.35f, shells, 0.25f);
        SetLoot("Enemy_Francotirador", bandage, 0.3f, shells, 0.15f);
    }

    private static void SetLoot(string dataName, GameObject bandage, float bandageChance, GameObject shells, float shellChance)
    {
        EnemyDataSO data = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(Root + "/Data/Enemies/" + dataName + ".asset");
        if (data == null) return;

        SetChance(data, bandage, bandageChance);
        SetChance(data, shells, shellChance);
        EditorUtility.SetDirty(data);
    }

    private static void SetChance(EnemyDataSO data, GameObject prefab, float chance)
    {
        if (prefab == null) return;
        LootEntry entry = data.loot.FirstOrDefault(l => l.prefab == prefab);
        if (entry == null)
        {
            entry = new LootEntry { prefab = prefab };
            data.loot.Add(entry);
        }
        entry.chance = chance;
    }

    private static void ViewPart(string name, Transform parent, Vector3 position, Vector3 scale, Material material, float pitch)
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
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static T Find<T>(Transform root, string name) where T : Component
    {
        Transform child = root.Find(name);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static T GetOrCreate<T>(string path, System.Action<T> configure, System.Func<T> factory = null) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;

        if (factory != null) asset = factory();
        else asset = ScriptableObject.CreateInstance(typeof(T)) as T;
        configure?.Invoke(asset);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
