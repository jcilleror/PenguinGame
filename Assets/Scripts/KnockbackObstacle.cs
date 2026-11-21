using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KnockbackObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 moveOffset = new Vector3(0f, 0f, -2.5f);
    public float speed = 1.5f;

    [Header("Knockback Settings")]
    public float knockbackForce = 8f;
    public float upwardLift = 3f;
    public float knockbackDuration = 0.25f;

    private Vector3 startPos;
    private Vector3 endPos;
    private Collider obstacleCollider;
    private bool isKnockingBack = false;

    private void Start()
    {
        startPos = transform.position;
        endPos = startPos + moveOffset;
        obstacleCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (moveOffset == Vector3.zero) return;

        float progress = Mathf.PingPong(Time.time * speed / moveOffset.magnitude, 1f);
        transform.position = Vector3.Lerp(startPos, endPos, progress);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isKnockingBack && other.CompareTag("Player"))
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null)
            {
                // Calculate horizontal push direction from the center of the ball to the center of the player
                Vector3 pushDir = other.bounds.center - obstacleCollider.bounds.center;
                pushDir.y = 0f;

                if (pushDir == Vector3.zero)
                    pushDir = -other.transform.forward;
                else
                    pushDir.Normalize();

                StartCoroutine(ApplyKnockback(cc, pushDir));
            }
        }
    }

    private IEnumerator ApplyKnockback(CharacterController cc, Vector3 direction)
    {
        isKnockingBack = true;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            // Check cc.enabled in case the player hits the respawn floor mid-knockback
            if (cc != null && cc.enabled)
            {
                Vector3 pushVelocity = (direction * knockbackForce) + (Vector3.up * upwardLift);
                cc.Move(pushVelocity * Time.deltaTime);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isKnockingBack = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 start = Application.isPlaying ? startPos : transform.position;
        Vector3 end = start + moveOffset;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.3f);
    }
}