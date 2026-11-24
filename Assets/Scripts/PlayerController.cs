using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Cinemachine;

/// <summary>
/// Handles third-person movement, rotation, gravity, and optional animation parameters
/// for the player character using a CharacterController and the Unity Input System.
/// </summary>
public class PlayerController : NetworkBehaviour
{
    // Variables for input configuration and player movement.
    public InputActionAsset InputActions;
    public CharacterController playerCharacterController;

    // Cloned input asset and action map for the local player instance.
    private InputActionAsset runtimeInputActions;
    private InputActionMap runtimePlayerMap;

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
    /// Retrieves the input actions used by the controller before gameplay starts.
    /// Caching these actions avoids repeatedly looking them up during every frame.
    /// </summary>
    private void Awake()
    {
        // Resolve actions from the assigned InputActions asset when available.
        if (InputActions != null)
        {
            var playerMap = InputActions.FindActionMap("Player", false);
            if (playerMap != null)
            {
                playerMoveAction = playerMap.FindAction("Move", false);
                playerJumpAction = playerMap.FindAction("Jump", false);
            }
        }

        if (playerMoveAction == null && InputSystem.actions != null)
        {
            playerMoveAction = InputSystem.actions.FindAction("Move");
        }

        if (playerJumpAction == null && InputSystem.actions != null)
        {
            playerJumpAction = InputSystem.actions.FindAction("Jump");
        }
    }


    /// <summary>
    /// Reads the current movement input and updates movement, rotation, and jumping.
    /// Unity calls this method once per frame, making it suitable for frame-based
    /// input processing and character movement.
    /// </summary>
    private void Update()
    {
        if (IsOwner) {
            if (playerMoveAction != null)
            {
                playerMoveAmount = playerMoveAction.ReadValue<Vector2>();
            }
            PlayerMoveAndRotate();
            Jump();
        }
    }

    
    /// <summary>
    /// Disables the camera, audio listener and the Cinemachine camera for non-owner clients to prevent them from seeing through the player's perspective.
    /// This is called when the player is spawned on the network.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        bool isLocalPlayer = IsOwner;

        // Create and enable a dedicated input asset copy for the owning player.
        if (isLocalPlayer && InputActions != null)
        {
            runtimeInputActions = Instantiate(InputActions);
            runtimePlayerMap = runtimeInputActions.FindActionMap("Player", false);

            if (runtimePlayerMap != null)
            {
                playerMoveAction = runtimePlayerMap.FindAction("Move", false);
                playerJumpAction = runtimePlayerMap.FindAction("Jump", false);
                runtimePlayerMap.Enable();
            }
        }
        else if (runtimePlayerMap != null)
        {
            runtimePlayerMap.Disable();
        }

        // Enable or disable the camera and audio listener based on whether this is the local player.
        Camera cam = playerCamera != null
            ? playerCamera.GetComponent<Camera>()
            : GetComponentInChildren<Camera>(true);

        if (cam != null)
        {
            cam.gameObject.SetActive(isLocalPlayer);

            AudioListener listener = cam.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = isLocalPlayer;
            }
        }

        // Enable or disable the Cinemachine camera based on whether this is the local player.
        var cinemachineCam = GetComponentInChildren<CinemachineCamera>(true);
        if (cinemachineCam != null)
        {
            cinemachineCam.gameObject.SetActive(isLocalPlayer);
        }
    }


    /// <summary>
    /// Disables and cleans up the player's runtime input asset when the networked object despawns.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        // Disable the runtime action map and destroy the cloned input asset.
        if (runtimePlayerMap != null)
        {
            runtimePlayerMap.Disable();
            runtimePlayerMap = null;
        }

        if (runtimeInputActions != null)
        {
            Destroy(runtimeInputActions);
            runtimeInputActions = null;
        }

        base.OnNetworkDespawn();
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

            if(playerJumpAction != null && playerJumpAction.WasPressedThisFrame())
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