using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using FlagBandits.Player;
using FlagBandits.Combat;
using FlagBandits.Gameplay;

namespace FlagBandits.Network
{
    /// <summary>
    /// Splits local vs remote players. The owner (State Authority) keeps input,
    /// movement, shooting and the camera. Remote copies are driven only by NetworkTransform.
    /// Handles death/respawn lifecycle, flag carrier sync, and team synchronization.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayer : NetworkBehaviour
    {
        private static readonly List<NetworkPlayer> activePlayers = new List<NetworkPlayer>();
        public static IReadOnlyList<NetworkPlayer> ActivePlayers => activePlayers;

        [Networked] public Team PlayerTeam { get; set; }

        public bool IsDead => networkHealth != null && (bool)networkHealth.IsDead;
        public bool IsCarryingFlag { get; private set; }

        public static bool TryGetPlayer(PlayerRef playerRef, out NetworkPlayer result)
        {
            result = null;
            if (playerRef == PlayerRef.None) return false;

            for (int i = activePlayers.Count - 1; i >= 0; i--)
            {
                var p = activePlayers[i];
                if (p == null)
                {
                    activePlayers.RemoveAt(i);
                    continue;
                }

                if (p.Object != null && p.Object.IsValid && p.Object.StateAuthority == playerRef)
                {
                    result = p;
                    return true;
                }
            }
            return false;
        }

        private NetworkHealth networkHealth;
        private NetworkTransform networkTransform;
        private PlayerMovement playerMovement;
        private CharacterController charController;
        private PlayerInput playerInput;
        private PlayerShooter playerShooter;

        private Renderer playerRenderer;
        private Color originalColor = Color.white;
        private Coroutine flashCoroutine;
        private float respawnTimer = 0f;
        private bool respawnPending = false;
        private bool respawnTeleported = false;
        private float lastKnownHealth = 100f;
        private ChangeDetector changeDetector;

        private const float RespawnTeleportLeadTime = 0.25f;

        private void Awake()
        {
            playerRenderer = GetComponentInChildren<Renderer>();
            if (playerRenderer != null && playerRenderer.material != null)
            {
                if (playerRenderer.material.HasProperty("_BaseColor"))
                {
                    originalColor = playerRenderer.material.GetColor("_BaseColor");
                }
                else if (playerRenderer.material.HasProperty("_Color"))
                {
                    originalColor = playerRenderer.material.color;
                }
            }
        }

        private void Start()
        {
            // Offline fallback: if loaded in a scene without Fusion running (e.g. OfflineTest)
            if (Object == null || !Object.IsValid)
            {
                EnableLocalPlayer();
                ApplyTeamVisual(PlayerTeam);
            }
        }

        public override void Spawned()
        {
            if (!activePlayers.Contains(this))
            {
                activePlayers.Add(this);
            }

            changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            bool isLocal = HasStateAuthority || HasInputAuthority || (Object != null && Object.StateAuthority == Runner.LocalPlayer);
            Debug.Log($"[NetworkPlayer] Spawned! isLocal: {isLocal}, HasStateAuthority: {HasStateAuthority}, StateAuthority: {Object.StateAuthority}, LocalPlayer: {Runner.LocalPlayer}, Team: {PlayerTeam}");
            gameObject.name = isLocal ? $"Player (Local - {PlayerTeam})" : $"Player (Remote {Object.StateAuthority} - {PlayerTeam})";

            networkHealth = GetComponent<NetworkHealth>();
            networkTransform = GetComponent<NetworkTransform>();
            playerMovement = GetComponent<PlayerMovement>();
            charController = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();
            playerShooter = GetComponent<PlayerShooter>();

            if (networkHealth != null)
            {
                lastKnownHealth = networkHealth.CurrentHealth;
                networkHealth.OnHealthChanged += HandleHealthVisual;
                networkHealth.OnDeath += HandleDeathVisual;
                networkHealth.OnRespawned += HandleRespawnVisual;
            }

            ApplyTeamVisual(PlayerTeam);

            if (isLocal)
            {
                EnableLocalPlayer();
            }
            else
            {
                // Ensure remote players have input, movement, and shooting disabled.
                if (playerInput != null)
                {
                    playerInput.DeactivateInput();
                    playerInput.enabled = false;
                }

                if (playerMovement != null) playerMovement.enabled = false;
                if (playerShooter != null) playerShooter.enabled = false;
                // Keep CharacterController enabled so remote player has hit detection collider!
                if (charController != null) charController.enabled = true;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            if (respawnPending)
            {
                respawnTimer -= Runner.DeltaTime;

                // Teleport to spawn BEFORE IsDead clears so NetworkTransform syncs the
                // spawn position to the Master Client before the player can grab flags again.
                if (!respawnTeleported && respawnTimer <= RespawnTeleportLeadTime)
                {
                    TeleportToSpawnWhileDead();
                }

                if (respawnTimer <= 0f)
                {
                    CompleteRespawn();
                }
                return;
            }

            bool carrying = Object != null && NetworkFlag.IsPlayerCarryingAnyFlag(Object.StateAuthority);
            IsCarryingFlag = carrying;

            if (playerMovement != null && playerMovement.enabled)
            {
                playerMovement.SetCarryingFlag(carrying);
                playerMovement.ExecuteMovement(Runner.DeltaTime);
            }
        }

        public override void Render()
        {
            IsCarryingFlag = Object != null && Object.IsValid && NetworkFlag.IsPlayerCarryingAnyFlag(Object.StateAuthority);

            if (changeDetector != null)
            {
                foreach (var change in changeDetector.DetectChanges(this))
                {
                    if (change == nameof(PlayerTeam))
                    {
                        ApplyTeamVisual(PlayerTeam);
                    }
                }
            }
        }

        public void ApplyTeamVisual(Team team)
        {
            Color teamColor = GetTeamColor(team);
            originalColor = teamColor;
            if (flashCoroutine == null)
            {
                SetRendererColor(teamColor);
            }
        }

        public static Color GetTeamColor(Team team)
        {
            switch (team)
            {
                case Team.TeamA:
                    return new Color(0.2f, 0.5f, 1f); // Blue
                case Team.TeamB:
                    return new Color(0.95f, 0.25f, 0.25f); // Red
                default:
                    return Color.white;
            }
        }

        private void EnableLocalPlayer()
        {
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
            if (playerShooter == null) playerShooter = GetComponent<PlayerShooter>();
            if (charController == null) charController = GetComponent<CharacterController>();
            if (networkHealth == null) networkHealth = GetComponent<NetworkHealth>();

            // Enable input / movement / shooting ONLY for the local player.
            if (playerInput != null)
            {
                playerInput.enabled = true;
                playerInput.ActivateInput();
                if (playerInput.actions != null)
                {
                    playerInput.actions.FindActionMap("Player")?.Enable();
                }
            }

            if (playerMovement != null)
            {
                playerMovement.enabled = true;
                playerMovement.IsNetworked = (Runner != null && Runner.IsRunning);
            }
            if (playerShooter != null) playerShooter.enabled = true;
            if (charController != null) charController.enabled = true;

            // Point the scene camera at the local player.
            var cam = UnityEngine.Object.FindAnyObjectByType<ThirdPersonCamera>();
            if (cam != null)
            {
                cam.Target = transform;
                playerMovement?.SetCameraTransform(cam.transform);
            }
            else
            {
                Debug.LogWarning("[NetworkPlayer] No ThirdPersonCamera in scene.");
            }

            // Subscribe to death for respawn logic.
            if (networkHealth != null)
            {
                networkHealth.OnDeath -= HandleLocalDeath;
                networkHealth.OnDeath += HandleLocalDeath;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            activePlayers.Remove(this);
            IsCarryingFlag = false;
            if (playerMovement != null) playerMovement.SetCarryingFlag(false);

            if (networkHealth != null)
            {
                networkHealth.OnHealthChanged -= HandleHealthVisual;
                networkHealth.OnDeath -= HandleDeathVisual;
                networkHealth.OnRespawned -= HandleRespawnVisual;
                networkHealth.OnDeath -= HandleLocalDeath;
            }
        }

        private void HandleHealthVisual(float current, float max)
        {
            if (current < lastKnownHealth)
            {
                FlashDamage();
            }
            lastKnownHealth = current;
        }

        private void FlashDamage()
        {
            if (playerRenderer == null) return;
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private System.Collections.IEnumerator FlashRoutine()
        {
            Color flashCol = PlayerTeam == Team.TeamB ? new Color(1f, 0.95f, 0.2f) : Color.red;
            SetRendererColor(flashCol);
            yield return new WaitForSeconds(0.12f);
            SetRendererColor(originalColor);
            flashCoroutine = null;
        }

        private void SetRendererColor(Color col)
        {
            if (playerRenderer == null || playerRenderer.material == null) return;
            if (playerRenderer.material.HasProperty("_BaseColor"))
            {
                playerRenderer.material.SetColor("_BaseColor", col);
            }
            else if (playerRenderer.material.HasProperty("_Color"))
            {
                playerRenderer.material.color = col;
            }
        }

        private void HandleDeathVisual(Vector3 deathPos)
        {
            if (playerRenderer != null) playerRenderer.enabled = false;
            if (charController != null) charController.enabled = false;
        }

        private void HandleRespawnVisual()
        {
            if (playerRenderer != null)
            {
                playerRenderer.enabled = true;
                SetRendererColor(originalColor);
            }
            if (charController != null) charController.enabled = true;
        }

        private void HandleLocalDeath(Vector3 deathPos)
        {
            if (!HasStateAuthority) return;

            float delay = networkHealth != null ? networkHealth.RespawnDelay : 3f;
            respawnTimer = delay;
            respawnPending = true;
            respawnTeleported = false;
            IsCarryingFlag = false;

            if (playerMovement != null)
            {
                playerMovement.SetCarryingFlag(false);
                playerMovement.enabled = false;
            }
            if (playerShooter != null) playerShooter.enabled = false;
            if (playerInput != null) playerInput.enabled = false;
        }

        private void TeleportToSpawnWhileDead()
        {
            respawnTeleported = true;

            Vector3 targetPos = playerMovement != null ? playerMovement.SpawnPosition : transform.position;
            Quaternion targetRot = playerMovement != null ? playerMovement.SpawnRotation : transform.rotation;

            if (playerMovement != null)
            {
                playerMovement.ResetToSpawn(keepControllerDisabled: true);
            }

            if (networkTransform != null && Runner != null && Runner.IsRunning)
            {
                networkTransform.Teleport(targetPos, targetRot);
            }
        }

        private void CompleteRespawn()
        {
            if (!respawnTeleported)
            {
                TeleportToSpawnWhileDead();
            }

            respawnPending = false;
            respawnTimer = 0f;

            if (networkHealth != null)
            {
                networkHealth.Respawn();
            }

            if (playerInput != null)
            {
                playerInput.enabled = true;
                playerInput.ActivateInput();
                if (playerInput.actions != null)
                {
                    playerInput.actions.FindActionMap("Player")?.Enable();
                }
            }
            if (playerMovement != null)
            {
                playerMovement.SetCarryingFlag(false);
                playerMovement.enabled = true;
            }
            if (playerShooter != null) playerShooter.enabled = true;
        }

        private void OnGUI()
        {
            if (!HasStateAuthority) return;

            float hp = networkHealth != null ? networkHealth.CurrentHealth : 100f;
            float maxHp = networkHealth != null ? networkHealth.MaxHealth : 100f;
            bool dead = networkHealth != null && (bool)networkHealth.IsDead;

            float barWidth = Mathf.Clamp(Screen.width * 0.28f, 240f, 420f);
            float barHeight = Mathf.Clamp(Screen.height * 0.035f, 26f, 44f);
            float x = (Screen.width - barWidth) * 0.5f;
            float bottomMargin = Mathf.Max(35f, Screen.height * 0.06f);
            float y = Screen.height - barHeight - bottomMargin;

            // Team Badge
            string teamLabel = PlayerTeam == Team.TeamA ? "TEAM BLUE" : (PlayerTeam == Team.TeamB ? "TEAM RED" : "UNASSIGNED");
            Color badgeBg = PlayerTeam == Team.TeamA ? new Color(0.15f, 0.4f, 0.9f, 0.9f) : (PlayerTeam == Team.TeamB ? new Color(0.85f, 0.2f, 0.2f, 0.9f) : new Color(0.3f, 0.3f, 0.3f, 0.9f));
            float badgeWidth = Mathf.Clamp(barWidth * 0.45f, 130f, 220f);
            float badgeHeight = Mathf.Clamp(barHeight * 0.75f, 20f, 32f);
            float badgeX = (Screen.width - badgeWidth) * 0.5f;
            float badgeY = y - badgeHeight - 4f;

            if (IsCarryingFlag && !dead)
            {
                float carryW = Mathf.Clamp(barWidth * 0.85f, 220f, 340f);
                float carryH = badgeHeight;
                float carryX = (Screen.width - carryW) * 0.5f;
                float carryY = badgeY - carryH - 4f;
                GUI.color = new Color(0.95f, 0.72f, 0.1f, 0.95f);
                GUI.DrawTexture(new Rect(carryX, carryY, carryW, carryH), Texture2D.whiteTexture);
                GUI.color = Color.black;
                var carryStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = Mathf.Clamp(Mathf.RoundToInt(carryH * 0.58f), 10, 15)
                };
                GUI.Label(new Rect(carryX, carryY, carryW, carryH), "CARRYING ENEMY FLAG (SLOWED)", carryStyle);
            }

            GUI.color = badgeBg;
            GUI.DrawTexture(new Rect(badgeX, badgeY, badgeWidth, badgeHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var badgeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(badgeHeight * 0.6f), 10, 16)
            };
            GUI.Label(new Rect(badgeX, badgeY, badgeWidth, badgeHeight), teamLabel, badgeStyle);

            // Health bar container
            GUI.Box(new Rect(x - 2, y - 2, barWidth + 4, barHeight + 4), "");
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

            // Health fill
            float pct = Mathf.Clamp01(hp / maxHp);
            GUI.color = pct > 0.35f ? new Color(0.2f, 0.85f, 0.3f, 0.95f) : new Color(0.9f, 0.2f, 0.2f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, barWidth * pct, barHeight), Texture2D.whiteTexture);

            // Health text
            GUI.color = Color.white;
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(barHeight * 0.55f), 12, 22);
            var textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = fontSize
            };

            if (dead)
            {
                GUI.Label(new Rect(x, y, barWidth, barHeight), $"RESPAWNING IN {Mathf.Max(0f, respawnTimer):F1}s", textStyle);
            }
            else
            {
                GUI.Label(new Rect(x, y, barWidth, barHeight), $"HEALTH: {Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(maxHp)}", textStyle);
            }
        }
    }
}
