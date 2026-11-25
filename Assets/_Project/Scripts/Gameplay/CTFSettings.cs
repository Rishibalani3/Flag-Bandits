using UnityEngine;

namespace FlagBandits.Gameplay
{
    /// <summary>
    /// Tunable Capture The Flag rules, radii, timers, and visual offsets.
    /// </summary>
    [CreateAssetMenu(fileName = "CTFSettings", menuName = "Flag Bandits/CTF Settings")]
    public class CTFSettings : ScriptableObject
    {
        [Header("Flag Interaction Radii")]
        [Tooltip("Horizontal/3D distance within which a living enemy can pick up a flag")]
        public float flagPickupRadius = 1.8f;

        [Tooltip("Distance within which a living teammate returns their own dropped flag")]
        public float flagReturnRadius = 1.8f;

        [Tooltip("Distance from friendly flag pedestal to capture a carried enemy flag (Step 3.3)")]
        public float flagCaptureRadius = 2.2f;

        [Tooltip("Maximum vertical difference allowed to pick up or return a flag")]
        public float maxInteractionHeightDiff = 2.5f;

        [Header("Flag Timers")]
        [Tooltip("Seconds a dropped flag stays on the field before returning to base")]
        public float flagDroppedReturnDelay = 10f;

        [Tooltip("Brief grace cooldown after dropping/returning before anyone can pick up the flag")]
        public float flagPickupCooldownAfterDrop = 0.5f;

        [Tooltip("Grace delay after a player respawns before they can pick up any flag (prevents latency/teleport grabs)")]
        public float postRespawnPickupDelay = 0.5f;

        [Header("Flag Rules")]
        [Tooltip("If true, touching your own dropped flag immediately returns it to base")]
        public bool touchOwnDroppedFlagToReturn = true;

        [Tooltip("If true, your own flag must be AtBase to capture the enemy flag (Step 3.3)")]
        public bool requireOwnFlagAtBaseToScore = true;

        [Header("Flag Visuals")]
        [Tooltip("Local offset relative to carrier transform while Carried")]
        public Vector3 flagCarryOffset = new Vector3(0f, 1.35f, -0.45f);

        [Tooltip("Forward pitch angle (degrees) when attached to carrier's back")]
        public float carryTiltAngle = -20f;

        [Tooltip("Vertical bobbing amplitude while flag is Dropped")]
        public float droppedBobAmplitude = 0.12f;

        [Tooltip("Vertical bobbing speed (radians/sec) while flag is Dropped")]
        public float droppedBobSpeed = 3.0f;

        [Tooltip("Rotation speed (deg/sec) around Y while flag is Dropped")]
        public float droppedRotateSpeed = 90f;

        [Header("Match Rules (Step 3.3)")]
        [Tooltip("Captures needed to win a match")]
        public int scoreToWin = 3;

        [Tooltip("Match duration in seconds (360 = 6 minutes)")]
        public float matchDurationSeconds = 360f;
    }
}
