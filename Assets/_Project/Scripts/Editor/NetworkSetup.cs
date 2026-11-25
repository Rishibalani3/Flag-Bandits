// Editor script to set up the NetworkTest scene and configure the Player prefab
// for Photon Fusion networking. Run via: unity command script run --path Assets/_Project/Scripts/Editor/NetworkSetup.cs

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Fusion;
using FlagBandits.Gameplay;
using FlagBandits.UI;

public static class NetworkSetup
{
    private const string CtfSettingsPath = "Assets/_Project/Settings/CTFSettings.asset";
    private const string FlagBluePrefabPath = "Assets/_Project/Prefabs/Flag_Blue.prefab";
    private const string FlagRedPrefabPath = "Assets/_Project/Prefabs/Flag_Red.prefab";

    [MenuItem("Flag Bandits/Setup Network Scene")]
    public static void SetupNetworkScene()
    {
        // 1. Ensure CTF settings ScriptableObject exists.
        CTFSettings ctfSettings = EnsureCTFSettingsAsset();

        // 2. Configure the Player prefab and Flag prefabs with network components.
        SetupPlayerPrefab();
        NetworkObject flagBlue = CreateOrUpdateFlagPrefab(FlagBluePrefabPath, Team.TeamA, "Assets/_Project/Materials/FlagBlueMat.mat", new Color(0.2f, 0.5f, 1f), ctfSettings);
        NetworkObject flagRed = CreateOrUpdateFlagPrefab(FlagRedPrefabPath, Team.TeamB, "Assets/_Project/Materials/FlagRedMat.mat", new Color(0.95f, 0.25f, 0.25f), ctfSettings);

        // 3. Rebuild Fusion prefab dependency hash and config asset so newly created prefabs register cleanly.
        Fusion.Editor.NetworkProjectConfigImporter.RebuildPrefabHash();
        AssetDatabase.ImportAsset("Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion", ImportAssetOptions.ForceUpdate);

        // 4. Create / update the NetworkTest scene.
        CreateNetworkTestScene(flagBlue, flagRed);

        Debug.Log("[NetworkSetup] Done. Player + Flag prefabs configured, NetworkTest scene created.");
    }

    private static CTFSettings EnsureCTFSettingsAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<CTFSettings>(CtfSettingsPath);
        if (existing != null) return existing;

        var settings = ScriptableObject.CreateInstance<CTFSettings>();
        AssetDatabase.CreateAsset(settings, CtfSettingsPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[NetworkSetup] Created CTFSettings at {CtfSettingsPath}");
        return settings;
    }

