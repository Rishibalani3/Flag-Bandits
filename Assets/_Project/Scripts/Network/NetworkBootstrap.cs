using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using FlagBandits.Gameplay;

namespace FlagBandits.Network
{
    /// <summary>
    /// Starts Photon Fusion 2 in Shared Mode, joins a fixed dev room,
    /// assigns player team, and spawns this client's player at team base.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        [SerializeField] private NetworkObject playerPrefab;
        [SerializeField] private NetworkObject flagBluePrefab;
        [SerializeField] private NetworkObject flagRedPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private string sessionName = "dev-room";
        [SerializeField] private string fixedRegion = "asia";
        [SerializeField] private int maxPlayers = 10;
        [SerializeField] private float spawnSpacing = 3f;

        [Header("Debug UI")]
        [SerializeField] private bool showDebugUI = true;

        private NetworkRunner runner;
        private Team localTeam = Team.None;
        private bool hasLocalTeam = false;

        private async void Start()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[NetworkBootstrap] Player prefab not assigned.");
                return;
            }

            runner = gameObject.AddComponent<NetworkRunner>();
            runner.ProvideInput = false;

            FusionAppSettings customSettings = null;
            if (PhotonAppSettings.TryGetGlobal(out var globalSettings) && globalSettings != null && globalSettings.AppSettings != null)
            {
                customSettings = globalSettings.AppSettings.GetCopy();
                if (!string.IsNullOrEmpty(fixedRegion))
                {
                    customSettings.FixedRegion = fixedRegion;
                }
            }

            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,
                PlayerCount = maxPlayers,
                CustomPhotonAppSettings = customSettings,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

            if (!result.Ok)
            {
                Debug.LogError($"[NetworkBootstrap] StartGame failed: {result.ShutdownReason} {result.ErrorMessage}");
                return;
            }

            string regionInfo = runner.SessionInfo.IsValid ? runner.SessionInfo.Region : fixedRegion;
            Debug.Log($"[NetworkBootstrap] Joined '{sessionName}' (Region: {regionInfo}) as {runner.LocalPlayer} (MasterClient: {runner.IsSharedModeMasterClient})");

            if (runner.IsSharedModeMasterClient)
            {
                EnsureFlagsSpawned();
            }

            await SpawnLocalPlayer();
        }

        private void EnsureFlagsSpawned()
        {
            var zones = FindObjectsByType<TeamBaseZone>();
            SpawnFlagForTeamIfMissing(Team.TeamA, flagBluePrefab, zones, 0);
            SpawnFlagForTeamIfMissing(Team.TeamB, flagRedPrefab, zones, 1);
        }

        private void SpawnFlagForTeamIfMissing(Team team, NetworkObject prefab, TeamBaseZone[] zones, int fallbackSpawnIndex)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[NetworkBootstrap] Flag prefab for {team} is not assigned.");
                return;
            }

            if (NetworkFlag.FindFlag(team) != null)
            {
                return;
            }

            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            bool foundBase = false;

            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null && zones[i].Team == team)
                {
                    pos = zones[i].FlagPedestal.position;
                    rot = zones[i].FlagPedestal.rotation;
                    foundBase = true;
                    break;
                }
            }

            if (!foundBase && spawnPoints != null && fallbackSpawnIndex < spawnPoints.Length && spawnPoints[fallbackSpawnIndex] != null)
            {
                pos = spawnPoints[fallbackSpawnIndex].position;
                pos.y = 0f;
                rot = spawnPoints[fallbackSpawnIndex].rotation;
            }

            runner.Spawn(prefab, pos, rot, null);
        }

        private async Task SpawnLocalPlayer()
        {
            // Wait briefly for existing remote players to replicate if joining an active room
            int otherPlayersExpected = Mathf.Max(0, runner.ActivePlayers.Count() - 1);
            float waited = 0f;
            while (otherPlayersExpected > 0 && waited < 0.5f)
            {
                var existing = FindObjectsByType<NetworkPlayer>();
                if (existing.Length >= otherPlayersExpected) break;
                await Task.Delay(50);
                waited += 0.05f;
            }

            int teamACount = 0;
            int teamBCount = 0;
            foreach (var p in FindObjectsByType<NetworkPlayer>())
            {
                if (p == null || p.Object == null || !p.Object.IsValid) continue;
                if (p.PlayerTeam == Team.TeamA) teamACount++;
                else if (p.PlayerTeam == Team.TeamB) teamBCount++;
            }

            Team assignedTeam;
            if (teamACount != teamBCount)
            {
                assignedTeam = (teamACount < teamBCount) ? Team.TeamA : Team.TeamB;
            }
            else
            {
                // Equal or empty room: alternate by PlayerId (Player 1 -> Team A, Player 2 -> Team B)
                assignedTeam = (runner.LocalPlayer.PlayerId % 2 == 1) ? Team.TeamA : Team.TeamB;
            }

            localTeam = assignedTeam;
            hasLocalTeam = true;

            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            bool foundTeamSpawn = false;

            foreach (var zone in FindObjectsByType<TeamBaseZone>())
            {
                if (zone != null && zone.Team == assignedTeam && zone.SpawnPoint != null)
                {
                    pos = zone.SpawnPoint.position;
                    rot = zone.SpawnPoint.rotation;
                    foundTeamSpawn = true;
                    break;
                }
            }

            if (!foundTeamSpawn)
            {
                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    // Index 0 = Team A Base (SpawnPoint_A), Index 1 = Team B Base (SpawnPoint_B)
                    int index = (assignedTeam == Team.TeamA) ? 0 : 1;
                    if (index >= spawnPoints.Length) index = 0;
                    pos = spawnPoints[index].position;
                    rot = spawnPoints[index].rotation;
                }
                else
                {
                    int slot = runner.LocalPlayer.PlayerId % 10;
                    pos = Vector3.up + Vector3.right * (slot * spawnSpacing);
                    rot = Quaternion.identity;
                }
            }

            runner.Spawn(playerPrefab, pos, rot, runner.LocalPlayer, (r, obj) =>
            {
                var netPlayer = obj.GetComponent<NetworkPlayer>();
                if (netPlayer != null)
                {
                    netPlayer.PlayerTeam = assignedTeam;
                }
            });
        }

        private void OnGUI()
        {
            if (!showDebugUI || runner == null) return;

            var richStyle = new GUIStyle(GUI.skin.label) { richText = true };
            string regionStr = runner.SessionInfo.IsValid ? runner.SessionInfo.Region : fixedRegion;

            GUILayout.BeginArea(new Rect(10, 10, 340, 140));
            GUILayout.Label($"<b>Session:</b> {sessionName} ({regionStr})", richStyle);
            GUILayout.Label($"<b>Player:</b> {runner.LocalPlayer}");

            if (!hasLocalTeam)
            {
                GUILayout.Label("<b>Your Team:</b> Connecting...", richStyle);
            }
            else
            {
                string teamColorHex = localTeam == Team.TeamA ? "#4da6ff" : "#ff5555";
                string teamTitle = localTeam == Team.TeamA ? "Team A (Blue)" : "Team B (Red)";
                GUILayout.Label($"<b>Your Team:</b> <color={teamColorHex}><b>{teamTitle}</b></color>", richStyle);
            }

            GUILayout.Label($"<b>Players in room:</b> {runner.ActivePlayers.Count()}");
            GUILayout.Label($"<b>Master Client:</b> {runner.IsSharedModeMasterClient}");
            GUILayout.EndArea();
        }
    }
}
