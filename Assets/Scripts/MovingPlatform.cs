using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Offset from Start Position")]
    public Vector3 moveOffset = new Vector3(5f, 0f, 0f);
    public float speed = 2f;

    private Vector3 startPos;
    private Vector3 endPos;

    private void Start()
    {
        startPos = transform.position;
        endPos = startPos + moveOffset;
    }

    private void Update()
    {
        if (moveOffset == Vector3.zero) return;

        float progress = Mathf.PingPong(Time.time * speed / moveOffset.magnitude, 1f);
        transform.position = Vector3.Lerp(startPos, endPos, progress);
    }

    // Parent the player to the platform so they move along with it smoothly
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            other.transform.SetParent(transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            other.transform.SetParent(null);
    }
}