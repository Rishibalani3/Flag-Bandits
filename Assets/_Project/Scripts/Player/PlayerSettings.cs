using UnityEngine;

namespace FlagBandits.Player
{
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "Flag Bandits/Player Settings")]
    public class PlayerSettings : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Base walking speed in m/s")]
        public float walkSpeed = 6f;

        [Tooltip("Sprint speed in m/s")]
        public float sprintSpeed = 9f;

        [Tooltip("Flag carrier speed penalty multiplier")]
        public float flagCarrierSpeedMultiplier = 0.75f;

        [Header("Jump & Gravity")]
        [Tooltip("Jump apex height in meters")]
        public float jumpHeight = 1.8f;

        [Tooltip("Gravity acceleration (negative value)")]
        public float gravity = -20f;

        [Tooltip("Small downward force applied when grounded to stick to ground")]
        public float groundedGravity = -2f;

        [Header("Rotation")]
        [Tooltip("How quickly the character turns to face move direction")]
        public float rotationSpeed = 15f;
    }
}
