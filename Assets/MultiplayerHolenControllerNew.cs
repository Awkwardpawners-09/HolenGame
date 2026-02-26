using Cinemachine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerHolenControllerNew : MonoBehaviourPunCallbacks
{
    // ─────────────────────────────────────────────
    //  LAUNCH MODE ENUM
    // ─────────────────────────────────────────────
    public enum LaunchMode
    {
        Default = 0,   // Swipe speed/distance → force, straight horizontal line
        Arc = 1,   // Finger raycast on Ground → holen arcs UP and lands exactly there
        Downward = 2    // Finger raycast on Ground → holen launches from raised position DOWN to there
    }

    // ─────────────────────────────────────────────
    //  REFERENCES
    // ─────────────────────────────────────────────
    [Header("References")]
    public GameObject holenBallPrefab;
    public Transform ballSpawnPoint;
    public Camera mainCamera;
    public CinemachineVirtualCamera activePlayerCamera;
    public CinemachineVirtualCamera birdsEyeCamera;
    public TMP_Text playerLabelText;
    public TMP_Text turnDisplayText;
    public Transform cameraSpawnPoint;

    // ─────────────────────────────────────────────
    //  UI
    // ─────────────────────────────────────────────
    [Header("UI")]
    public GameObject loadingUI;
    public GameObject swipeIndicator;
    public LineRenderer trajectoryLine;

    [Header("Launch Mode UI Buttons")]
    public Button buttonModeDefault;
    public Button buttonModeArc;
    public Button buttonModeDownward;

    [Header("Holen Change UI")]
    [Tooltip("The button that toggles the inventory panel open/closed")]
    public Button changeHolenButton;
    [Tooltip("The GameObject containing HolenChangeInventory's panel — toggled by the button")]
    public GameObject inventoryPanel;
    [Tooltip("Text visible to BOTH players showing whose turn it is and what they are doing")]
    public TMP_Text statusText;

    // ─────────────────────────────────────────────
    //  ACTIVE MODE
    // ─────────────────────────────────────────────
    [Header("Active Launch Mode")]
    public LaunchMode activeLaunchMode = LaunchMode.Default;

    // ─────────────────────────────────────────────
    //  HOLEN CHANGE SETTINGS
    // ─────────────────────────────────────────────
    [Header("Holen Change Settings")]
    [Tooltip("Cooldown in seconds before another holen can be selected")]
    public float changeHolenCooldown = 1f;

    // ─────────────────────────────────────────────
    //  PLAYER INFO
    // ─────────────────────────────────────────────
    [Header("Player Info")]
    public bool isPlayer1;

    // ─────────────────────────────────────────────
    //  SWIPE SETTINGS
    // ─────────────────────────────────────────────
    [Header("Swipe Settings (All Modes)")]
    public float minSwipeDistance = 50f;
    public float maxSwipeDistance = 500f;
    public float minLaunchForce = 5f;
    public float maxLaunchForce = 100f;
    [Tooltip("Time window enforced in Default mode (seconds). Set to 0 to disable.")]
    public float swipeTimeWindow = 1f;
    [Tooltip("Time window enforced in Arc mode (seconds). Set to 0 to disable.")]
    public float arcSwipeTimeWindow = 0f;
    [Tooltip("Time window enforced in Downward mode (seconds). Set to 0 to disable.")]
    public float downwardSwipeTimeWindow = 0f;
    public bool requireTouchOnBall = false;
    public float swipeDeadZone = 20f;
    public string ballLayerName = "HolenBall";

    [Header("Force Calculation (Default Mode Only)")]
    public float speedMultiplier = 0.05f;
    public bool useSpeedForce = true;

    // ─────────────────────────────────────────────
    //  ARC MODE SETTINGS
    // ─────────────────────────────────────────────
    [Header("Arc Mode Settings")]
    public string groundLayerName = "Ground";
    [Tooltip("Launch angle above horizontal (degrees). 45° = max range.")]
    public float arcAngle = 45f;
    [Tooltip("1.0 = exact. Reduce if overshooting, increase if undershooting.")]
    [Range(0.1f, 2.0f)]
    public float arcForceMultiplier = 1.0f;

    [Header("Arc Drag Targeting")]
    [Tooltip("How far ahead of the finger drag the target indicator moves.")]
    public float arcDragAmplification = 2.5f;
    [Tooltip("Maximum world-space radius the target can travel from the holen.")]
    public float arcMaxTargetRadius = 20f;

    // ─────────────────────────────────────────────
    //  DOWNWARD MODE SETTINGS
    // ─────────────────────────────────────────────
    [Header("Downward Mode Settings")]
    public float downwardSpawnHeightOffset = 4f;
    [Tooltip("Angle below horizontal in degrees.")]
    public float downwardAngle = 30f;
    [Tooltip("1.0 = exact. Reduce if overshooting, increase if undershooting.")]
    [Range(0.1f, 2.0f)]
    public float downwardForceMultiplier = 1.0f;

    // ─────────────────────────────────────────────
    //  TRAJECTORY LINE SETTINGS
    // ─────────────────────────────────────────────
    [Header("Trajectory Settings")]
    [Tooltip("Points on arc/downward trajectory line")]
    public int trajectoryPointCount = 40;
    [Tooltip("Time step per arc point (smaller = smoother)")]
    public float trajectoryTimeStep = 0.08f;
    [Tooltip("How far the Default mode straight line extends (world units)")]
    public float defaultLineLength = 10f;

    [Header("Line Color & Animation")]
    [Tooltip("Color of the trajectory line. Alpha controls transparency.")]
    public Color lineColor = new Color(1f, 0f, 0f, 0.5f);
    [Tooltip("How fast the dash travels along the line")]
    public float lineAnimationSpeed = 2f;
    [Tooltip("Fraction of the line visible at once (0.6 = 60% of path shown)")]
    [Range(0.01f, 1f)]
    public float lineVisibleFraction = 0.6f;

    // ─────────────────────────────────────────────
    //  CAMERA SETTINGS
    // ─────────────────────────────────────────────
    [Header("Camera Settings")]
    [Tooltip("Camera offset while the holen is at the spawn point (aiming)")]
    public Vector3 cameraFollowOffset = new Vector3(0f, 8f, -6f);
    [Tooltip("Screen Y for the holen while AIMING (0.8 = upper part of screen)")]
    public float cameraAimScreenY = 0.80f;
    [Tooltip("Screen Y for the holen while IN FLIGHT. 0.5 = dead centre of screen.")]
    public float cameraFlightScreenY = 0.5f;

    // ─────────────────────────────────────────────
    //  SOUND
    // ─────────────────────────────────────────────
    [Header("Sound")]
    [Tooltip("AudioSource to play the launch sound through. If left empty the script will try to find one on this GameObject.")]
    public AudioSource audioSource;
    [Tooltip("Sound clip that plays the moment the holen is launched.")]
    public AudioClip launchSoundClip;

    // ─────────────────────────────────────────────
    //  TARGETING INDICATOR
    // ─────────────────────────────────────────────
    [Header("Targeting Indicator")]
    [Tooltip("3D GameObject that shows where the holen will land. Only visible in Arc and Downward modes while aiming.")]
    public GameObject targetIndicator;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────
    private int ballLayer;
    private int groundLayerMask;
    private Transform defaultLookAtTarget;

    // Multiplayer turn state
    private bool isReady = false;
    private bool isTurn = false;
    private string playerRole = "";

    // Swipe state
    private Vector2 swipeStartPos;
    private Vector2 swipeEndPos;
    private float swipeStartTime;
    private bool isSwiping = false;
    private Vector3 swipeWorldStart;

    // Raycast target (Arc & Downward)
    private Vector3 raycastTarget;
    private bool raycastTargetValid;

    // Animated line
    private Vector3[] fullTrajectoryPoints;
    private float lineAnimOffset = 0f;

    // Holen change state
    private bool isInventoryOpen = false;
    private bool isHolenLaunched = false;
    private bool isOnChangeCooldown = false;

    // Public accessor for other scripts
    public GameObject currentHolenBall { get; private set; }

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────
    void Start()
    {
        ballLayer = LayerMask.NameToLayer(ballLayerName);
        groundLayerMask = LayerMask.GetMask(groundLayerName);

        if (ballLayer == -1)
            Debug.LogWarning($"Layer '{ballLayerName}' not found.");
        if (groundLayerMask == 0)
            Debug.LogWarning($"Layer '{groundLayerName}' not found. Arc/Downward targeting won't work.");

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        DisableControls();
        SetInitialCameraPosition();

        if (activePlayerCamera != null)
            defaultLookAtTarget = activePlayerCamera.LookAt;

        if (swipeIndicator != null)
            swipeIndicator.SetActive(false);

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
            trajectoryLine.startColor = lineColor;
            trajectoryLine.endColor = lineColor;
        }

        if (targetIndicator != null)
            targetIndicator.SetActive(false);

        // Wire launch mode buttons
        if (buttonModeDefault != null) buttonModeDefault.onClick.AddListener(() => SetLaunchMode(LaunchMode.Default));
        if (buttonModeArc != null) buttonModeArc.onClick.AddListener(() => SetLaunchMode(LaunchMode.Arc));
        if (buttonModeDownward != null) buttonModeDownward.onClick.AddListener(() => SetLaunchMode(LaunchMode.Downward));

        // Wire inventory button
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (changeHolenButton != null)
            changeHolenButton.onClick.AddListener(OnChangeHolenButtonPressed);

        SetChangeHolenButtonInteractable(false);
        RefreshModeButtonVisuals();

        SetCameraView(false);
        StartCoroutine(GameStartSequence());
    }

    void Update()
    {
        // Block swipe while inventory is open, holen is launched, or it is not this player's turn
        if (isTurn && currentHolenBall != null && !isReady && !isInventoryOpen && !isHolenLaunched)
        {
            HandleSwipeInput();

            if (isSwiping)
            {
                BuildLiveTrajectoryPoints();
                AnimateAndDrawLine();
            }
            else
            {
                if (trajectoryLine != null)
                    trajectoryLine.enabled = false;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  GAME START SEQUENCE
    // ─────────────────────────────────────────────
    private IEnumerator GameStartSequence()
    {
        loadingUI.SetActive(true);
        Debug.Log("Waiting for both players to connect...");

        while (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            yield return null;

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

    // ─────────────────────────────────────────────
    //  LAUNCH MODE SELECTION
    // ─────────────────────────────────────────────
    public void SetLaunchMode(LaunchMode mode)
    {
        if (isHolenLaunched)
        {
            Debug.Log("[MultiplayerHolenController] Cannot change mode while holen is in flight.");
            return;
        }
        if (activeLaunchMode == mode) return;

        activeLaunchMode = mode;
        Debug.Log($"[MultiplayerHolenController] Mode → {mode}");
        RefreshModeButtonVisuals();

        // Respawn so the spawn height adjusts for Downward mode
        if (isTurn && !isHolenLaunched)
            RespawnHolenBall();
    }

    public void SetLaunchModeDefault() => SetLaunchMode(LaunchMode.Default);
    public void SetLaunchModeArc() => SetLaunchMode(LaunchMode.Arc);
    public void SetLaunchModeDownward() => SetLaunchMode(LaunchMode.Downward);

    private void RefreshModeButtonVisuals()
    {
        SetButtonSelected(buttonModeDefault, activeLaunchMode == LaunchMode.Default);
        SetButtonSelected(buttonModeArc, activeLaunchMode == LaunchMode.Arc);
        SetButtonSelected(buttonModeDownward, activeLaunchMode == LaunchMode.Downward);
    }

    private void SetButtonSelected(Button btn, bool selected)
    {
        if (btn == null) return;
        ColorBlock cb = btn.colors;
        cb.normalColor = selected ? new Color(0.3f, 0.8f, 0.3f) : Color.white;
        btn.colors = cb;
    }

    // ─────────────────────────────────────────────
    //  HOLEN CHANGE LOGIC
    // ─────────────────────────────────────────────
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
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        isInventoryOpen = true;
        Debug.Log($"{playerRole} opened the Holen inventory.");
    }

    private void CloseInventory()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        isInventoryOpen = false;
        Debug.Log($"{playerRole} closed the Holen inventory.");
    }

    /// <summary>
    /// Call this from HolenSlotUI when the player taps a Holen in the inventory panel.
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

    // ─────────────────────────────────────────────
    //  STATUS TEXT (visible to both players via RPC)
    // ─────────────────────────────────────────────
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
            case "idle": return $"{activeName} Turn";
            case "changing": return $"{activeName} is changing their pamato";
            case "launched": return $"{activeName} Attacks!";
            default: return $"{activeName} Turn";
        }
    }

    private string GetOpponentName() => isPlayer1 ? "Player 2" : "Player 1";

    // ─────────────────────────────────────────────
    //  SWIPE INPUT
    // ─────────────────────────────────────────────
    private void HandleSwipeInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (!requireTouchOnBall || IsTouchingBall(touch.position))
                        StartSwipe(touch.position);
                    break;
                case TouchPhase.Moved:
                    if (isSwiping) UpdateSwipe(touch.position);
                    break;
                case TouchPhase.Ended:
                    if (isSwiping) EndSwipe(touch.position);
                    break;
                case TouchPhase.Canceled:
                    if (isSwiping) CancelSwipe();
                    break;
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (!requireTouchOnBall || IsTouchingBall(Input.mousePosition))
                StartSwipe(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isSwiping)
            UpdateSwipe(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0) && isSwiping)
            EndSwipe(Input.mousePosition);
    }

    private bool IsTouchingBall(Vector2 screenPos)
    {
        if (currentHolenBall == null) return false;
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f) && hit.collider.gameObject == currentHolenBall)
            return true;
        Vector3 ballScreen = mainCamera.WorldToScreenPoint(currentHolenBall.transform.position);
        return Vector2.Distance(screenPos, new Vector2(ballScreen.x, ballScreen.y)) < 100f;
    }

    private void StartSwipe(Vector2 screenPos)
    {
        isSwiping = true;
        swipeStartPos = screenPos;
        swipeStartTime = Time.time;
        swipeWorldStart = currentHolenBall.transform.position;
        raycastTargetValid = false;

        if (swipeIndicator != null) swipeIndicator.SetActive(true);
        Debug.Log($"Swipe started at: {screenPos}");
    }

    private void UpdateSwipe(Vector2 screenPos)
    {
        swipeEndPos = screenPos;

        if (activeLaunchMode == LaunchMode.Arc)
        {
            // Drag-amplification targeting for Arc mode
            Vector3 holenWorldPos = currentHolenBall.transform.position;
            Vector3 holenScreen3 = mainCamera.WorldToScreenPoint(holenWorldPos);
            Vector2 holenScreen2 = new Vector2(holenScreen3.x, holenScreen3.y);
            Vector2 dragDelta = screenPos - holenScreen2;

            if (dragDelta.magnitude < swipeDeadZone)
            {
                raycastTargetValid = false;
                if (targetIndicator != null) targetIndicator.SetActive(false);
                return;
            }

            Plane groundPlane = new Plane(Vector3.up, holenWorldPos);
            Ray holenRay = mainCamera.ScreenPointToRay(holenScreen2);
            Ray fingerRay = mainCamera.ScreenPointToRay(screenPos);

            if (groundPlane.Raycast(holenRay, out float holenDist) &&
                groundPlane.Raycast(fingerRay, out float fingerDist))
            {
                Vector3 holenGroundPt = holenRay.GetPoint(holenDist);
                Vector3 fingerGroundPt = fingerRay.GetPoint(fingerDist);
                Vector3 worldDelta = fingerGroundPt - holenGroundPt;
                Vector3 amplified = worldDelta * arcDragAmplification;

                if (amplified.magnitude > arcMaxTargetRadius)
                    amplified = amplified.normalized * arcMaxTargetRadius;

                raycastTarget = holenGroundPt + amplified;
                raycastTargetValid = true;

                if (targetIndicator != null)
                {
                    targetIndicator.SetActive(true);
                    targetIndicator.transform.position = raycastTarget;
                }
            }
            else
            {
                raycastTargetValid = false;
                if (targetIndicator != null) targetIndicator.SetActive(false);
            }
        }
        else if (activeLaunchMode == LaunchMode.Downward)
        {
            // Direct ground raycast for Downward mode
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayerMask))
            {
                raycastTarget = hit.point;
                raycastTargetValid = true;

                if (targetIndicator != null)
                {
                    targetIndicator.SetActive(true);
                    targetIndicator.transform.position = raycastTarget;
                }
            }
            else
            {
                raycastTargetValid = false;
                if (targetIndicator != null) targetIndicator.SetActive(false);
            }
        }
        else // Default mode
        {
            if (targetIndicator != null) targetIndicator.SetActive(false);

            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            Plane pl = new Plane(Vector3.up, currentHolenBall.transform.position);
            if (pl.Raycast(ray, out float dist))
            {
                raycastTarget = ray.GetPoint(dist);
                raycastTargetValid = true;
            }
        }
    }

    private void EndSwipe(Vector2 screenPos)
    {
        swipeEndPos = screenPos;
        float swipeTime = Time.time - swipeStartTime;
        Vector2 swipeDelta = swipeEndPos - swipeStartPos;
        float swipeDistance = swipeDelta.magnitude;

        isSwiping = false;

        if (trajectoryLine != null) trajectoryLine.enabled = false;
        if (swipeIndicator != null) swipeIndicator.SetActive(false);
        if (targetIndicator != null) targetIndicator.SetActive(false);

        float activeTimeWindow = activeLaunchMode switch
        {
            LaunchMode.Arc => arcSwipeTimeWindow,
            LaunchMode.Downward => downwardSwipeTimeWindow,
            _ => swipeTimeWindow
        };

        bool valid = swipeDistance >= minSwipeDistance &&
                     (activeTimeWindow <= 0f || swipeTime <= activeTimeWindow);

        if (!valid)
        {
            Debug.Log($"[MultiplayerHolenController] Invalid swipe. Dist={swipeDistance:F0} Time={swipeTime:F2}");
            return;
        }

        Vector3 launchVelocity = ComputeLaunchVelocity(swipeDelta, swipeTime, swipeDistance);
        if (launchVelocity == Vector3.zero)
        {
            Debug.LogWarning("[MultiplayerHolenController] Could not compute launch velocity.");
            return;
        }

        // Trigger the networked shoot
        ShootHolen(launchVelocity);
    }

    private void CancelSwipe()
    {
        isSwiping = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
        if (swipeIndicator != null) swipeIndicator.SetActive(false);
        if (targetIndicator != null) targetIndicator.SetActive(false);
        Debug.Log("Swipe cancelled.");
    }

    // ─────────────────────────────────────────────
    //  LAUNCH VELOCITY COMPUTATION
    // ─────────────────────────────────────────────
    private Vector3 ComputeLaunchVelocity(Vector2 swipeDelta, float swipeTime, float swipeDistance)
    {
        switch (activeLaunchMode)
        {
            case LaunchMode.Arc:
                {
                    if (!raycastTargetValid) { Debug.LogWarning("[Arc] No valid ground target."); return Vector3.zero; }
                    Vector3 spawnPos = currentHolenBall.transform.position;
                    Vector3 toTarget = raycastTarget - spawnPos;
                    float horizDist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
                    float vertDiff = raycastTarget.y - spawnPos.y;
                    float rawSpeed = SolveArcSpeed(horizDist, vertDiff, arcAngle);
                    if (rawSpeed <= 0f) return Vector3.zero;
                    float speed = rawSpeed * arcForceMultiplier;
                    float rad = arcAngle * Mathf.Deg2Rad;
                    Vector3 horizDir = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
                    return horizDir * (speed * Mathf.Cos(rad)) + Vector3.up * (speed * Mathf.Sin(rad));
                }

            case LaunchMode.Downward:
                {
                    if (!raycastTargetValid) { Debug.LogWarning("[Downward] No valid ground target."); return Vector3.zero; }
                    Vector3 spawnPos = currentHolenBall.transform.position;
                    Vector3 toTarget = raycastTarget - spawnPos;
                    float horizDist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
                    float vertDiff = raycastTarget.y - spawnPos.y;
                    float rawSpeed = SolveDownwardSpeed(horizDist, vertDiff, downwardAngle);
                    if (rawSpeed <= 0f) return Vector3.zero;
                    float speed = rawSpeed * downwardForceMultiplier;
                    float rad = downwardAngle * Mathf.Deg2Rad;
                    Vector3 horizDir = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
                    return horizDir * (speed * Mathf.Cos(rad)) - Vector3.up * (speed * Mathf.Sin(rad));
                }

            default: // Default
                {
                    Vector3 swipeWorldDir = Vector3.zero;
                    if (raycastTargetValid)
                    {
                        swipeWorldDir = raycastTarget - swipeWorldStart;
                        swipeWorldDir.y = 0f;
                        swipeWorldDir.Normalize();
                    }
                    if (swipeWorldDir.sqrMagnitude < 0.01f)
                    {
                        Vector3 camFwd = mainCamera.transform.forward; camFwd.y = 0f; camFwd.Normalize();
                        Vector3 camRight = mainCamera.transform.right; camRight.y = 0f; camRight.Normalize();
                        swipeWorldDir = (camRight * swipeDelta.x + camFwd * swipeDelta.y).normalized;
                    }

                    float force;
                    if (useSpeedForce)
                    {
                        float sp = swipeDistance / Mathf.Max(swipeTime, 0.01f);
                        force = Mathf.Clamp(sp * speedMultiplier, minLaunchForce, maxLaunchForce);
                    }
                    else
                    {
                        float nd = Mathf.InverseLerp(minSwipeDistance, maxSwipeDistance, swipeDistance);
                        force = Mathf.Lerp(minLaunchForce, maxLaunchForce, nd);
                        float sp2 = swipeDistance / Mathf.Max(swipeTime, 0.01f);
                        force = Mathf.Lerp(force, maxLaunchForce, Mathf.Clamp01(sp2 / 2000f));
                    }
                    return swipeWorldDir * force;
                }
        }
    }

    // ─────────────────────────────────────────────
    //  PHYSICS SOLVERS
    // ─────────────────────────────────────────────
    private float SolveArcSpeed(float horizDist, float vertDiff, float angleDeg)
    {
        float θ = angleDeg * Mathf.Deg2Rad;
        float g = Mathf.Abs(Physics.gravity.y);
        float cosθ = Mathf.Cos(θ);
        float tanθ = Mathf.Tan(θ);
        float denom = 2f * cosθ * cosθ * (horizDist * tanθ - vertDiff);
        if (denom <= 0f) { Debug.LogWarning($"[Arc Solver] Unreachable. hDist={horizDist:F2} vDiff={vertDiff:F2}"); return -1f; }
        return Mathf.Sqrt((g * horizDist * horizDist) / denom);
    }

    private float SolveDownwardSpeed(float horizDist, float vertDiff, float angleDeg)
    {
        float θ = angleDeg * Mathf.Deg2Rad;
        float g = Mathf.Abs(Physics.gravity.y);
        float cosθ = Mathf.Cos(θ);
        float tanθ = Mathf.Tan(θ);
        float denom = 2f * cosθ * cosθ * (-horizDist * tanθ - vertDiff);
        if (denom <= 0f) { Debug.LogWarning($"[Downward Solver] Unreachable. hDist={horizDist:F2} vDiff={vertDiff:F2}"); return -1f; }
        return Mathf.Sqrt((g * horizDist * horizDist) / denom);
    }

    // ─────────────────────────────────────────────
    //  TRAJECTORY BUILDING  (only runs while isSwiping)
    // ─────────────────────────────────────────────
    private void BuildLiveTrajectoryPoints()
    {
        if (currentHolenBall == null) return;

        switch (activeLaunchMode)
        {
            case LaunchMode.Arc:
                {
                    if (!raycastTargetValid) return;
                    Vector3 spawnPos = currentHolenBall.transform.position;
                    Vector3 toTarget = raycastTarget - spawnPos;
                    float horizDist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
                    float vertDiff = raycastTarget.y - spawnPos.y;
                    float rawSpeed = SolveArcSpeed(horizDist, vertDiff, arcAngle);
                    if (rawSpeed <= 0f) return;
                    float speed = rawSpeed * arcForceMultiplier;
                    float rad = arcAngle * Mathf.Deg2Rad;
                    Vector3 horizDir = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
                    Vector3 vel = horizDir * (speed * Mathf.Cos(rad)) + Vector3.up * (speed * Mathf.Sin(rad));
                    BuildPhysicsPoints(spawnPos, vel);
                    break;
                }
            case LaunchMode.Downward:
                {
                    if (!raycastTargetValid) return;
                    Vector3 spawnPos = currentHolenBall.transform.position;
                    Vector3 toTarget = raycastTarget - spawnPos;
                    float horizDist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
                    float vertDiff = raycastTarget.y - spawnPos.y;
                    float rawSpeed = SolveDownwardSpeed(horizDist, vertDiff, downwardAngle);
                    if (rawSpeed <= 0f) return;
                    float speed = rawSpeed * downwardForceMultiplier;
                    float rad = downwardAngle * Mathf.Deg2Rad;
                    Vector3 horizDir = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
                    Vector3 vel = horizDir * (speed * Mathf.Cos(rad)) - Vector3.up * (speed * Mathf.Sin(rad));
                    BuildPhysicsPoints(spawnPos, vel);
                    break;
                }
            default:
                {
                    Vector3 spawnPos = currentHolenBall.transform.position;
                    Vector3 dir = Vector3.zero;
                    if (raycastTargetValid) { dir = raycastTarget - swipeWorldStart; dir.y = 0f; dir.Normalize(); }
                    if (dir.sqrMagnitude < 0.01f) { dir = mainCamera.transform.forward; dir.y = 0f; dir.Normalize(); }
                    BuildStraightPoints(spawnPos, dir);
                    break;
                }
        }
    }

    private void BuildPhysicsPoints(Vector3 startPos, Vector3 velocity)
    {
        fullTrajectoryPoints = new Vector3[trajectoryPointCount];
        for (int i = 0; i < trajectoryPointCount; i++)
        {
            float t = i * trajectoryTimeStep;
            fullTrajectoryPoints[i] = startPos + velocity * t + 0.5f * Physics.gravity * t * t;
        }
    }

    private void BuildStraightPoints(Vector3 startPos, Vector3 direction)
    {
        fullTrajectoryPoints = new Vector3[2];
        fullTrajectoryPoints[0] = startPos;
        fullTrajectoryPoints[1] = startPos + direction * defaultLineLength;
    }

    // ─────────────────────────────────────────────
    //  ANIMATED LINE RENDERING
    // ─────────────────────────────────────────────
    private void AnimateAndDrawLine()
    {
        if (trajectoryLine == null || fullTrajectoryPoints == null || fullTrajectoryPoints.Length < 2)
            return;

        lineAnimOffset = (lineAnimOffset + Time.deltaTime * lineAnimationSpeed) % 1f;

        int totalPts = fullTrajectoryPoints.Length;
        int visibleCount = Mathf.Max(2, Mathf.RoundToInt(totalPts * lineVisibleFraction));
        int startIdx = Mathf.FloorToInt(lineAnimOffset * totalPts);

        trajectoryLine.enabled = true;
        trajectoryLine.positionCount = visibleCount;

        for (int i = 0; i < visibleCount; i++)
            trajectoryLine.SetPosition(i, fullTrajectoryPoints[(startIdx + i) % totalPts]);

        trajectoryLine.startColor = lineColor;
        trajectoryLine.endColor = lineColor;
    }

    // ─────────────────────────────────────────────
    //  CAMERA
    // ─────────────────────────────────────────────
    private void SetInitialCameraPosition()
    {
        if (cameraSpawnPoint != null)
        {
            mainCamera.transform.position = cameraSpawnPoint.position;
            mainCamera.transform.rotation = cameraSpawnPoint.rotation;
        }
    }

    private void ApplyAimCameraSettings()
    {
        if (activePlayerCamera == null) return;
        var transposer = activePlayerCamera.GetCinemachineComponent<CinemachineTransposer>();
        var composer = activePlayerCamera.GetCinemachineComponent<CinemachineComposer>();
        if (transposer != null) transposer.m_FollowOffset = cameraFollowOffset;
        if (composer != null) composer.m_ScreenY = cameraAimScreenY;
    }

    private void ApplyFlightCameraSettings()
    {
        if (activePlayerCamera == null) return;
        var transposer = activePlayerCamera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null) transposer.m_FollowOffset = Vector3.zero;
        var composer = activePlayerCamera.GetCinemachineComponent<CinemachineComposer>();
        if (composer != null) composer.m_ScreenY = cameraFlightScreenY;
    }

    /// <summary>
    /// Switches between active-player (close follow) and birds-eye camera based on whose turn it is.
    /// </summary>
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

    // ─────────────────────────────────────────────
    //  HOLEN BALL SPAWN
    // ─────────────────────────────────────────────
    private void SpawnHolenBall()
    {
        // Determine spawn position — Downward mode spawns higher
        Vector3 spawnPos = ballSpawnPoint.position;
        if (activeLaunchMode == LaunchMode.Downward)
            spawnPos += Vector3.up * downwardSpawnHeightOffset;

        currentHolenBall = PhotonNetwork.Instantiate(holenBallPrefab.name, spawnPos, Quaternion.identity);

        if (ballLayer != -1)
            currentHolenBall.layer = ballLayer;

        Rigidbody rb = currentHolenBall.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        if (activePlayerCamera != null && isTurn)
        {
            activePlayerCamera.Follow = currentHolenBall.transform;
            activePlayerCamera.LookAt = currentHolenBall.transform;
            ApplyAimCameraSettings();
        }

        if (targetIndicator != null)
            targetIndicator.SetActive(false);

        Debug.Log($"{playerRole} spawned Holen Ball: {holenBallPrefab.name} (Mode: {activeLaunchMode})");
    }

    /// <summary>
    /// Destroys and re-spawns the holen ball (e.g. when changing launch mode).
    /// </summary>
    private void RespawnHolenBall()
    {
        if (currentHolenBall != null)
        {
            PhotonNetwork.Destroy(currentHolenBall);
            currentHolenBall = null;
        }
        SpawnHolenBall();
    }

    // ─────────────────────────────────────────────
    //  SHOOT / TURN
    // ─────────────────────────────────────────────
    public bool IsTurn() => isTurn;

    /// <summary>
    /// Called locally once a valid swipe is computed. Sends launch velocity over the network.
    /// </summary>
    public void ShootHolen(Vector3 launchVelocity)
    {
        if (!isTurn || isReady || currentHolenBall == null) return;

        isReady = true;
        isHolenLaunched = true;

        SetChangeHolenButtonInteractable(false);
        CloseInventory();

        photonView.RPC("RPC_ShootHolen", RpcTarget.All, launchVelocity);
        UpdateStatusText("launched");
        StartCoroutine(CompleteTurn());

        Debug.Log($"{playerRole} launched Holen Ball. Velocity={launchVelocity} Mode={activeLaunchMode}");
    }

    [PunRPC]
    private void RPC_ShootHolen(Vector3 launchVelocity)
    {
        if (currentHolenBall == null) return;

        Rigidbody rb = currentHolenBall.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.AddForce(launchVelocity, ForceMode.VelocityChange);

        if (activePlayerCamera != null && isTurn)
        {
            activePlayerCamera.Follow = null;
            activePlayerCamera.LookAt = currentHolenBall.transform;
            ApplyFlightCameraSettings();
        }

        if (audioSource != null && launchSoundClip != null)
            audioSource.PlayOneShot(launchSoundClip);
    }

    private IEnumerator CompleteTurn()
    {
        yield return new WaitForSeconds(7f);

        if (isTurn && activePlayerCamera != null)
        {
            activePlayerCamera.Follow = null;
            activePlayerCamera.LookAt = defaultLookAtTarget != null ? defaultLookAtTarget : null;

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

    // ─────────────────────────────────────────────
    //  ENABLE / DISABLE CONTROLS
    // ─────────────────────────────────────────────
    private void DisableControls()
    {
        if (swipeIndicator != null) swipeIndicator.SetActive(false);
        if (trajectoryLine != null) trajectoryLine.enabled = false;
        if (targetIndicator != null) targetIndicator.SetActive(false);
        SetCameraView(false);
    }

    private void EnableControls()
    {
        SetCameraView(true);
    }

    // ─────────────────────────────────────────────
    //  END / SWITCH TURN
    // ─────────────────────────────────────────────
    private void EndTurn()
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

        Debug.Log($"{playerRole} ended their turn. Switching to other player.");

        PVPScore scoreManager = FindObjectOfType<PVPScore>();
        if (scoreManager != null)
            scoreManager.OnTurnEnd();

        photonView.RPC("SwitchTurn", RpcTarget.Others);
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

        Debug.Log($"{playerRole}'s turn started");
    }
}