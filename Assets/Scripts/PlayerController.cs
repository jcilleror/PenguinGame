using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles third-person movement, rotation, gravity, and optional animation parameters
/// for the player character using a CharacterController and the Unity Input System.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("CharacterController component attached to the player GameObject.")]
    public CharacterController controller;

    [Tooltip("Transform of the camera used to calculate movement direction relative to view.")]
    public Transform cameraTransform;

    private Animator animator;

    [Header("Movement Settings")]
    [Tooltip("Enables or disables player movement input and motion.")]
    public bool canMove = true;

    [Tooltip("Horizontal movement speed in meters per second.")]
    public float moveSpeed = 5f;

    [Tooltip("Turn speed used to smoothly rotate the character toward the movement direction.")]
    public float rotationSpeed = 12f;

    [Header("Gravity Settings")]
    [Tooltip("Downward acceleration applied when airborne (m/s^2).")]
    public float gravity = 9.81f;

    [Tooltip("Small downward velocity applied while grounded to keep CharacterController in contact with the ground.")]
    public float groundedGravity = -2f;

    private float verticalVelocity;

    [Header("Animation Settings (Optional)")]
    [Tooltip("Smooth damp time for blending the speed parameter.")]
    public float animationSmoothTime = 0.1f;

    [Tooltip("Name of the Float parameter in the Animator controller (if present).")]
    public string speedParamName = "Speed";

    private float currentAnimSpeed;
    private float animSpeedRef;

    // Normalized horizontal input vector (x = horizontal/strafe, z = forward/back)
    private Vector3 moveInput;

    private void Awake()
    {
        // Fallback to the CharacterController on this GameObject if not assigned in the Inspector
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        // Look for an Animator in child objects (common for character rigs/models)
        animator = GetComponentInChildren<Animator>();

        // Fallback to Camera.main if no camera transform is explicitly assigned
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        // Stop processing if movement is disabled or CharacterController is missing
        if (!canMove || controller == null) return;

        ReadMovementInput();
        MovePlayer();
        UpdateAnimator();
    }

    /// <summary>
    /// Reads movement input from keyboard (WASD / Arrow keys) or gamepad left stick.
    /// </summary>
    private void ReadMovementInput()
    {
        Vector3 input = Vector3.zero;

        // Keyboard input
        if (Keyboard.current != null)
        {
            float right = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) ? 1f : 0f;
            float left = (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) ? 1f : 0f;
            float forward = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) ? 1f : 0f;
            float backward = (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) ? 1f : 0f;

            input.x = right - left;
            input.z = forward - backward;
        }

        // Gamepad left stick input (overrides if stick is actively deflected)
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.001f)
            {
                input.x = stick.x;
                input.z = stick.y;
            }
        }

        // Clamp magnitude to 1 so diagonal movement is not faster than straight movement
        moveInput = Vector3.ClampMagnitude(input, 1f);
    }

    /// <summary>
    /// Converts input into camera-relative world movement, rotates the character,
    /// applies gravity, and moves the CharacterController.
    /// </summary>
    private void MovePlayer()
    {
        Vector3 horizontalMove = Vector3.zero;

        if (moveInput.sqrMagnitude > 0.001f)
        {
            Vector3 camForward = Vector3.forward;
            Vector3 camRight = Vector3.right;

            // Flatten camera forward and right vectors onto the XZ plane
            if (cameraTransform != null)
            {
                camForward = cameraTransform.forward;
                camRight = cameraTransform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();
            }

            // Compute desired world-space movement direction
            Vector3 moveDirection = (camForward * moveInput.z + camRight * moveInput.x).normalized;
            horizontalMove = moveDirection * moveSpeed;

            // Smoothly rotate character to face the movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Apply grounded snap or airborne gravity
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // Combine horizontal velocity and vertical gravity velocity
        Vector3 finalVelocity = horizontalMove;
        finalVelocity.y = verticalVelocity;

        // CharacterController.Move expects a displacement delta per frame
        controller.Move(finalVelocity * Time.deltaTime);
    }

    /// <summary>
    /// Smoothly updates the Animator speed parameter if an Animator and matching parameter exist.
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator == null) return;

        float targetSpeed = moveInput.magnitude;
        currentAnimSpeed = Mathf.SmoothDamp(currentAnimSpeed, targetSpeed, ref animSpeedRef, animationSmoothTime);

        // Only set the parameter if it exists on the Animator to avoid console warnings
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == speedParamName && param.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(speedParamName, currentAnimSpeed);
                break;
            }
        }
    }
}