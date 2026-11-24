using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using FlagBandits.Player;
using FlagBandits.Combat;

namespace FlagBandits.Network
{
    /// <summary>
    /// Splits local vs remote players. The owner (State Authority) keeps input,
    /// movement, shooting and the camera. Remote copies are driven only by NetworkTransform.
    /// Handles death/respawn lifecycle.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayer : NetworkBehaviour
    {
        private NetworkHealth networkHealth;
        private PlayerMovement playerMovement;
        private CharacterController charController;
        private PlayerInput playerInput;
        private PlayerShooter playerShooter;

        private Renderer playerRenderer;
        private Color originalColor = Color.white;
        private Coroutine flashCoroutine;
        private float respawnTimer = 0f;
        private float lastKnownHealth = 100f;

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
            }
        }

        public override void Spawned()
        {
            bool isLocal = HasStateAuthority || HasInputAuthority || (Object != null && Object.StateAuthority == Runner.LocalPlayer);
            Debug.Log($"[NetworkPlayer] Spawned! isLocal: {isLocal}, HasStateAuthority: {HasStateAuthority}, StateAuthority: {Object.StateAuthority}, LocalPlayer: {Runner.LocalPlayer}");
            gameObject.name = isLocal ? "Player (Local)" : $"Player (Remote {Object.StateAuthority})";

            networkHealth = GetComponent<NetworkHealth>();
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

            if (playerMovement != null && playerMovement.enabled)
            {
                playerMovement.ExecuteMovement(Runner.DeltaTime);
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
            var cam = UnityEngine.Object.FindFirstObjectByType<ThirdPersonCamera>();
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

        private void Update()
        {
            if (respawnTimer > 0f)
            {
                respawnTimer -= Time.deltaTime;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
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
            SetRendererColor(Color.red);
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

            // Disable movement/shooting while dead.
            if (playerMovement != null) playerMovement.enabled = false;
            if (playerShooter != null) playerShooter.enabled = false;
            if (playerInput != null) playerInput.enabled = false;

            StartCoroutine(RespawnAfterDelay(delay));
        }

        private System.Collections.IEnumerator RespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (networkHealth != null)
            {
                networkHealth.Respawn();
            }

            // Teleport back to spawn.
            if (playerMovement != null)
            {
                playerMovement.ResetToSpawn();
            }

            // Re-enable controls.
            if (playerInput != null)
            {
                playerInput.enabled = true;
                playerInput.ActivateInput();
                if (playerInput.actions != null)
                {
                    playerInput.actions.FindActionMap("Player")?.Enable();
                }
            }
            if (playerMovement != null) playerMovement.enabled = true;
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
