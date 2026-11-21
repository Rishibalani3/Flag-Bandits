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

        public override void Spawned()
        {
            bool isLocal = HasStateAuthority;
            gameObject.name = isLocal ? "Player (Local)" : $"Player (Remote {Object.StateAuthority})";

            networkHealth = GetComponent<NetworkHealth>();
            playerMovement = GetComponent<PlayerMovement>();
            charController = GetComponent<CharacterController>();

            // Enable input / movement / shooting ONLY for the local player.
            SetEnabled<PlayerInput>(isLocal);
            SetEnabled<PlayerMovement>(isLocal);
            SetEnabled<PlayerShooter>(isLocal);

            // Remote players don't need a CharacterController (NetworkTransform drives them).
            if (!isLocal && charController != null)
            {
                charController.enabled = false;
            }

            if (isLocal)
            {
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
                    networkHealth.OnDeath += HandleLocalDeath;
                }
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
            SetEnabled<PlayerMovement>(false);
            SetEnabled<PlayerShooter>(false);

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
            SetEnabled<PlayerMovement>(true);
            SetEnabled<PlayerShooter>(true);
        }

        private void SetEnabled<T>(bool enabled) where T : UnityEngine.Behaviour
        {
            var c = GetComponent<T>();
            if (c != null) c.enabled = enabled;
        }
    }
}
