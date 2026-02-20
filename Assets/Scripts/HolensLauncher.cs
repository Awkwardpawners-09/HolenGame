using UnityEngine;
using Cinemachine;
using UnityEngine.UI;

public class HolensLauncher : MonoBehaviour
{
    [Header("References")]
    public Transform holensPosition;
    public GameObject holensBallPrefab;
    public CinemachineVirtualCamera cinemachineCamera;
    public Camera mainCamera;
    public Transform cameraSpawnPoint;

    [Header("UI")]
    public GameObject swipeIndicator; // Optional: Visual feedback during swipe
    public LineRenderer trajectoryLine; // Optional: Show predicted trajectory

    [Header("Swipe Settings")]
    public float minSwipeDistance = 50f;
    public float maxSwipeDistance = 500f;
    public float minLaunchForce = 5f;
    public float maxLaunchForce = 100f;
    public float swipeTimeWindow = 1f;
    public bool requireTouchOnBall = false;
    public float swipeDeadZone = 20f;
    public string ballLayerName = "HolenBall";
    [Header("Force Calculation")]
    public float speedMultiplier = 0.05f; // How much swipe speed affects force
    public bool useSpeedForce = true; // Use speed-based calculation
    private int ballLayer;

    [Header("Camera Settings")]
    public Vector3 cameraFollowOffset = new Vector3(0f, 8f, -6f);
    public float cameraAimScreenY = 0.80f;

    [Header("Holen System")]
    public HolenChanger holenChanger;

    [Header("Life System")]
    [Tooltip("Time to wait before deducting life after launch (seconds)")]
    public float lifeDeductionDelay = 6.5f;

    private GameObject currentBall;
    private bool isBusy = false;
    private bool hasLaunched = false;
    private Transform defaultLookAtTarget;

    // Swipe detection variables
    private Vector2 swipeStartPos;
    private Vector2 swipeEndPos;
    private float swipeStartTime;
    private bool isSwiping = false;
    private Vector3 swipeWorldStart;
    private Vector3 swipeWorldEnd;

    void Start()
    {
        // Get or create the ball layer
        ballLayer = LayerMask.NameToLayer(ballLayerName);
        if (ballLayer == -1)
        {
            Debug.LogWarning($"Layer '{ballLayerName}' not found. Ball detection may not work properly.");
        }

        // Setup main camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        SetInitialCameraPosition();

        if (cinemachineCamera != null)
            defaultLookAtTarget = cinemachineCamera.LookAt;

        if (swipeIndicator != null)
            swipeIndicator.SetActive(false);

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        // If HolenChanger exists, sync the initial prefab with it
        if (holenChanger != null)
        {
            HolenData startingHolenData = holenChanger.GetCurrentHolenData();
            if (startingHolenData != null && startingHolenData.holenPrefab != null)
            {
                holensBallPrefab = startingHolenData.holenPrefab;
                Debug.Log($"Using HolenChanger's default: {startingHolenData.holenName}");
            }
        }

        // Spawn the initial ball (uses holensBallPrefab from Inspector or HolenChanger)
        SpawnBall();
    }

    void Update()
    {
        if (!isBusy && !hasLaunched && currentBall != null)
        {
            HandleSwipeInput();
        }
    }

    private void SetInitialCameraPosition()
    {
        if (cameraSpawnPoint != null)
        {
            mainCamera.transform.position = cameraSpawnPoint.position;
            mainCamera.transform.rotation = cameraSpawnPoint.rotation;
        }
    }

    private void AdjustCameraPosition()
    {
        if (cinemachineCamera != null)
        {
            CinemachineTransposer transposer = cinemachineCamera.GetCinemachineComponent<CinemachineTransposer>();
            CinemachineComposer composer = cinemachineCamera.GetCinemachineComponent<CinemachineComposer>();
            if (transposer != null && composer != null)
            {
                transposer.m_FollowOffset = cameraFollowOffset;
                composer.m_ScreenY = cameraAimScreenY;
            }
        }
    }

