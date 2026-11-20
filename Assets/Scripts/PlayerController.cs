using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles third-person movement, rotation, gravity, and optional animation parameters
/// for the player character using a CharacterController and the Unity Input System.
/// </summary>
public class PlayerController : MonoBehaviour
{
    // Variables for input configuration and player movement.
    public InputActionAsset InputActions;
    public CharacterController playerCharacterController;

    // Variables for the player movement and jump input actions.
    private InputAction playerMoveAction;
    private InputAction playerJumpAction;

    [SerializeField]
    private Transform playerCamera;

    private Vector2 playerMoveAmount;
    [SerializeField]
    private float jumpHeight = 5.0f;
    
    [SerializeField]
    private float playerWalkSpeed = 5.0f;
    [SerializeField]
    private float playerRotateDampening = 0.1f;

    // Variable for storing the current rotation smoothing velocity.
    private float turnSmoothingVelocity;

    private float verticalVelocity = 0f;
    private float gravity = -9.81f;


    /// <summary>
    /// Enables the player's input action map when this component becomes active.
    /// Keeping this tied to the component lifecycle prevents player input from
    /// remaining active while the player object is disabled.
    /// </summary>
    private void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
    }


    /// <summary>
    /// Disables the player's input action map when this component is deactivated.
    /// This mirrors <see cref="OnEnable"/> and avoids reading input from an
    /// inactive player object.
    /// </summary>
    private void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
    }


    /// <summary>
    /// Retrieves the input actions used by the controller before gameplay starts.
    /// Caching these actions avoids repeatedly looking them up during every frame.
    /// </summary>
    private void Awake()
    {
        playerMoveAction = InputSystem.actions.FindAction("Move");
        playerJumpAction = InputSystem.actions.FindAction("Jump");
    }


    /// <summary>
    /// Reads the current movement input and updates movement, rotation, and jumping.
    /// Unity calls this method once per frame, making it suitable for frame-based
    /// input processing and character movement.
    /// </summary>
    private void Update()
    {
        playerMoveAmount = playerMoveAction.ReadValue<Vector2>();
        PlayerMoveAndRotate();
        Jump();
    }

    
    /// <summary>
    /// Moves the player relative to the camera's horizontal orientation and rotates
    /// the character smoothly towards the direction of movement. It also applies
    /// the current vertical velocity so gravity and jumping use the same movement
    /// operation as horizontal motion.
    /// </summary>
    private void PlayerMoveAndRotate()
    {
        // Convert 2D input into a horizontal 3D direction and normalize it so
        // diagonal input does not make the player move faster.
        Vector3 playerDirection = new Vector3(playerMoveAmount.x, 0f, playerMoveAmount.y).normalized;
        Vector3 verticalMove = new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime;

        if(playerDirection.magnitude >= 0.1f)
        {
            // Add the camera's yaw so "forward" input follows the camera view
            // instead of always using the world's forward direction.
            float targetAngle = Mathf.Atan2(playerDirection.x, playerDirection.z) * Mathf.Rad2Deg + playerCamera.eulerAngles.y;
            float smoothTargetAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothingVelocity, playerRotateDampening);

            transform.rotation = Quaternion.Euler(0f, smoothTargetAngle, 0f);

            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            playerCharacterController.Move(moveDirection.normalized * playerWalkSpeed * Time.deltaTime + verticalMove);
        }
        else
        {
            // Continue applying vertical movement while the player is not moving
            // horizontally, allowing the character to fall or land correctly.
            playerCharacterController.Move(verticalMove);
        }
    }


    /// <summary>
    /// Updates the player's vertical velocity. When grounded, the velocity is
    /// reset to a small downward value to keep the CharacterController grounded;
    /// otherwise gravity is accumulated over time. A jump replaces that value
    /// with the configured jump velocity.
    /// </summary>
    private void Jump()
    {
        if(playerCharacterController.isGrounded)
        {
            // A slight downward force helps maintain contact with uneven ground.
            verticalVelocity = -1f;

            if(playerJumpAction.WasPressedThisFrame())
            {
                verticalVelocity = jumpHeight;
            }
        }
        else
        {
            // Multiply gravity by deltaTime so falling behaves consistently
            // regardless of the frame rate.
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

}