    private static Material CreateOrLoadColorMaterial(string matPath, Color color)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static NetworkObject CreateOrUpdateFlagPrefab(string prefabPath, Team team, string bannerMatPath, Color color, CTFSettings ctfSettings)
    {
        GameObject root = new GameObject(team == Team.TeamA ? "Flag_Blue" : "Flag_Red");
        try
        {
            var netObj = root.AddComponent<NetworkObject>();
            var flags = netObj.Flags | NetworkObjectFlags.MasterClientObject;
            flags &= ~NetworkObjectFlags.AllowStateAuthorityOverride;
            flags &= ~NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;
            netObj.Flags = flags;

            var netFlag = root.AddComponent<NetworkFlag>();

            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);

            Material poleMat = CreateOrLoadColorMaterial("Assets/_Project/Materials/FlagPoleMat.mat", new Color(0.8f, 0.82f, 0.85f));
            Material bannerMat = CreateOrLoadColorMaterial(bannerMatPath, color);

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            pole.transform.SetParent(visualRoot.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            pole.transform.localScale = new Vector3(0.08f, 1.2f, 0.08f);
            pole.GetComponent<Renderer>().sharedMaterial = poleMat;

            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "Banner";
            Object.DestroyImmediate(banner.GetComponent<Collider>());
            banner.transform.SetParent(visualRoot.transform, false);
            banner.transform.localPosition = new Vector3(0.45f, 2.05f, 0f);
            banner.transform.localScale = new Vector3(0.85f, 0.55f, 0.06f);
            var bannerRenderer = banner.GetComponent<Renderer>();
            bannerRenderer.sharedMaterial = bannerMat;

            var so = new SerializedObject(netFlag);
            so.FindProperty("flagTeam").intValue = (int)team;
            so.FindProperty("settings").objectReferenceValue = ctfSettings;
            so.FindProperty("visualRoot").objectReferenceValue = visualRoot.transform;
            var renderersProp = so.FindProperty("teamColoredRenderers");
            renderersProp.arraySize = 1;
            renderersProp.GetArrayElementAtIndex(0).objectReferenceValue = bannerRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            AssetDatabase.SetLabels(savedPrefab, new[] { Fusion.Editor.NetworkProjectConfigImporter.FusionPrefabTag });
            EditorUtility.SetDirty(savedPrefab);
            AssetDatabase.SaveAssets();

            Debug.Log($"[NetworkSetup] Configured flag prefab at {prefabPath}");
            return savedPrefab.GetComponent<NetworkObject>();
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void SetupPlayerPrefab()
    {
        string prefabPath = "Assets/_Project/Prefabs/Player.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[NetworkSetup] Player prefab not found at {prefabPath}");
            return;
        }

        // Open the prefab for editing.
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

        bool modified = false;

        // Add NetworkObject if missing.
        if (prefabRoot.GetComponent<NetworkObject>() == null)
        {
            prefabRoot.AddComponent<NetworkObject>();
            modified = true;
            Debug.Log("[NetworkSetup] Added NetworkObject to Player prefab.");
        }

        // Add NetworkTransform if missing.
        if (prefabRoot.GetComponent<NetworkTransform>() == null)
        {
            prefabRoot.AddComponent<NetworkTransform>();
            modified = true;
            Debug.Log("[NetworkSetup] Added NetworkTransform to Player prefab.");
        }

        // Add NetworkPlayer if missing.
        if (prefabRoot.GetComponent<FlagBandits.Network.NetworkPlayer>() == null)
        {
            prefabRoot.AddComponent<FlagBandits.Network.NetworkPlayer>();
            modified = true;
            Debug.Log("[NetworkSetup] Added NetworkPlayer to Player prefab.");
        }

        // Add NetworkHealth if missing.
        if (prefabRoot.GetComponent<FlagBandits.Network.NetworkHealth>() == null)
        {
            var nh = prefabRoot.AddComponent<FlagBandits.Network.NetworkHealth>();
            // Assign the HealthSettings if one exists.
            var healthSettings = AssetDatabase.LoadAssetAtPath<FlagBandits.Combat.HealthSettings>(
                "Assets/_Project/Settings/PlayerHealthSettings.asset");
            if (healthSettings == null)
            {
                // Try default name.
                healthSettings = AssetDatabase.LoadAssetAtPath<FlagBandits.Combat.HealthSettings>(
                    "Assets/_Project/Settings/HealthSettings.asset");
            }
            if (healthSettings != null)
            {
                nh.SetSettings(healthSettings);
            }
            modified = true;
            Debug.Log("[NetworkSetup] Added NetworkHealth to Player prefab.");
        }

        // Add NetworkShooter if missing.
        if (prefabRoot.GetComponent<FlagBandits.Network.NetworkShooter>() == null)
        {
            prefabRoot.AddComponent<FlagBandits.Network.NetworkShooter>();
            modified = true;
            Debug.Log("[NetworkSetup] Added NetworkShooter to Player prefab.");
        }

        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            Debug.Log("[NetworkSetup] Player prefab saved.");
        }
        else
        {
            Debug.Log("[NetworkSetup] Player prefab already has all network components.");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);