    private void HandleSwipeInput()
    {
        // Touch input for mobile
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (!requireTouchOnBall || IsTouchingBall(touch.position))
                {
                    StartSwipe(touch.position);
                }
            }
            else if (touch.phase == TouchPhase.Moved && isSwiping)
            {
                UpdateSwipe(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                EndSwipe(touch.position);
            }
            else if (touch.phase == TouchPhase.Canceled && isSwiping)
            {
                CancelSwipe();
            }
        }
        // Mouse input for testing in editor
        else if (Input.GetMouseButtonDown(0))
        {
            if (!requireTouchOnBall || IsTouchingBall(Input.mousePosition))
            {
                StartSwipe(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButton(0) && isSwiping)
        {
            UpdateSwipe(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            EndSwipe(Input.mousePosition);
        }
    }

    private bool IsTouchingBall(Vector2 screenPosition)
    {
        if (currentBall == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // First try direct raycast to ball
        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.collider.gameObject == currentBall)
            {
                Debug.Log("Touch detected on ball!");
                return true;
            }
        }

        // Alternative: Check screen distance to ball
        Vector3 ballScreenPos = mainCamera.WorldToScreenPoint(currentBall.transform.position);
        float screenDistance = Vector2.Distance(screenPosition, new Vector2(ballScreenPos.x, ballScreenPos.y));

        // Allow touch within 100 pixels of the ball on screen
        if (screenDistance < 100f)
        {
            Debug.Log("Touch detected near ball!");
            return true;
        }

        return false;
    }

    private void StartSwipe(Vector2 screenPosition)
    {
        isSwiping = true;
        swipeStartPos = screenPosition;
        swipeStartTime = Time.time;

        // Use ball's position as the starting point for direction calculation
        swipeWorldStart = currentBall.transform.position;

        if (swipeIndicator != null)
            swipeIndicator.SetActive(true);

        Debug.Log($"Swipe started at screen pos: {screenPosition}");
    }

    private void UpdateSwipe(Vector2 screenPosition)
    {
        swipeEndPos = screenPosition;

        // Calculate swipe direction in screen space first
        Vector2 swipeDelta = swipeEndPos - swipeStartPos;

        // Only update if swipe is beyond dead zone
        if (swipeDelta.magnitude < swipeDeadZone)
            return;

        // Convert swipe direction to world direction
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Plane groundPlane = new Plane(Vector3.up, currentBall.transform.position);
        float distance;

        if (groundPlane.Raycast(ray, out distance))
        {
            swipeWorldEnd = ray.GetPoint(distance);
        }

        // Optional: Show trajectory preview
        if (trajectoryLine != null)
        {
            ShowTrajectoryPreview();
        }

        Debug.Log($"Swiping... Delta: {swipeDelta.magnitude}");
    }

    private void ShowTrajectoryPreview()
    {
        // Calculate direction from ball position to swipe end point
        Vector3 direction = (swipeWorldEnd - swipeWorldStart);
        direction.y = 0;
        direction.Normalize();

        float swipeDistance = (swipeEndPos - swipeStartPos).magnitude;
        float force = CalculateLaunchForce(swipeDistance);

        trajectoryLine.enabled = true;
        trajectoryLine.positionCount = 15;

        Vector3 velocity = direction * force;
        Vector3 currentPos = currentBall.transform.position;

        for (int i = 0; i < 15; i++)
        {
            float t = i * 0.1f;
            Vector3 point = currentPos + velocity * t + 0.5f * Physics.gravity * t * t;
            trajectoryLine.SetPosition(i, point);
        }
    }

    private void EndSwipe(Vector2 screenPosition)
    {
        swipeEndPos = screenPosition;
        float swipeTime = Time.time - swipeStartTime;
        Vector2 swipeDelta = swipeEndPos - swipeStartPos;
        float swipeDistance = swipeDelta.magnitude;

        isSwiping = false;

        if (swipeIndicator != null)
            swipeIndicator.SetActive(false);

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        Debug.Log($"Swipe ended: Distance={swipeDistance}, Time={swipeTime}, MinRequired={minSwipeDistance}");

        // Validate swipe
        if (swipeDistance >= minSwipeDistance && swipeTime <= swipeTimeWindow)
        {
            // Calculate launch direction in world space
            Vector3 swipeDirection = (swipeWorldEnd - swipeWorldStart);
            swipeDirection.y = 0; // Keep it on horizontal plane
            swipeDirection.Normalize();

            // Fallback: If world direction calculation failed, use screen direction
            if (swipeDirection.magnitude < 0.1f)
            {
                // Convert screen swipe to world direction relative to camera
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;

                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                swipeDirection = (cameraRight * swipeDelta.x + cameraForward * swipeDelta.y).normalized;
            }

            // Calculate force based on BOTH distance and speed
            float force;

            if (useSpeedForce)
            {
                // Speed-based calculation (pixels per second)
                float swipeSpeed = swipeDistance / swipeTime;
                force = swipeSpeed * speedMultiplier;
                force = Mathf.Clamp(force, minLaunchForce, maxLaunchForce);

                Debug.Log($"SHOOTING! Speed={swipeSpeed:F2} px/s, Force={force:F2}, Direction={swipeDirection}");
            }
            else
            {
                // Distance-based calculation
                force = CalculateLaunchForce(swipeDistance);

                // Add speed bonus
                float speed = swipeDistance / swipeTime;
                float speedBonus = Mathf.Clamp01(speed / 2000f); // Normalize speed
                force = Mathf.Lerp(force, maxLaunchForce, speedBonus);

                Debug.Log($"SHOOTING! Distance={swipeDistance:F2}, Speed={speed:F2}, Force={force:F2}, Direction={swipeDirection}");
            }

            StartCoroutine(LaunchSequence(swipeDirection, force));
        }
        else
        {
            Debug.Log($"Invalid swipe: Distance={swipeDistance} (min: {minSwipeDistance}), Time={swipeTime}");
        }
    }

    private void CancelSwipe()
    {
        isSwiping = false;

        if (swipeIndicator != null)
            swipeIndicator.SetActive(false);

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        Debug.Log("Swipe cancelled");
    }

    private float CalculateLaunchForce(float swipeDistance)
    {
        // Normalize swipe distance to force range
        float normalizedDistance = Mathf.InverseLerp(minSwipeDistance, maxSwipeDistance, swipeDistance);
        return Mathf.Lerp(minLaunchForce, maxLaunchForce, normalizedDistance);
    }

    System.Collections.IEnumerator LaunchSequence(Vector3 direction, float force)
    {
        isBusy = true;
        hasLaunched = true;

        // Disable holen changer buttons during launch
        if (holenChanger != null)
        {
            holenChanger.DisableButtons();
        }

        LaunchBall(direction, force);

        // Wait for the specified delay before deducting life
        yield return new WaitForSeconds(lifeDeductionDelay);

        // Deduct life after the turn
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.OnHolenRespawn();
            Debug.Log("[HolensLauncher] Life deducted after turn");
        }

        // Wait the remaining time to complete the 7 second sequence
        yield return new WaitForSeconds(7f - lifeDeductionDelay);

        // Reset camera position and look target
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Follow = null;

            if (defaultLookAtTarget != null)
            {
                cinemachineCamera.LookAt = defaultLookAtTarget;
            }
            else
            {
                cinemachineCamera.LookAt = null;
            }

            if (cameraSpawnPoint != null)
            {
                mainCamera.transform.position = cameraSpawnPoint.position;
                mainCamera.transform.rotation = cameraSpawnPoint.rotation;
            }
        }

