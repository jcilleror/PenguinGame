using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PenguinBellyDash : NetworkBehaviour
{
    [Header("Map Restriction")]
    [Tooltip("If true, the dash only works when HockeyScoreManager is present or the scene name matches")]
    public bool restrictToHockeyMap = true;
    [Tooltip("Fallback scene name keyword if HockeyScoreManager is not found")]
    public string hockeySceneKeyword = "Hokey";

    [Header("Dash Settings")]
    [Tooltip("Trigger the belly slide dash with Right Mouse Button")]
    public bool useRightMouseButton = true;
    [Tooltip("Optional keyboard key to also trigger the dash (set to None to disable)")]
    public Key dashKey = Key.None;
    [Tooltip("Forward impulse speed during the belly slide")]
    public float dashSpeed = 9f;
    [Tooltip("How long the belly slide lasts in seconds")]
    public float dashDuration = 0.45f;
    [Tooltip("Cooldown time in seconds before dashing again")]
    public float dashCooldown = 1.5f;

    [Header("Belly Tilt Visuals (Assign ClayerPlaceHolder)")]
    [Tooltip("Child object containing the 3D penguin mesh (ClayerPlaceHolder)")]
    public Transform visualModel;
    [Tooltip("Forward pitch angle in degrees (85 = flat on belly)")]
    public float bellyPitchAngle = 85f;
    [Tooltip("Vertical offset while sliding (keep >= 0 so the mesh stays above ice Y = 0.235)")]
    public float bellyHeightOffset = 0.12f;
    [Tooltip("How fast the penguin tilts onto its belly and stands back up")]
    public float tiltSmoothSpeed = 18f;

    private CharacterController characterController;
    private Rigidbody rb;

    private bool isDashing = false;
    private float nextDashTime = 0f;
    private Vector3 activeDashDirection;

    private Quaternion defaultVisualLocalRot;
    private Vector3 defaultVisualLocalPos;

    private NetworkVariable<bool> netIsDashing = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Awake()
    {
        // Search on self or child in case the prefab has a nested player object
        characterController = GetComponentInChildren<CharacterController>();
        rb = GetComponentInChildren<Rigidbody>();

        if (visualModel != null)
        {
            defaultVisualLocalRot = visualModel.localRotation;
            defaultVisualLocalPos = visualModel.localPosition;
        }
    }

    private void Update()
    {
        if (!IsInHockeyMap()) return;

        UpdateBellyTiltVisual();

        if (!IsOwner) return;

        if (!isDashing && Time.time >= nextDashTime && IsDashInputPressed())
        {
            StartCoroutine(PerformBellyDash());
        }
    }

    private void FixedUpdate()
    {
        // Apply physics velocity in FixedUpdate if the player uses a non-kinematic Rigidbody
        if (!IsOwner || !isDashing) return;

        if (rb != null && !rb.isKinematic && (characterController == null || !characterController.enabled))
        {
            Vector3 targetVel = activeDashDirection * dashSpeed;
            targetVel.y = Mathf.Min(rb.linearVelocity.y, 0f);
            rb.linearVelocity = targetVel;
        }
    }

    private bool IsDashInputPressed()
    {
        bool rightClickPressed = useRightMouseButton &&
                                 Mouse.current != null &&
                                 Mouse.current.rightButton.wasPressedThisFrame;

        bool keyPressed = dashKey != Key.None &&
                          Keyboard.current != null &&
                          Keyboard.current[dashKey].wasPressedThisFrame;

        return rightClickPressed || keyPressed;
    }

    private bool IsInHockeyMap()
    {
        if (!restrictToHockeyMap) return true;

        if (HockeyScoreManager.Instance != null) return true;

        string currentScene = SceneManager.GetActiveScene().name;
        return !string.IsNullOrEmpty(hockeySceneKeyword) &&
               currentScene.IndexOf(hockeySceneKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private IEnumerator PerformBellyDash()
    {
        isDashing = true;
        netIsDashing.Value = true;
        nextDashTime = Time.time + dashCooldown;

        // Capture horizontal forward direction from the visual model (or root if null)
        Transform forwardRef = visualModel != null ? visualModel : transform;
        Vector3 flatForward = forwardRef.forward;
        flatForward.y = 0f;
        activeDashDirection = flatForward.sqrMagnitude > 0.001f ? flatForward.normalized : transform.forward;

        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            if (!IsInHockeyMap()) break;

            if (characterController != null && characterController.enabled)
            {
                // Move horizontally with strong dash impulse, keeping vertical velocity neutral
                Vector3 moveStep = activeDashDirection * dashSpeed * Time.deltaTime;
                characterController.Move(moveStep);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        netIsDashing.Value = false;
    }

    private void UpdateBellyTiltVisual()
    {
        if (visualModel == null) return;

        bool sliding = netIsDashing.Value;

        Quaternion targetRot = sliding
            ? defaultVisualLocalRot * Quaternion.Euler(bellyPitchAngle, 0f, 0f)
            : defaultVisualLocalRot;

        Vector3 targetPos = sliding
            ? defaultVisualLocalPos + new Vector3(0f, bellyHeightOffset, 0f)
            : defaultVisualLocalPos;

        visualModel.localRotation = Quaternion.Slerp(
            visualModel.localRotation,
            targetRot,
            Time.deltaTime * tiltSmoothSpeed
        );

        visualModel.localPosition = Vector3.Lerp(
            visualModel.localPosition,
            targetPos,
            Time.deltaTime * tiltSmoothSpeed
        );
    }

    public float GetCooldownProgress()
    {
        if (Time.time >= nextDashTime) return 1f;
        return 1f - Mathf.Clamp01((nextDashTime - Time.time) / dashCooldown);
    }
}