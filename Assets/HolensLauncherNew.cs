using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using System.Collections;

public class HolensLauncherNew : MonoBehaviour
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
    public Transform holensPosition;
    public GameObject holensBallPrefab;
    public CinemachineVirtualCamera cinemachineCamera;
    public Camera mainCamera;
    public Transform cameraSpawnPoint;

    // ─────────────────────────────────────────────
    //  UI
    // ─────────────────────────────────────────────
    [Header("UI")]
    public GameObject swipeIndicator;
    public LineRenderer trajectoryLine;

    [Header("Launch Mode UI Buttons")]
    public Button buttonModeDefault;
    public Button buttonModeArc;
    public Button buttonModeDownward;

    // ─────────────────────────────────────────────
    //  ACTIVE MODE
    // ─────────────────────────────────────────────
    [Header("Active Launch Mode")]
    public LaunchMode activeLaunchMode = LaunchMode.Default;

    // ─────────────────────────────────────────────
    //  SWIPE SETTINGS
    // ─────────────────────────────────────────────
    [Header("Swipe Settings (All Modes)")]
    public float minSwipeDistance = 50f;
    public float maxSwipeDistance = 500f;
    public float minLaunchForce = 5f;
    public float maxLaunchForce = 100f;
    [Tooltip("Time window only enforced in Default mode")]
    public float swipeTimeWindow = 1f;
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
    [Tooltip("How far ahead of the finger drag the target indicator moves. " +
             "Higher = target overshoots further, harder to aim precisely.")]
    public float arcDragAmplification = 2.5f;
    [Tooltip("Maximum world-space radius the target can travel from the holen. " +
             "Keeps targets reachable but limits how far a wild drag goes.")]
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
    [Tooltip("Color of the trajectory line. Alpha controls transparency (0.5 = 50%).")]
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
    [Tooltip("AudioSource to play the launch sound through. " +
             "If left empty the script will try to find one on this GameObject.")]
    public AudioSource audioSource;
    [Tooltip("Sound clip that plays the moment the holen is launched.")]
    public AudioClip launchSoundClip;

    // ─────────────────────────────────────────────
    //  TARGETING INDICATOR
    // ─────────────────────────────────────────────
    [Header("Targeting Indicator")]
    [Tooltip("3D GameObject that shows where the holen will land. " +
             "Assign it here — it should be disabled in the scene by default. " +
             "Only visible in Arc and Downward modes while the player is aiming.")]
    public GameObject targetIndicator;

    // ─────────────────────────────────────────────
    //  HOLEN SYSTEM
    // ─────────────────────────────────────────────
    [Header("Holen System")]
    public HolenChanger holenChanger;

    [Header("Holen Select Buttons")]
    [Tooltip("Assign the Button for Holen 1 here — no OnClick setup needed, wired automatically.")]
    public Button holenSelectButton1;
    [Tooltip("Assign the Button for Holen 2 here — no OnClick setup needed, wired automatically.")]
    public Button holenSelectButton2;
    [Tooltip("Assign the Button for Holen 3 here — no OnClick setup needed, wired automatically.")]
    public Button holenSelectButton3;

    // ─────────────────────────────────────────────
    //  LIFE SYSTEM
    // ─────────────────────────────────────────────
    [Header("Life System")]
    public float lifeDeductionDelay = 6.5f;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────
    private int ballLayer;
    private int groundLayerMask;
    private GameObject currentBall;
    private bool isBusy = false;
    private bool hasLaunched = false;
    private Transform defaultLookAtTarget;

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

        // Auto-grab AudioSource from this GameObject if not assigned in Inspector
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        SetInitialCameraPosition();

        if (cinemachineCamera != null)
            defaultLookAtTarget = cinemachineCamera.LookAt;

        if (swipeIndicator != null)
            swipeIndicator.SetActive(false);

        // ── Trajectory line starts hidden ──────────────────────────────────────
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
            trajectoryLine.startColor = lineColor;
            trajectoryLine.endColor = lineColor;
        }

        // Target indicator is always hidden until the player aims in Arc/Downward mode
        if (targetIndicator != null)
            targetIndicator.SetActive(false);

        // Wire up launch mode buttons
        if (buttonModeDefault != null) buttonModeDefault.onClick.AddListener(() => SetLaunchMode(LaunchMode.Default));
        if (buttonModeArc != null) buttonModeArc.onClick.AddListener(() => SetLaunchMode(LaunchMode.Arc));
        if (buttonModeDownward != null) buttonModeDownward.onClick.AddListener(() => SetLaunchMode(LaunchMode.Downward));

        // Wire up holen select buttons directly here — no OnClick needed in Inspector
        if (holenSelectButton1 != null) holenSelectButton1.onClick.AddListener(() => SelectHolenSlot(1));
        if (holenSelectButton2 != null) holenSelectButton2.onClick.AddListener(() => SelectHolenSlot(2));
        if (holenSelectButton3 != null) holenSelectButton3.onClick.AddListener(() => SelectHolenSlot(3));

        RefreshModeButtonVisuals();
        SpawnCurrentHolen();
    }

    void Update()
    {
        if (!isBusy && !hasLaunched && currentBall != null)
        {
            HandleSwipeInput();

            if (isSwiping)
            {
                // Only build and show the trajectory while the finger is actively held down
                BuildLiveTrajectoryPoints();
                AnimateAndDrawLine();
            }
            else
            {
                // Hide the line completely when not aiming
                if (trajectoryLine != null)
                    trajectoryLine.enabled = false;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  HOLEN SELECTION  (called by the wired buttons)
    // ─────────────────────────────────────────────
    /// <summary>
    /// Selects a holen slot (1, 2, or 3) directly on the launcher.
    /// Destroys the current ball and spawns the new one — identical flow to SetLaunchMode.
    /// Remove any OnClick entries from your buttons; they are wired automatically in Start().
    /// </summary>
    private void SelectHolenSlot(int slot)
    {
        if (hasLaunched || isBusy)
        {
            Debug.Log("[HolensLauncher] Cannot change holen while busy or in flight.");
            return;
        }

        if (holenChanger == null)
        {
            Debug.LogWarning("[HolensLauncher] holenChanger is not assigned.");
            return;
        }

        // Determine which HolenData to use
        HolenData targetData = slot switch
        {
            1 => holenChanger.holen1Data,
            2 => holenChanger.holen2Data,
            3 => holenChanger.holen3Data,
            _ => null
        };

        if (targetData == null || targetData.holenPrefab == null)
        {
            Debug.LogWarning($"[HolensLauncher] HolenData for slot {slot} is null or has no prefab.");
            return;
        }

        // Update HolenChanger's internal selection so its UI icon stays in sync
        // We call the existing setter logic directly via a new lightweight path:
        holenChanger.SetCurrentHolenDataDirect(targetData);

        // Update our own tracked prefab and swap the ball — same as SetLaunchMode does
        holensBallPrefab = targetData.holenPrefab;
        SpawnCurrentHolen();
    }

    // ─────────────────────────────────────────────
    //  LAUNCH MODE SELECTION
    // ─────────────────────────────────────────────
    public void SetLaunchMode(LaunchMode mode)
    {
        if (hasLaunched)
        {
            Debug.Log("[HolensLauncher] Cannot change mode while holen is in flight.");
            return;
        }
        if (activeLaunchMode == mode) return;

        activeLaunchMode = mode;
        Debug.Log($"[HolensLauncher] Mode → {mode}");
        RefreshModeButtonVisuals();
        SpawnCurrentHolen();
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
        if (cinemachineCamera == null) return;
        var transposer = cinemachineCamera.GetCinemachineComponent<CinemachineTransposer>();
        var composer = cinemachineCamera.GetCinemachineComponent<CinemachineComposer>();
        if (transposer != null) transposer.m_FollowOffset = cameraFollowOffset;
        if (composer != null) composer.m_ScreenY = cameraAimScreenY;
    }

    private void ApplyFlightCameraSettings()
    {
        if (cinemachineCamera == null) return;
        var transposer = cinemachineCamera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null) transposer.m_FollowOffset = Vector3.zero;
        var composer = cinemachineCamera.GetCinemachineComponent<CinemachineComposer>();
        if (composer != null) composer.m_ScreenY = cameraFlightScreenY;
    }

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
        if (currentBall == null) return false;
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f) && hit.collider.gameObject == currentBall)
            return true;
        Vector3 ballScreen = mainCamera.WorldToScreenPoint(currentBall.transform.position);
        return Vector2.Distance(screenPos, new Vector2(ballScreen.x, ballScreen.y)) < 100f;
    }

    private void StartSwipe(Vector2 screenPos)
    {
        isSwiping = true;
        swipeStartPos = screenPos;
        swipeStartTime = Time.time;
        swipeWorldStart = currentBall.transform.position;
        raycastTargetValid = false;

        if (swipeIndicator != null) swipeIndicator.SetActive(true);
    }

    private void UpdateSwipe(Vector2 screenPos)
    {
        swipeEndPos = screenPos;

        if (activeLaunchMode == LaunchMode.Arc)
        {
            // ── NEW ARC DRAG TARGETING ─────────────────────────────────────────
            // The player drags FROM the holen's screen position.
            // We project the drag delta into world space (on the ground plane at
            // holen height) then amplify it so the target shoots ahead of the
            // finger — making the mechanic harder to use precisely.

            Vector3 holenWorldPos = currentBall.transform.position;
            Vector3 holenScreenPos3 = mainCamera.WorldToScreenPoint(holenWorldPos);
            Vector2 holenScreenPos2 = new Vector2(holenScreenPos3.x, holenScreenPos3.y);

            // Raw drag delta from holen screen position
            Vector2 dragDelta = screenPos - holenScreenPos2;

            // Only activate targeting once past the dead zone
            if (dragDelta.magnitude < swipeDeadZone)
            {
                raycastTargetValid = false;
                if (targetIndicator != null) targetIndicator.SetActive(false);
                return;
            }

            // Convert two screen points to world points on the holen's ground plane
            Plane groundPlane = new Plane(Vector3.up, holenWorldPos);

            Ray holenRay = mainCamera.ScreenPointToRay(holenScreenPos2);
            Ray fingerRay = mainCamera.ScreenPointToRay(screenPos);

            if (groundPlane.Raycast(holenRay, out float holenDist) &&
                groundPlane.Raycast(fingerRay, out float fingerDist))
            {
                Vector3 holenGroundPt = holenRay.GetPoint(holenDist);
                Vector3 fingerGroundPt = fingerRay.GetPoint(fingerDist);

                // World-space delta from holen to where the finger points on the ground
                Vector3 worldDelta = fingerGroundPt - holenGroundPt;

                // Amplify so target races ahead of the finger
                Vector3 amplifiedDelta = worldDelta * arcDragAmplification;

                // Clamp to max radius so it stays reachable
                if (amplifiedDelta.magnitude > arcMaxTargetRadius)
                    amplifiedDelta = amplifiedDelta.normalized * arcMaxTargetRadius;

                raycastTarget = holenGroundPt + amplifiedDelta;
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
            // Downward mode keeps the original direct-raycast targeting
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
                if (targetIndicator != null)
                    targetIndicator.SetActive(false);
            }
        }
        else // Default mode
        {
            if (targetIndicator != null)
                targetIndicator.SetActive(false);

            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            Plane plane = new Plane(Vector3.up, currentBall.transform.position);
            if (plane.Raycast(ray, out float dist))
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

        bool valid = swipeDistance >= minSwipeDistance &&
                     (activeLaunchMode != LaunchMode.Default || swipeTime <= swipeTimeWindow);

        if (!valid)
        {
            Debug.Log($"[HolensLauncher] Invalid swipe. Dist={swipeDistance:F0} Time={swipeTime:F2}");
            return;
        }

        Vector3 launchVelocity = ComputeLaunchVelocity(swipeDelta, swipeTime, swipeDistance);
        if (launchVelocity == Vector3.zero)
        {
            Debug.LogWarning("[HolensLauncher] Could not compute launch velocity — finger may not be over Ground layer.");
            return;
        }

        StartCoroutine(LaunchSequence(launchVelocity));
    }

    private void CancelSwipe()
    {
        isSwiping = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
        if (swipeIndicator != null) swipeIndicator.SetActive(false);
        if (targetIndicator != null) targetIndicator.SetActive(false);
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
                    Vector3 spawnPos = currentBall.transform.position;
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
                    Vector3 spawnPos = currentBall.transform.position;
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
        if (currentBall == null) return;

        switch (activeLaunchMode)
        {
            case LaunchMode.Arc:
                {
                    if (!raycastTargetValid) return;
                    Vector3 spawnPos = currentBall.transform.position;
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
                    Vector3 spawnPos = currentBall.transform.position;
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
                    Vector3 spawnPos = currentBall.transform.position;
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
    //  LAUNCH SEQUENCE
    // ─────────────────────────────────────────────
    IEnumerator LaunchSequence(Vector3 launchVelocity)
    {
        isBusy = true;
        hasLaunched = true;

        if (trajectoryLine != null) trajectoryLine.enabled = false;
        if (holenChanger != null) holenChanger.DisableButtons();

        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
            levelManager.OnTurnStarted();

        LaunchBall(launchVelocity);

        yield return new WaitForSeconds(lifeDeductionDelay);

        if (levelManager != null)
        {
            levelManager.OnHolenRespawn();
            Debug.Log("[HolensLauncher] Life deducted after turn");
        }

        yield return new WaitForSeconds(7f - lifeDeductionDelay);

        // Reset camera
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Follow = null;
            cinemachineCamera.LookAt = defaultLookAtTarget != null ? defaultLookAtTarget : null;
            if (cameraSpawnPoint != null)
            {
                mainCamera.transform.position = cameraSpawnPoint.position;
                mainCamera.transform.rotation = cameraSpawnPoint.rotation;
            }
        }

        // Destroy launched ball and sweep strays
        if (currentBall != null)
        {
            currentBall.tag = "Untagged";
            DestroyImmediate(currentBall);
            currentBall = null;
        }
        foreach (GameObject stray in GameObject.FindGameObjectsWithTag("Ball"))
        {
            stray.transform.parent = null;
            DestroyImmediate(stray);
        }
        for (int i = holensPosition.childCount - 1; i >= 0; i--)
            DestroyImmediate(holensPosition.GetChild(i).gameObject);

        isBusy = false;
        hasLaunched = false;
        isSwiping = false;

        SpawnCurrentHolen();

        if (holenChanger != null) holenChanger.EnableButtons();

        if (levelManager != null)
            levelManager.OnTurnEnded();
    }

    // ─────────────────────────────────────────────
    //  LAUNCH BALL
    // ─────────────────────────────────────────────
    void LaunchBall(Vector3 velocity)
    {
        if (currentBall == null) return;

        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        currentBall.transform.parent = null;
        rb.isKinematic = false;
        rb.AddForce(velocity, ForceMode.VelocityChange);

        if (cinemachineCamera != null)
        {
            cinemachineCamera.Follow = null;
            cinemachineCamera.LookAt = currentBall.transform;
            ApplyFlightCameraSettings();
        }

        if (audioSource != null && launchSoundClip != null)
            audioSource.PlayOneShot(launchSoundClip);

        Debug.Log($"[Launch] Mode={activeLaunchMode} Speed={velocity.magnitude:F2}");
    }

    // ─────────────────────────────────────────────
    //  SPAWN — SINGLE AUTHORITATIVE METHOD
    // ─────────────────────────────────────────────
    private void SpawnCurrentHolen()
    {
        // 1. Determine the correct prefab
        GameObject prefabToUse = holensBallPrefab;
        if (holenChanger != null)
        {
            HolenData data = holenChanger.GetCurrentHolenData();
            if (data != null && data.holenPrefab != null)
            {
                prefabToUse = data.holenPrefab;
                holensBallPrefab = prefabToUse;
            }
        }

        if (prefabToUse == null)
        {
            Debug.LogError("[HolensLauncher] No holen prefab available to spawn!");
            return;
        }

        // 2. Destroy the existing ball and sweep all strays BEFORE instantiating
        if (currentBall != null)
        {
            currentBall.transform.parent = null;
            currentBall.tag = "Untagged";
            DestroyImmediate(currentBall);
            currentBall = null;
        }

        foreach (GameObject stray in GameObject.FindGameObjectsWithTag("Ball"))
        {
            stray.transform.parent = null;
            DestroyImmediate(stray);
        }

        for (int i = holensPosition.childCount - 1; i >= 0; i--)
            DestroyImmediate(holensPosition.GetChild(i).gameObject);

        if (targetIndicator != null)
            targetIndicator.SetActive(false);

        // 3. Spawn exactly one new ball
        Vector3 spawnPos = holensPosition.position;
        if (activeLaunchMode == LaunchMode.Downward)
            spawnPos += Vector3.up * downwardSpawnHeightOffset;

        currentBall = Instantiate(prefabToUse, spawnPos, holensPosition.rotation);
        currentBall.transform.SetParent(holensPosition, worldPositionStays: true);
        currentBall.tag = "Ball";
        if (ballLayer != -1) currentBall.layer = ballLayer;
        currentBall.GetComponent<Rigidbody>().isKinematic = true;

        // 4. Camera
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Follow = currentBall.transform;
            cinemachineCamera.LookAt = currentBall.transform;
            ApplyAimCameraSettings();
        }

        Debug.Log($"[Spawn] Mode={activeLaunchMode} Prefab={prefabToUse.name}");
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────
    public void ChangeBallPrefab(GameObject newPrefab)
    {
        if (hasLaunched)
        {
            Debug.Log("[HolensLauncher] Cannot change holen while in flight.");
            return;
        }
        holensBallPrefab = newPrefab;
        SpawnCurrentHolen();
    }

    public bool GetIsBusy() => isBusy;
    public bool GetHasLaunched() => hasLaunched;
}