        // Destroy the old ball
        if (currentBall != null)
        {
            Destroy(currentBall);
            currentBall = null;
        }

        isBusy = false;
        hasLaunched = false;
        isSwiping = false;

        // Update to use the currently selected holen from HolenChanger
        if (holenChanger != null)
        {
            HolenData currentHolenData = holenChanger.GetCurrentHolenData();
            holensBallPrefab = currentHolenData.holenPrefab;
            Debug.Log($"Next spawn will use: {currentHolenData.holenName}");
        }

        // Spawn new ball for next turn (uses updated holensBallPrefab)
        SpawnBall();

        // Re-enable holen changer buttons after spawn
        if (holenChanger != null)
        {
            holenChanger.EnableButtons();
        }
    }

    void LaunchBall(Vector3 direction, float force)
    {
        if (currentBall == null) return;

        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        currentBall.transform.parent = null;
        rb.isKinematic = false;
        rb.AddForce(direction * force, ForceMode.Impulse);

        if (cinemachineCamera != null)
        {
            // Stop following, just look at the ball (same as multiplayer)
            cinemachineCamera.Follow = null;
            cinemachineCamera.LookAt = currentBall.transform;
        }

        Debug.Log($"Ball launched with force: {force}, direction: {direction}");
    }

    void SpawnBall(GameObject ballPrefab = null)
    {
        if (ballPrefab == null)
        {
            ballPrefab = holensBallPrefab;  // Use the selected Holen ball prefab
        }

        if (currentBall != null)
            Destroy(currentBall);

        currentBall = Instantiate(ballPrefab, holensPosition.position, holensPosition.rotation);
        currentBall.transform.parent = holensPosition;

        // Set the tag and layer of the spawned ball
        currentBall.tag = "Ball";

        if (ballLayer != -1)
        {
            currentBall.layer = ballLayer;
        }

        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        // Setup camera to follow and look at the ball
        if (cinemachineCamera != null && currentBall != null)
        {
            cinemachineCamera.Follow = currentBall.transform;
            cinemachineCamera.LookAt = currentBall.transform;
            AdjustCameraPosition();
        }

        Debug.Log("New ball spawned and ready");
    }

    public void ChangeBallPrefab(GameObject newPrefab)
    {
        // Destroy the current ball if it's there
        if (currentBall != null)
        {
            Destroy(currentBall);
        }

        // Spawn the new ball with the updated prefab
        SpawnBall(newPrefab);
    }

    // Create a public getter method for isBusy
    public bool GetIsBusy()
    {
        return isBusy;
    }
}