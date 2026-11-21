using UnityEngine;
using System.Linq;
using Fusion;

namespace FlagBandits.Network
{
    /// <summary>
    /// Starts Photon Fusion 2 in Shared Mode, joins a fixed dev room,
    /// and spawns this client's player. Dev-only: menus/matchmaking come in Phase 7.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        [SerializeField] private NetworkObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private string sessionName = "dev-room";
        [SerializeField] private int maxPlayers = 10;
        [SerializeField] private float spawnSpacing = 3f;

        [Header("Debug UI")]
        [SerializeField] private bool showDebugUI = true;

        private NetworkRunner runner;

        private async void Start()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[NetworkBootstrap] Player prefab not assigned.");
                return;
            }

            runner = gameObject.AddComponent<NetworkRunner>();
            runner.ProvideInput = false;

            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,
                PlayerCount = maxPlayers,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

            if (!result.Ok)
            {
                Debug.LogError($"[NetworkBootstrap] StartGame failed: {result.ShutdownReason} {result.ErrorMessage}");
                return;
            }

            Debug.Log($"[NetworkBootstrap] Joined '{sessionName}' as {runner.LocalPlayer} (MasterClient: {runner.IsSharedModeMasterClient})");
            SpawnLocalPlayer();
        }

        private void SpawnLocalPlayer()
        {
            int slot = runner.LocalPlayer.PlayerId % 10;

            Vector3 pos;
            Quaternion rot;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int index = slot % spawnPoints.Length;
                pos = spawnPoints[index].position;
                rot = spawnPoints[index].rotation;
            }
            else
            {
                // Fallback: spread along X axis.
                pos = Vector3.up + Vector3.right * (slot * spawnSpacing);
                rot = Quaternion.identity;
            }

            runner.Spawn(playerPrefab, pos, rot, runner.LocalPlayer);
        }

        private void OnGUI()
        {
            if (!showDebugUI || runner == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label($"<b>Session:</b> {sessionName}", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label($"<b>Player:</b> {runner.LocalPlayer}");
            GUILayout.Label($"<b>Players in room:</b> {runner.ActivePlayers.Count()}");
            GUILayout.Label($"<b>Master Client:</b> {runner.IsSharedModeMasterClient}");
            GUILayout.EndArea();
        }
    }
}
