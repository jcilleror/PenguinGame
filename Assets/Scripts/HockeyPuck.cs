using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HockeyPuck : MonoBehaviour
{
    [Header("Puck Physics Limits")]
    public float maxSpeed = 16f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // Clamp maximum velocity so the puck never tunnels through rink walls
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
}