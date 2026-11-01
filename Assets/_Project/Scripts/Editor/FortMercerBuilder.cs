using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FortMercerBuilder
{
    private const string Root = "Assets/_Project";
    private const string SourceScene = Root + "/Scenes/Campamento.unity";
    private const string TargetScene = Root + "/Scenes/FortMercer.unity";
    private const string MaterialFolder = Root + "/Materials/FortMercer";
    private const string PrefabFolder = Root + "/Prefabs/FortMercer";
    private const string EnemyDataFolder = Root + "/Data/Enemies";

    private const float WallHeight = 5f;
    private const float InnerWallHeight = 4.5f;

    private static Material adobe;
    private static Material adobeDark;
    private static Material wood;
    private static Material woodDark;
    private static Material roofMat;
    private static Material sand;
    private static Material road;
    private static Material rock;
    private static Material cactus;
    private static Material metal;
    private static Material gold;
    private static Material water;
    private static Material bellMat;

    private static EnemyAI pistolero;
    private static EnemyAI fusilero;
    private static EnemyAI sniper;
    private static GameObject explosiveBarrel;

    [MenuItem("Cowboy FPS/Construir Fort Mercer (escena nueva)")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene) != null)
        {
            if (!EditorUtility.DisplayDialog("Fort Mercer ya existe", "¿Querés reconstruirlo desde cero? Se pierden los cambios hechos en esa escena.", "Reconstruir", "Cancelar")) return;
            AssetDatabase.DeleteAsset(TargetScene);
        }

        if (!AssetDatabase.CopyAsset(SourceScene, TargetScene))
        {
            EditorUtility.DisplayDialog("Error", "No pude copiar la escena Campamento.", "OK");
            return;
        }

        EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        EnsureFolder(MaterialFolder);
        EnsureFolder(PrefabFolder);
        LoadAssets();
        CleanScene();

        Transform level = new GameObject("FortMercer").transform;
        BuildGround(Group("Exterior", level));
        BuildFortWalls(Group("Murallas", level));
        BuildFrontYard(Group("1_PatioEntrada", level));
        BuildWestWing(Group("2_CuartelOeste", level));
        BuildNorthAlley(Group("3_CallejonNorte", level));
        LockedDoor courtyardDoor = BuildEastWing(Group("4_GaleriasEste", level));
        LockedDoor cantinaDoor = BuildCourtyard(Group("5_PatioFinal", level));
        BuildExteriorProps(Group("Exterior_Props", level));

        Physics.SyncTransforms();
        PlaceGameplay(courtyardDoor, cantinaDoor);
        CowboyStage4Tools.GenerateCoverPoints();
        SetupLighting();
        AddToBuildSettings();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Fort Mercer construido",
            "La escena FortMercer quedó creada y abierta.\n\nÚltimo paso: seleccioná 'NavMesh' en la jerarquía, apretá 'Bake' y guardá con Ctrl+S.", "OK");
    }

    private static void LoadAssets()
    {
        adobe = Mat("Mat_Adobe", new Color(0.82f, 0.72f, 0.55f));
        adobeDark = Mat("Mat_Adobe_Oscuro", new Color(0.7f, 0.6f, 0.45f));
        wood = Mat("Mat_Madera_Clara", new Color(0.55f, 0.4f, 0.25f));
        woodDark = Mat("Mat_Madera_Oscura", new Color(0.35f, 0.24f, 0.15f));
        roofMat = Mat("Mat_Techo", new Color(0.45f, 0.33f, 0.22f));
        sand = Mat("Mat_Arena", new Color(0.86f, 0.76f, 0.56f));
        road = Mat("Mat_Camino", new Color(0.72f, 0.6f, 0.42f));
        rock = Mat("Mat_Roca", new Color(0.55f, 0.45f, 0.38f));
        cactus = Mat("Mat_Cactus", new Color(0.3f, 0.45f, 0.25f));
        metal = Mat("Mat_Hierro", new Color(0.15f, 0.15f, 0.15f));
        gold = Mat("Mat_Oro", new Color(1f, 0.8f, 0.2f));
        water = Mat("Mat_Agua", new Color(0.25f, 0.4f, 0.45f));
        bellMat = Mat("Mat_Bronce", new Color(0.7f, 0.55f, 0.2f));

        pistolero = AssetDatabase.LoadAssetAtPath<EnemyAI>(Root + "/Prefabs/Enemies/Enemy_Pistolero.prefab");
        fusilero = AssetDatabase.LoadAssetAtPath<EnemyAI>(Root + "/Prefabs/Enemies/Enemy_Fusilero.prefab");
        sniper = AssetDatabase.LoadAssetAtPath<EnemyAI>(Root + "/Prefabs/Enemies/Enemy_Francotirador.prefab");
        explosiveBarrel = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Explosivos/Barril_Explosivo.prefab");
    }

    private static void CleanScene()
    {
        string[] keep = { "Player", "HUD", "EventSystem", "Directional Light", "Global Volume", "NavMesh" };
        foreach (GameObject rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
            if (!keep.Contains(rootObject.name)) Object.DestroyImmediate(rootObject);

        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface != null)
        {
            surface.RemoveData();
            surface.navMeshData = null;
        }
    }

    private static void BuildGround(Transform parent)
    {
        Box("Suelo", parent, new Vector3(0f, -0.5f, -20f), new Vector3(260f, 1f, 260f), sand);
        Box("Camino", parent, new Vector3(0f, 0.005f, -62f), new Vector3(7f, 0.01f, 64f), road, false);

        Transform bounds = Group("Limites", parent);
        InvisibleWall(bounds, new Vector3(0f, 5f, -100f), new Vector3(140f, 10f, 1f));
        InvisibleWall(bounds, new Vector3(0f, 5f, 40f), new Vector3(140f, 10f, 1f));
        InvisibleWall(bounds, new Vector3(-70f, 5f, -30f), new Vector3(1f, 10f, 140f));
        InvisibleWall(bounds, new Vector3(70f, 5f, -30f), new Vector3(1f, 10f, 140f));
    }

    private static void BuildFortWalls(Transform parent)
    {
        WallX("Muralla_Sur", parent, -30f, -45f, 45f, WallHeight, 1.2f, adobe, 3.6f, (-3f, 3f));
        WallX("Muralla_Norte", parent, 30f, -45f, 45f, WallHeight, 1.2f, adobe);
        WallZ("Muralla_Oeste", parent, -45f, -30f, 30f, WallHeight, 1.2f, adobe);
        WallZ("Muralla_Este", parent, 45f, -30f, 30f, WallHeight, 1.2f, adobe);

        Transform tower = Group("Torre_Porton", parent);
        Box("Pilar_Izquierdo", tower, new Vector3(-3.9f, 3.5f, -30f), new Vector3(1.8f, 7f, 2.4f), adobeDark);
        Box("Pilar_Derecho", tower, new Vector3(3.9f, 3.5f, -30f), new Vector3(1.8f, 7f, 2.4f), adobeDark);
        Box("Plataforma", tower, new Vector3(0f, 6.6f, -30f), new Vector3(9.6f, 0.8f, 2.8f), adobeDark);
        Box("Parapeto", tower, new Vector3(0f, 7.4f, -31.3f), new Vector3(9.6f, 0.8f, 0.25f), adobeDark);
        Box("Techito", tower, new Vector3(0f, 9f, -30f), new Vector3(4f, 0.15f, 3f), roofMat, false);
        Box("Poste_Techito_A", tower, new Vector3(-1.8f, 8f, -29f), new Vector3(0.15f, 2f, 0.15f), woodDark, false);
        Box("Poste_Techito_B", tower, new Vector3(1.8f, 8f, -29f), new Vector3(0.15f, 2f, 0.15f), woodDark, false);

        WallX("Muro_Interior_Sur", parent, -8f, -45f, 45f, InnerWallHeight, 1f, adobeDark, 2.8f, (-36f, -31f));
    }

    private static void BuildFrontYard(Transform parent)
    {
        SniperTower("Torre_SO", parent, new Vector3(-40.5f, 0f, -25.5f), 5f);
        SniperTower("Torre_SE", parent, new Vector3(40.5f, 0f, -25.5f), 5f);

        LowWall(parent, new Vector3(-20f, 0f, -18f), 6f, 0f);
        LowWall(parent, new Vector3(12f, 0f, -20f), 5f, 90f);
        LowWall(parent, new Vector3(25f, 0f, -14f), 6f, 0f);
        LowWall(parent, new Vector3(-8f, 0f, -13f), 4f, 0f);
        LowWall(parent, new Vector3(34f, 0f, -22f), 4f, 90f);

        CrateStack(parent, new Vector3(-28f, 0f, -24f), 2);
        Crate(parent, new Vector3(-10f, 0f, -22f), 1.1f, 15f);
        Crate(parent, new Vector3(6f, 0f, -15f), 1f, 40f);
        CrateStack(parent, new Vector3(18f, 0f, -25f), 3);
        Crate(parent, new Vector3(30f, 0f, -19f), 1.2f, 10f);
        Crate(parent, new Vector3(-34f, 0f, -16f), 1f, 0f);
        Barrel(parent, new Vector3(-15f, 0f, -26f));
        Barrel(parent, new Vector3(-14.3f, 0f, -26.6f));
        Barrel(parent, new Vector3(22f, 0f, -11f));
        Trough(parent, new Vector3(0f, 0f, -18f), 0f);
        Wagon(parent, new Vector3(8f, 0f, -26f), 30f, false);
        Box("Mesa", parent, new Vector3(-22f, 0.45f, -11.5f), new Vector3(1.8f, 0.9f, 0.9f), woodDark);
    }

    private static void BuildWestWing(Transform parent)
    {
        Building("Cuartel", parent, new Vector2(-44.4f, -6f), new Vector2(-36f, 18f), 4f, adobe,
            ('E', 2f, 3.5f), ('E', 10f, 11.5f));
        WallX("Cuartel_Particion", parent, 6f, -44.4f, -36f, 4f, 0.3f, adobe, 2.6f, (-41f, -39.5f));

        Building("Armeria", parent, new Vector2(-28f, -4f), new Vector2(-19f, 14f), 4f, adobeDark,
            ('W', 3f, 4.5f));
        Porch("Porche_Armeria", parent, new Vector2(-31f, -2f), new Vector2(-28f, 12f), 3.2f, 'W');

        Stairs("Escalera_Techo_Cuartel", parent, new Vector3(-35.2f, 0f, -5.5f), Vector3.forward, 1.4f, 4.3f, wood);
        Box("Pasarela_Techos", parent, new Vector3(-32f, 4.18f, 11f), new Vector3(8.4f, 0.2f, 1.6f), wood);
        Box("Baranda_Pasarela_A", parent, new Vector3(-32f, 4.7f, 10.25f), new Vector3(8.4f, 0.08f, 0.08f), woodDark, false);
        Box("Baranda_Pasarela_B", parent, new Vector3(-32f, 4.7f, 11.75f), new Vector3(8.4f, 0.08f, 0.08f), woodDark, false);

        Crate(parent, new Vector3(-32f, 0f, -2f), 1f, 20f);
        CrateStack(parent, new Vector3(-30.5f, 0f, 8f), 2);
        Wagon(parent, new Vector3(-25f, 0f, 17f), 90f, false);
        Barrel(parent, new Vector3(-21f, 0f, -6f));
        Barrel(parent, new Vector3(-20.3f, 0f, -6.5f));
        Box("Catre_1", parent, new Vector3(-43.3f, 0.3f, -3f), new Vector3(1.6f, 0.6f, 0.9f), woodDark);
        Box("Catre_2", parent, new Vector3(-43.3f, 0.3f, 1f), new Vector3(1.6f, 0.6f, 0.9f), woodDark);
        Box("Escritorio_Capataz", parent, new Vector3(-42.5f, 0.45f, 15f), new Vector3(1.4f, 0.9f, 2.4f), woodDark);
    }

    private static void BuildNorthAlley(Transform parent)
    {
        CrateStack(parent, new Vector3(-30f, 0f, 24f), 2);
        Crate(parent, new Vector3(-10f, 0f, 26f), 1.2f, 25f);
        Crate(parent, new Vector3(12f, 0f, 23f), 1f, 0f);
        CrateStack(parent, new Vector3(28f, 0f, 26f), 3);
        Wagon(parent, new Vector3(0f, 0f, 25.5f), 90f, true);
        Barrel(parent, new Vector3(20f, 0f, 27.5f));
        Barrel(parent, new Vector3(20.8f, 0f, 27.9f));
    }

    private static LockedDoor BuildEastWing(Transform parent)
    {
        WallZ("Muro_Patio_Este", parent, 18f, -8f, 20f, InnerWallHeight, 1f, adobeDark, 2.8f, (5f, 8f));
        Porch("Galeria_Este", parent, new Vector2(18.6f, -7f), new Vector2(21.6f, 19f), 3.2f, 'E');

        Building("Almacen", parent, new Vector2(34f, -6.5f), new Vector2(44.4f, 9f), 4.5f, adobe, ('W', 0f, 3f));
        Building("Oficina", parent, new Vector2(30f, 12f), new Vector2(44.4f, 19.5f), 4f, adobeDark, ('S', 34f, 35.5f));
        WaterTower(parent, new Vector3(27f, 0f, -2f));

        CrateStack(parent, new Vector3(26f, 0f, 6f), 2);
        Crate(parent, new Vector3(29f, 0f, 15f), 1f, 30f);
        CrateStack(parent, new Vector3(40f, 0f, -1f), 3);
        Crate(parent, new Vector3(42f, 0f, 6f), 1.2f, 0f);
        Barrel(parent, new Vector3(23f, 0f, 16f));

        return Door("Puerta_Patio", parent, new Vector3(18f, 0f, 5f), Vector3.forward, 3f, 2.8f, -100f, true, true, woodDark);
    }

    private static LockedDoor BuildCourtyard(Transform parent)
    {
        WallZ("Muro_Patio_Oeste", parent, -18f, -8f, 20f, InnerWallHeight, 1f, adobeDark);
        WallX("Muro_Patio_Norte", parent, 20f, -18f, 18f, InnerWallHeight, 1f, adobeDark);

        Box("Plataforma_Central", parent, new Vector3(0f, 1f, 6f), new Vector3(6f, 2f, 6f), wood);
        Box("Baranda_Este", parent, new Vector3(2.95f, 2.45f, 6f), new Vector3(0.1f, 0.9f, 6f), woodDark);
        Box("Baranda_Oeste", parent, new Vector3(-2.95f, 2.45f, 6f), new Vector3(0.1f, 0.9f, 6f), woodDark);
        Stairs("Escalera_Plataforma_Norte", parent, new Vector3(0f, 0f, 11.8f), Vector3.back, 2f, 2f, wood);
        Stairs("Escalera_Plataforma_Sur", parent, new Vector3(0f, 0f, 0.2f), Vector3.forward, 2f, 2f, wood);

        Porch("Galeria_Norte", parent, new Vector2(-9.5f, 16.5f), new Vector2(17.5f, 19.5f), 3.2f, 'S');
        Porch("Galeria_Sur", parent, new Vector2(-17.5f, -7.5f), new Vector2(10f, -4.5f), 3.2f, 'N');

        Building("Cantina", parent, new Vector2(-17.5f, 11f), new Vector2(-10f, 19.5f), 4f, adobe, ('S', -14.5f, -12.5f));
        Box("Barra_Cantina", parent, new Vector3(-12f, 0.55f, 17f), new Vector3(3.5f, 1.1f, 0.7f), woodDark);

        Trough(parent, new Vector3(9f, 0f, -1f), 90f);
        Wagon(parent, new Vector3(-8f, 0f, -1f), 20f, true);
        CrateStack(parent, new Vector3(12f, 0f, 12f), 2);
        Crate(parent, new Vector3(-6f, 0f, 14f), 1f, 15f);
        Barrel(parent, new Vector3(6f, 0f, 15f));
        Barrel(parent, new Vector3(-15f, 0f, 2f));
        LowWall(parent, new Vector3(9f, 0f, 9f), 3f, 90f);

        return Door("Puerta_Cantina", parent, new Vector3(-14.5f, 0f, 11f), Vector3.right, 2f, 2.6f, -100f, true, false, woodDark);
    }

    private static void BuildExteriorProps(Transform parent)
    {
        Rock(parent, new Vector3(-15f, 0f, -50f), 3f);
        Rock(parent, new Vector3(18f, 0f, -58f), 4f);
        Rock(parent, new Vector3(-25f, 0f, -70f), 5f);
        Rock(parent, new Vector3(25f, 0f, -40f), 2.5f);
        Rock(parent, new Vector3(-10f, 0f, -38f), 2f);
        Rock(parent, new Vector3(12f, 0f, -36f), 1.8f);
        Rock(parent, new Vector3(-40f, 0f, -45f), 6f);
        Rock(parent, new Vector3(45f, 0f, -60f), 7f);

        Cactus(parent, new Vector3(-8f, 0f, -60f));
        Cactus(parent, new Vector3(9f, 0f, -66f));
        Cactus(parent, new Vector3(-20f, 0f, -45f));
        Cactus(parent, new Vector3(22f, 0f, -50f));
        Cactus(parent, new Vector3(5f, 0f, -48f));
        Cactus(parent, new Vector3(-30f, 0f, -60f));

        Wagon(parent, new Vector3(-5f, 0f, -52f), 70f, false);
        Wagon(parent, new Vector3(6f, 0f, -44f), -20f, true);
        Crate(parent, new Vector3(-6f, 0f, -34f), 1.1f, 10f);
        CrateStack(parent, new Vector3(7f, 0f, -35f), 2);
        Box("Poste_Atadero", parent, new Vector3(-3f, 0.6f, -72f), new Vector3(2.5f, 0.1f, 0.1f), woodDark, false);
    }

    private static void PlaceGameplay(LockedDoor courtyardDoor, LockedDoor cantinaDoor)
    {
        GameObject player = GameObject.FindWithTag("Player");
        player.transform.SetPositionAndRotation(new Vector3(0f, 0f, -78f), Quaternion.identity);
        SetBool(player.GetComponent<PlayerDeath>(), "reloadSceneOnDeath", false);

        PlayerKeyring keyring = GetOrAdd<PlayerKeyring>(player);
        PlayerInteractor interactor = GetOrAdd<PlayerInteractor>(player);
        Camera playerCamera = player.GetComponentInChildren<Camera>(true);
        Wire(interactor, ("input", player.GetComponent<PlayerInputReader>()), ("playerCamera", playerCamera));

        Transform gameplay = new GameObject("Gameplay").transform;
        Transform enemies = Group("Enemigos", gameplay);
        Transform pickups = Group("Pickups", gameplay);
        Transform zones = Group("Zonas", gameplay);

        GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Explosivos/Porton_Destructible.prefab");
        GameObject gateObject = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab, gameplay);
        gateObject.name = "Porton_Principal";
        gateObject.transform.position = new Vector3(0f, 0f, -30f);
        gateObject.transform.localScale = new Vector3(1.5f, 1.2f, 1f);
        DestructibleBarrier mainGate = gateObject.GetComponent<DestructibleBarrier>();

        Transform exterior = Group("Exterior", enemies);
        PlaceEnemy(pistolero, "Guardia_Porton_A", exterior, new Vector3(-8f, 0f, -38f), 180f, new Vector3(16f, 0f, 0f));
        PlaceEnemy(pistolero, "Guardia_Porton_B", exterior, new Vector3(10f, 0f, -41f), 180f, new Vector3(-6f, 0f, -4f), new Vector3(0f, 0f, 4f));
        PlaceEnemy(fusilero, "Fusilero_Muralla", exterior, new Vector3(-14f, 0f, -33f), 180f);
        PlaceEnemy(sniper, "Francotirador_Porton", exterior, new Vector3(0f, 7f, -29.8f), 180f);

        Transform front = Group("1_PatioEntrada", enemies);
        PlaceEnemy(pistolero, "Pistolero_F1", front, new Vector3(-12f, 0f, -20f), 180f, new Vector3(10f, 0f, 0f));
        PlaceEnemy(pistolero, "Pistolero_F2", front, new Vector3(15f, 0f, -16f), 180f, new Vector3(0f, 0f, -8f), new Vector3(8f, 0f, -8f));
        PlaceEnemy(pistolero, "Pistolero_F3", front, new Vector3(-30f, 0f, -20f), 90f, new Vector3(0f, 0f, 6f));
        PlaceEnemy(fusilero, "Fusilero_F1", front, new Vector3(25f, 0f, -24f), 180f, new Vector3(-6f, 0f, 0f));
        PlaceEnemy(fusilero, "Fusilero_F2", front, new Vector3(-5f, 0f, -11f), 180f);
        PlaceEnemy(sniper, "Francotirador_SO", front, new Vector3(-40.5f, 5f, -25.5f), 135f);
        PlaceEnemy(sniper, "Francotirador_SE", front, new Vector3(40.5f, 5f, -25.5f), 225f);

        AlarmBell bell = BuildAlarmBell(gameplay, new Vector3(-29f, 0f, -12f));
        BuildReinforcements(gameplay, bell, new[] { new Vector3(-33.5f, 0f, -4f), new Vector3(35f, 0f, -14f) });

        Transform west = Group("2_CuartelOeste", enemies);
        EnemyAI capataz = PlaceEnemy(pistolero, "El_Capataz", west, new Vector3(-40f, 0f, 13f), 90f);
        MakeKeyCarrier(capataz, CreateKeyPrefab());
        PlaceEnemy(pistolero, "Guardia_Cuartel", west, new Vector3(-38f, 0f, 8f), 90f, new Vector3(0f, 0f, 7f));
        PlaceEnemy(pistolero, "Pistolero_Calle", west, new Vector3(-32f, 0f, 4f), 180f, new Vector3(0f, 0f, -8f), new Vector3(0f, 0f, 10f));
        PlaceEnemy(fusilero, "Fusilero_Techo", west, new Vector3(-40f, 4.3f, 2f), 90f, new Vector3(0f, 0f, -6f), new Vector3(0f, 0f, 8f));
        PlaceEnemy(pistolero, "Pistolero_Armeria", west, new Vector3(-23f, 0f, 8f), 270f);

        Transform north = Group("3_CallejonNorte", gameplay);
        PlayerZoneTrigger ambushZone = Zone("Zona_Emboscada_Norte", zones, new Vector3(0f, 2f, 25f), new Vector3(20f, 4f, 9f));
        SpawnGroup("Emboscada_Norte", north, ambushZone, true,
            (pistolero, new Vector3(42f, 0f, 25f), 270f),
            (pistolero, new Vector3(40f, 0f, 22f), 270f),
            (fusilero, new Vector3(36f, 0f, 27.5f), 270f));

        Transform east = Group("4_GaleriasEste", enemies);
        PlaceEnemy(fusilero, "Fusilero_E1", east, new Vector3(31f, 0f, 8f), 270f, new Vector3(0f, 0f, -6f));
        PlaceEnemy(fusilero, "Fusilero_E2", east, new Vector3(24f, 0f, -5f), 0f);
        PlaceEnemy(pistolero, "Pistolero_E1", east, new Vector3(28f, 0f, 15f), 180f, new Vector3(8f, 0f, 0f));
        PlaceEnemy(pistolero, "Pistolero_E2", east, new Vector3(38f, 0f, 2f), 270f);
        PlaceEnemy(sniper, "Francotirador_Almacen", east, new Vector3(39f, 4.8f, 1f), 0f);

        BossController boss = BuildBoss(gameplay, cantinaDoor);
        PlayerZoneTrigger courtyardZone = Zone("Zona_Patio", zones, new Vector3(-1.5f, 2f, 6f), new Vector3(30f, 4f, 26f));
        PlayerZoneTrigger exitZone = Zone("Zona_Salida", zones, new Vector3(0f, 2f, -50f), new Vector3(40f, 4f, 8f));

        Transform escape = Group("Escape", gameplay);
        List<EnemySpawnGroup> escapeGroups = new List<EnemySpawnGroup>
        {
            SpawnGroup("Escape_Este", escape, Zone("Zona_Escape_Este", zones, new Vector3(31f, 2f, 6f), new Vector3(26f, 4f, 26f)), false,
                (pistolero, new Vector3(40f, 0f, 17f), 270f),
                (pistolero, new Vector3(24f, 0f, -6f), 0f),
                (fusilero, new Vector3(42f, 0f, -4f), 270f)),
            SpawnGroup("Escape_Norte", escape, Zone("Zona_Escape_Norte", zones, new Vector3(0f, 2f, 25f), new Vector3(20f, 4f, 9f)), false,
                (pistolero, new Vector3(-42f, 0f, 25f), 90f),
                (pistolero, new Vector3(-30f, 0f, 22f), 90f),
                (fusilero, new Vector3(-38f, 0f, 28f), 90f)),
            SpawnGroup("Escape_Oeste", escape, Zone("Zona_Escape_Oeste", zones, new Vector3(-31f, 2f, 6f), new Vector3(26f, 4f, 26f)), false,
                (pistolero, new Vector3(-32f, 0f, -6f), 0f),
                (pistolero, new Vector3(-24f, 0f, -6f), 0f),
                (fusilero, new Vector3(-40f, 4.3f, -4f), 90f)),
            SpawnGroup("Escape_Frente", escape, Zone("Zona_Escape_Frente", zones, new Vector3(0f, 2f, -19f), new Vector3(88f, 4f, 20f)), false,
                (pistolero, new Vector3(-6f, 0f, -27f), 0f),
                (pistolero, new Vector3(6f, 0f, -27f), 0f),
                (fusilero, new Vector3(-20f, 0f, -26f), 0f),
                (fusilero, new Vector3(20f, 0f, -26f), 0f),
                (pistolero, new Vector3(0f, 0f, -38f), 0f))
        };

        PlacePickups(pickups);
        PlaceExplosiveBarrels(gameplay);

        HUDRefs hud = BuildHud(player, boss);

        GameObject managers = new GameObject("GameManager");
        GameStateManager gameState = managers.AddComponent<GameStateManager>();
        Wire(gameState,
            ("input", player.GetComponent<PlayerInputReader>()),
            ("look", player.GetComponent<PlayerLook>()),
            ("playerDeath", player.GetComponent<PlayerDeath>()),
            ("pausePanel", hud.pausePanel),
            ("victoryPanel", hud.victoryPanel),
            ("defeatPanel", hud.defeatPanel),
            ("victoryStatsText", hud.victoryStats));
        hud.ConnectButtons(gameState);

        MissionDirector director = managers.AddComponent<MissionDirector>();
        Wire(director,
            ("objectives", hud.objectives),
            ("gameState", gameState),
            ("keyring", keyring),
            ("mainGate", mainGate),
            ("courtyardDoor", courtyardDoor),
            ("courtyardZone", courtyardZone),
            ("boss", boss),
            ("exitZone", exitZone));
        SerializedObject serialized = new SerializedObject(director);
        SerializedProperty groups = serialized.FindProperty("escapeGroups");
        groups.arraySize = escapeGroups.Count;
        for (int i = 0; i < escapeGroups.Count; i++) groups.GetArrayElementAtIndex(i).objectReferenceValue = escapeGroups[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EnsureEventSystem();
    }

    private static void PlacePickups(Transform parent)
    {
        GameObject ammo45 = LoadPickup("Pickup_Municion_45Colt");
        GameObject ammo4440 = LoadPickup("Pickup_Municion_4440");
        GameObject bandage = LoadPickup("Pickup_Vendaje");
        GameObject dynamite = LoadPickup("Pickup_Dinamita");

        Put(dynamite, parent, new Vector3(3f, 0f, -70f));
        Put(ammo4440, parent, new Vector3(-3f, 0f, -70f));
        Put(ammo45, parent, new Vector3(-20f, 0f, -16f));
        Put(ammo4440, parent, new Vector3(10f, 0f, -12f));
        Put(bandage, parent, new Vector3(-38f, 0f, -12f));
        Put(dynamite, parent, new Vector3(-30f, 0f, -6f));
        Put(bandage, parent, new Vector3(-42f, 0f, -4f));
        Put(ammo4440, parent, new Vector3(-42f, 4.3f, 16f));
        Put(ammo45, parent, new Vector3(-38f, 0f, 27f));
        Put(bandage, parent, new Vector3(22f, 0f, -4f));
        Put(dynamite, parent, new Vector3(36f, 0f, -4f));
        Put(ammo45, parent, new Vector3(30f, 0f, 18f));
        Put(ammo4440, parent, new Vector3(22f, 0f, 12f));
    }

    private static void PlaceExplosiveBarrels(Transform parent)
    {
        if (explosiveBarrel == null) return;
        Transform group = Group("Barriles_Explosivos", parent);
        Vector3[] positions =
        {
            new Vector3(3f, 0.5f, -12f), new Vector3(3.8f, 0.5f, -12.6f), new Vector3(-24f, 0.5f, -21f),
            new Vector3(33f, 0.5f, 1f), new Vector3(33f, 0.5f, 2.2f), new Vector3(38f, 0.5f, 4f), new Vector3(25f, 0.5f, 10f),
            new Vector3(14f, 0.5f, -6f), new Vector3(-3f, 0.5f, 16f)
        };
        foreach (Vector3 position in positions) Put(explosiveBarrel, group, position);
    }

    private static BossController BuildBoss(Transform parent, LockedDoor cantinaDoor)
    {
        EnemyDataSO duel = GetOrCreateData("Enemy_DirtyHolliday", d =>
        {
            d.displayName = "Dirty Holliday";
            d.combatStyle = EnemyCombatStyle.Assault;
            d.maxHealth = 500f;
            d.staggerTime = 0.1f;
            d.corpseLifetime = 60f;
            d.deathImpulse = 6f;
            d.chaseSpeed = 4f;
            d.viewDistance = 45f;
            d.viewAngle = 360f;
            d.memoryTime = 30f;
            d.searchDuration = 120f;
            d.damage = 9f;
            d.pelletsPerShot = 8;
            d.pelletSpread = 4f;
            d.falloffStartDistance = 8f;
            d.falloffEndDistance = 18f;
            d.minFalloffMultiplier = 0.1f;
            d.timeBetweenShots = 0.4f;
            d.magazineSize = 2;
            d.reloadTime = 2.5f;
            d.attackRange = 20f;
            d.bulletSpeed = 90f;
            d.baseSpread = 1f;
            d.spreadPerMeter = 0f;
            d.preferredMinDistance = 3f;
            d.preferredMaxDistance = 8f;
            d.usesCover = true;
            d.coverChance = 0.2f;
            d.canRingAlarm = false;
            d.repositionIntervalMin = 1.5f;
            d.repositionIntervalMax = 2.5f;
            d.loot = new List<LootEntry>();
        });

        ThrowableDataSO dynamite = AssetDatabase.LoadAssetAtPath<ThrowableDataSO>(Root + "/Data/Explosivos/Dinamita.asset");
        EnemyDataSO diablo = GetOrCreateData("Enemy_DirtyHolliday_Diablo", d =>
        {
            EditorUtility.CopySerialized(duel, d);
            d.name = "Enemy_DirtyHolliday_Diablo";
            d.displayName = "Dirty Holliday (Diablo)";
            d.staggerTime = 0f;
            d.chaseSpeed = 5.5f;
            d.damage = 12.6f;
            d.reloadTime = 1.8f;
            d.timeBetweenShots = 0.3f;
            d.throwable = dynamite;
            d.dynamiteCount = 6;
            d.dynamiteCooldown = 5f;
            d.dynamiteLevelMin = 12;
            d.dynamiteLevelMax = 14;
            d.dynamiteRollInterval = 4f;
            d.dynamiteMinDistance = 5f;
            d.dynamiteMaxDistance = 20f;
        });

        EnemyAI bossPrefab = CreateBossPrefab(duel);
        EnemyAI bossInstance = (EnemyAI)PrefabUtility.InstantiatePrefab(bossPrefab, parent);
        bossInstance.name = "Dirty_Holliday";
        bossInstance.transform.SetPositionAndRotation(new Vector3(0f, 0f, 13.5f), Quaternion.LookRotation(Vector3.right));

        Transform retreatPoint = Marker("Boss_Refugio", parent, new Vector3(-14f, 0f, 15.5f));
        Transform returnPoint = Marker("Boss_Regreso", parent, new Vector3(-13.5f, 0f, 9f));

        Transform waveRoot = Group("Oleada_Holliday", parent);
        EnemySpawnGroup wave = SpawnGroup("Oleada_Holliday", waveRoot, null, false,
            (pistolero, new Vector3(-13.5f, 0f, 9.5f), 180f),
            (pistolero, new Vector3(8f, 0f, 18.5f), 180f),
            (pistolero, new Vector3(5f, 0f, -6f), 0f),
            (fusilero, new Vector3(-14f, 4.3f, 15f), 90f),
            (fusilero, new Vector3(-4f, 0f, 18f), 180f));

        BossController boss = bossInstance.GetComponent<BossController>();
        Wire(boss,
            ("diabloData", diablo),
            ("retreatPoint", retreatPoint),
            ("returnPoint", returnPoint),
            ("hideoutDoor", cantinaDoor),
            ("reinforcementWave", wave));
        return boss;
    }

    private static EnemyAI CreateBossPrefab(EnemyDataSO data)
    {
        string path = PrefabFolder + "/Boss_DirtyHolliday.prefab";
        EnemyAI existing = AssetDatabase.LoadAssetAtPath<EnemyAI>(path);
        if (existing != null) return existing;

        GameObject source = (GameObject)PrefabUtility.InstantiatePrefab(pistolero.gameObject);
        PrefabUtility.UnpackPrefabInstance(source, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        source.name = "Boss_DirtyHolliday";

        Transform visual = source.transform.Find("Visual");
        visual.localScale = Vector3.one * 1.12f;
        Material coat = Mat("Mat_Abrigo_Holliday", new Color(0.12f, 0.1f, 0.1f));
        Material hat = Mat("Mat_Sombrero_Holliday", new Color(0.85f, 0.83f, 0.78f));
        SetMaterial(visual, "Cuerpo", coat);
        SetMaterial(visual, "Sombrero_Ala", hat);
        SetMaterial(visual, "Sombrero_Copa", hat);

        Transform gun = visual.Find("Arma");
        if (gun != null)
        {
            gun.localPosition = new Vector3(0.17f, 1.2f, 0.55f);
            gun.localScale = new Vector3(0.05f, 0.06f, 0.6f);
            GameObject second = Object.Instantiate(gun.gameObject, visual);
            second.name = "Arma_Cano2";
            second.transform.localPosition = new Vector3(0.23f, 1.2f, 0.55f);
        }
        Transform muzzle = visual.Find("Muzzle");
        if (muzzle != null) muzzle.localPosition = new Vector3(0.2f, 1.22f, 0.9f);

        Wire(source.GetComponent<EnemyAI>(), ("data", data));
        source.AddComponent<BossController>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
        Object.DestroyImmediate(source);
        return prefab.GetComponent<EnemyAI>();
    }

    private static GameObject CreateKeyPrefab()
    {
        string path = PrefabFolder + "/Pickup_Llave_Patio.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("Pickup_Llave_Patio");
        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.6f, 0f);
        trigger.size = new Vector3(1.2f, 1.2f, 1.2f);

        Transform visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);
        visual.localPosition = new Vector3(0f, 0.6f, 0f);
        LocalBox("Aro", visual, new Vector3(0f, 0.12f, 0f), new Vector3(0.14f, 0.14f, 0.03f), gold);
        LocalBox("Cana", visual, new Vector3(0f, -0.05f, 0f), new Vector3(0.035f, 0.26f, 0.03f), gold);
        LocalBox("Diente_1", visual, new Vector3(0.05f, -0.14f, 0f), new Vector3(0.07f, 0.03f, 0.03f), gold);
        LocalBox("Diente_2", visual, new Vector3(0.04f, -0.09f, 0f), new Vector3(0.05f, 0.03f, 0.03f), gold);

        GameObject lightObject = new GameObject("Brillo");
        lightObject.transform.SetParent(visual, false);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.85f, 0.4f);
        light.range = 3f;
        light.intensity = 2.5f;

        KeyPickup key = root.AddComponent<KeyPickup>();
        Wire(key, ("visual", visual));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void MakeKeyCarrier(EnemyAI enemy, GameObject keyPrefab)
    {
        KeyCarrier carrier = enemy.gameObject.AddComponent<KeyCarrier>();
        Wire(carrier, ("keyPrefab", keyPrefab));

        Transform visual = enemy.transform.Find("Visual");
        if (visual != null)
        {
            LocalBox("Panuelo_Dorado", visual, new Vector3(0f, 1.45f, 0.18f), new Vector3(0.36f, 0.08f, 0.1f), gold);
            LocalBox("Llavero", visual, new Vector3(-0.3f, 0.95f, 0.05f), new Vector3(0.06f, 0.12f, 0.03f), gold);
        }
    }

    private static AlarmBell BuildAlarmBell(Transform parent, Vector3 position)
    {
        GameObject bellRoot = new GameObject("Campana_Alarma");
        bellRoot.transform.SetParent(parent);
        bellRoot.transform.position = position;
        Box("Poste", bellRoot.transform, new Vector3(0f, 1.5f, 0f) + position, new Vector3(0.15f, 3f, 0.15f), woodDark);
        Box("Travesano", bellRoot.transform, new Vector3(0.35f, 2.9f, 0f) + position, new Vector3(0.8f, 0.12f, 0.12f), woodDark, false);

        Transform pivot = new GameObject("PivoteCampana").transform;
        pivot.SetParent(bellRoot.transform, false);
        pivot.localPosition = new Vector3(0.6f, 2.85f, 0f);
        GameObject bell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bell.name = "Campana";
        Object.DestroyImmediate(bell.GetComponent<Collider>());
        bell.transform.SetParent(pivot, false);
        bell.transform.localPosition = new Vector3(0f, -0.2f, 0f);
        bell.transform.localScale = new Vector3(0.35f, 0.18f, 0.35f);
        bell.GetComponent<MeshRenderer>().sharedMaterial = bellMat;

        Transform ringPoint = new GameObject("PuntoToque").transform;
        ringPoint.SetParent(bellRoot.transform, false);
        ringPoint.localPosition = new Vector3(0.6f, 0f, -0.8f);

        AlarmBell alarm = bellRoot.AddComponent<AlarmBell>();
        Wire(alarm, ("ringPoint", ringPoint), ("bellVisual", pivot));
        return alarm;
    }

    private static void BuildReinforcements(Transform parent, AlarmBell bell, Vector3[] points)
    {
        GameObject spawnerObject = new GameObject("Refuerzos_Campana");
        spawnerObject.transform.SetParent(parent);
        ReinforcementSpawner spawner = spawnerObject.AddComponent<ReinforcementSpawner>();

        SerializedObject serialized = new SerializedObject(spawner);
        serialized.FindProperty("triggerBell").objectReferenceValue = bell;
        SerializedProperty spawnPoints = serialized.FindProperty("spawnPoints");
        spawnPoints.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++)
            spawnPoints.GetArrayElementAtIndex(i).objectReferenceValue = Marker("Spawn_" + i, spawnerObject.transform, points[i]);

        SerializedProperty waves = serialized.FindProperty("waves");
        waves.arraySize = 2;
        FillWave(waves.GetArrayElementAtIndex(0), 3f, pistolero, pistolero);
        FillWave(waves.GetArrayElementAtIndex(1), 10f, pistolero, fusilero);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void FillWave(SerializedProperty wave, float delay, params EnemyAI[] list)
    {
        wave.FindPropertyRelative("delayBefore").floatValue = delay;
        wave.FindPropertyRelative("delayBetweenSpawns").floatValue = 0.6f;
        SerializedProperty enemies = wave.FindPropertyRelative("enemies");
        enemies.arraySize = list.Length;
        for (int i = 0; i < list.Length; i++) enemies.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
    }

    private static EnemySpawnGroup SpawnGroup(string name, Transform parent, PlayerZoneTrigger zone, bool armedOnStart, params (EnemyAI prefab, Vector3 position, float yaw)[] spawns)
    {
        GameObject groupObject = new GameObject(name);
        groupObject.transform.SetParent(parent);
        EnemySpawnGroup group = groupObject.AddComponent<EnemySpawnGroup>();

        SerializedObject serialized = new SerializedObject(group);
        serialized.FindProperty("activationZone").objectReferenceValue = zone;
        serialized.FindProperty("armedOnStart").boolValue = armedOnStart;
        SerializedProperty entries = serialized.FindProperty("entries");
        entries.arraySize = spawns.Length;
        for (int i = 0; i < spawns.Length; i++)
        {
            Transform point = Marker("Spawn_" + i, groupObject.transform, spawns[i].position);
            point.rotation = Quaternion.Euler(0f, spawns[i].yaw, 0f);
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("prefab").objectReferenceValue = spawns[i].prefab;
            entry.FindPropertyRelative("point").objectReferenceValue = point;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return group;
    }

    private static EnemyAI PlaceEnemy(EnemyAI prefab, string name, Transform parent, Vector3 position, float yaw, params Vector3[] routeOffsets)
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

    private struct HUDRefs
    {
        public ObjectiveHUD objectives;
        public GameObject pausePanel;
        public GameObject victoryPanel;
        public GameObject defeatPanel;
        public TMP_Text victoryStats;
        public List<(Button button, string action)> buttons;

        public void ConnectButtons(GameStateManager manager)
        {
            foreach ((Button button, string action) in buttons)
            {
                UnityAction call;
                if (action == "resume") call = manager.Resume;
                else if (action == "restart") call = manager.Restart;
                else call = manager.Quit;
                UnityEventTools.AddPersistentListener(button.onClick, call);
            }
        }
    }

    private static HUDRefs BuildHud(GameObject player, BossController boss)
    {
        AmmoHUD ammoHud = Object.FindFirstObjectByType<AmmoHUD>(FindObjectsInactive.Include);
        Transform hud = ammoHud.transform;
        HUDRefs refs = new HUDRefs { buttons = new List<(Button, string)>() };

        RectTransform objectivePanel = Rect("Objetivo", hud, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -36f), new Vector2(720f, 90f));
        Text("Titulo", objectivePanel, "OBJETIVO", 18f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), Vector2.zero, new Vector2(720f, 24f)).color = new Color(1f, 0.75f, 0.35f);
        TMP_Text objectiveText = Text("Texto", objectivePanel, "", 26f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, -26f), new Vector2(720f, 64f));
        objectiveText.textWrappingMode = TextWrappingModes.Normal;

        TMP_Text hint = Text("Aviso", hud, "", 24f, FontStyles.Italic, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(1000f, 40f));
        hint.color = new Color(1f, 0.9f, 0.7f, 0f);

        ObjectiveHUD objectives = hud.gameObject.AddComponent<ObjectiveHUD>();
        Wire(objectives, ("objectiveText", objectiveText), ("hintText", hint));
        refs.objectives = objectives;

        TMP_Text prompt = Text("Interaccion", hud, "", 26f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(800f, 36f));
        InteractionPromptUI promptUI = hud.gameObject.AddComponent<InteractionPromptUI>();
        Wire(promptUI, ("interactor", player.GetComponent<PlayerInteractor>()), ("label", prompt));

        RectTransform bossRoot = Rect("BarraJefe", hud, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(720f, 60f));
        TMP_Text bossName = Text("Nombre", bossRoot, "Dirty Holliday", 28f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(720f, 34f));
        RectTransform bossBackground = Rect("Fondo", bossRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(700f, 16f));
        Img(bossBackground, new Color(0f, 0f, 0f, 0.6f));
        RectTransform bossFill = Rect("Relleno", bossBackground, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        Image bossFillImage = Img(bossFill, new Color(0.85f, 0.65f, 0.3f));
        bossRoot.gameObject.SetActive(false);
        BossHealthBar bossBar = hud.gameObject.AddComponent<BossHealthBar>();
        Wire(bossBar, ("boss", boss), ("root", bossRoot.gameObject), ("nameText", bossName), ("fill", bossFill), ("fillImage", bossFillImage));

        refs.pausePanel = EndPanel("Pausa", hud, "PAUSA", "Fort Mercer", new Color(1f, 0.9f, 0.75f), refs.buttons,
            ("Continuar", "resume"), ("Reiniciar", "restart"), ("Salir", "quit"), out _);
        refs.victoryPanel = EndPanel("Victoria", hud, "VENGANZA CUMPLIDA", "Dirty Holliday ya no va a lastimar a nadie.", new Color(1f, 0.8f, 0.35f), refs.buttons,
            ("Jugar de nuevo", "restart"), ("Salir", "quit"), out TMP_Text stats);
        refs.victoryStats = stats;
        refs.defeatPanel = EndPanel("Derrota", hud, "HAS MUERTO", "La banda de Holliday sigue suelta.", new Color(0.9f, 0.25f, 0.2f), refs.buttons,
            ("Reintentar", "restart"), ("Salir", "quit"), out _);

        return refs;
    }

    private static GameObject EndPanel(string name, Transform parent, string title, string subtitle, Color titleColor,
        List<(Button, string)> buttons, (string label, string action) first, (string label, string action) second, out TMP_Text stats)
    {
        return EndPanel(name, parent, title, subtitle, titleColor, buttons, out stats, first, second);
    }

    private static GameObject EndPanel(string name, Transform parent, string title, string subtitle, Color titleColor,
        List<(Button, string)> buttons, (string label, string action) first, (string label, string action) second, (string label, string action) third, out TMP_Text stats)
    {
        return EndPanel(name, parent, title, subtitle, titleColor, buttons, out stats, first, second, third);
    }

    private static GameObject EndPanel(string name, Transform parent, string title, string subtitle, Color titleColor,
        List<(Button, string)> buttons, out TMP_Text stats, params (string label, string action)[] actions)
    {
        RectTransform panel = Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Img(panel, new Color(0.05f, 0.03f, 0.02f, 0.82f)).raycastTarget = true;

        Text("Titulo", panel, title, 72f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1400f, 90f)).color = titleColor;
        Text("Subtitulo", panel, subtitle, 28f, FontStyles.Italic, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 40f));
        stats = Text("Estadisticas", panel, "", 24f, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 75f), new Vector2(1400f, 34f));

        for (int i = 0; i < actions.Length; i++)
        {
            RectTransform buttonRect = Rect("Boton_" + actions[i].label, panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f - i * 80f), new Vector2(340f, 62f));
            Image background = Img(buttonRect, new Color(0.35f, 0.22f, 0.12f, 0.95f));
            background.raycastTarget = true;
            Button button = buttonRect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
            colors.pressedColor = new Color(0.8f, 0.6f, 0.4f);
            button.colors = colors;
            Text("Texto", buttonRect, actions[i].label, 30f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 62f));
            buttons.Add((button, actions[i].action));
        }

        panel.gameObject.SetActive(false);
        return panel.gameObject;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.transform.SetAsLastSibling();
    }

    private static void SetupLighting()
    {
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type != LightType.Directional) continue;
            light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            light.color = new Color(1f, 0.92f, 0.8f);
            light.intensity = 1.5f;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.86f, 0.78f, 0.64f);
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 260f;
    }

    private static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(s => s.path != TargetScene).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(TargetScene, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static LockedDoor Door(string name, Transform parent, Vector3 hingePosition, Vector3 alongDirection, float width, float height, float openAngle, bool startsLocked, bool playerCanOpen, Material material)
    {
        GameObject doorRoot = new GameObject(name);
        doorRoot.transform.SetParent(parent);
        doorRoot.transform.position = hingePosition;
        doorRoot.transform.rotation = Quaternion.LookRotation(alongDirection);

        Transform hinge = new GameObject("Bisagra").transform;
        hinge.SetParent(doorRoot.transform, false);
        Box("Hoja", hinge, Vector3.zero, new Vector3(0.18f, height, width), material);
        Transform panel = hinge.Find("Hoja");
        panel.localPosition = new Vector3(0f, height * 0.5f, width * 0.5f);
        panel.localRotation = Quaternion.identity;
        Box("Refuerzo", hinge, Vector3.zero, new Vector3(0.22f, 0.2f, width * 0.95f), metal, false);
        hinge.Find("Refuerzo").localPosition = new Vector3(0f, height * 0.7f, width * 0.5f);

        NavMeshObstacle obstacle = hinge.gameObject.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = new Vector3(0f, height * 0.5f, width * 0.5f);
        obstacle.size = new Vector3(0.4f, height, width);
        obstacle.carving = true;
        doorRoot.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        LockedDoor door = doorRoot.AddComponent<LockedDoor>();
        SerializedObject serialized = new SerializedObject(door);
        serialized.FindProperty("hinge").objectReferenceValue = hinge;
        serialized.FindProperty("obstacle").objectReferenceValue = obstacle;
        serialized.FindProperty("openAngle").floatValue = openAngle;
        serialized.FindProperty("startsLocked").boolValue = startsLocked;
        serialized.FindProperty("playerCanOpen").boolValue = playerCanOpen;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return door;
    }

    private static void WallX(string name, Transform parent, float z, float x0, float x1, float height, float thickness, Material material, float doorHeight = 2.6f, params (float a, float b)[] openings)
    {
        Transform group = Group(name, parent);
        float cursor = x0;
        foreach ((float a, float b) in openings.OrderBy(o => o.a))
        {
            if (a > cursor) Box("Tramo", group, new Vector3((cursor + a) * 0.5f, height * 0.5f, z), new Vector3(a - cursor, height, thickness), material);
            if (height > doorHeight) Box("Dintel", group, new Vector3((a + b) * 0.5f, (doorHeight + height) * 0.5f, z), new Vector3(b - a, height - doorHeight, thickness), material);
            cursor = b;
        }
        if (x1 > cursor) Box("Tramo", group, new Vector3((cursor + x1) * 0.5f, height * 0.5f, z), new Vector3(x1 - cursor, height, thickness), material);
    }

    private static void WallZ(string name, Transform parent, float x, float z0, float z1, float height, float thickness, Material material, float doorHeight = 2.6f, params (float a, float b)[] openings)
    {
        Transform group = Group(name, parent);
        float cursor = z0;
        foreach ((float a, float b) in openings.OrderBy(o => o.a))
        {
            if (a > cursor) Box("Tramo", group, new Vector3(x, height * 0.5f, (cursor + a) * 0.5f), new Vector3(thickness, height, a - cursor), material);
            if (height > doorHeight) Box("Dintel", group, new Vector3(x, (doorHeight + height) * 0.5f, (a + b) * 0.5f), new Vector3(thickness, height - doorHeight, b - a), material);
            cursor = b;
        }
        if (z1 > cursor) Box("Tramo", group, new Vector3(x, height * 0.5f, (cursor + z1) * 0.5f), new Vector3(thickness, height, z1 - cursor), material);
    }

    private static void Building(string name, Transform parent, Vector2 min, Vector2 max, float height, Material material, params (char side, float a, float b)[] doors)
    {
        Transform group = Group(name, parent);
        const float t = 0.4f;
        (float, float)[] Openings(char side) => doors.Where(d => d.side == side).Select(d => (d.a, d.b)).ToArray();

        WallX("Pared_Sur", group, min.y + t * 0.5f, min.x, max.x, height, t, material, 2.6f, Openings('S'));
        WallX("Pared_Norte", group, max.y - t * 0.5f, min.x, max.x, height, t, material, 2.6f, Openings('N'));
        WallZ("Pared_Oeste", group, min.x + t * 0.5f, min.y + t, max.y - t, height, t, material, 2.6f, Openings('W'));
        WallZ("Pared_Este", group, max.x - t * 0.5f, min.y + t, max.y - t, height, t, material, 2.6f, Openings('E'));

        Vector3 center = new Vector3((min.x + max.x) * 0.5f, height + 0.15f, (min.y + max.y) * 0.5f);
        Box("Techo", group, center, new Vector3(max.x - min.x + 0.3f, 0.3f, max.y - min.y + 0.3f), roofMat);
        Box("Cornisa", group, center + Vector3.up * 0.35f, new Vector3(max.x - min.x + 0.3f, 0.4f, 0.25f), material).transform.position += new Vector3(0f, 0f, (max.y - min.y) * 0.5f);
    }

    private static void Porch(string name, Transform parent, Vector2 min, Vector2 max, float height, char openSide)
    {
        Transform group = Group(name, parent);
        Vector3 center = new Vector3((min.x + max.x) * 0.5f, height, (min.y + max.y) * 0.5f);
        Box("Techo", group, center, new Vector3(max.x - min.x, 0.15f, max.y - min.y), roofMat);

        bool alongX = openSide == 'N' || openSide == 'S';
        float fixedCoordinate = openSide switch
        {
            'N' => max.y - 0.15f,
            'S' => min.y + 0.15f,
            'E' => max.x - 0.15f,
            _ => min.x + 0.15f
        };
        float start = alongX ? min.x : min.y;
        float end = alongX ? max.x : max.y;
        int posts = Mathf.Max(2, Mathf.CeilToInt((end - start) / 4f) + 1);
        for (int i = 0; i < posts; i++)
        {
            float c = Mathf.Lerp(start + 0.15f, end - 0.15f, i / (float)(posts - 1));
            Vector3 position = alongX ? new Vector3(c, height * 0.5f, fixedCoordinate) : new Vector3(fixedCoordinate, height * 0.5f, c);
            Box("Columna", group, position, new Vector3(0.25f, height, 0.25f), woodDark);
        }
    }

    private static void Stairs(string name, Transform parent, Vector3 bottom, Vector3 direction, float width, float rise, Material material)
    {
        Transform group = Group(name, parent);
        int steps = Mathf.CeilToInt(rise / 0.25f);
        float stepHeight = rise / steps;
        const float run = 0.35f;
        Quaternion rotation = Quaternion.LookRotation(direction);

        for (int i = 0; i < steps; i++)
        {
            float h = stepHeight * (i + 1);
            Vector3 position = bottom + direction * (run * i + run * 0.5f) + Vector3.up * (h * 0.5f);
            GameObject step = Box("Escalon_" + i, group, position, new Vector3(width, h, run), material);
            step.transform.rotation = rotation;
        }
    }

    private static void SniperTower(string name, Transform parent, Vector3 basePosition, float height)
    {
        Transform group = Group(name, parent);
        Box("Cuerpo", group, basePosition + Vector3.up * (height * 0.5f), new Vector3(5f, height, 5f), adobeDark);
        Box("Parapeto_N", group, basePosition + new Vector3(0f, height + 0.4f, 2.4f), new Vector3(5f, 0.8f, 0.2f), adobeDark);
        Box("Parapeto_S", group, basePosition + new Vector3(0f, height + 0.4f, -2.4f), new Vector3(5f, 0.8f, 0.2f), adobeDark);
        Box("Parapeto_E", group, basePosition + new Vector3(2.4f, height + 0.4f, 0f), new Vector3(0.2f, 0.8f, 4.6f), adobeDark);
        Box("Parapeto_O", group, basePosition + new Vector3(-2.4f, height + 0.4f, 0f), new Vector3(0.2f, 0.8f, 4.6f), adobeDark);
    }

    private static void WaterTower(Transform parent, Vector3 position)
    {
        Transform group = Group("Tanque_Agua", parent);
        for (int i = 0; i < 4; i++)
        {
            Vector3 offset = new Vector3(i % 2 == 0 ? -1.2f : 1.2f, 2f, i < 2 ? -1.2f : 1.2f);
            Box("Pata_" + i, group, position + offset, new Vector3(0.3f, 4f, 0.3f), woodDark);
        }
        Box("Base", group, position + Vector3.up * 4.1f, new Vector3(3.2f, 0.2f, 3.2f), woodDark);
        GameObject tank = Cylinder("Tanque", group, position + Vector3.up * 5.2f, new Vector3(2.8f, 1f, 2.8f), wood);
        tank.name = "Tanque";
    }

    private static void LowWall(Transform parent, Vector3 position, float length, float yaw)
    {
        GameObject wall = Box("Muro_Bajo", parent, position + Vector3.up * 0.55f, new Vector3(length, 1.1f, 0.5f), adobe);
        wall.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private static void Crate(Transform parent, Vector3 position, float size, float yaw)
    {
        GameObject crate = Box("Cajon", parent, position + Vector3.up * (size * 0.5f), Vector3.one * size, wood);
        crate.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private static void CrateStack(Transform parent, Vector3 position, int count)
    {
        Crate(parent, position, 1.1f, 0f);
        if (count > 1) Crate(parent, position + new Vector3(1.15f, 0f, 0.1f), 1.1f, 8f);
        if (count > 2) Crate(parent, position + new Vector3(0.55f, 1.1f, 0.05f), 1f, 20f);
    }

    private static void Barrel(Transform parent, Vector3 position)
    {
        Cylinder("Barril", parent, position + Vector3.up * 0.5f, new Vector3(0.6f, 0.5f, 0.6f), woodDark);
    }

    private static void Trough(Transform parent, Vector3 position, float yaw)
    {
        Transform group = Group("Abrevadero", parent);
        GameObject body = Box("Cuerpo", group, position + Vector3.up * 0.4f, new Vector3(3f, 0.8f, 1f), woodDark);
        body.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        GameObject surface = Box("Agua", group, position + Vector3.up * 0.81f, new Vector3(2.8f, 0.02f, 0.8f), water, false);
        surface.transform.rotation = body.transform.rotation;
    }

    private static void Wagon(Transform parent, Vector3 position, float yaw, bool overturned)
    {
        Transform group = Group(overturned ? "Carreta_Volcada" : "Carreta", parent);
        group.position = position;
        group.rotation = Quaternion.Euler(0f, yaw, overturned ? 80f : 0f);

        Box("Caja", group, Vector3.zero, new Vector3(2f, 0.9f, 3.6f), wood);
        group.Find("Caja").localPosition = new Vector3(0f, 1.1f, 0f);
        for (int i = 0; i < 4; i++)
        {
            Vector3 local = new Vector3(i % 2 == 0 ? -1.1f : 1.1f, 0.6f, i < 2 ? -1.2f : 1.2f);
            GameObject wheel = Cylinder("Rueda_" + i, group, Vector3.zero, new Vector3(1.2f, 0.05f, 1.2f), woodDark, false);
            wheel.transform.localPosition = local;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        if (overturned) group.position = position + Vector3.up * 1.1f;
    }

    private static void Rock(Transform parent, Vector3 position, float size)
    {
        GameObject rockObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rockObject.name = "Roca";
        rockObject.transform.SetParent(parent);
        rockObject.transform.position = position + Vector3.up * size * 0.25f;
        rockObject.transform.localScale = new Vector3(size, size * 0.7f, size * 0.85f);
        rockObject.transform.rotation = Quaternion.Euler(0f, size * 37f, 0f);
        rockObject.GetComponent<MeshRenderer>().sharedMaterial = rock;
    }

    private static void Cactus(Transform parent, Vector3 position)
    {
        Transform group = Group("Cactus", parent);
        Cylinder("Tronco", group, position + Vector3.up * 1.4f, new Vector3(0.45f, 1.4f, 0.45f), cactus);
        Cylinder("Brazo_A", group, position + new Vector3(0.45f, 1.7f, 0f), new Vector3(0.3f, 0.5f, 0.3f), cactus, false);
        Cylinder("Brazo_B", group, position + new Vector3(-0.4f, 1.3f, 0f), new Vector3(0.28f, 0.4f, 0.28f), cactus, false);
    }

    private static void InvisibleWall(Transform parent, Vector3 center, Vector3 size)
    {
        GameObject wall = Box("Limite", parent, center, size, sand);
        wall.GetComponent<MeshRenderer>().enabled = false;
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

    private static Transform Marker(string name, Transform parent, Vector3 position)
    {
        Transform marker = new GameObject(name).transform;
        marker.SetParent(parent);
        marker.position = position;
        return marker;
    }

    private static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material material, bool collider = true)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        if (!collider) Object.DestroyImmediate(box.GetComponent<Collider>());
        box.transform.SetParent(parent, false);
        box.transform.position = center;
        box.transform.localScale = size;
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
        return box;
    }

    private static GameObject LocalBox(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material)
    {
        GameObject box = Box(name, parent, Vector3.zero, size, material, false);
        box.transform.localPosition = localPosition;
        box.transform.localRotation = Quaternion.identity;
        box.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        return box;
    }

    private static GameObject Cylinder(string name, Transform parent, Vector3 center, Vector3 scale, Material material, bool collider = true)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        if (!collider) Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.position = center;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;
        return cylinder;
    }

    private static Transform Group(string name, Transform parent = null)
    {
        Transform group = new GameObject(name).transform;
        if (parent != null) group.SetParent(parent, false);
        return group;
    }

    private static void Put(GameObject prefab, Transform parent, Vector3 position)
    {
        if (prefab == null) return;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.position = position;
    }

    private static GameObject LoadPickup(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Pickups/" + name + ".prefab");
    }

    private static void SetMaterial(Transform visual, string partName, Material material)
    {
        Transform part = visual.Find(partName);
        if (part != null) part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static EnemyDataSO GetOrCreateData(string name, System.Action<EnemyDataSO> configure)
    {
        string path = EnemyDataFolder + "/" + name + ".asset";
        EnemyDataSO data = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(path);
        if (data != null) return data;

        data = ScriptableObject.CreateInstance<EnemyDataSO>();
        configure(data);
        data.name = name;
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static Material Mat(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.1f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
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

    private static void SetBool(Object target, string property, bool value)
    {
        if (target == null) return;
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty prop = serialized.FindProperty(property);
        if (prop == null) return;
        prop.boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
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

    private static Image Img(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Text(string name, Transform parent, string content, float size, FontStyles style, TextAlignmentOptions alignment, Vector2 anchor, Vector2 position, Vector2 box)
    {
        RectTransform rect = Rect(name, parent, anchor, anchor, anchor, position, box);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
