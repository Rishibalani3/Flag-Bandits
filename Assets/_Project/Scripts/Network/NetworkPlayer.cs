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

            if (isLocal)
            {
                EnableLocalPlayer();
            }
            else
            {
                // Ensure remote players have input, movement, shooting, and CharacterController disabled.
                if (playerInput != null)
                {
                    playerInput.DeactivateInput();
                    playerInput.enabled = false;
                }

                if (playerMovement != null) playerMovement.enabled = false;
                if (playerShooter != null) playerShooter.enabled = false;
                if (charController != null) charController.enabled = false;
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

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (networkHealth != null)
            {
                networkHealth.OnDeath -= HandleLocalDeath;
            }
        }

        private void HandleLocalDeath(Vector3 deathPos)
        {
            if (!HasStateAuthority) return;

            // Disable movement/shooting while dead.
            if (playerMovement != null) playerMovement.enabled = false;
            if (playerShooter != null) playerShooter.enabled = false;
            if (playerInput != null) playerInput.enabled = false;

            // Schedule respawn after delay.
            float delay = networkHealth != null ? networkHealth.RespawnDelay : 3f;
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
    }
}
