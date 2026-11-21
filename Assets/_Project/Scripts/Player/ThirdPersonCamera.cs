using UnityEngine;
using UnityEngine.InputSystem;

namespace FlagBandits.Player
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target & Configuration")]
        [SerializeField] private Transform target;
        [SerializeField] private PlayerSettings settings;

        [Header("Current Orbit")]
        [SerializeField] private float yaw = 0f;
        [SerializeField] private float pitch = 12f;

        private bool isCursorLocked;
        private Camera cam;

        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                SnapBehindTarget();
            }
        }

        public PlayerSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        public bool IsCursorLocked => isCursorLocked;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }

            ResolveTarget();
            SnapBehindTarget();
        }

        private void Start()
        {
            ResolveTarget();
            SnapBehindTarget();
            SetCursorLock(true);
        }

        private void ResolveTarget()
        {
            if (target != null) return;

            var player = Object.FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                var cc = Object.FindFirstObjectByType<CharacterController>();
                if (cc != null) target = cc.transform;
            }
        }

        public void SnapBehindTarget()
        {
            if (target == null) return;

            float pivotHeight = settings != null ? settings.cameraPivotHeight : 1.6f;
            float maxDistance = settings != null ? settings.cameraDistance : 4.5f;
            Vector2 shoulderOffset = settings != null ? settings.cameraShoulderOffset : new Vector2(0.4f, 0.1f);

            Vector3 pivot = target.position + Vector3.up * pivotHeight;
            Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 offsetVector = cameraRotation * Vector3.right * shoulderOffset.x + Vector3.up * shoulderOffset.y;
            Vector3 adjustedPivot = pivot + offsetVector;
            Vector3 backwardDir = cameraRotation * -Vector3.forward;

            transform.position = adjustedPivot + backwardDir * maxDistance;
            transform.rotation = cameraRotation;
        }

        private void Update()
        {
            HandleCursorLock();
            HandleMouseLook();
        }

        private void HandleCursorLock()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLock(false);
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isCursorLocked)
            {
                SetCursorLock(true);
            }
        }

        public void SetCursorLock(bool locked)
        {
            isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleMouseLook()
        {
            if (!isCursorLocked)
            {
                return;
            }

            Vector2 lookDelta = Vector2.zero;

            if (Mouse.current != null)
            {
                lookDelta = Mouse.current.delta.ReadValue();
            }

            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.01f)
                {
                    lookDelta += stick * 120f * Time.deltaTime;
                }
            }

            float sensitivity = settings != null ? settings.mouseSensitivity : 2.0f;
            bool invertY = settings != null && settings.invertY;
            float minPitch = settings != null ? settings.minPitch : -35f;
            float maxPitch = settings != null ? settings.maxPitch : 65f;

            yaw += lookDelta.x * sensitivity * 0.1f;
            pitch += (invertY ? 1f : -1f) * lookDelta.y * sensitivity * 0.1f;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                ResolveTarget();
                if (target == null) return;
            }

            float pivotHeight = settings != null ? settings.cameraPivotHeight : 1.6f;
            float maxDistance = settings != null ? settings.cameraDistance : 4.5f;
            Vector2 shoulderOffset = settings != null ? settings.cameraShoulderOffset : new Vector2(0.4f, 0.1f);
            LayerMask collisionMask = settings != null ? settings.cameraCollisionLayers : ~0;

            Vector3 pivot = target.position + Vector3.up * pivotHeight;
            Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 offsetVector = cameraRotation * Vector3.right * shoulderOffset.x + Vector3.up * shoulderOffset.y;
            Vector3 adjustedPivot = pivot + offsetVector;

            Vector3 backwardDir = cameraRotation * -Vector3.forward;
            float actualDistance = maxDistance;

            // Sphere cast from pivot backwards to check for geometry collision
            RaycastHit[] hits = Physics.SphereCastAll(adjustedPivot, 0.2f, backwardDir, maxDistance, collisionMask);
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider.isTrigger) continue;
                if (hit.transform.root == target.root) continue;
                if (hit.distance < 0.05f) continue;

                float hitDist = Mathf.Clamp(hit.distance - 0.15f, 1.5f, maxDistance);
                if (hitDist < actualDistance)
                {
                    actualDistance = hitDist;
                }
            }

            transform.position = adjustedPivot + backwardDir * actualDistance;
            transform.rotation = cameraRotation;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && isCursorLocked)
            {
                SetCursorLock(false);
            }
        }
    }
}
