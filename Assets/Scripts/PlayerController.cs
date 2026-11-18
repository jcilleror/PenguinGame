using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : MonoBehaviour
{
    public CharacterController player;
    private Animator animator;
    
    // Flag to enable or disable player movement
    public bool canMove = true;
    
    // Player movement
    private Vector3 moveInput;
    public float gravity = 9.81f;
    private float verticalVelocity;

    // Animation smoothing variables
    [Header("Animation Smoothing")]
    // Velocity of the animation smoothing for X and Z axes
    public float animationSmoothTime = 0.1f;
    // Variables to store the current velocity of the animation smoothing for X and Z axes
    private float currentVelocityX;
    private float currentVelocityY;
    // Variables required for the SmoothDamp function to smoothly transition between animation states
    private float velocityXRef;
    private float velocityYRef;

    // Camera reference (PlayerRotation)
    public Camera playerCamera;
    private Vector3 camForward;
    private Vector3 camRight;
    private Vector3 movePlayer;

    [Header("Camera Crouch Settings")]
    public float standingCameraPosX = 0.019f;       // Camera position along the X-axis when standing
    public float standingCameraPosY = 2.3f;        // Normal camera height when standing
    public float standingCameraPosZ = 0.35f;        // Camera position along the Z-axis when standing
    public float crouchCameraPosX = -0.261f;        // Camera position along the X-axis when crouching
    public float crouchCameraPosY = 1.6f;          // Camera height when crouching
    public float crouchCameraPosZ = 0.44f;          // Camera position along the Z-axis when crouching
    public float cameraTransitionSpeed = 10f;       // Speed at which the camera transitions between heights

    // CharacterController reference for crouching
    [Header("Crouch Settings")]
    public float WalkVelocity = 5f;
    public float CrouchVelocity = 2.5f;
    public float StandingHeight = 2.4f;
    public float CrouchingHeight = 1.8f;

    private float CurrentVelocity;
    public bool isCrouching = false;


    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        player = GetComponent<CharacterController>();
        CurrentVelocity = WalkVelocity;
    }


    private void Update()
    {
        // 1. Check for crouch input and toggle crouch state.
        if (Keyboard.current != null && Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            isCrouching = !isCrouching; // Invert the crouch state when Left Control is pressed.

            if (isCrouching)
            {
                Crouch();
            }
            else
            {
                Stand();
            }
        }
        
        // 2. Check if player movement is enabled and update player movement accordingly.
        if (canMove)
        {
            Vector3 input = Vector3.zero;

            // Check for keyboard input
            if (Keyboard.current != null)
            {
                // Read keyboard input for movement
                float right = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) ? 1f : 0f;
                float left = (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) ? 1f : 0f;
                float forward = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) ? 1f : 0f;
                float backward = (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) ? 1f : 0f;

                // Combine keyboard input into a single vector
                input.x = right - left;
                input.y = 0f;
                input.z = forward - backward;
            }

            // Check for gamepad input if no keyboard input is detected
            if (Gamepad.current != null)
            {
                // Read gamepad input for movement
                Vector2 stickInput = Gamepad.current.leftStick.ReadValue();
                input.x = stickInput.x;
                input.z = stickInput.y;
                input.y = 0f;
            }

            // Clamp the input vector to ensure consistent movement speed in all directions
            moveInput = Vector3.ClampMagnitude(input, 1f);

            // Update animation state from actual movement input.
            if (animator != null)
            {
                float targetX = moveInput.x;
                float targetY = moveInput.z;

                // We use SmoothDamp to smoothly transition the animation parameters based on player input, creating a more natural movement feel.
                // Switch from 'currentVelocity' to 'target' in time 'animationSmoothTime'.
                currentVelocityX = Mathf.SmoothDamp(currentVelocityX, targetX, ref velocityXRef, animationSmoothTime);
                currentVelocityY = Mathf.SmoothDamp(currentVelocityY, targetY, ref velocityYRef, animationSmoothTime);
                
                // Set the animator parameters for movement speed and walking state
                animator.SetFloat("XSpeed", currentVelocityX);
                animator.SetFloat("YSpeed", currentVelocityY);
            }

            // Rotate movement direction based on camera orientation
            camDirection();
            movePlayer = moveInput.x * camRight + moveInput.z * camForward;

            // Apply the current movement speed (walk or crouch)
            movePlayer *= CurrentVelocity;

            // Apply gravity to the player
            setGravity();

            // Move the player using the CharacterController
            player.Move(movePlayer * Time.deltaTime);

            // Update the camera height based on crouch state
            UpdateCameraHeight();
        }
    }


    void Crouch()
    {
        animator.SetBool("isCrouching", true);
        CurrentVelocity = CrouchVelocity;
        
        // Adjust the CharacterController's height and center for crouching to the middle.
        if (player != null)
        {
            player.height = CrouchingHeight;
            player.center = new Vector3(0, CrouchingHeight / 2 + 0.025f, 0);
        }
    }


    void Stand()
    {
        animator.SetBool("isCrouching", false);
        CurrentVelocity = WalkVelocity;
        
        // Restore the CharacterController to its original height
        if (player != null)
        {
            player.height = StandingHeight;
            player.center = new Vector3(0, StandingHeight / 2, 0);
        }
    }  


    /**** 
    ***** Calculate the camera's forward and right vectors for movement direction 
    ****/
    void camDirection()
    {
        // Get the forward and right vectors of the camera
        camForward = playerCamera.transform.forward;
        camRight = playerCamera.transform.right;

        // Project the forward and right vectors onto the horizontal plane (y = 0)
        camForward.y = 0f;
        camRight.y = 0f;

        // Normalize the vectors to ensure consistent movement speed
        camForward = camForward.normalized;
        camRight = camRight.normalized;
    }


    /****
    ***** Apply gravity to the player and ensure they stay grounded when on the ground
    ****/
    void setGravity()
    {
        // Check if the player is grounded and apply gravity accordingly
        if (player.isGrounded && verticalVelocity < 0f)
        {
            // Keeps controller stuck to ground without accumulating downward speed.
            // Note: 2f is a small value to ensure the player stays grounded without causing a noticeable 
            //       jump when landing.
            verticalVelocity = -gravity * Time.deltaTime;               
        }
        else
        {
            // Accelerating downwards when in the air, simulating gravity.
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // Vertical velocity is in m/s; Move() multiplies by deltaTime once.
        movePlayer.y = verticalVelocity;
    }


    void UpdateCameraHeight()
    {
        if (playerCamera == null) return;

        // Determine the target X,Y and Z positions based on whether the player is crouching or standing
        float targetXPosition = isCrouching ? crouchCameraPosX : standingCameraPosX;
        float targetYPosition = isCrouching ? crouchCameraPosY : standingCameraPosY;
        float targetZPosition = isCrouching ? crouchCameraPosZ : standingCameraPosZ;

        // Get the current local position of the camera
        Vector3 newCameraPosition = playerCamera.transform.localPosition;

        // Make the X,Y and Z position of the camera smoothly transition to the target height
        newCameraPosition.x = Mathf.Lerp(playerCamera.transform.localPosition.x, targetXPosition, Time.deltaTime * cameraTransitionSpeed);
        newCameraPosition.y = Mathf.Lerp(playerCamera.transform.localPosition.y, targetYPosition, Time.deltaTime * cameraTransitionSpeed);
        newCameraPosition.z = Mathf.Lerp(playerCamera.transform.localPosition.z, targetZPosition, Time.deltaTime * cameraTransitionSpeed);

        // Apply the new position
        playerCamera.transform.localPosition = newCameraPosition;
    }

}
