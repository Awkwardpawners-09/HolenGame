using UnityEngine;

public class Turumpo : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform[] points;      // Add multiple points here
    public float speed = 3f;
    public bool loop = true;        // If false, will ping-pong instead

    [Header("Rotation Settings")]
    public bool rotateY = false;    // Check this to rotate
    public float rotationSpeed = 100f;

    private int currentIndex = 0;
    private int direction = 1;

    void Update()
    {
        if (points.Length < 2)
            return;

        MoveBetweenPoints();

        if (rotateY)
            RotateObject();
    }

    void MoveBetweenPoints()
    {
        Transform target = points[currentIndex];

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.position) < 0.01f)
        {
            if (loop)
            {
                currentIndex = (currentIndex + 1) % points.Length;
            }
            else
            {
                currentIndex += direction;

                if (currentIndex >= points.Length || currentIndex < 0)
                {
                    direction *= -1;
                    currentIndex += direction;
                }
            }
        }
    }

    void RotateObject()
    {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }
}