using UnityEngine;

namespace FlagBandits.Combat
{
    [CreateAssetMenu(fileName = "HealthSettings", menuName = "Flag Bandits/Health Settings")]
    public class HealthSettings : ScriptableObject
    {
        [Header("Health Values")]
        [Tooltip("Maximum starting health")]
        public float maxHealth = 100f;

        [Tooltip("Whether the entity can take damage")]
        public bool isInvulnerable = false;

        [Tooltip("Seconds before respawning after death")]
        public float respawnDelay = 3f;
    }
}
