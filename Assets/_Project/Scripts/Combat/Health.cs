using System;
using UnityEngine;

namespace FlagBandits.Combat
{
    public class Health : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private HealthSettings settings;

        [Header("Runtime State")]
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isDead;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => settings != null ? settings.maxHealth : 100f;
        public bool IsDead => isDead;

        public event Action<float, float> OnHealthChanged; // current, max
        public event Action<float, Vector3, Vector3> OnDamaged; // amount, hitPoint, hitNormal
        public event Action<Vector3> OnDeath; // death position

        private void Awake()
        {
            ResetHealth();
        }

        public void ResetHealth()
        {
            currentHealth = MaxHealth;
            isDead = false;
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (isDead)
            {
                return;
            }

            if (settings != null && settings.isInvulnerable)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
            OnDamaged?.Invoke(amount, hitPoint, hitNormal);

            if (currentHealth <= 0f)
            {
                Die(hitPoint);
            }
        }

        public void Heal(float amount)
        {
            if (isDead)
            {
                return;
            }

            currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        private void Die(Vector3 deathPoint)
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            OnDeath?.Invoke(deathPoint);
        }

        public void SetSettings(HealthSettings newSettings)
        {
            settings = newSettings;
            if (!isDead)
            {
                currentHealth = Mathf.Min(currentHealth, MaxHealth);
                OnHealthChanged?.Invoke(currentHealth, MaxHealth);
            }
        }
    }
}
