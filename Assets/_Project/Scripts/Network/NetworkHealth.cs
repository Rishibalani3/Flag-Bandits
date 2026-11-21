using UnityEngine;
using Fusion;

namespace FlagBandits.Network
{
    /// <summary>
    /// Networked wrapper for the Health system. Syncs health state via [Networked] properties.
    /// State Authority (the player's owner) processes damage; other clients see the results.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkHealth : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Combat.HealthSettings settings;

        // ---- Networked State ----
        [Networked] public float CurrentHealth { get; set; }
        [Networked] public NetworkBool IsDead { get; set; }

        // ---- Events (fired on ALL clients via OnChanged) ----
        public event System.Action<float, float> OnHealthChanged;   // current, max
        public event System.Action<Vector3> OnDeath;                // death position
        public event System.Action OnRespawned;

        public float MaxHealth => settings != null ? settings.maxHealth : 100f;
        public float RespawnDelay => settings != null ? settings.respawnDelay : 3f;

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                CurrentHealth = MaxHealth;
                IsDead = false;
            }
            cachedHealth = CurrentHealth;
            cachedIsDead = IsDead;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        // --- Networked property change detection ---
        // Fusion 2 Shared Mode: we detect changes in Render() by comparing
        // against locally cached values, since Changed<T> is not available.
        private float cachedHealth;
        private bool cachedIsDead;

        public override void Render()
        {
            if (Mathf.Abs(CurrentHealth - cachedHealth) > 0.01f)
            {
                cachedHealth = CurrentHealth;
                OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            }

            if ((bool)IsDead != cachedIsDead)
            {
                bool wasDead = cachedIsDead;
                cachedIsDead = IsDead;

                if (IsDead)
                {
                    OnDeath?.Invoke(transform.position);
                }
                else if (wasDead)
                {
                    OnRespawned?.Invoke();
                }
            }
        }

        /// <summary>
        /// Called by the VICTIM'S client to apply validated damage.
        /// Only the State Authority (owner) can modify [Networked] state.
        /// </summary>
        public void ApplyDamage(float amount)
        {
            if (!HasStateAuthority) return;
            if (IsDead) return;
            if (settings != null && settings.isInvulnerable) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            if (CurrentHealth <= 0f)
            {
                IsDead = true;
            }
        }

        /// <summary>
        /// Called on the victim's client when receiving a damage RPC from a shooter.
        /// Validates the hit before applying damage.
        /// </summary>
        public void ValidateAndApplyDamage(float damage, Vector3 shooterPos, float maxRange)
        {
            if (!HasStateAuthority) return;
            if (IsDead) return;

            // Validation: check range
            float distance = Vector3.Distance(transform.position, shooterPos);
            if (distance > maxRange * 1.2f) // 20% tolerance for lag
            {
                Debug.LogWarning($"[NetworkHealth] Rejected damage from {distance:F1}m (max {maxRange}m)");
                return;
            }

            // Validation: check line of sight
            Vector3 dirToShooter = (shooterPos - transform.position).normalized;
            if (Physics.Raycast(transform.position + Vector3.up, dirToShooter, distance, LayerMask.GetMask("Default")))
            {
                // Something blocks LoS — but could be the shooter's own collider, so be lenient
                // For MVP, just apply the damage. Stricter validation in Phase 9.
            }

            ApplyDamage(damage);
        }

        /// <summary>
        /// Respawn: reset health. Only State Authority can call.
        /// </summary>
        public void Respawn()
        {
            if (!HasStateAuthority) return;

            CurrentHealth = MaxHealth;
            IsDead = false;
        }

        public void SetSettings(Combat.HealthSettings newSettings)
        {
            settings = newSettings;
        }

        // ---- RPC: Shooter → Victim ----

        /// <summary>
        /// Called by the shooter's client. Executes on the victim's State Authority.
        /// Validates and applies damage.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_ReceiveDamage(float damage, Vector3 shooterPos, float maxRange)
        {
            ValidateAndApplyDamage(damage, shooterPos, maxRange);
        }
    }
}
