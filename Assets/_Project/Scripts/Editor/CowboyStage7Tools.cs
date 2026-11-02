using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class CowboyStage7Tools
{
    private const string Root = "Assets/_Project";
    private const string AudioFolder = Root + "/Audio/Generados";
    private const string SoundFolder = Root + "/Data/Sonidos";
    private const string VoiceFolder = Root + "/Data/Voces";
    private const string FxFolder = Root + "/Prefabs/Efectos";
    private const string UIFolder = Root + "/Materials/UI";

    private static readonly Dictionary<string, SoundSO> Sounds = new Dictionary<string, SoundSO>();

    [MenuItem("Cowboy FPS/Configurar Etapa 7 (sonido, efectos e intro)")]
    public static void SetupStage7()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Falta el Player", "Abrí la escena del juego (FortMercer) antes de ejecutar este menú.", "OK");
            return;
        }

        EnsureFolder(SoundFolder);
        EnsureFolder(VoiceFolder);
        EnsureFolder(FxFolder);
        EnsureFolder(UIFolder);

        CreateSounds();
        ParticleSystem dust = CreateBurstPrefab("FX_Impacto_Polvo", new Color(0.78f, 0.68f, 0.52f), new Color(0.6f, 0.5f, 0.38f), 12, 0.6f, 2.6f, 0.08f, 0.28f, 0.35f, 0.8f, 0.25f, 35f);
        ParticleSystem blood = CreateBurstPrefab("FX_Impacto_Sangre", new Color(0.5f, 0.05f, 0.04f), new Color(0.3f, 0.02f, 0.02f), 14, 1f, 3.2f, 0.05f, 0.16f, 0.3f, 0.6f, 1.2f, 30f);
        Sprite flashSprite = AssetDatabase.LoadAssetAtPath<Sprite>(UIFolder + "/Glint.png");

        SetupPlayerWeapons(player, flashSprite);
        SetupPlayer(player);
        SetupEnemyPrefabs(flashSprite);
        SetupDynamite();
        SetupExplosionVfx();
        SetupWorld(dust, blood);
        SetupInteractables();
        SetupHud(player);
        SetupPostProcessing(player);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Etapa 7 lista", "Sonido, efectos, feedback e intro configurados.\n\nGuardá la escena con Ctrl+S.", "OK");
    }

    private static void CreateSounds()
    {
        Sounds.Clear();
        Sound("Jugador_Revolver_Disparo", 0.9f, 0.95f, 1.05f, 0f, "Armas/Revolver_Disparo_1", "Armas/Revolver_Disparo_2", "Armas/Revolver_Disparo_3");
        Sound("Jugador_Winchester_Disparo", 1f, 0.95f, 1.03f, 0f, "Armas/Winchester_Disparo_1", "Armas/Winchester_Disparo_2", "Armas/Winchester_Disparo_3");
        Sound("Revolver_Martillo", 0.6f, 0.95f, 1.05f, 0f, "Armas/Revolver_Martillo");
        Sound("Winchester_Palanca", 0.7f, 0.95f, 1.05f, 0f, "Armas/Winchester_Palanca");
        Sound("Recarga_Bala", 0.55f, 0.92f, 1.08f, 0f, "Armas/Recarga_Bala");
        Sound("Recarga_Abrir", 0.55f, 0.95f, 1.05f, 0f, "Armas/Recarga_Abrir");
        Sound("Recarga_Cerrar", 0.55f, 0.95f, 1.05f, 0f, "Armas/Recarga_Cerrar");
        Sound("Arma_Vacia", 0.6f, 0.95f, 1.05f, 0f, "Armas/Arma_Vacia");
        Sound("Culatazo_Swing", 0.5f, 0.9f, 1.1f, 0f, "Armas/Culatazo_Swing");
        Sound("Culatazo_Golpe", 0.8f, 0.9f, 1.1f, 1f, "Armas/Culatazo_Golpe");

        Sound("Enemigo_Revolver_Disparo", 0.85f, 0.9f, 1.1f, 1f, "Armas/Revolver_Disparo_1", "Armas/Revolver_Disparo_2", "Armas/Revolver_Disparo_3").maxDistance = 80f;
        Sound("Enemigo_Rifle_Disparo", 0.9f, 0.9f, 1.05f, 1f, "Armas/Winchester_Disparo_1", "Armas/Winchester_Disparo_2", "Armas/Winchester_Disparo_3").maxDistance = 100f;
        Sound("Enemigo_Escopeta_Disparo", 1f, 0.92f, 1.05f, 1f, "Armas/Escopeta_Disparo_1", "Armas/Escopeta_Disparo_2").maxDistance = 90f;
        Sound("Enemigo_Recarga", 0.6f, 0.9f, 1.1f, 1f, "Armas/Recarga_Abrir", "Armas/Recarga_Bala");

        SoundSO fuse = Sound("Mecha_Loop", 0.35f, 1f, 1f, 1f, "Explosivos/Mecha_Loop");
        fuse.maxDistance = 15f;
        Sound("Fosforo", 0.6f, 0.95f, 1.05f, 0f, "Explosivos/Fosforo");
        Sound("Lanzar", 0.5f, 0.9f, 1.1f, 0f, "Explosivos/Lanzar");
        Sound("Explosion", 1f, 0.9f, 1.05f, 1f, "Explosivos/Explosion_1", "Explosivos/Explosion_2").maxDistance = 150f;
        Sound("Madera_Rota", 0.9f, 0.95f, 1.05f, 1f, "Explosivos/Madera_Rota");

        Sound("Campana_Alarma", 1f, 1f, 1f, 1f, "Mundo/Campana_Alarma").maxDistance = 120f;
        Sound("Puerta_Abrir", 0.8f, 0.95f, 1.05f, 1f, "Mundo/Puerta_Abrir");
        Sound("Puerta_Cerrar", 0.9f, 0.95f, 1.05f, 1f, "Mundo/Puerta_Cerrar");
        Sound("Puerta_Trabada", 0.7f, 0.95f, 1.05f, 1f, "Mundo/Puerta_Trabada");
        Sound("Viento_Loop", 0.12f, 1f, 1f, 0f, "Mundo/Viento_Loop");

        Sound("Paso_Arena", 0.22f, 0.9f, 1.1f, 0f, "Jugador/Paso_Arena_1", "Jugador/Paso_Arena_2", "Jugador/Paso_Arena_3", "Jugador/Paso_Arena_4", "Jugador/Paso_Arena_5");
        Sound("Aterrizaje", 0.4f, 0.9f, 1.05f, 0f, "Jugador/Aterrizaje");
        Sound("Vendaje", 0.6f, 0.95f, 1.05f, 0f, "Jugador/Vendaje");
        Sound("Dano_Recibido", 0.7f, 0.9f, 1.1f, 0f, "Jugador/Dano_Recibido").cooldown = 0.15f;
        Sound("Latido_Loop", 1f, 1f, 1f, 0f, "Jugador/Latido_Loop");

        Sound("Impacto_Tierra", 0.5f, 0.85f, 1.15f, 1f, "Impactos/Tierra_1", "Impactos/Tierra_2", "Impactos/Tierra_3").maxDistance = 30f;
        Sound("Impacto_Madera", 0.55f, 0.85f, 1.15f, 1f, "Impactos/Madera_1", "Impactos/Madera_2").maxDistance = 30f;
        Sound("Impacto_Carne", 0.65f, 0.85f, 1.15f, 1f, "Impactos/Carne_1", "Impactos/Carne_2", "Impactos/Carne_3").maxDistance = 30f;

        Sound("Pickup_Municion", 0.6f, 0.95f, 1.05f, 0f, "UI/Pickup_Municion");
        Sound("Pickup_Objeto", 0.6f, 0.95f, 1.05f, 0f, "UI/Pickup_Objeto");
        Sound("Pickup_Llave", 0.7f, 1f, 1f, 0f, "UI/Pickup_Llave");
        Sound("Hitmarker", 0.35f, 0.95f, 1.05f, 0f, "UI/Hitmarker");
        Sound("Headshot", 0.45f, 1f, 1f, 0f, "UI/Headshot");
        Sound("Kill", 0.5f, 1f, 1f, 0f, "UI/Kill");
    }

    private static SoundSO Sound(string name, float volume, float pitchMin, float pitchMax, float spatial, params string[] clipNames)
    {
        string path = SoundFolder + "/" + name + ".asset";
        SoundSO sound = AssetDatabase.LoadAssetAtPath<SoundSO>(path);
        bool created = sound == null;
        if (created) sound = ScriptableObject.CreateInstance<SoundSO>();

        if (created)
        {
            sound.clips = clipNames.Select(c => AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/" + c + ".wav")).Where(c => c != null).ToArray();
            sound.volume = volume;
            sound.pitchMin = pitchMin;
            sound.pitchMax = pitchMax;
            sound.spatialBlend = spatial;
            AssetDatabase.CreateAsset(sound, path);
        }

        EditorUtility.SetDirty(sound);
        Sounds[name] = sound;
        return sound;
    }

    private static SoundSO S(string name) => Sounds.TryGetValue(name, out SoundSO sound) ? sound : null;

    private static void SetupPlayerWeapons(GameObject player, Sprite flashSprite)
    {
        foreach (Weapon weapon in player.GetComponentsInChildren<Weapon>(true))
        {
            WeaponAudio audio = GetOrAdd<WeaponAudio>(weapon.gameObject);
            bool rifle = weapon is ProjectileWeapon;
            Wire(audio,
                ("weapon", weapon),
                ("fire", S(rifle ? "Jugador_Winchester_Disparo" : "Jugador_Revolver_Disparo")),
                ("cycle", S(rifle ? "Winchester_Palanca" : "Revolver_Martillo")),
                ("dryFire", S("Arma_Vacia")),
                ("reloadStart", S("Recarga_Abrir")),
                ("roundInserted", S("Recarga_Bala")),
                ("reloadEnd", S("Recarga_Cerrar")));
            SetFloat(audio, "cycleDelay", rifle ? 0.3f : 0.18f);

            if (weapon.Muzzle != null && weapon.Muzzle.GetComponentInChildren<MuzzleFlash>(true) == null)
                AddMuzzleFlash(weapon.Muzzle, flashSprite, weapon, null, 0.12f, false);
        }
    }

    private static void AddMuzzleFlash(Transform muzzle, Sprite sprite, Weapon playerWeapon, EnemyWeapon enemyWeapon, float size, bool shadows)
    {
        GameObject flash = new GameObject("Fogonazo");
        flash.transform.SetParent(muzzle, false);
        flash.transform.localScale = Vector3.one * size;
        SpriteRenderer spriteRenderer = flash.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        spriteRenderer.color = new Color(1f, 0.75f, 0.35f);
        spriteRenderer.sortingOrder = 50;

        Light light = flash.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.7f, 0.35f);
        light.range = 6f;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;

        MuzzleFlash muzzleFlash = flash.AddComponent<MuzzleFlash>();
        Wire(muzzleFlash, ("playerWeapon", playerWeapon), ("enemyWeapon", enemyWeapon), ("flashSprite", spriteRenderer), ("flashLight", light));
    }

    private static void SetupPlayer(GameObject player)
    {
        PlayerAudio audio = GetOrAdd<PlayerAudio>(player);
        Wire(audio,
            ("health", player.GetComponent<Health>()),
            ("bandages", player.GetComponent<PlayerBandages>()),
            ("thrower", player.GetComponent<PlayerThrowController>()),
            ("melee", player.GetComponent<PlayerMelee>()),
            ("hurt", S("Dano_Recibido")),
            ("bandage", S("Vendaje")),
            ("lightFuse", S("Fosforo")),
            ("throwSound", S("Lanzar")),
            ("meleeSwing", S("Culatazo_Swing")),
            ("meleeHit", S("Culatazo_Golpe")));

        PlayerFootsteps steps = GetOrAdd<PlayerFootsteps>(player);
        Wire(steps, ("motor", player.GetComponent<PlayerMotor>()), ("footstep", S("Paso_Arena")), ("landing", S("Aterrizaje")));

        Transform hand = player.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "DinamitaMano");
        if (hand != null) AddLoop(hand.gameObject, S("Mecha_Loop"), 0f);

        Transform heartbeatTransform = player.transform.Find("Latido");
        GameObject heartbeatObject = heartbeatTransform != null ? heartbeatTransform.gameObject : new GameObject("Latido");
        heartbeatObject.transform.SetParent(player.transform, false);
        AudioSource heartbeat = GetOrAdd<AudioSource>(heartbeatObject);
        heartbeat.clip = S("Latido_Loop") != null && S("Latido_Loop").HasClips ? S("Latido_Loop").clips[0] : null;
        heartbeat.playOnAwake = false;
        heartbeat.loop = true;
        heartbeat.spatialBlend = 0f;
        heartbeat.volume = 0f;

        LowHealthEffects lowHealth = GetOrAdd<LowHealthEffects>(player);
        Wire(lowHealth, ("health", player.GetComponent<Health>()), ("volume", Object.FindFirstObjectByType<Volume>()), ("heartbeat", heartbeat));
    }

    private static void SetupEnemyPrefabs(Sprite flashSprite)
    {
        EnemyVoiceSO pistoleroVoice = Voice("Voz_Pistolero", 1f);
        EnemyVoiceSO fusileroVoice = Voice("Voz_Fusilero", 0.92f);
        EnemyVoiceSO sniperVoice = Voice("Voz_Francotirador", 0.85f);
        EnemyVoiceSO bossVoice = Voice("Voz_DirtyHolliday", 0.8f);

        SetupEnemyPrefab(Root + "/Prefabs/Enemies/Enemy_Pistolero.prefab", S("Enemigo_Revolver_Disparo"), pistoleroVoice, flashSprite);
        SetupEnemyPrefab(Root + "/Prefabs/Enemies/Enemy_Fusilero.prefab", S("Enemigo_Rifle_Disparo"), fusileroVoice, flashSprite);
        SetupEnemyPrefab(Root + "/Prefabs/Enemies/Enemy_Francotirador.prefab", S("Enemigo_Rifle_Disparo"), sniperVoice, flashSprite);
        SetupEnemyPrefab(Root + "/Prefabs/FortMercer/Boss_DirtyHolliday.prefab", S("Enemigo_Escopeta_Disparo"), bossVoice, flashSprite);
    }

    private static EnemyVoiceSO Voice(string name, float pitch)
    {
        string path = VoiceFolder + "/" + name + ".asset";
        EnemyVoiceSO voice = AssetDatabase.LoadAssetAtPath<EnemyVoiceSO>(path);
        if (voice != null) return voice;
        voice = ScriptableObject.CreateInstance<EnemyVoiceSO>();
        voice.basePitch = pitch;
        AssetDatabase.CreateAsset(voice, path);
        return voice;
    }

    private static void SetupEnemyPrefab(string path, SoundSO fire, EnemyVoiceSO voice, Sprite flashSprite)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        EnemyWeapon weapon = root.GetComponent<EnemyWeapon>();
        EnemyAudio audio = root.GetComponent<EnemyAudio>();
        if (audio == null) audio = root.AddComponent<EnemyAudio>();
        Wire(audio, ("weapon", weapon), ("fire", fire), ("reload", S("Enemigo_Recarga")));

        if (root.GetComponent<AudioSource>() == null) root.AddComponent<AudioSource>();
        EnemyVoice enemyVoice = root.GetComponent<EnemyVoice>();
        if (enemyVoice == null) enemyVoice = root.AddComponent<EnemyVoice>();
        Wire(enemyVoice, ("voice", voice), ("ai", root.GetComponent<EnemyAI>()), ("weapon", weapon), ("health", root.GetComponent<Health>()));

        if (weapon != null && weapon.Muzzle != null && weapon.Muzzle.GetComponentInChildren<MuzzleFlash>(true) == null)
            AddMuzzleFlash(weapon.Muzzle, flashSprite, null, weapon, 0.35f, false);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void SetupDynamite()
    {
        string path = Root + "/Prefabs/Explosivos/Dinamita.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        AddLoop(root, S("Mecha_Loop"), 1f);
        if (root.transform.Find("Chispas") == null)
        {
            GameObject sparks = new GameObject("Chispas");
            sparks.transform.SetParent(root.transform, false);
            sparks.transform.localPosition = new Vector3(0f, 0f, 0.14f);
            ParticleSystem ps = ConfigureParticles(sparks, new Color(1f, 0.85f, 0.35f), new Color(1f, 0.5f, 0.15f), 0, 1f, 2.5f, 0.015f, 0.035f, 0.15f, 0.35f, 0.6f, 40f, true);
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 45f;
        }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void SetupExplosionVfx()
    {
        string path = Root + "/Prefabs/Explosivos/Explosion_VFX.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root.transform.Find("Escombros") == null)
        {
            GameObject debris = new GameObject("Escombros");
            debris.transform.SetParent(root.transform, false);
            debris.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ConfigureParticles(debris, new Color(0.45f, 0.35f, 0.25f), new Color(0.25f, 0.2f, 0.15f), 45, 6f, 14f, 0.06f, 0.18f, 1f, 2f, 1.6f, 70f, false);

            GameObject smoke = new GameObject("HumoParticulas");
            smoke.transform.SetParent(root.transform, false);
            smoke.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ConfigureParticles(smoke, new Color(0.35f, 0.32f, 0.3f, 0.7f), new Color(0.2f, 0.18f, 0.17f, 0.6f), 25, 1f, 3.5f, 1.5f, 3f, 1.5f, 3f, -0.05f, 60f, false);
        }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void SetupWorld(ParticleSystem dust, ParticleSystem blood)
    {
        GameObject audioRoot = GameObject.Find("Audio_Mundo");
        if (audioRoot == null) audioRoot = new GameObject("Audio_Mundo");

        GetOrAdd<AudioManager>(audioRoot);

        WorldAudio world = GetOrAdd<WorldAudio>(audioRoot);
        Wire(world, ("explosion", S("Explosion")), ("ammoPickup", S("Pickup_Municion")), ("itemPickup", S("Pickup_Objeto")), ("keyPickup", S("Pickup_Llave")));

        CombatImpactFX impacts = GetOrAdd<CombatImpactFX>(audioRoot);
        Wire(impacts, ("dustPrefab", dust), ("bloodPrefab", blood), ("dirtImpact", S("Impacto_Tierra")), ("woodImpact", S("Impacto_Madera")), ("fleshImpact", S("Impacto_Carne")));

        Transform ambience = audioRoot.transform.Find("Viento");
        GameObject wind = ambience != null ? ambience.gameObject : new GameObject("Viento");
        wind.transform.SetParent(audioRoot.transform, false);
        AddLoop(wind, S("Viento_Loop"), 1f);
    }

    private static void SetupInteractables()
    {
        foreach (LockedDoor door in Object.FindObjectsByType<LockedDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            InteractableAudio audio = GetOrAdd<InteractableAudio>(door.gameObject);
            Wire(audio, ("door", door), ("doorOpen", S("Puerta_Abrir")), ("doorClose", S("Puerta_Cerrar")), ("doorLocked", S("Puerta_Trabada")));
        }
        foreach (AlarmBell bell in Object.FindObjectsByType<AlarmBell>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            InteractableAudio audio = GetOrAdd<InteractableAudio>(bell.gameObject);
            Wire(audio, ("bell", bell), ("bellRing", S("Campana_Alarma")));
        }
        foreach (DestructibleBarrier barrier in Object.FindObjectsByType<DestructibleBarrier>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            InteractableAudio audio = GetOrAdd<InteractableAudio>(barrier.gameObject);
            Wire(audio, ("barrier", barrier), ("barrierBreak", S("Madera_Rota")));
        }
    }

    private static void SetupHud(GameObject player)
    {
        AmmoHUD ammoHud = Object.FindFirstObjectByType<AmmoHUD>(FindObjectsInactive.Include);
        if (ammoHud == null) return;
        Transform hud = ammoHud.transform;
        Camera camera = player.GetComponentInChildren<Camera>(true);

        if (Object.FindFirstObjectByType<HitMarkerUI>(FindObjectsInactive.Include) == null)
        {
            RectTransform marker = Rect("Hitmarker", hud, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
            CanvasGroup group = marker.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            List<Image> lines = new List<Image>();
            for (int i = 0; i < 4; i++)
            {
                float angle = 45f + i * 90f;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
                RectTransform line = Rect("Linea_" + i, marker, new Vector2(0.5f, 0.5f), direction * 13f, new Vector2(3f, 12f));
                line.localEulerAngles = new Vector3(0f, 0f, angle);
                Image image = line.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                lines.Add(image);
            }

            HitMarkerUI hitMarker = marker.gameObject.AddComponent<HitMarkerUI>();
            Wire(hitMarker, ("group", group), ("melee", player.GetComponent<PlayerMelee>()), ("hitSound", S("Hitmarker")), ("criticalSound", S("Headshot")), ("killSound", S("Kill")));
            SerializedObject serialized = new SerializedObject(hitMarker);
            SerializedProperty array = serialized.FindProperty("lines");
            array.arraySize = lines.Count;
            for (int i = 0; i < lines.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = lines[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        if (Object.FindFirstObjectByType<DamageDirectionUI>(FindObjectsInactive.Include) == null)
        {
            Sprite arc = GetOrCreateArcSprite();
            RectTransform container = Rect("DireccionDaño", hud, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 340f));
            List<Image> indicators = new List<Image>();
            for (int i = 0; i < 4; i++)
            {
                RectTransform indicator = Rect("Indicador_" + i, container, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 340f));
                Image image = indicator.gameObject.AddComponent<Image>();
                image.sprite = arc;
                image.color = new Color(0.85f, 0.1f, 0.05f, 0f);
                image.raycastTarget = false;
                indicators.Add(image);
            }

            DamageDirectionUI direction = container.gameObject.AddComponent<DamageDirectionUI>();
            Wire(direction, ("health", player.GetComponent<Health>()), ("player", player.transform), ("cameraTransform", camera.transform));
            SerializedObject serialized = new SerializedObject(direction);
            SerializedProperty array = serialized.FindProperty("indicators");
            array.arraySize = indicators.Count;
            for (int i = 0; i < indicators.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = indicators[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        if (Object.FindFirstObjectByType<IntroSequence>(FindObjectsInactive.Include) == null) BuildIntro(hud, player);
    }

    private static void BuildIntro(Transform hud, GameObject player)
    {
        RectTransform root = Stretch("Intro", hud);
        root.SetAsLastSibling();
        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.04f, 0.025f, 0.015f, 1f);
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();

        GameObject story = Card(root, "Historia", "UNA BALA POR CADA UNO",
            "Durante años cabalgaste con la banda. Robaste, mentiste y sobreviviste.\n\n" +
            "Hasta que encontraste algo por lo que valía la pena dejar todo. Tomaste el dinero de la banda y escapaste con tu esposo, lejos de las armas y del polvo.\n\n" +
            "El Líder se enteró. Cuando volviste a casa, tu esposo ya estaba muerto.\n\n" +
            "Ahora tus antiguos compañeros son lugartenientes, cada uno dueño de su propio territorio. El primero de la lista es <color=#E8B04A>Dirty Holliday</color>, atrincherado en <color=#E8B04A>Fort Mercer</color>.\n\n" +
            "<i>No vas a dejar a ninguno con vida.</i>");

        GameObject controls = Card(root, "Controles", "CONTROLES",
            "<color=#E8B04A>WASD</color>  Moverse          <color=#E8B04A>Mouse</color>  Mirar\n" +
            "<color=#E8B04A>Clic izq.</color>  Disparar          <color=#E8B04A>Clic der.</color>  Apuntar\n" +
            "<color=#E8B04A>R</color>  Recargar          <color=#E8B04A>1 / 2 / Rueda</color>  Cambiar arma\n" +
            "<color=#E8B04A>Shift</color>  Correr          <color=#E8B04A>Ctrl / C</color>  Agacharse\n" +
            "<color=#E8B04A>Espacio</color>  Saltar          <color=#E8B04A>V</color>  Culatazo\n" +
            "<color=#E8B04A>G</color> (mantener)  Dinamita          <color=#E8B04A>H</color>  Vendaje\n" +
            "<color=#E8B04A>E</color>  Interactuar          <color=#E8B04A>Esc</color>  Pausa\n\n" +
            "<size=80%><i>Mantené apretada la G para encender la mecha y soltala para tirar. Si la sostenés de más, te explota en la mano.</i></size>");
        controls.SetActive(false);

        TMP_Text hint = Text("Continuar", root, "Presioná ESPACIO o hacé clic para continuar", 24f, FontStyles.Italic, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1200f, 40f));
        hint.color = new Color(1f, 0.9f, 0.75f, 0f);

        IntroSequence intro = root.gameObject.AddComponent<IntroSequence>();
        Wire(intro, ("input", player.GetComponent<PlayerInputReader>()), ("look", player.GetComponent<PlayerLook>()), ("group", group), ("continueHint", hint));
        SerializedObject serialized = new SerializedObject(intro);
        SerializedProperty cards = serialized.FindProperty("cards");
        cards.arraySize = 2;
        cards.GetArrayElementAtIndex(0).objectReferenceValue = story;
        cards.GetArrayElementAtIndex(1).objectReferenceValue = controls;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject Card(Transform parent, string name, string title, string body)
    {
        RectTransform card = Stretch(name, parent);
        TMP_Text titleText = Text("Titulo", card, title, 64f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1400f, 90f));
        titleText.color = new Color(0.91f, 0.69f, 0.29f);
        titleText.characterSpacing = 6f;

        RectTransform line = Rect("Separador", card, new Vector2(0.5f, 0.5f), new Vector2(0f, 280f), new Vector2(500f, 3f));
        Image lineImage = line.gameObject.AddComponent<Image>();
        lineImage.color = new Color(0.91f, 0.69f, 0.29f, 0.6f);

        TMP_Text bodyText = Text("Cuerpo", card, body, 30f, FontStyles.Normal, TextAlignmentOptions.Top, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1300f, 540f));
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.lineSpacing = 8f;
        bodyText.color = new Color(0.95f, 0.9f, 0.82f);
        return card.gameObject;
    }

    private static void SetupPostProcessing(GameObject player)
    {
        Camera camera = player.GetComponentInChildren<Camera>(true);
        UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
        if (cameraData != null) cameraData.renderPostProcessing = true;

        Volume volume = Object.FindFirstObjectByType<Volume>();
        if (volume == null)
        {
            GameObject volumeObject = new GameObject("Global Volume");
            volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
        }
        if (volume.sharedProfile == null)
        {
            VolumeProfile created = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(created, Root + "/Data/PostProceso_Western.asset");
            volume.sharedProfile = created;
        }

        VolumeProfile profile = volume.sharedProfile;

        Tonemapping tonemapping = GetOverride<Tonemapping>(profile);
        tonemapping.mode.Override(TonemappingMode.ACES);

        ColorAdjustments color = GetOverride<ColorAdjustments>(profile);
        color.postExposure.Override(0.15f);
        color.contrast.Override(14f);
        color.colorFilter.Override(new Color(1f, 0.94f, 0.85f));
        color.saturation.Override(-6f);

        Vignette vignette = GetOverride<Vignette>(profile);
        vignette.intensity.Override(0.28f);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(new Color(0.15f, 0.07f, 0.03f));

        Bloom bloom = GetOverride<Bloom>(profile);
        bloom.intensity.Override(0.35f);
        bloom.threshold.Override(1.1f);

        FilmGrain grain = GetOverride<FilmGrain>(profile);
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.18f);

        EditorUtility.SetDirty(profile);
        Wire(Object.FindFirstObjectByType<LowHealthEffects>(), ("volume", volume));
    }

    private static T GetOverride<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
        }
        component.active = true;
        return component;
    }

    private static void AddLoop(GameObject target, SoundSO sound, float volumeMultiplier)
    {
        if (sound == null) return;
        AudioSource source = GetOrAdd<AudioSource>(target);
        source.playOnAwake = false;
        LoopingSound loop = GetOrAdd<LoopingSound>(target);
        Wire(loop, ("sound", sound));
        SetFloat(loop, "volumeMultiplier", volumeMultiplier > 0f ? volumeMultiplier : 0.25f);
    }

    private static ParticleSystem CreateBurstPrefab(string name, Color a, Color b, int count, float speedMin, float speedMax, float sizeMin, float sizeMax, float lifeMin, float lifeMax, float gravity, float cone)
    {
        string path = FxFolder + "/" + name + ".prefab";
        ParticleSystem existing = AssetDatabase.LoadAssetAtPath<ParticleSystem>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject(name);
        ParticleSystem ps = ConfigureParticles(root, a, b, count, speedMin, speedMax, sizeMin, sizeMax, lifeMin, lifeMax, gravity, cone, false);
        ParticleSystem.MainModule main = ps.main;
        main.stopAction = ParticleSystemStopAction.Destroy;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<ParticleSystem>();
    }

    private static ParticleSystem ConfigureParticles(GameObject go, Color a, Color b, int burst, float speedMin, float speedMax, float sizeMin, float sizeMax, float lifeMin, float lifeMax, float gravity, float cone, bool loop)
    {
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = loop;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        if (burst > 0) emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = cone;
        shape.radius = 0.05f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));

        ParticleSystemRenderer particleRenderer = go.GetComponent<ParticleSystemRenderer>();
        RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline != null && pipeline.defaultParticleMaterial != null) particleRenderer.sharedMaterial = pipeline.defaultParticleMaterial;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
        return ps;
    }

    private static Sprite GetOrCreateArcSprite()
    {
        string path = UIFolder + "/ArcoDaño.png";
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
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Abs(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(radius - 0.86f) / 0.07f);
                float arc = Mathf.Clamp01(1f - angle / 28f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, ring) * Mathf.SmoothStep(0f, 1f, arc)));
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

    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform Stretch(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TMP_Text Text(string name, Transform parent, string content, float size, FontStyles style, TextAlignmentOptions alignment, Vector2 anchor, Vector2 position, Vector2 box)
    {
        RectTransform rect = Rect(name, parent, anchor, position, box);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.richText = true;
        return text;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
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

    private static void SetFloat(Object target, string property, float value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty prop = serialized.FindProperty(property);
        if (prop == null) return;
        prop.floatValue = value;
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
