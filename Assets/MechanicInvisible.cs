using UnityEngine;

/// <summary>
/// When this GameObject collides with another GameObject that has the specified tag,
/// the MeshRenderers on all assigned target objects will be disabled.
/// </summary>
public class MechanicInvisible : MonoBehaviour
{
    [Header("Collision Settings")]
    [Tooltip("The tag to look for on the colliding object.")]
    public string targetTag = "Player";

    [Header("Objects to Hide")]
    [Tooltip("Assign all GameObjects whose MeshRenderer should be turned off on collision.")]
    public GameObject[] objectsToHide;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            DisableMeshRenderers();
        }
    }

    // Also supports trigger colliders
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(targetTag))
        {
            DisableMeshRenderers();
        }
    }

    private void DisableMeshRenderers()
    {
        if (objectsToHide == null || objectsToHide.Length == 0)
        {
            Debug.LogWarning($"[CollisionMeshToggle] No objects assigned on '{gameObject.name}'.");
            return;
        }

        foreach (GameObject obj in objectsToHide)
        {
            if (obj == null) continue;

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = false;
            }
            else
            {
                Debug.LogWarning($"[CollisionMeshToggle] '{obj.name}' has no MeshRenderer component.");
            }
        }
    }
}