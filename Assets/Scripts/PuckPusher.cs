using UnityEngine;

public class PuckPusher : MonoBehaviour
{
    [Header("Shot / Push Settings")]
    [Tooltip("Impulse force applied to the puck when the penguin hits it")]
    public float hitForce = 8f;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Check if the collided object has a Rigidbody and the "Puck" tag
        Rigidbody puckRb = hit.collider.attachedRigidbody;
        if (puckRb == null || !hit.gameObject.CompareTag("Puck")) return;

        // Avoid pushing the puck downward into the ice floor
        if (hit.moveDirection.y < -0.3f) return;

        // Calculate a purely horizontal direction based on the penguin's movement
        Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z).normalized;

        // Apply an instant impulse so the puck shoots and slides across the ice
        puckRb.AddForce(pushDirection * hitForce, ForceMode.Impulse);
    }
}