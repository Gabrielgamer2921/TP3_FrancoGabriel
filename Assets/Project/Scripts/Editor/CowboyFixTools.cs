using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CowboyFixTools
{
    private const string Root = "Assets/Project";

    [MenuItem("Cowboy FPS/Corregir límites, muros y escopeta")]
    public static void ApplyFixes()
    {
        GameObject level = GameObject.Find("FortMercer");
        GameObject player = GameObject.FindWithTag("Player");
        if (level == null || player == null)
        {
            EditorUtility.DisplayDialog("Abrí FortMercer", "Este menú se aplica sobre la escena FortMercer.", "OK");
            return;
        }

        RemoveGalleryStairs(level.transform);
        AddWallBlockers(level.transform);
        BuildMapBounds(level.transform);
        MoveRoadReinforcements();
        RebuildShotgun(player);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Correcciones aplicadas",
            "Escalera eliminada, muros bloqueados, límites del mapa y escopeta rehecha.\n\nÚltimo paso: seleccioná 'NavMesh', apretá 'Bake' y guardá con Ctrl+S.", "OK");
    }

    private static void RemoveGalleryStairs(Transform level)
    {
        Transform stairs = level.Find("Rutas_Alternativas/Ruta_Techos_Galeria");
        if (stairs != null) Object.DestroyImmediate(stairs.gameObject);
    }

    private static void AddWallBlockers(Transform level)
    {
        Transform existing = level.Find("Bloqueos_Muros");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        Transform group = new GameObject("Bloqueos_Muros").transform;
        group.SetParent(level);

        const float outerTop = 5f;
        const float innerTop = 4.5f;
        const float extra = 6f;

        Blocker(group, "Muralla_Sur_Oeste", new Vector3(-25f, outerTop + extra * 0.5f, -30f), new Vector3(40f, extra, 1.4f));
        Blocker(group, "Muralla_Sur_Este", new Vector3(25f, outerTop + extra * 0.5f, -30f), new Vector3(40f, extra, 1.4f));
        Blocker(group, "Muralla_Norte", new Vector3(0f, outerTop + extra * 0.5f, 30f), new Vector3(92f, extra, 1.4f));
        Blocker(group, "Muralla_Oeste", new Vector3(-45f, outerTop + extra * 0.5f, 0f), new Vector3(1.4f, extra, 62f));
        Blocker(group, "Muralla_Este", new Vector3(45f, outerTop + extra * 0.5f, 0f), new Vector3(1.4f, extra, 62f));

        Blocker(group, "Patio_Oeste", new Vector3(-18f, innerTop + extra * 0.5f, 6f), new Vector3(1.2f, extra, 29f));
        Blocker(group, "Patio_Este", new Vector3(18f, innerTop + extra * 0.5f, 6f), new Vector3(1.2f, extra, 29f));
        Blocker(group, "Patio_Norte", new Vector3(0f, innerTop + extra * 0.5f, 20f), new Vector3(37f, extra, 1.2f));
        Blocker(group, "Patio_Sur", new Vector3(0f, innerTop + extra * 0.5f, -8f), new Vector3(37f, extra, 1.2f));
    }

    private static void BuildMapBounds(Transform level)
    {
        Transform oldBounds = level.Find("Exterior/Limites");
        if (oldBounds != null) Object.DestroyImmediate(oldBounds.gameObject);
        Transform existing = level.Find("Limites_Mapa");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        Transform group = new GameObject("Limites_Mapa").transform;
        group.SetParent(level);
        Material rock = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/FortMercer/Mat_Roca.mat");

        Bound(group, rock, new Vector3(-22f, 0f, -84f), new Vector3(-22f, 0f, -30f));
        Bound(group, rock, new Vector3(-22f, 0f, -84f), new Vector3(22f, 0f, -84f));
        Bound(group, rock, new Vector3(22f, 0f, -84f), new Vector3(22f, 0f, -62f));
        Bound(group, rock, new Vector3(22f, 0f, -62f), new Vector3(62f, 0f, -62f));
        Bound(group, rock, new Vector3(62f, 0f, -62f), new Vector3(62f, 0f, 14f));
        Bound(group, rock, new Vector3(45.6f, 0f, 14f), new Vector3(62f, 0f, 14f));
    }

    private static void Bound(Transform parent, Material rock, Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float length = delta.magnitude;
        Vector3 center = (from + to) * 0.5f;

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Limite";
        wall.transform.SetParent(parent);
        wall.transform.position = center + Vector3.up * 6f;
        wall.transform.rotation = Quaternion.LookRotation(delta.normalized);
        wall.transform.localScale = new Vector3(1f, 12f, length + 1f);
        wall.GetComponent<MeshRenderer>().enabled = false;

        int count = Mathf.Max(2, Mathf.RoundToInt(length / 6f));
        for (int i = 0; i <= count; i++)
        {
            Vector3 position = Vector3.Lerp(from, to, i / (float)count);
            float size = 2.2f + Mathf.PerlinNoise(position.x * 0.3f, position.z * 0.3f) * 2.5f;
            GameObject rockObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rockObject.name = "Roca_Limite";
            rockObject.transform.SetParent(parent);
            rockObject.transform.position = position + Vector3.up * size * 0.25f;
            rockObject.transform.localScale = new Vector3(size, size * 0.75f, size * 0.9f);
            rockObject.transform.rotation = Quaternion.Euler(0f, position.x * 13f + position.z * 7f, 0f);
            if (rock != null) rockObject.GetComponent<MeshRenderer>().sharedMaterial = rock;
        }
    }

    private static void MoveRoadReinforcements()
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "Camino_Refuerzo_1") t.position = new Vector3(-18f, 0f, -72f);
            else if (t.name == "Camino_Refuerzo_2") t.position = new Vector3(18f, 0f, -72f);
        }
    }

    private static void RebuildShotgun(GameObject player)
    {
        Camera camera = player.GetComponentInChildren<Camera>(true);
        Transform gun = camera.transform.Find("WeaponHolder/Escopeta_Holliday");
        if (gun == null) return;

        Transform model = gun.Find("Modelo");
        for (int i = model.childCount - 1; i >= 0; i--) Object.DestroyImmediate(model.GetChild(i).gameObject);

        Material metal = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Armas/Mat_Arma_Metal.mat");
        Material wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Armas/Mat_Arma_Madera.mat");
        Material silver = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Armas/Mat_Arma_Plata.mat");
        if (silver == null)
        {
            silver = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            silver.SetColor("_BaseColor", new Color(0.72f, 0.72f, 0.74f));
            silver.SetFloat("_Metallic", 0.8f);
            silver.SetFloat("_Smoothness", 0.55f);
            AssetDatabase.CreateAsset(silver, Root + "/Materials/Armas/Mat_Arma_Plata.mat");
        }

        Part("Cano_Izquierdo", model, new Vector3(-0.012f, 0.03f, 0.39f), new Vector3(0.022f, 0.022f, 0.54f), metal, 0f);
        Part("Cano_Derecho", model, new Vector3(0.012f, 0.03f, 0.39f), new Vector3(0.022f, 0.022f, 0.54f), metal, 0f);
        Part("Banda", model, new Vector3(0f, 0.0425f, 0.39f), new Vector3(0.01f, 0.006f, 0.54f), metal, 0f);
        Part("Punto_Mira", model, new Vector3(0f, 0.049f, 0.65f), new Vector3(0.006f, 0.008f, 0.006f), silver, 0f);
        Part("Cajon", model, new Vector3(0f, 0.015f, 0.04f), new Vector3(0.05f, 0.055f, 0.16f), silver, 0f);
        Part("Martillo_Izquierdo", model, new Vector3(-0.028f, 0.048f, -0.01f), new Vector3(0.012f, 0.03f, 0.02f), silver, -25f);
        Part("Martillo_Derecho", model, new Vector3(0.028f, 0.048f, -0.01f), new Vector3(0.012f, 0.03f, 0.02f), silver, -25f);
        Part("Guardamano", model, new Vector3(0f, 0.006f, 0.22f), new Vector3(0.05f, 0.028f, 0.16f), wood, 0f);
        Part("Guardamonte", model, new Vector3(0f, -0.025f, 0f), new Vector3(0.01f, 0.025f, 0.05f), metal, 0f);
        Part("Culata", model, new Vector3(0f, -0.06f, -0.15f), new Vector3(0.045f, 0.075f, 0.26f), wood, -18f);

        Transform muzzle = gun.Find("Muzzle");
        if (muzzle != null) muzzle.localPosition = new Vector3(0f, 0.03f, 0.67f);
        Transform aimPoint = gun.Find("AimPoint");
        if (aimPoint != null) aimPoint.localPosition = new Vector3(0f, 0.05f, 0.08f);

        gun.localPosition = new Vector3(0.17f, -0.2f, 0.32f);
        gun.localRotation = Quaternion.identity;

        HitscanWeapon weapon = gun.GetComponent<HitscanWeapon>();
        SerializedObject serialized = new SerializedObject(weapon);
        serialized.FindProperty("aimDistanceFromCamera").floatValue = 0.3f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material, float pitch)
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

    private static void Blocker(Transform parent, string name, Vector3 center, Vector3 size)
    {
        GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.name = "Bloqueo_" + name;
        blocker.transform.SetParent(parent);
        blocker.transform.position = center;
        blocker.transform.localScale = size;
        blocker.GetComponent<MeshRenderer>().enabled = false;
    }
}
