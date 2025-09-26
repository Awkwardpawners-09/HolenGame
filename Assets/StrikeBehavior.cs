using UnityEngine;

public class StrikeBehavior : MonoBehaviour
{
    public float forceMultiplier = 10f; // How much force is transferred to the hit object

    private void OnCollisionEnter(Collision collision)
    {
        // Get the RigidBody of the object this one collided with
        Rigidbody targetRigidbody = collision.gameObject.GetComponent<Rigidbody>();

        if (targetRigidbody != null)
        {
            // Transfer the force to the target
            Vector3 collisionForce = GetComponent<Rigidbody>().velocity * forceMultiplier;
            targetRigidbody.AddForce(collisionForce, ForceMode.Impulse);
        }

        // Stop the current object's movement after impact
        GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
}
