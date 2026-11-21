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
        [Tooltip("How quickly the character turns to face aim/move direction")]
        public float rotationSpeed = 15f;

        [Header("Camera & Mouse Look")]
        [Tooltip("Mouse look sensitivity multiplier")]
        public float mouseSensitivity = 2.0f;

        [Tooltip("Invert vertical look axis")]
        public bool invertY = false;

        [Tooltip("Minimum vertical look angle (looking up)")]
        public float minPitch = -35f;

        [Tooltip("Maximum vertical look angle (looking down)")]
        public float maxPitch = 65f;

        [Tooltip("Camera distance from target")]
        public float cameraDistance = 5.0f;

        [Tooltip("Camera pivot height above player")]
        public float cameraPivotHeight = 1.6f;

        [Tooltip("Side shoulder offset (X = right/left, Y = up/down)")]
        public Vector2 cameraShoulderOffset = new Vector2(0.35f, 0f);

        [Tooltip("Obstacle layers that occlude the camera view")]
        public LayerMask cameraCollisionLayers = ~0;

        [Header("Fall & Boundaries")]
        [Tooltip("Y-level below which the player resets back to spawn")]
        public float fallResetY = -10f;
    }
}
