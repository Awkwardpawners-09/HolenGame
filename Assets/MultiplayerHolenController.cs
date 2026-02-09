using Cinemachine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// COMPLETE MULTIPLAYER HOLEN CONTROLLER
/// - Full Cinemachine camera system
/// - Swipe/flick mechanics with trajectory preview
/// - Inventory selection system
/// - Turn-based gameplay (works with TurnManager)
/// - OPTIONAL: Integrated physics sync (can disable HolenPhysicsSync if needed)
/// </summary>
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
    public GameObject swipeIndicator;
    public LineRenderer trajectoryLine;

    [Header("Holen Change UI")]
    [Tooltip("The button that toggles the inventory panel open/closed")]
    public Button changeHolenButton;
    [Tooltip("The GameObject containing HolenChangeInventory's panel")]
    public GameObject inventoryPanel;
    [Tooltip("Text visible to BOTH players showing whose turn it is")]
    public TMP_Text statusText;

    [Header("Holen Change Settings")]
    [Tooltip("Cooldown in seconds before another holen can be selected")]
    public float changeHolenCooldown = 1f;

    [Header("Player Info")]
    public bool isPlayer1;

    [Header("Swipe Settings")]
    public float minSwipeDistance = 50f;
    public float maxSwipeDistance = 500f;
    public float minLaunchForce = 5f;
    public float maxLaunchForce = 100f;
    public float swipeTimeWindow = 1f;
    public string ballLayerName = "HolenBall";
    public bool requireTouchOnBall = false;
    public float swipeDeadZone = 20f;

    [Header("Force Calculation")]
    public float speedMultiplier = 0.05f;
    public bool useSpeedForce = true;
    private int ballLayer;

    [Header("Camera Settings")]
    public Vector3 cameraFollowOffset = new Vector3(0f, 8f, -6f);
    public float cameraAimScreenY = 0.80f;

    [Header("Turn Management")]
    [Tooltip("Use external TurnManager instead of built-in turn system")]
    public bool useExternalTurnManager = true;

    [Header("Physics Sync Settings (Optional)")]
    [Tooltip("Enable integrated physics sync (allows you to remove HolenPhysicsSync script)")]
    public bool useIntegratedPhysicsSync = false;
    [Tooltip("Auto-sync all holens in scene")]
    public bool autoSyncAllHolens = true;
    [Tooltip("Velocity threshold to consider holen stopped")]
    public float velocityThreshold = 0.02f;
    [Tooltip("How long to wait before allowing sleep")]
    public float sleepDelay = 0.5f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Public properties
    public GameObject currentHolenBall { get; private set; }

    // State variables
    private bool isReady = false;
    private bool isTurn = false;
    private string playerRole = "";
    private Transform defaultLookAtTarget;

    // Swipe detection
    private Vector2 swipeStartPos;
    private Vector2 swipeEndPos;
    private float swipeStartTime;
    private bool isSwiping = false;
    private Vector3 swipeWorldStart;
    private Vector3 swipeWorldEnd;

    // Holen change state
    private bool isInventoryOpen = false;
    private bool isHolenLaunched = false;
    private bool isOnChangeCooldown = false;

    // Integrated physics sync (optional)
    private Dictionary<int, HolenSyncData> syncedHolens = new Dictionary<int, HolenSyncData>();

    private class HolenSyncData
    {
        public GameObject gameObject;
        public Rigidbody rigidbody;
        public PhotonView photonView;
        public bool isSleeping;
        public float timeSinceLastImpact;

        public HolenSyncData(GameObject go)
        {
            gameObject = go;
            rigidbody = go.GetComponent<Rigidbody>();
            photonView = go.GetComponent<PhotonView>();
            isSleeping = false;
            timeSinceLastImpact = 0f;
        }
    }

    void Start()
    {
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

        // Holen change init
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (changeHolenButton != null)
            changeHolenButton.onClick.AddListener(OnChangeHolenButtonPressed);

        SetChangeHolenButtonInteractable(false);

        SetCameraView(false);

        // Start integrated physics sync if enabled
        if (useIntegratedPhysicsSync && autoSyncAllHolens)
        {
            InvokeRepeating(nameof(RegisterAllHolens), 1f, 2f);
        }

        // Initialize based on turn management mode
        if (useExternalTurnManager)
        {
            Debug.Log("[MultiplayerHolenController] Using external TurnManager - waiting for turn assignment");
        }
        else
        {
            StartCoroutine(GameStartSequence());
        }
    }

    void Update()
    {
        if (isTurn && currentHolenBall != null && !isReady && !isInventoryOpen && !isHolenLaunched)
        {
            HandleSwipeInput();
        }
    }

    void FixedUpdate()
    {
        // Update integrated physics sync if enabled
        if (useIntegratedPhysicsSync)
        {
            UpdatePhysicsSync();
        }
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

        if (isPlayer1)
        {
            isTurn = true;
            EnableControls();
            SetChangeHolenButtonInteractable(true);
            SpawnHolenBall();
            UpdateStatusText("idle");
            Debug.Log("Player 1's turn has started.");
        }
        else
        {
            UpdateLocalStatusText("idle", "Player 1");
        }
    }

    #region TURN MANAGEMENT

    /// <summary>
    /// Called by TurnManager to start this player's turn
    /// </summary>
    public void StartTurn()
    {
        isTurn = true;
        isReady = false;
        isSwiping = false;
        isHolenLaunched = false;
        isInventoryOpen = false;
        isOnChangeCooldown = false;

        EnableControls();
        SetChangeHolenButtonInteractable(true);
        SpawnHolenBall();
        UpdateStatusText("idle");

        if (showDebugInfo)
            Debug.Log($"{playerRole}'s turn started");
    }

    /// <summary>
    /// Called by TurnManager to end this player's turn
    /// </summary>
    public void EndTurn()
    {
        isTurn = false;
        isReady = false;
        isSwiping = false;
        isHolenLaunched = false;
        isInventoryOpen = false;
        isOnChangeCooldown = false;

        CloseInventory();
        SetChangeHolenButtonInteractable(false);
        DisableControls();

        if (showDebugInfo)
            Debug.Log($"{playerRole} turn ended");
    }

    public bool IsTurn()
    {
        return isTurn;
    }

    public bool IsMyTurn()
    {
        return isTurn;
    }

    #endregion

    #region HOLEN INVENTORY

    private void OnChangeHolenButtonPressed()
    {
        if (isHolenLaunched || !isTurn) return;

        if (isInventoryOpen)
        {
            CloseInventory();
            UpdateStatusText("idle");
        }
        else
        {
            OpenInventory();
            UpdateStatusText("changing");
        }
    }

    private void OpenInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);

        isInventoryOpen = true;

        if (showDebugInfo)
            Debug.Log($"{playerRole} opened the Holen inventory.");
    }

    private void CloseInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        isInventoryOpen = false;

        if (showDebugInfo)
            Debug.Log($"{playerRole} closed the Holen inventory.");
    }

    /// <summary>
    /// Called from HolenSlotClickHandler when player selects a holen from inventory
    /// </summary>
    public void OnHolenSelectedFromInventory(GameObject newHolenPrefab)
    {
        if (isHolenLaunched || !isTurn || isOnChangeCooldown) return;
        if (newHolenPrefab == null) return;

        StartCoroutine(ChangeCooldown());

        if (currentHolenBall != null)
        {
            PhotonNetwork.Destroy(currentHolenBall);
            currentHolenBall = null;
        }

        holenBallPrefab = newHolenPrefab;
        SpawnHolenBall();

        if (showDebugInfo)
            Debug.Log($"{playerRole} changed Holen to: {newHolenPrefab.name}");
    }

    private IEnumerator ChangeCooldown()
    {
        isOnChangeCooldown = true;
        yield return new WaitForSeconds(changeHolenCooldown);
        isOnChangeCooldown = false;
    }

    private void SetChangeHolenButtonInteractable(bool value)
    {
        if (changeHolenButton != null)
            changeHolenButton.interactable = value;
    }

    #endregion

    #region STATUS TEXT

    private void UpdateStatusText(string state)
    {
        string activeName = isTurn ? playerRole : GetOpponentName();
        photonView.RPC("RPC_UpdateStatusText", RpcTarget.All, state, activeName);
    }

    private void UpdateLocalStatusText(string state, string activeName)
    {
        if (statusText == null) return;
        statusText.text = BuildStatusString(state, activeName);
    }

    [PunRPC]
    private void RPC_UpdateStatusText(string state, string activeName)
    {
        UpdateLocalStatusText(state, activeName);
    }

    private string BuildStatusString(string state, string activeName)
    {
        switch (state)
        {
            case "idle":
                return $"{activeName} Turn";
            case "changing":
                return $"{activeName} is changing their pamato";
            case "launched":
                return $"{activeName} Attacks!";
            default:
                return $"{activeName} Turn";
        }
    }

    private string GetOpponentName()
    {
        return isPlayer1 ? "Player 2" : "Player 1";
    }

    #endregion

    #region SWIPE INPUT

    private void HandleSwipeInput()
    {
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

        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.collider.gameObject == currentHolenBall)
            {
                if (showDebugInfo)
                    Debug.Log("Touch detected on ball!");
                return true;
            }
        }

        Vector3 ballScreenPos = mainCamera.WorldToScreenPoint(currentHolenBall.transform.position);
        float screenDistance = Vector2.Distance(screenPosition, new Vector2(ballScreenPos.x, ballScreenPos.y));

        if (screenDistance < 100f)
        {
            if (showDebugInfo)
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
        swipeWorldStart = currentHolenBall.transform.position;

        if (swipeIndicator != null)
            swipeIndicator.SetActive(true);

        if (showDebugInfo)
            Debug.Log($"Swipe started at screen pos: {screenPosition}");
    }

    private void UpdateSwipe(Vector2 screenPosition)
    {
        swipeEndPos = screenPosition;
        Vector2 swipeDelta = swipeEndPos - swipeStartPos;

        if (swipeDelta.magnitude < swipeDeadZone)
            return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Plane groundPlane = new Plane(Vector3.up, currentHolenBall.transform.position);
        float distance;

        if (groundPlane.Raycast(ray, out distance))
        {
            swipeWorldEnd = ray.GetPoint(distance);
        }

        if (trajectoryLine != null)
        {
            ShowTrajectoryPreview();
        }

        if (showDebugInfo)
            Debug.Log($"Swiping... Delta: {swipeDelta.magnitude}");
    }

    private void ShowTrajectoryPreview()
    {
        Vector3 direction = (swipeWorldEnd - swipeWorldStart).normalized;

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

        if (showDebugInfo)
            Debug.Log($"Swipe ended: Distance={swipeDistance}, Time={swipeTime}, MinRequired={minSwipeDistance}");

        if (swipeDistance >= minSwipeDistance && swipeTime <= swipeTimeWindow)
        {
            Vector3 swipeDirection = (swipeWorldEnd - swipeWorldStart);
            swipeDirection.y = 0;
            swipeDirection.Normalize();

            if (swipeDirection.magnitude < 0.1f)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;

                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                swipeDirection = (cameraRight * swipeDelta.x + cameraForward * swipeDelta.y).normalized;
            }

            float force;

            if (useSpeedForce)
            {
                float swipeSpeed = swipeDistance / swipeTime;
                force = swipeSpeed * speedMultiplier;
                force = Mathf.Clamp(force, minLaunchForce, maxLaunchForce);

                if (showDebugInfo)
                    Debug.Log($"SHOOTING! Speed={swipeSpeed:F2} px/s, Force={force:F2}, Direction={swipeDirection}");
            }
            else
            {
                force = CalculateLaunchForce(swipeDistance);

                float speed = swipeDistance / swipeTime;
                float speedBonus = Mathf.Clamp01(speed / 2000f);
                force = Mathf.Lerp(force, maxLaunchForce, speedBonus);

                if (showDebugInfo)
                    Debug.Log($"SHOOTING! Distance={swipeDistance:F2}, Speed={speed:F2}, Force={force:F2}, Direction={swipeDirection}");
            }

            ShootHolen(swipeDirection, force);
        }
        else
        {
            if (showDebugInfo)
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

        if (showDebugInfo)
            Debug.Log("Swipe cancelled");
    }

    private float CalculateLaunchForce(float swipeDistance)
    {
        float normalizedDistance = Mathf.InverseLerp(minSwipeDistance, maxSwipeDistance, swipeDistance);
        return Mathf.Lerp(minLaunchForce, maxLaunchForce, normalizedDistance);
    }

    #endregion

    #region CAMERA

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

    #endregion

    #region HOLEN BALL SPAWN AND SHOOT

    private void SpawnHolenBall()
    {
        currentHolenBall = PhotonNetwork.Instantiate(holenBallPrefab.name, ballSpawnPoint.position, Quaternion.identity);

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

        // Register with integrated physics sync if enabled
        if (useIntegratedPhysicsSync)
        {
            RegisterHolen(currentHolenBall);
        }

        if (showDebugInfo)
            Debug.Log($"{playerRole} spawned Holen Ball: {holenBallPrefab.name}");
    }

    public void ShootHolen(Vector3 direction, float force)
    {
        if (isTurn && !isReady && currentHolenBall != null)
        {
            isReady = true;
            isHolenLaunched = true;

            SetChangeHolenButtonInteractable(false);
            CloseInventory();

            photonView.RPC("RPC_ShootHolen", RpcTarget.All, direction, force);

            UpdateStatusText("launched");

            StartCoroutine(CompleteTurn());

            if (showDebugInfo)
                Debug.Log($"{playerRole} launched Holen Ball with force: {force}, direction: {direction}");
        }
    }

    [PunRPC]
    private void RPC_ShootHolen(Vector3 direction, float force)
    {
        if (currentHolenBall != null)
        {
            Rigidbody rb = currentHolenBall.GetComponent<Rigidbody>();

            // Set kinematic to false on ALL clients
            rb.isKinematic = false;

            // Apply force after physics initialization
            StartCoroutine(ApplyForceNextFrame(rb, direction, force));

            if (activePlayerCamera != null && isTurn)
            {
                activePlayerCamera.Follow = null;
                activePlayerCamera.LookAt = currentHolenBall.transform;
            }
        }
    }

    private IEnumerator ApplyForceNextFrame(Rigidbody rb, Vector3 direction, float force)
    {
        yield return new WaitForFixedUpdate();

        if (rb != null)
        {
            rb.AddForce(direction * force, ForceMode.Impulse);

            // Notify HolenPhysicsSync if present (for backwards compatibility)
            HolenPhysicsSync physicsSync = rb.GetComponent<HolenPhysicsSync>();
            if (physicsSync != null)
            {
                physicsSync.WakeUp();
            }

            // Wake up integrated physics sync if enabled
            if (useIntegratedPhysicsSync)
            {
                PhotonView pv = rb.GetComponent<PhotonView>();
                if (pv != null && syncedHolens.ContainsKey(pv.ViewID))
                {
                    syncedHolens[pv.ViewID].isSleeping = false;
                    syncedHolens[pv.ViewID].timeSinceLastImpact = 0f;
                }
            }

            if (showDebugInfo)
                Debug.Log($"[MultiplayerHolenController] Force applied: {force} in direction {direction}");
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

        InternalEndTurn();
    }

    #endregion

    #region TURN COMPLETION

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

    private void InternalEndTurn()
    {
        isTurn = false;
        isReady = false;
        isSwiping = false;
        isHolenLaunched = false;
        isInventoryOpen = false;
        isOnChangeCooldown = false;

        CloseInventory();
        SetChangeHolenButtonInteractable(false);
        DisableControls();

        if (showDebugInfo)
            Debug.Log($"{playerRole} ended their turn.");

        // Notify PVPScore
        PVPScore scoreManager = FindObjectOfType<PVPScore>();
        if (scoreManager != null)
        {
            scoreManager.OnTurnEnd();
        }

        if (useExternalTurnManager)
        {
            // Let TurnManager handle the turn transition
            StartCoroutine(WaitForHolensToStopThenNotifyTurnManager());
        }
        else
        {
            // Use built-in turn system
            photonView.RPC("SwitchTurn", RpcTarget.Others);
        }
    }

    private IEnumerator WaitForHolensToStopThenNotifyTurnManager()
    {
        // NEW: Add a maximum wait time of 5 seconds instead of 30
        float maxWaitTime = 5f; // Wait maximum 5 seconds for holens to stop
        float minWaitTime = 2f; // Wait at least 2 seconds to see if anything happens
        float elapsed = 0f;

        if (showDebugInfo)
            Debug.Log("[MultiplayerHolenController] Waiting for holens to stop or timeout...");

        // Wait minimum time first
        yield return new WaitForSeconds(minWaitTime);
        elapsed = minWaitTime;

        // Then check if holens have stopped, but with a shorter timeout
        while (elapsed < maxWaitTime)
        {
            bool allStopped;

            if (useIntegratedPhysicsSync)
            {
                allStopped = AreAllHolensStopped();
            }
            else
            {
                // Use HolenPhysicsSync if available
                allStopped = HolenPhysicsSync.AreAllHolensStopped();
            }

            if (allStopped)
            {
                if (showDebugInfo)
                    Debug.Log($"[MultiplayerHolenController] All holens stopped after {elapsed:F1}s");
                break;
            }

            yield return new WaitForSeconds(0.3f);
            elapsed += 0.3f;
        }

        if (elapsed >= maxWaitTime)
        {
            if (showDebugInfo)
                Debug.Log("[MultiplayerHolenController] Timeout reached - ending turn anyway");
        }

        // Notify TurnManager
        var turnManager = FindObjectOfType<TurnManager>();
        if (turnManager != null)
        {
            if (showDebugInfo)
                Debug.Log("[MultiplayerHolenController] Notifying TurnManager that turn is complete");

            turnManager.OnPlayerTurnComplete();
        }
        else
        {
            Debug.LogWarning("[MultiplayerHolenController] TurnManager not found!");
        }
    }


    [PunRPC]
    private void SwitchTurn()
    {
        isTurn = true;
        isReady = false;
        isSwiping = false;
        isHolenLaunched = false;
        isInventoryOpen = false;
        isOnChangeCooldown = false;

        EnableControls();
        SetChangeHolenButtonInteractable(true);
        SpawnHolenBall();
        UpdateStatusText("idle");

        if (showDebugInfo)
            Debug.Log($"{playerRole}'s turn started");
    }

    #endregion

    #region INTEGRATED PHYSICS SYNC (OPTIONAL)

    private void RegisterAllHolens()
    {
        if (!useIntegratedPhysicsSync) return;

        GameObject[] allHolens = GameObject.FindGameObjectsWithTag("Objective");

        foreach (GameObject holen in allHolens)
        {
            if (holen == null) continue;

            PhotonView pv = holen.GetComponent<PhotonView>();
            if (pv == null) continue;

            if (!syncedHolens.ContainsKey(pv.ViewID))
            {
                RegisterHolen(holen);
            }
        }
    }

    public void RegisterHolen(GameObject holen)
    {
        if (!useIntegratedPhysicsSync) return;
        if (holen == null) return;

        PhotonView pv = holen.GetComponent<PhotonView>();
        if (pv == null) return;

        Rigidbody rb = holen.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Initialize physics settings
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezePositionY |
                        RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationZ;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;

        syncedHolens[pv.ViewID] = new HolenSyncData(holen);

        if (showDebugInfo)
            Debug.Log($"[MultiplayerHolenController] Registered {holen.name} for physics sync");
    }

    public void UnregisterHolen(GameObject holen)
    {
        if (!useIntegratedPhysicsSync) return;
        if (holen == null) return;

        PhotonView pv = holen.GetComponent<PhotonView>();
        if (pv != null && syncedHolens.ContainsKey(pv.ViewID))
        {
            syncedHolens.Remove(pv.ViewID);
        }
    }

    private void UpdatePhysicsSync()
    {
        if (!useIntegratedPhysicsSync) return;

        // Clean up destroyed holens
        List<int> toRemove = new List<int>();
        foreach (var kvp in syncedHolens)
        {
            if (kvp.Value.gameObject == null)
            {
                toRemove.Add(kvp.Key);
            }
        }
        foreach (int key in toRemove)
        {
            syncedHolens.Remove(key);
        }

        // Update each holen
        foreach (var syncData in syncedHolens.Values)
        {
            if (syncData.gameObject == null || syncData.rigidbody == null)
                continue;

            syncData.timeSinceLastImpact += Time.fixedDeltaTime;

            float totalVelocity = syncData.rigidbody.velocity.magnitude +
                                 syncData.rigidbody.angularVelocity.magnitude;
            bool shouldSleep = totalVelocity < velocityThreshold &&
                              syncData.timeSinceLastImpact > sleepDelay;

            if (shouldSleep && !syncData.isSleeping)
            {
                syncData.isSleeping = true;
                syncData.rigidbody.velocity = Vector3.zero;
                syncData.rigidbody.angularVelocity = Vector3.zero;

                if (showDebugInfo && syncData.photonView.IsMine)
                    Debug.Log($"[MultiplayerHolenController] {syncData.gameObject.name} stopped moving");
            }
            else if (!shouldSleep && syncData.isSleeping)
            {
                syncData.isSleeping = false;
            }
        }
    }

    public bool AreAllHolensStopped()
    {
        if (!useIntegratedPhysicsSync)
        {
            // Fall back to HolenPhysicsSync if available
            return HolenPhysicsSync.AreAllHolensStopped();
        }

        foreach (var syncData in syncedHolens.Values)
        {
            if (syncData.gameObject != null && !syncData.isSleeping)
                return false;
        }
        return true;
    }

    #endregion

    void OnDestroy()
    {
        if (changeHolenButton != null)
            changeHolenButton.onClick.RemoveListener(OnChangeHolenButtonPressed);

        CancelInvoke();
    }


}