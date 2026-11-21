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

        public bool IsGrounded => isGrounded;
        public bool IsMoving { get; private set; }
        public Vector3 Velocity => controller.velocity;

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

        private void SetupInputActions()
        {
            if (playerInput != null && playerInput.actions != null)
            {
                moveAction = playerInput.actions["Move"];
                jumpAction = playerInput.actions["Jump"];
                sprintAction = playerInput.actions["Sprint"];
            }
        }

        private void Update()
        {
            if (settings == null)
            {
                return;
            }

            CheckGrounded();
            HandleMovement();
            HandleJumpAndGravity();
        }

        private void CheckGrounded()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = settings.groundedGravity;
            }
        }

        private void HandleMovement()
        {
            Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector3 moveDirection = Vector3.zero;

            if (input.sqrMagnitude > 0.001f)
            {
                if (cameraTransform != null)
                {
                    Vector3 camForward = cameraTransform.forward;
                    Vector3 camRight = cameraTransform.right;
                    camForward.y = 0f;
                    camRight.y = 0f;
                    camForward.Normalize();
                    camRight.Normalize();

                    moveDirection = (camForward * input.y + camRight * input.x).normalized;
                }
                else
                {
                    moveDirection = new Vector3(input.x, 0f, input.y).normalized;
                }

                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, settings.rotationSpeed * Time.deltaTime);

                IsMoving = true;
            }
            else
            {
                IsMoving = false;
            }

            bool isSprinting = sprintAction != null && sprintAction.IsPressed() && !isCarryingFlag;
            float targetSpeed = isSprinting ? settings.sprintSpeed : settings.walkSpeed;

            if (isCarryingFlag)
            {
                targetSpeed *= settings.flagCarrierSpeedMultiplier;
            }

            controller.Move(moveDirection * (targetSpeed * Time.deltaTime));
        }

        private void HandleJumpAndGravity()
        {
            bool jumpPressed = jumpAction != null && jumpAction.WasPressedThisFrame();

            if (isGrounded && jumpPressed)
            {
                verticalVelocity.y = Mathf.Sqrt(2f * settings.jumpHeight * -settings.gravity);
            }

            verticalVelocity.y += settings.gravity * Time.deltaTime;
            controller.Move(verticalVelocity * Time.deltaTime);
        }

        public void SetCarryingFlag(bool carrying)
        {
            isCarryingFlag = carrying;
        }

        public void SetCameraTransform(Transform cam)
        {
            cameraTransform = cam;
        }
    }
}
