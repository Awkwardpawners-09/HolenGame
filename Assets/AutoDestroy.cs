using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [Tooltip("Time in seconds before this object is destroyed")]
    public float lifetime = 3f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}