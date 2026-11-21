// Editor script to set up the NetworkTest scene and configure the Player prefab
// for Photon Fusion networking. Run via: unity command script run --path Assets/_Project/Scripts/Editor/NetworkSetup.cs

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Fusion;

public static class NetworkSetup
{
    [MenuItem("Flag Bandits/Setup Network Scene")]
    public static void SetupNetworkScene()
    {
        // 1. Configure the Player prefab with network components.
        SetupPlayerPrefab();

        // 2. Create the NetworkTest scene.
        CreateNetworkTestScene();

        Debug.Log("[NetworkSetup] Done. Player prefab configured, NetworkTest scene created.");
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
    }

    private static void CreateNetworkTestScene()
    {
        string scenePath = "Assets/_Project/Scenes/NetworkTest.unity";

        // Create a new empty scene.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // --- Ground plane ---
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5f, 1f, 5f); // 50×50m

        // Set a grey material.
        var groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.35f, 0.4f, 0.35f);
            groundRenderer.material = mat;
            // Save material as asset.
            string matPath = "Assets/_Project/Materials/GroundMat_Network.mat";
            AssetDatabase.CreateAsset(mat, matPath);
        }

        // --- Spawn Points ---
        GameObject spawnA = new GameObject("SpawnPoint_A");
        spawnA.transform.position = new Vector3(-8f, 1f, 0f);
        spawnA.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        GameObject spawnB = new GameObject("SpawnPoint_B");
        spawnB.transform.position = new Vector3(8f, 1f, 0f);
        spawnB.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        // --- Low walls for cover ---
        CreateWall("WallCenter", new Vector3(0f, 1f, 0f), new Vector3(0.3f, 2f, 4f));
        CreateWall("WallLeft", new Vector3(-4f, 1f, 6f), new Vector3(4f, 2f, 0.3f));
        CreateWall("WallRight", new Vector3(4f, 1f, -6f), new Vector3(4f, 2f, 0.3f));

        // --- Perimeter walls ---
        CreateWall("PerimeterN", new Vector3(0f, 1.5f, 25f), new Vector3(50f, 3f, 0.5f));
        CreateWall("PerimeterS", new Vector3(0f, 1.5f, -25f), new Vector3(50f, 3f, 0.5f));
        CreateWall("PerimeterE", new Vector3(25f, 1.5f, 0f), new Vector3(0.5f, 3f, 50f));
        CreateWall("PerimeterW", new Vector3(-25f, 1.5f, 0f), new Vector3(0.5f, 3f, 50f));

        // --- NetworkBootstrap ---
        GameObject bootstrapObj = new GameObject("NetworkBootstrap");
        var bootstrap = bootstrapObj.AddComponent<FlagBandits.Network.NetworkBootstrap>();

        // Assign player prefab via SerializedObject.
        string prefabPath = "Assets/_Project/Prefabs/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (playerPrefab != null)
        {
            var networkObj = playerPrefab.GetComponent<NetworkObject>();
            if (networkObj != null)
            {
                var so = new SerializedObject(bootstrap);
                var prefabProp = so.FindProperty("playerPrefab");
                if (prefabProp != null)
                {
                    prefabProp.objectReferenceValue = networkObj;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                var spawnsProp = so.FindProperty("spawnPoints");
                if (spawnsProp != null)
                {
                    spawnsProp.arraySize = 2;
                    spawnsProp.GetArrayElementAtIndex(0).objectReferenceValue = spawnA.transform;
                    spawnsProp.GetArrayElementAtIndex(1).objectReferenceValue = spawnB.transform;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
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
                // Assign PlayerSettings if available.
                var playerSettings = AssetDatabase.LoadAssetAtPath<FlagBandits.Player.PlayerSettings>(
                    "Assets/_Project/Settings/PlayerSettings.asset");
                if (playerSettings != null)
                {
                    tpc.Settings = playerSettings;
                }
            }
        }

        // --- Directional light (if not already present from DefaultGameObjects) ---
        var existingLight = Object.FindFirstObjectByType<Light>();
        if (existingLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.84f);
            light.intensity = 1.5f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // Save the scene.
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();
        Debug.Log($"[NetworkSetup] NetworkTest scene saved to {scenePath}");
    }

    private static void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;

        var wallRenderer = wall.GetComponent<Renderer>();
        if (wallRenderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.5f, 0.45f, 0.4f);
            wallRenderer.material = mat;
        }
    }
}
#endif
