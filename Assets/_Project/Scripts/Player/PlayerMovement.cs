using UnityEngine;
using UnityEngine.InputSystem;

namespace FlagBandits.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private PlayerSettings settings;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController controller;
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction sprintAction;

        private Vector3 verticalVelocity;
        private bool isGrounded;
        private bool isCarryingFlag;
        private bool jumpRequested;
        private Vector2 lastInput;

        private Vector3 spawnPosition;
        private Quaternion spawnRotation;

        public bool IsGrounded => isGrounded;
        public bool IsMoving { get; private set; }
        public bool IsNetworked { get; set; } = false;
        public Vector3 Velocity => controller != null ? controller.velocity : Vector3.zero;
        public Vector3 SpawnPosition => spawnPosition;
        public Quaternion SpawnRotation => spawnRotation;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            SetupInputActions();
        }

        private void OnEnable()
        {
            SetupInputActions();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            jumpAction?.Disable();
            sprintAction?.Disable();
        }

        private void Start()
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        private void SetupInputActions()
        {
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (playerInput != null && playerInput.actions != null)
            {
                var playerMap = playerInput.actions.FindActionMap("Player");
                if (playerMap != null && !playerMap.enabled)
                {
                    playerMap.Enable();
                }

                moveAction = playerInput.actions["Move"];
                jumpAction = playerInput.actions["Jump"];
                sprintAction = playerInput.actions["Sprint"];

                moveAction?.Enable();
                jumpAction?.Enable();
                sprintAction?.Enable();
            }
        }

        private void Update()
        {
            // Buffer jump input in Update so key taps are never missed between network ticks
            if ((jumpAction != null && jumpAction.WasPressedThisFrame())
                || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame))
            {
                jumpRequested = true;
            }

            // In offline mode (no Fusion runner), drive movement from Unity's Update
            if (!IsNetworked)
            {
                ExecuteMovement(Time.deltaTime);
            }
        }

        public void ExecuteMovement(float deltaTime)
        {
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PlayerSettings>();
            }

            if (controller == null)
            {
                controller = GetComponent<CharacterController>();
            }

            if (controller != null && !controller.enabled)
            {
                controller.enabled = true;
            }

            if (controller == null || !controller.enabled) return;

            CheckFallReset();
            CheckGrounded();
            HandleMovement(deltaTime);
            HandleJumpAndGravity(deltaTime);
        }

        private void CheckFallReset()
        {
            float resetThreshold = settings != null ? settings.fallResetY : -10f;
            if (transform.position.y < resetThreshold)
            {
                Debug.LogWarning($"[PlayerMovement] ResetToSpawn triggered! Y={transform.position.y} < {resetThreshold}");
                ResetToSpawn();
            }
        }

        public void ResetToSpawn(bool keepControllerDisabled = false)
        {
            if (controller != null)
            {
                controller.enabled = false;
            }

            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            verticalVelocity = Vector3.zero;

            if (controller != null && !keepControllerDisabled)
            {
                controller.enabled = true;
            }
        }

        public void SetSpawnPoint(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            spawnRotation = rotation;
        }

        private void CheckGrounded()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = settings.groundedGravity;
            }
        }

        private void HandleMovement(float deltaTime)
        {
            Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

            // Direct keyboard fallback in case PlayerInput device pairing or control scheme is unassigned
            if (input.sqrMagnitude < 0.001f && Keyboard.current != null)
            {
                float x = 0f;
                float y = 0f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;

                Vector2 kbInput = new Vector2(x, y);
                if (kbInput.sqrMagnitude > 0.001f)
                {
                    input = kbInput.normalized;
                }
            }

            lastInput = input;
            Vector3 moveDirection = Vector3.zero;

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (input.sqrMagnitude > 0.001f)
            {
                if (cameraTransform != null)
                {
                    Vector3 camForward = cameraTransform.forward;
                    Vector3 camRight = cameraTransform.right;
                    camForward.y = 0f;
                    camRight.y = 0f;
                    if (camForward.sqrMagnitude < 0.001f) camForward = Vector3.forward;
                    if (camRight.sqrMagnitude < 0.001f) camRight = Vector3.right;
                    camForward.Normalize();
                    camRight.Normalize();

                    moveDirection = (camForward * input.y + camRight * input.x).normalized;

                    // Body turns to face camera aim direction when moving
                    Quaternion targetRotation = Quaternion.LookRotation(camForward, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, settings.rotationSpeed * deltaTime);
                }
                else
                {
                    moveDirection = new Vector3(input.x, 0f, input.y).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, settings.rotationSpeed * deltaTime);
                }

                IsMoving = true;
            }
            else
            {
                IsMoving = false;
            }

            bool isSprinting = ((sprintAction != null && sprintAction.IsPressed())
                || (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)))
                && !isCarryingFlag;
            float targetSpeed = isSprinting ? settings.sprintSpeed : settings.walkSpeed;

            if (isCarryingFlag)
            {
                targetSpeed *= settings.flagCarrierSpeedMultiplier;
            }

            controller.Move(moveDirection * (targetSpeed * deltaTime));
        }

        private void HandleJumpAndGravity(float deltaTime)
        {
            if (isGrounded && jumpRequested)
            {
                verticalVelocity.y = Mathf.Sqrt(2f * settings.jumpHeight * -settings.gravity);
                jumpRequested = false;
            }
            else if (!isGrounded)
            {
                jumpRequested = false;
            }

            verticalVelocity.y += settings.gravity * deltaTime;
            controller.Move(verticalVelocity * deltaTime);
        }

        public void FaceAimDirection(bool instant = false)
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (cameraTransform == null) return;

            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(camForward, Vector3.up);
                if (instant)
                {
                    transform.rotation = targetRotation;
                }
                else
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, settings.rotationSpeed * Time.deltaTime);
                }
            }
        }

        public void SetCarryingFlag(bool carrying)
        {
            isCarryingFlag = carrying;
        }

        public void SetCameraTransform(Transform cam)
        {
            cameraTransform = cam;
        }

        [Header("Debug")]
        [SerializeField] private bool showDebugHUD = false;

        private void OnGUI()
        {
            if (!showDebugHUD) return;

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(10, 110, 380, 180), GUI.skin.box);
            GUILayout.Label("<b>--- PlayerMovement Live Debug ---</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label($"Mode: {(IsNetworked ? "Network (FixedUpdateNetwork)" : "Offline (Update)")}");
            GUILayout.Label($"PM: {enabled} | CC: {(controller != null && controller.enabled)} | Grounded: {isGrounded}");
            GUILayout.Label($"MoveAction: {moveAction?.ReadValue<Vector2>()} | Input: {lastInput}");
            if (Keyboard.current != null)
            {
                bool w = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
                bool a = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
                bool s = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
                bool d = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
                GUILayout.Label($"Keyboard: W/Up:{w} A/Left:{a} S/Down:{s} D/Right:{d}");
            }
            GUILayout.Label($"Position: {transform.position}");
            GUILayout.EndArea();
        }
    }
}
