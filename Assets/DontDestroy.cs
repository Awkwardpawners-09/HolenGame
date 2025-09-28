using UnityEngine;

public class DontDestroy : MonoBehaviour
{
    // Called when the script starts
    void Start()
    {
        // This makes the object persist across scene loads
        DontDestroyOnLoad(gameObject);
    }
}