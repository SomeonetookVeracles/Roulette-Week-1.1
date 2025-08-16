using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Path Points")]
    public Transform pointA; // Start point
    public Transform pointB; // End point
    public float speed = 2f; // Movement speed

    private bool goingToB = true; // Direction flag
    private Transform playerParentBefore;

    void Update()
    {
        // Determine target
        Transform target = goingToB ? pointB : pointA;

        // Move platform toward target
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        // If platform reached target, reverse direction
        if (Vector3.Distance(transform.position, target.position) < 0.01f)
        {
            goingToB = !goingToB;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            playerParentBefore = collision.transform.parent;
            collision.transform.SetParent(transform);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.transform.SetParent(playerParentBefore);
        }
    }
}
