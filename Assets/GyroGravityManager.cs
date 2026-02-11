using UnityEngine;

public class GyroGravityManager : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Tooltip("Multiplier for gravity strength")]
    public float gravityMultiplier = 9.81f;

    [Tooltip("How smoothly gravity direction changes")]
    public float smoothing = 5f;

    [Tooltip("Enable gyroscope control")]
    public bool useGyroscope = true;

    [Header("Fallback Settings")]
    [Tooltip("Default gravity direction when gyroscope is disabled")]
    public Vector2 defaultGravity = new Vector2(0, -9.81f);

    [Header("Debug")]
    [Tooltip("Show gravity direction info")]
    public bool showDebugInfo = true;

    private bool gyroSupported = false;
    private Vector2 targetGravity;

    void Start()
    {
        // Check if gyroscope is supported
        gyroSupported = SystemInfo.supportsGyroscope;

        if (gyroSupported && useGyroscope)
        {
            // Enable the gyroscope
            Input.gyro.enabled = true;
            Debug.Log("Gyroscope enabled for UI physics!");
        }
        else
        {
            if (!gyroSupported)
            {
                Debug.LogWarning("Gyroscope not supported on this device. Using default gravity.");
            }

            // Set default gravity
            Physics2D.gravity = defaultGravity;
        }

        targetGravity = Physics2D.gravity;
    }

    void Update()
    {
        if (gyroSupported && useGyroscope && Input.gyro.enabled)
        {
            UpdateGravityFromGyro();
        }
        else if (!useGyroscope)
        {
            // Use default gravity
            targetGravity = defaultGravity;
            Physics2D.gravity = Vector2.Lerp(Physics2D.gravity, targetGravity, smoothing * Time.deltaTime);
        }
    }

    void UpdateGravityFromGyro()
    {
        // Get device's gravity vector from gyroscope
        Vector3 deviceGravity = Input.gyro.gravity;

        // Convert to 2D gravity
        // X and Y from the device orientation
        Vector2 newGravity = new Vector2(deviceGravity.x, deviceGravity.y);

        // Apply gravity multiplier
        newGravity *= gravityMultiplier;

        // Smooth the gravity transition
        targetGravity = newGravity;
        Physics2D.gravity = Vector2.Lerp(Physics2D.gravity, targetGravity, smoothing * Time.deltaTime);
    }

    // Toggle gyroscope on/off
    public void ToggleGyroscope(bool enabled)
    {
        useGyroscope = enabled;

        if (gyroSupported)
        {
            Input.gyro.enabled = enabled;
        }

        if (!enabled)
        {
            Physics2D.gravity = defaultGravity;
        }
    }

    // Set gravity multiplier at runtime
    public void SetGravityMultiplier(float multiplier)
    {
        gravityMultiplier = multiplier;
    }

    void OnGUI()
    {
        if (showDebugInfo)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 18;
            style.normal.textColor = Color.white;

            int y = 10;
            int lineHeight = 25;

            GUI.Label(new Rect(10, y, 500, lineHeight),
                "Physics2D Gravity: " + Physics2D.gravity.ToString("F2"), style);
            y += lineHeight;

            GUI.Label(new Rect(10, y, 500, lineHeight),
                "Gravity Magnitude: " + Physics2D.gravity.magnitude.ToString("F2"), style);
            y += lineHeight;

            GUI.Label(new Rect(10, y, 500, lineHeight),
                "Gyro Supported: " + gyroSupported, style);
            y += lineHeight;

            if (gyroSupported)
            {
                GUI.Label(new Rect(10, y, 500, lineHeight),
                    "Gyro Enabled: " + Input.gyro.enabled, style);
                y += lineHeight;

                if (Input.gyro.enabled)
                {
                    GUI.Label(new Rect(10, y, 500, lineHeight),
                        "Device Gravity: " + Input.gyro.gravity.ToString("F2"), style);
                    y += lineHeight;

                    GUI.Label(new Rect(10, y, 500, lineHeight),
                        "Device Attitude: " + Input.gyro.attitude.eulerAngles.ToString("F0"), style);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (showDebugInfo && Application.isPlaying)
        {
            // Draw gravity direction arrow in scene view
            Vector3 gravityDir = Physics2D.gravity.normalized;
            Vector3 startPos = Vector3.zero;

            if (Camera.main != null)
            {
                startPos = Camera.main.transform.position;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(startPos, gravityDir * 3f);

            // Draw arrow head
            Vector3 right = Quaternion.Euler(0, 0, 135) * gravityDir * 0.8f;
            Vector3 left = Quaternion.Euler(0, 0, -135) * gravityDir * 0.8f;

            Vector3 arrowTip = startPos + gravityDir * 3f;
            Gizmos.DrawRay(arrowTip, right);
            Gizmos.DrawRay(arrowTip, left);
        }
    }
}