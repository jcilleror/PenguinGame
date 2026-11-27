using System.Collections;
using UnityEngine;

public class StadiumSpectator : MonoBehaviour
{
    [Header("Idle Cheering (Continuous)")]
    [Tooltip("Height of the small cheering hop")]
    public float hopHeight = 0.15f;
    [Tooltip("Speed of the cheering rhythm")]
    public float cheerSpeed = 4f;
    [Tooltip("Side-to-side wobble angle")]
    public float wobbleAngle = 10f;

    [Header("Goal Celebration Burst")]
    [Tooltip("Higher jump height when a goal is scored")]
    public float celebrationJumpHeight = 0.30f;
    [Tooltip("Jump speed during goal celebration")]
    public float celebrationSpeed = 8f;
    public float celebrationDuration = 2.0f;

    [Header("Optional Target (Track the puck)")]
    public Transform lookAtTarget;
    public bool trackTargetHorizontally = true;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private float randomPhaseOffset;
    private float individualSpeedMultiplier;
    private bool isCelebrating = false;

    private void Start()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;

        // Randomize phase and speed so spectators don't move like synchronized robots
        randomPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
        individualSpeedMultiplier = Random.Range(0.85f, 1.2f);
    }

    private void Update()
    {
        if (trackTargetHorizontally && lookAtTarget != null)
        {
            Vector3 direction = lookAtTarget.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 3f);
                baseLocalRotation = transform.localRotation;
            }
        }

        if (isCelebrating) return;

        float time = (Time.time * cheerSpeed * individualSpeedMultiplier) + randomPhaseOffset;

        // Continuous happy hop
        float yOffset = Mathf.Abs(Mathf.Sin(time)) * hopHeight;
        transform.localPosition = baseLocalPosition + Vector3.up * yOffset;

        // Side-to-side wobble
        float zTilt = Mathf.Sin(time) * wobbleAngle;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, zTilt);
    }

    public void TriggerGoalCelebration()
    {
        if (!isCelebrating && gameObject.activeInHierarchy)
        {
            StartCoroutine(CelebrateRoutine());
        }
    }

    private IEnumerator CelebrateRoutine()
    {
        isCelebrating = true;
        float elapsed = 0f;

        while (elapsed < celebrationDuration)
        {
            float time = (elapsed * celebrationSpeed * individualSpeedMultiplier) + randomPhaseOffset;

            // Higher celebratory jumps without spinning
            float jumpCycle = Mathf.Abs(Mathf.Sin(time));
            transform.localPosition = baseLocalPosition + Vector3.up * (jumpCycle * celebrationJumpHeight);

            // Slightly more energetic side-to-side wobble
            float zTilt = Mathf.Sin(time) * (wobbleAngle * 1.4f);
            transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, zTilt);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;
        isCelebrating = false;
    }
}