using UnityEngine;

namespace FlagBandits.Combat
{
    [CreateAssetMenu(fileName = "WeaponSettings", menuName = "Flag Bandits/Weapon Settings")]
    public class WeaponSettings : ScriptableObject
    {
        [Header("General")]
        [Tooltip("Name of the weapon")]
        public string weaponName = "Basic Pistol";

        [Tooltip("Automatic fire while holding trigger")]
        public bool isAutomatic = true;

        [Header("Combat Stats")]
        [Tooltip("Damage dealt per hit")]
        public float damage = 20f;

        [Tooltip("Rate of fire in rounds per second")]
        public float fireRate = 5f;

        [Tooltip("Maximum effective range in meters")]
        public float maxRange = 60f;

        [Tooltip("Physics impulse force applied to rigidbodies upon impact")]
        public float impactForce = 150f;

        [Header("Visuals & FX")]
        [Tooltip("Duration in seconds that the bullet tracer line is visible")]
        public float tracerDuration = 0.04f;

        [Tooltip("Layer mask of colliders this weapon can hit")]
        public LayerMask hitLayers = ~0;
    }
}