        var loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (loadedPrefab != null)
        {
            AssetDatabase.SetLabels(loadedPrefab, new[] { Fusion.Editor.NetworkProjectConfigImporter.FusionPrefabTag });
        }
    }

    private static void CreateNetworkTestScene(NetworkObject flagBlue, NetworkObject flagRed)
    {
        string scenePath = "Assets/_Project/Scenes/NetworkTest.unity";

        // Create a new empty scene.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // --- Ground plane ---
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5f, 1f, 5f); // 50×50m

        var groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            groundRenderer.sharedMaterial = CreateOrLoadColorMaterial(
                "Assets/_Project/Materials/GroundMat_Network.mat",
                new Color(0.35f, 0.4f, 0.35f));
        }

        // --- Team Bases, Pedestals, and Spawn Points ---
        GameObject spawnA = new GameObject("SpawnPoint_A");
        spawnA.transform.position = new Vector3(-11f, 1f, 0f);
        spawnA.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        GameObject spawnB = new GameObject("SpawnPoint_B");
        spawnB.transform.position = new Vector3(11f, 1f, 0f);
        spawnB.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        CreateTeamBaseZone(
            "Base_TeamA",
            Team.TeamA,
            new Vector3(-14f, 0f, 0f),
            spawnA.transform,
            "Assets/_Project/Materials/PedestalBlueMat.mat",
            new Color(0.2f, 0.5f, 1f));

        CreateTeamBaseZone(
            "Base_TeamB",
            Team.TeamB,
            new Vector3(14f, 0f, 0f),
            spawnB.transform,
            "Assets/_Project/Materials/PedestalRedMat.mat",
            new Color(0.95f, 0.25f, 0.25f));

        // --- Low walls for cover ---
        Material wallMat = CreateOrLoadColorMaterial("Assets/_Project/Materials/WallMat_Network.mat", new Color(0.5f, 0.45f, 0.4f));
        CreateWall("WallCenter", new Vector3(0f, 1f, 0f), new Vector3(0.3f, 2f, 4f), wallMat);
        CreateWall("WallLeft", new Vector3(-4f, 1f, 6f), new Vector3(4f, 2f, 0.3f), wallMat);
        CreateWall("WallRight", new Vector3(4f, 1f, -6f), new Vector3(4f, 2f, 0.3f), wallMat);

        // --- Perimeter walls ---
        CreateWall("PerimeterN", new Vector3(0f, 1.5f, 25f), new Vector3(50f, 3f, 0.5f), wallMat);
        CreateWall("PerimeterS", new Vector3(0f, 1.5f, -25f), new Vector3(50f, 3f, 0.5f), wallMat);
        CreateWall("PerimeterE", new Vector3(25f, 1.5f, 0f), new Vector3(0.5f, 3f, 50f), wallMat);
        CreateWall("PerimeterW", new Vector3(-25f, 1.5f, 0f), new Vector3(0.5f, 3f, 50f), wallMat);

        // --- NetworkBootstrap + CtfDebugHud ---
        GameObject bootstrapObj = new GameObject("NetworkBootstrap");
        var bootstrap = bootstrapObj.AddComponent<FlagBandits.Network.NetworkBootstrap>();
        bootstrapObj.AddComponent<CtfDebugHud>();

        AssetDatabase.Refresh();
        string prefabPath = "Assets/_Project/Prefabs/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject flagBlueGo = AssetDatabase.LoadAssetAtPath<GameObject>(FlagBluePrefabPath);
        GameObject flagRedGo = AssetDatabase.LoadAssetAtPath<GameObject>(FlagRedPrefabPath);
        NetworkObject reloadedFlagBlue = flagBlueGo != null ? flagBlueGo.GetComponent<NetworkObject>() : flagBlue;
        NetworkObject reloadedFlagRed = flagRedGo != null ? flagRedGo.GetComponent<NetworkObject>() : flagRed;

        if (playerPrefab != null)
        {
            var networkObj = playerPrefab.GetComponent<NetworkObject>();
            var so = new SerializedObject(bootstrap);
            if (networkObj != null)
            {
                so.FindProperty("playerPrefab").objectReferenceValue = networkObj;
            }
            if (reloadedFlagBlue != null)
            {
                so.FindProperty("flagBluePrefab").objectReferenceValue = reloadedFlagBlue;
            }
            if (reloadedFlagRed != null)
            {
                so.FindProperty("flagRedPrefab").objectReferenceValue = reloadedFlagRed;
            }
            Debug.Log($"[NetworkSetup] Wired prefabs: player={networkObj != null}, blue={reloadedFlagBlue != null}, red={reloadedFlagRed != null}");

            var spawnsProp = so.FindProperty("spawnPoints");
            if (spawnsProp != null)
            {
                spawnsProp.arraySize = 2;
                spawnsProp.GetArrayElementAtIndex(0).objectReferenceValue = spawnA.transform;
                spawnsProp.GetArrayElementAtIndex(1).objectReferenceValue = spawnB.transform;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- CrosshairUI ---
        GameObject crosshairObj = new GameObject("CrosshairUI");
        crosshairObj.AddComponent<FlagBandits.UI.CrosshairUI>();

        // --- ThirdPersonCamera on Main Camera ---
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            if (mainCam.GetComponent<FlagBandits.Player.ThirdPersonCamera>() == null)
            {
                var tpc = mainCam.gameObject.AddComponent<FlagBandits.Player.ThirdPersonCamera>();
                var playerSettings = AssetDatabase.LoadAssetAtPath<FlagBandits.Player.PlayerSettings>(
                    "Assets/_Project/Settings/PlayerSettings.asset");
                if (playerSettings != null)
                {
                    tpc.Settings = playerSettings;
                }
            }
        }

        // --- Directional light (if not already present from DefaultGameObjects) ---
        var existingLight = Object.FindAnyObjectByType<Light>();
        if (existingLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.84f);
            light.intensity = 1.5f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();
        Debug.Log($"[NetworkSetup] NetworkTest scene saved to {scenePath}");
    }

    private static void CreateTeamBaseZone(string name, Team team, Vector3 pedestalPos, Transform spawnPoint, string padMatPath, Color padColor)
    {
        GameObject baseObj = new GameObject(name);
        baseObj.transform.position = pedestalPos;
        var zone = baseObj.AddComponent<TeamBaseZone>();

        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = team == Team.TeamA ? "Pedestal_A" : "Pedestal_B";
        Object.DestroyImmediate(pedestal.GetComponent<Collider>());
        pedestal.transform.SetParent(baseObj.transform, false);
        pedestal.transform.localPosition = new Vector3(0f, 0.025f, 0f);
        pedestal.transform.localScale = new Vector3(2.4f, 0.05f, 2.4f);
        pedestal.GetComponent<Renderer>().sharedMaterial = CreateOrLoadColorMaterial(padMatPath, padColor);

        GameObject anchor = new GameObject("FlagPedestalAnchor");
        anchor.transform.SetParent(baseObj.transform, false);
        anchor.transform.localPosition = Vector3.zero;

        var so = new SerializedObject(zone);
        so.FindProperty("team").intValue = (int)team;
        so.FindProperty("flagPedestal").objectReferenceValue = anchor.transform;
        so.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateWall(string name, Vector3 position, Vector3 scale, Material wallMat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;

        var wallRenderer = wall.GetComponent<Renderer>();
        if (wallRenderer != null && wallMat != null)
        {
            wallRenderer.sharedMaterial = wallMat;
        }
    }
}
#endif
