using UnityEngine;

public class Respawn : MonoBehaviour
{
    [Tooltip("Drag the empty GameObject placed above the starting wooden platform here")]
    public Transform startingPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TeleportToStart(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TeleportToStart(collision.gameObject);
        }
    }

    private void TeleportToStart(GameObject player)
    {
        if (startingPoint == null)
        {
            Debug.LogWarning("Starting Point is not assigned in the Respawn script!");
            return;
        }

        // Temporarily disable CharacterController (if used) to allow direct position override
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = startingPoint.position;
        player.transform.rotation = startingPoint.rotation;

        // Reset physics velocity if the player uses a Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (cc != null) cc.enabled = true;
    }
}