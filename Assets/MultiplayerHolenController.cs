using Cinemachine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerHolenController : MonoBehaviourPunCallbacks
{
    [Header("References")]
    public GameObject holenBallPrefab;
    public Transform ballSpawnPoint;
    public Camera mainCamera;
    public CinemachineVirtualCamera activePlayerCamera;
    public CinemachineVirtualCamera birdsEyeCamera;
    public TMP_Text playerLabelText;
    public TMP_Text turnDisplayText;
    public Transform cameraSpawnPoint;

    [Header("UI")]
    public GameObject loadingUI;
    public GameObject swipeIndicator; // Optional: Visual feedback during swipe
    public LineRenderer trajectoryLine; // Optional: Show predicted trajectory

    [Header("Player Info")]
    public bool isPlayer1;

    [Header("Swipe Settings")]
    public float minSwipeDistance = 50f;
    public float maxSwipeDistance = 500f;
    public float minLaunchForce = 5f;
    public float maxLaunchForce = 100f;
    public float swipeTimeWindow = 1f; // Max time for a valid swipe
    public string ballLayerName = "HolenBall"; // Layer name for the ball
    public bool requireTouchOnBall = false; // Set to false for easier swiping
    public float swipeDeadZone = 20f; // Minimum distance before registering as swipe
    [Header("Force Calculation")]
    public float speedMultiplier = 0.05f; // How much swipe speed affects force
    public bool useSpeedForce = true; // Use speed-based calculation
    private int ballLayer;

    [Header("Camera Settings")]
    public Vector3 cameraFollowOffset = new Vector3(0f, 8f, -6f);
    public float cameraAimScreenY = 0.80f;

    public GameObject currentHolenBall { get; private set; }
    private bool isReady = false;
    private bool isTurn = false;
    private string playerRole = "";
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

        DisableControls();
        SetInitialCameraPosition();

        if (activePlayerCamera != null)
            defaultLookAtTarget = activePlayerCamera.LookAt;

        if (swipeIndicator != null)
            swipeIndicator.SetActive(false);

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        SetCameraView(false);
        StartCoroutine(GameStartSequence());
    }

    private IEnumerator GameStartSequence()
    {
        loadingUI.SetActive(true);
        Debug.Log("Waiting for both players to connect...");

        while (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            yield return null;
        }

        Debug.Log("Both players are connected.");
        yield return new WaitForSeconds(3f);

        isPlayer1 = PhotonNetwork.LocalPlayer.ActorNumber == 1;
        playerRole = isPlayer1 ? "Player 1" : "Player 2";

        Debug.Log($"Local player assigned as {playerRole}");

        loadingUI.SetActive(false);
        UpdateTurnDisplayText();

        if (isPlayer1)
        {
            isTurn = true;
            EnableControls();
            SpawnHolenBall();
            UpdateTurnDisplayText();
            Debug.Log("Player 1's turn has started.");
        }
    }

    void Update()
    {
        if (isTurn && currentHolenBall != null && !isReady)
        {
            HandleSwipeInput();
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
                // Check if touch started on/near the ball (or allow anywhere if requireTouchOnBall is false)
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
        if (currentHolenBall == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // First try direct raycast to ball
        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.collider.gameObject == currentHolenBall)
            {
                Debug.Log("Touch detected on ball!");
                return true;
            }
        }

        // Alternative: Check screen distance to ball
        Vector3 ballScreenPos = mainCamera.WorldToScreenPoint(currentHolenBall.transform.position);
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
        swipeWorldStart = currentHolenBall.transform.position;

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
        // Project the swipe onto the ground plane from ball's position
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Plane groundPlane = new Plane(Vector3.up, currentHolenBall.transform.position);
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
        Vector3 direction = (swipeWorldEnd - swipeWorldStart).normalized;

        // Handle case where direction is invalid
        if (direction.magnitude < 0.1f)
            return;

        float swipeDistance = Vector2.Distance(swipeStartPos, swipeEndPos);
        float force = CalculateLaunchForce(swipeDistance);

        trajectoryLine.enabled = true;
        trajectoryLine.positionCount = 15;

        Vector3 velocity = direction * force;
        Vector3 currentPos = currentHolenBall.transform.position;

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

            ShootHolen(swipeDirection, force);
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
        if (activePlayerCamera != null)
        {
            CinemachineTransposer transposer = activePlayerCamera.GetCinemachineComponent<CinemachineTransposer>();
            CinemachineComposer composer = activePlayerCamera.GetCinemachineComponent<CinemachineComposer>();
            if (transposer != null)
            {
                transposer.m_FollowOffset = cameraFollowOffset;
                composer.m_ScreenY = cameraAimScreenY;
            }
        }
    }

    private void SpawnHolenBall()
    {
        currentHolenBall = PhotonNetwork.Instantiate(holenBallPrefab.name, ballSpawnPoint.position, Quaternion.identity);

        // Set the ball's layer for detection
        if (ballLayer != -1)
        {
            currentHolenBall.layer = ballLayer;
        }

        Rigidbody rb = currentHolenBall.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        if (activePlayerCamera != null && currentHolenBall != null && isTurn)
        {
            activePlayerCamera.Follow = currentHolenBall.transform;
            activePlayerCamera.LookAt = currentHolenBall.transform;
            AdjustCameraPosition();
        }

        Debug.Log($"{playerRole} spawned Holen Ball");
    }

    public bool IsTurn()
    {
        return isTurn;
    }

    public void ShootHolen(Vector3 direction, float force)
    {
        if (isTurn && !isReady && currentHolenBall != null)
        {
            isReady = true;

            photonView.RPC("RPC_ShootHolen", RpcTarget.All, direction, force);

            StartCoroutine(CompleteTurn());
            Debug.Log($"{playerRole} launched Holen Ball with force: {force}, direction: {direction}");
        }
    }

    [PunRPC]
    private void RPC_ShootHolen(Vector3 direction, float force)
    {
        if (currentHolenBall != null)
        {
            Rigidbody rb = currentHolenBall.GetComponent<Rigidbody>();
            rb.isKinematic = false;

            rb.AddForce(direction * force, ForceMode.Impulse);

            if (activePlayerCamera != null && isTurn)
            {
                // Stop following the ball, just look at it
                activePlayerCamera.Follow = null;
                activePlayerCamera.LookAt = currentHolenBall.transform;
            }
        }
    }

    private IEnumerator CompleteTurn()
    {
        yield return new WaitForSeconds(7f);

        if (isTurn && activePlayerCamera != null)
        {
            activePlayerCamera.Follow = null;

            if (defaultLookAtTarget != null)
            {
                activePlayerCamera.LookAt = defaultLookAtTarget;
            }
            else
            {
                activePlayerCamera.LookAt = null;
            }

            if (cameraSpawnPoint != null)
            {
                mainCamera.transform.position = cameraSpawnPoint.position;
                mainCamera.transform.rotation = cameraSpawnPoint.rotation;
            }
        }

        if (currentHolenBall != null)
            PhotonNetwork.Destroy(currentHolenBall);

        currentHolenBall = null;
        isReady = false;
        isSwiping = false;

        EndTurn();
    }

    private void DisableControls()
    {
        if (swipeIndicator != null)
            swipeIndicator.SetActive(false);

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        SetCameraView(false);
    }

    private void EnableControls()
    {
        SetCameraView(true);
    }

    private void UpdateTurnDisplayText()
    {
        if (turnDisplayText != null)
        {
            turnDisplayText.text = isTurn ? "Your Turn - Swipe to Shoot!" : "Opponent's Turn";
        }

        if (playerLabelText != null)
        {
            playerLabelText.text = isPlayer1 ? "Player 1" : "Player 2";
        }
    }

    private void EndTurn()
    {
        isTurn = false;
        isReady = false;
        isSwiping = false;
        DisableControls();
        UpdateTurnDisplayText();

        Debug.Log($"{playerRole} ended their turn. Switching to other player.");

        PVPScore scoreManager = FindObjectOfType<PVPScore>();
        if (scoreManager != null)
        {
            scoreManager.OnTurnEnd();
        }

        photonView.RPC("SwitchTurn", RpcTarget.Others);
    }

    private void SetCameraView(bool isActiveTurn)
    {
        if (activePlayerCamera != null && birdsEyeCamera != null)
        {
            if (isActiveTurn)
            {
                activePlayerCamera.Priority = 20;
                birdsEyeCamera.Priority = 10;
            }
            else
            {
                activePlayerCamera.Priority = 10;
                birdsEyeCamera.Priority = 20;
            }
        }
    }

    [PunRPC]
    private void SwitchTurn()
    {
        isTurn = true;
        isReady = false;
        isSwiping = false;

        EnableControls();
        UpdateTurnDisplayText();
        SpawnHolenBall();

        Debug.Log($"{playerRole}'s turn started");
    }
}