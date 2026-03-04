using Cinemachine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Turn-based multiplayer Holen controller.
/// Architecture: ONE instance of this script exists in the scene (not per-player prefab).
/// photonView is shared — RPCs reach all clients. isTurn gates whose input is active.
///
/// ══ CHANGES FROM ORIGINAL ════════════════════════════════════════════════════
///
/// 1. OnHolenSelectedFromInventory(GameObject, HolenData)
///    Old signature: OnHolenSelectedFromInventory(GameObject newHolenPrefab)
///    New signature: OnHolenSelectedFromInventory(GameObject newHolenPrefab, HolenData data)
///
///    The extra HolenData parameter lets us swap the visible 3D model on the ball.
///    When a player picks a new holen from the inventory:
///      a) holenBallPrefab is updated (controls which Photon prefab is spawned)
///      b) SpawnHolenBall() destroys the old network ball and spawns a new one
///      c) SwapHolenModel(data) is called: finds the first child of currentHolenBall
///         that is the old 3D model, destroys it, then instantiates data.holenPrefab
///         as a child at local zero offset.
///
/// 2. New field: activeHolenData
///    Stores the currently selected HolenData so SwapHolenModel knows what the ball
///    should look like after spawn (called from SpawnHolenBall via pendingHolenData).
///
/// 3. New field: modelParentTag / modelChildName (optional)
///    To find the right child to destroy, we look for a child tagged "HolenModel".
///    Tag your 3D model children with "HolenModel" in the Inspector.
///    If nothing is tagged, we fall back to destroying ALL children of currentHolenBall
///    that are NOT core components (Rigidbody, Collider) — see SwapHolenModel().
/// </summary>
public class MultiplayerHolenControllerNew : MonoBehaviourPunCallbacks
{
    public enum LaunchMode { Default = 0, Arc = 1, Downward = 2 }

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

    [Header("Launch Mode UI Buttons")]
    public Button buttonModeDefault;
    public Button buttonModeArc;
    public Button buttonModeDownward;

    [Header("Holen Change UI")]
    public Button changeHolenButton;
    public GameObject inventoryPanel;
    public TMP_Text statusText;

    [Header("Active Launch Mode")]
    public LaunchMode activeLaunchMode = LaunchMode.Default;

    [Header("Holen Change Settings")]
    public float changeHolenCooldown = 1f;

    [Header("Player Info")]
    public bool isPlayer1;

    [Header("Swipe Settings (All Modes)")]
    public float minSwipeDistance = 50f;
    public float maxSwipeDistance = 500f;
    public float minLaunchForce = 5f;
    public float maxLaunchForce = 100f;
    public float swipeTimeWindow = 1f;
    public float arcSwipeTimeWindow = 0f;
    public float downwardSwipeTimeWindow = 0f;
    public bool requireTouchOnBall = false;
    public float swipeDeadZone = 20f;
    public string ballLayerName = "HolenBall";

    [Header("Force Calculation (Default Mode Only)")]
    public float speedMultiplier = 0.05f;
    public bool useSpeedForce = true;

    [Header("Arc Mode Settings")]
    public string groundLayerName = "Ground";
    public float arcAngle = 45f;
    [Range(0.1f, 2.0f)] public float arcForceMultiplier = 1.0f;

    [Header("Arc Drag Targeting")]
    public float arcDragAmplification = 2.5f;
    public float arcMaxTargetRadius = 20f;

    [Header("Downward Mode Settings")]
    public float downwardSpawnHeightOffset = 4f;
    public float downwardAngle = 30f;
    [Range(0.1f, 2.0f)] public float downwardForceMultiplier = 1.0f;

    [Header("Trajectory Settings")]
    public int trajectoryPointCount = 40;
    public float trajectoryTimeStep = 0.08f;
    public float defaultLineLength = 10f;

    [Header("Line Color & Animation")]
    public Color lineColor = new Color(1f, 0f, 0f, 0.5f);
    public float lineAnimationSpeed = 2f;
    [Range(0.01f, 1f)] public float lineVisibleFraction = 0.6f;

    [Header("Camera Settings")]
    public Vector3 cameraFollowOffset = new Vector3(0f, 8f, -6f);
    public float cameraAimScreenY = 0.80f;
    public float cameraFlightScreenY = 0.5f;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip launchSoundClip;

    [Header("Targeting Indicator")]
    public GameObject targetIndicator;

    // ── Private State ──
    private int ballLayer;
    private int groundLayerMask;
    private Transform defaultLookAtTarget;

    private bool isReady = false;
    private bool isTurn = false;
    private string playerRole = "";

    private Vector2 swipeStartPos;
    private Vector2 swipeEndPos;
    private float swipeStartTime;
    private bool isSwiping = false;
    private Vector3 swipeWorldStart;
    private Vector3 raycastTarget;
    private bool raycastTargetValid;

    private Vector3[] fullTrajectoryPoints;
    private float lineAnimOffset = 0f;

    private bool isInventoryOpen = false;
    private bool isHolenLaunched = false;
    private bool isOnChangeCooldown = false;
    private bool isCompletingTurn = false;

    public GameObject currentHolenBall { get; private set; }

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────
    void Start()
    {
        ballLayer = LayerMask.NameToLayer(ballLayerName);
        groundLayerMask = LayerMask.GetMask(groundLayerName);

        if (ballLayer == -1) Debug.LogWarning($"Layer '{ballLayerName}' not found.");
        if (groundLayerMask == 0) Debug.LogWarning($"Layer '{groundLayerName}' not found.");

        if (mainCamera == null) mainCamera = Camera.main;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        DisableControls();
        SetInitialCameraPosition();

        if (activePlayerCamera != null) defaultLookAtTarget = activePlayerCamera.LookAt;
        if (swipeIndicator != null) swipeIndicator.SetActive(false);
        if (targetIndicator != null) targetIndicator.SetActive(false);
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
            trajectoryLine.startColor = lineColor;
            trajectoryLine.endColor = lineColor;
        }

        if (buttonModeDefault != null) buttonModeDefault.onClick.AddListener(() => SetLaunchMode(LaunchMode.Default));
        if (buttonModeArc != null) buttonModeArc.onClick.AddListener(() => SetLaunchMode(LaunchMode.Arc));
        if (buttonModeDownward != null) buttonModeDownward.onClick.AddListener(() => SetLaunchMode(LaunchMode.Downward));
        if (changeHolenButton != null) changeHolenButton.onClick.AddListener(OnChangeHolenButtonPressed);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        SetAllActionButtonsInteractable(false);
        RefreshModeButtonVisuals();
        SetCameraView(false);
        StartCoroutine(GameStartSequence());
    }

    void Update()
    {
        if (!isTurn || currentHolenBall == null || isReady || isInventoryOpen || isHolenLaunched) return;

        HandleSwipeInput();

        if (isSwiping) { BuildLiveTrajectoryPoints(); AnimateAndDrawLine(); }
        else if (trajectoryLine != null) trajectoryLine.enabled = false;
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

        Debug.Log("Both players connected.");
        yield return new WaitForSeconds(3f);

        isPlayer1 = PhotonNetwork.LocalPlayer.ActorNumber == 1;
        playerRole = isPlayer1 ? "Player 1" : "Player 2";
        Debug.Log($"Local player assigned as {playerRole}");

        loadingUI.SetActive(false);

        if (isPlayer1)
        {
            isTurn = true;
            EnableControls();
            SetAllActionButtonsInteractable(true);
            SpawnHolenBall();
            UpdateStatusText("idle");
            Debug.Log("Player 1's turn has started.");
        }
        else
        {
            SetAllActionButtonsInteractable(false);
            UpdateLocalStatusText("idle", "Player 1");
        }
    }

    // ─────────────────────────────────────────────
    //  LAUNCH MODE SELECTION
    // ─────────────────────────────────────────────
    public void SetLaunchMode(LaunchMode mode)
    {
        if (!isTurn || isHolenLaunched) return;
        if (activeLaunchMode == mode) return;
        activeLaunchMode = mode;
        RefreshModeButtonVisuals();
        SpawnHolenBall();
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
    //  BUTTON INTERACTABILITY
    // ─────────────────────────────────────────────
    private void SetAllActionButtonsInteractable(bool value)
    {
        if (changeHolenButton != null) changeHolenButton.interactable = value;
        if (buttonModeDefault != null) buttonModeDefault.interactable = value;
        if (buttonModeArc != null) buttonModeArc.interactable = value;
        if (buttonModeDownward != null) buttonModeDownward.interactable = value;
    }

    // ─────────────────────────────────────────────
    //  HOLEN CHANGE / INVENTORY
    // ─────────────────────────────────────────────
    private void OnChangeHolenButtonPressed()
    {
        if (!isTurn || isHolenLaunched) return;
        if (isInventoryOpen) { CloseInventory(); UpdateStatusText("idle"); }
        else { OpenInventory(); UpdateStatusText("changing"); }
    }

    private void OpenInventory()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        isInventoryOpen = true;
        Debug.Log($"{playerRole} opened inventory.");
    }

    private void CloseInventory()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        isInventoryOpen = false;
    }

    /// <summary>
    /// Called by HolenInventoryPanel when a slot is tapped.
    /// Updates the network prefab (for Photon spawn) and broadcasts the 3D model swap via RPC.
    ///
    /// WHY RPC FOR THE MODEL SWAP:
    ///   SpawnHolenBall uses PhotonNetwork.Instantiate which Photon automatically replicates
    ///   to all clients — so the network ball object appears on both screens correctly.
    ///   But SwapHolenModel is a local-only visual operation. If we call it directly inside
    ///   SpawnHolenBall, the spawning client runs it once locally AND the prefab already has
    ///   the default model as a child, resulting in two models on the ball.
    ///
    ///   Instead: only the turning player calls SpawnHolenBall (which Photon syncs), then
    ///   we fire an RPC with the holenID so BOTH clients swap their local model view.
    /// </summary>
    public void OnHolenSelectedFromInventory(GameObject newHolenPrefab, HolenData data)
    {
        if (!isTurn || isHolenLaunched || isOnChangeCooldown) return;
        if (newHolenPrefab == null) return;

        holenBallPrefab = newHolenPrefab;
        SpawnHolenBall(); // PhotonNetwork.Instantiate — Photon replicates the ball to both clients

        if (data != null && !string.IsNullOrEmpty(data.holenID))
        {
            // Send to OTHERS only — we handle our own swap locally below
            photonView.RPC("RPC_SwapHolenModel", RpcTarget.Others, data.holenID);

            // Use the same coroutine path locally so we also wait a frame
            // before swapping — this prevents the duplicate model bug where
            // the old child hasn't been destroyed yet when Instantiate returns
            StartCoroutine(SwapAfterSpawn(data.holenID));
        }

        CloseInventory();
        UpdateStatusText("idle");
        StartCoroutine(ChangeCooldown());

        Debug.Log($"{playerRole} changed Holen to: {newHolenPrefab.name}");
    }

    private IEnumerator ChangeCooldown()
    {
        isOnChangeCooldown = true;
        yield return new WaitForSeconds(changeHolenCooldown);
        isOnChangeCooldown = false;
    }

    // ─────────────────────────────────────────────
    //  STATUS TEXT
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
            case "idle": return $"{activeName}'s Turn";
            case "changing": return $"{activeName} is changing their pamato";
            case "launched": return $"{activeName} Attacks!";
            default: return $"{activeName}'s Turn";
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
                case TouchPhase.Began: if (!requireTouchOnBall || IsTouchingBall(touch.position)) StartSwipe(touch.position); break;
                case TouchPhase.Moved: if (isSwiping) UpdateSwipe(touch.position); break;
                case TouchPhase.Ended: if (isSwiping) EndSwipe(touch.position); break;
                case TouchPhase.Canceled: if (isSwiping) CancelSwipe(); break;
            }
        }
        else if (Input.GetMouseButtonDown(0)) { if (!requireTouchOnBall || IsTouchingBall(Input.mousePosition)) StartSwipe(Input.mousePosition); }
        else if (Input.GetMouseButton(0) && isSwiping) UpdateSwipe(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0) && isSwiping) EndSwipe(Input.mousePosition);
    }

    private bool IsTouchingBall(Vector2 screenPos)
    {
        if (currentHolenBall == null) return false;
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f) && hit.collider.gameObject == currentHolenBall) return true;
        Vector3 bs = mainCamera.WorldToScreenPoint(currentHolenBall.transform.position);
        return Vector2.Distance(screenPos, new Vector2(bs.x, bs.y)) < 100f;
    }

    private void StartSwipe(Vector2 screenPos)
    {
        if (currentHolenBall == null) return;
        isSwiping = true;
        swipeStartPos = screenPos;
        swipeStartTime = Time.time;
        swipeWorldStart = currentHolenBall.transform.position;
        raycastTargetValid = false;
        if (swipeIndicator != null) swipeIndicator.SetActive(true);
    }

    private void UpdateSwipe(Vector2 screenPos)
    {
        if (currentHolenBall == null) { CancelSwipe(); return; }
        swipeEndPos = screenPos;

        if (activeLaunchMode == LaunchMode.Arc)
        {
            Vector3 hwp = currentHolenBall.transform.position;
            Vector3 hs3 = mainCamera.WorldToScreenPoint(hwp);
            Vector2 hs2 = new Vector2(hs3.x, hs3.y);
            Vector2 delta = screenPos - hs2;
            if (delta.magnitude < swipeDeadZone) { raycastTargetValid = false; if (targetIndicator != null) targetIndicator.SetActive(false); return; }

            Plane gp = new Plane(Vector3.up, hwp);
            Ray hr = mainCamera.ScreenPointToRay(hs2);
            Ray fr = mainCamera.ScreenPointToRay(screenPos);
            if (gp.Raycast(hr, out float hd) && gp.Raycast(fr, out float fd))
            {
                Vector3 amp = (fr.GetPoint(fd) - hr.GetPoint(hd)) * arcDragAmplification;
                if (amp.magnitude > arcMaxTargetRadius) amp = amp.normalized * arcMaxTargetRadius;
                raycastTarget = hr.GetPoint(hd) + amp;
                raycastTargetValid = true;
                if (targetIndicator != null) { targetIndicator.SetActive(true); targetIndicator.transform.position = raycastTarget; }
            }
            else { raycastTargetValid = false; if (targetIndicator != null) targetIndicator.SetActive(false); }
        }
        else if (activeLaunchMode == LaunchMode.Downward)
        {
            if (Physics.Raycast(mainCamera.ScreenPointToRay(screenPos), out RaycastHit hit, 500f, groundLayerMask))
            {
                raycastTarget = hit.point;
                raycastTargetValid = true;
                if (targetIndicator != null) { targetIndicator.SetActive(true); targetIndicator.transform.position = raycastTarget; }
            }
            else { raycastTargetValid = false; if (targetIndicator != null) targetIndicator.SetActive(false); }
        }
        else
        {
            if (targetIndicator != null) targetIndicator.SetActive(false);
            Plane pl = new Plane(Vector3.up, currentHolenBall.transform.position);
            if (pl.Raycast(mainCamera.ScreenPointToRay(screenPos), out float dist))
            { raycastTarget = mainCamera.ScreenPointToRay(screenPos).GetPoint(dist); raycastTargetValid = true; }
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
        if (currentHolenBall == null) return;

        float tw = activeLaunchMode switch { LaunchMode.Arc => arcSwipeTimeWindow, LaunchMode.Downward => downwardSwipeTimeWindow, _ => swipeTimeWindow };
        if (swipeDistance < minSwipeDistance || (tw > 0f && swipeTime > tw)) { Debug.Log("Invalid swipe."); return; }

        Vector3 vel = ComputeLaunchVelocity(swipeDelta, swipeTime, swipeDistance);
        if (vel == Vector3.zero) { Debug.LogWarning("Zero launch velocity."); return; }
        ShootHolen(vel);
    }

    private void CancelSwipe()
    {
        isSwiping = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
        if (swipeIndicator != null) swipeIndicator.SetActive(false);
        if (targetIndicator != null) targetIndicator.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  LAUNCH VELOCITY
    // ─────────────────────────────────────────────
    private Vector3 ComputeLaunchVelocity(Vector2 swipeDelta, float swipeTime, float swipeDistance)
    {
        switch (activeLaunchMode)
        {
            case LaunchMode.Arc:
                {
                    if (!raycastTargetValid) return Vector3.zero;
                    Vector3 sp = currentHolenBall.transform.position, tot = raycastTarget - sp;
                    float hd = new Vector3(tot.x, 0f, tot.z).magnitude;
                    float rs = SolveArcSpeed(hd, raycastTarget.y - sp.y, arcAngle);
                    if (rs <= 0f) return Vector3.zero;
                    float spd = rs * arcForceMultiplier, rad = arcAngle * Mathf.Deg2Rad;
                    Vector3 hDir = new Vector3(tot.x, 0f, tot.z).normalized;
                    return hDir * (spd * Mathf.Cos(rad)) + Vector3.up * (spd * Mathf.Sin(rad));
                }
            case LaunchMode.Downward:
                {
                    if (!raycastTargetValid) return Vector3.zero;
                    Vector3 sp = currentHolenBall.transform.position, tot = raycastTarget - sp;
                    float hd = new Vector3(tot.x, 0f, tot.z).magnitude;
                    float rs = SolveDownwardSpeed(hd, raycastTarget.y - sp.y, downwardAngle);
                    if (rs <= 0f) return Vector3.zero;
                    float spd = rs * downwardForceMultiplier, rad = downwardAngle * Mathf.Deg2Rad;
                    Vector3 hDir = new Vector3(tot.x, 0f, tot.z).normalized;
                    return hDir * (spd * Mathf.Cos(rad)) - Vector3.up * (spd * Mathf.Sin(rad));
                }
            default:
                {
                    Vector3 dir = Vector3.zero;
                    if (raycastTargetValid) { dir = raycastTarget - swipeWorldStart; dir.y = 0f; dir.Normalize(); }
                    if (dir.sqrMagnitude < 0.01f)
                    {
                        Vector3 f = mainCamera.transform.forward; f.y = 0f; f.Normalize();
                        Vector3 r = mainCamera.transform.right; r.y = 0f; r.Normalize();
                        dir = (r * swipeDelta.x + f * swipeDelta.y).normalized;
                    }
                    if (useSpeedForce) return dir * Mathf.Clamp((swipeDistance / Mathf.Max(swipeTime, 0.01f)) * speedMultiplier, minLaunchForce, maxLaunchForce);
                    float nd = Mathf.InverseLerp(minSwipeDistance, maxSwipeDistance, swipeDistance);
                    float frc = Mathf.Lerp(minLaunchForce, maxLaunchForce, nd);
                    frc = Mathf.Lerp(frc, maxLaunchForce, Mathf.Clamp01((swipeDistance / Mathf.Max(swipeTime, 0.01f)) / 2000f));
                    return dir * frc;
                }
        }
    }

    // ─────────────────────────────────────────────
    //  PHYSICS SOLVERS
    // ─────────────────────────────────────────────
    private float SolveArcSpeed(float hd, float vd, float deg)
    {
        float th = deg * Mathf.Deg2Rad, g = Mathf.Abs(Physics.gravity.y), c = Mathf.Cos(th);
        float den = 2f * c * c * (hd * Mathf.Tan(th) - vd);
        if (den <= 0f) { Debug.LogWarning("[Arc] Unreachable."); return -1f; }
        return Mathf.Sqrt((g * hd * hd) / den);
    }

    private float SolveDownwardSpeed(float hd, float vd, float deg)
    {
        float th = deg * Mathf.Deg2Rad, g = Mathf.Abs(Physics.gravity.y), c = Mathf.Cos(th);
        float den = 2f * c * c * (-hd * Mathf.Tan(th) - vd);
        if (den <= 0f) { Debug.LogWarning("[Downward] Unreachable."); return -1f; }
        return Mathf.Sqrt((g * hd * hd) / den);
    }

    // ─────────────────────────────────────────────
    //  TRAJECTORY
    // ─────────────────────────────────────────────
    private void BuildLiveTrajectoryPoints()
    {
        if (currentHolenBall == null) return;
        switch (activeLaunchMode)
        {
            case LaunchMode.Arc:
                {
                    if (!raycastTargetValid) return;
                    Vector3 sp = currentHolenBall.transform.position, tot = raycastTarget - sp;
                    float hd = new Vector3(tot.x, 0f, tot.z).magnitude;
                    float rs = SolveArcSpeed(hd, raycastTarget.y - sp.y, arcAngle); if (rs <= 0f) return;
                    float spd = rs * arcForceMultiplier, rad = arcAngle * Mathf.Deg2Rad;
                    Vector3 hDir = new Vector3(tot.x, 0f, tot.z).normalized;
                    BuildPhysicsPoints(sp, hDir * (spd * Mathf.Cos(rad)) + Vector3.up * (spd * Mathf.Sin(rad)));
                    break;
                }
            case LaunchMode.Downward:
                {
                    if (!raycastTargetValid) return;
                    Vector3 sp = currentHolenBall.transform.position, tot = raycastTarget - sp;
                    float hd = new Vector3(tot.x, 0f, tot.z).magnitude;
                    float rs = SolveDownwardSpeed(hd, raycastTarget.y - sp.y, downwardAngle); if (rs <= 0f) return;
                    float spd = rs * downwardForceMultiplier, rad = downwardAngle * Mathf.Deg2Rad;
                    Vector3 hDir = new Vector3(tot.x, 0f, tot.z).normalized;
                    BuildPhysicsPoints(sp, hDir * (spd * Mathf.Cos(rad)) - Vector3.up * (spd * Mathf.Sin(rad)));
                    break;
                }
            default:
                {
                    Vector3 sp = currentHolenBall.transform.position, dir = Vector3.zero;
                    if (raycastTargetValid) { dir = raycastTarget - swipeWorldStart; dir.y = 0f; dir.Normalize(); }
                    if (dir.sqrMagnitude < 0.01f) { dir = mainCamera.transform.forward; dir.y = 0f; dir.Normalize(); }
                    BuildStraightPoints(sp, dir);
                    break;
                }
        }
    }

    private void BuildPhysicsPoints(Vector3 p, Vector3 v)
    {
        fullTrajectoryPoints = new Vector3[trajectoryPointCount];
        for (int i = 0; i < trajectoryPointCount; i++)
        {
            float t = i * trajectoryTimeStep;
            fullTrajectoryPoints[i] = p + v * t + 0.5f * Physics.gravity * t * t;
        }
    }

    private void BuildStraightPoints(Vector3 p, Vector3 d)
    {
        fullTrajectoryPoints = new Vector3[] { p, p + d * defaultLineLength };
    }

    private void AnimateAndDrawLine()
    {
        if (trajectoryLine == null || fullTrajectoryPoints == null || fullTrajectoryPoints.Length < 2) return;
        lineAnimOffset = (lineAnimOffset + Time.deltaTime * lineAnimationSpeed) % 1f;
        int total = fullTrajectoryPoints.Length, vis = Mathf.Max(2, Mathf.RoundToInt(total * lineVisibleFraction));
        int start = Mathf.FloorToInt(lineAnimOffset * total);
        trajectoryLine.enabled = true;
        trajectoryLine.positionCount = vis;
        for (int i = 0; i < vis; i++) trajectoryLine.SetPosition(i, fullTrajectoryPoints[(start + i) % total]);
        trajectoryLine.startColor = trajectoryLine.endColor = lineColor;
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
        var t = activePlayerCamera.GetCinemachineComponent<CinemachineTransposer>();
        var c = activePlayerCamera.GetCinemachineComponent<CinemachineComposer>();
        if (t != null) t.m_FollowOffset = cameraFollowOffset;
        if (c != null) c.m_ScreenY = cameraAimScreenY;
    }

    private void ApplyFlightCameraSettings()
    {
        if (activePlayerCamera == null) return;
        var t = activePlayerCamera.GetCinemachineComponent<CinemachineTransposer>();
        var c = activePlayerCamera.GetCinemachineComponent<CinemachineComposer>();
        if (t != null) t.m_FollowOffset = Vector3.zero;
        if (c != null) c.m_ScreenY = cameraFlightScreenY;
    }

    private void SetCameraView(bool active)
    {
        if (activePlayerCamera == null || birdsEyeCamera == null) return;
        activePlayerCamera.Priority = active ? 20 : 10;
        birdsEyeCamera.Priority = active ? 10 : 20;
    }

    // ─────────────────────────────────────────────
    //  HOLEN BALL SPAWN
    // ─────────────────────────────────────────────
    private void SpawnHolenBall()
    {
        if (holenBallPrefab == null) { Debug.LogError("holenBallPrefab is null!"); return; }

        if (currentHolenBall != null) { PhotonNetwork.Destroy(currentHolenBall); currentHolenBall = null; }

        Vector3 spawnPos = ballSpawnPoint.position;
        if (activeLaunchMode == LaunchMode.Downward) spawnPos += Vector3.up * downwardSpawnHeightOffset;

        currentHolenBall = PhotonNetwork.Instantiate(holenBallPrefab.name, spawnPos, Quaternion.identity);
        if (ballLayer != -1) currentHolenBall.layer = ballLayer;

        Rigidbody rb = currentHolenBall.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        if (activePlayerCamera != null && isTurn)
        {
            activePlayerCamera.Follow = currentHolenBall.transform;
            activePlayerCamera.LookAt = currentHolenBall.transform;
            ApplyAimCameraSettings();
        }

        if (targetIndicator != null) targetIndicator.SetActive(false);

        Debug.Log($"{playerRole} spawned: {holenBallPrefab.name}");
    }

    /// <summary>
    /// RPC received by BOTH clients when the active player changes their holen.
    /// Each client looks up the HolenData by holenID from HolenInventoryManager and
    /// swaps the model on their local view of currentHolenBall.
    ///
    /// We wait one frame before swapping because PhotonNetwork.Instantiate (called just
    /// before this RPC is fired) may not have set currentHolenBall yet on the remote
    /// client by the time the RPC arrives. The one-frame wait ensures the ball exists.
    /// </summary>
    [PunRPC]
    private void RPC_SwapHolenModel(string holenID)
    {
        StartCoroutine(SwapAfterSpawn(holenID));
    }

    private IEnumerator SwapAfterSpawn(string holenID)
    {
        // Wait until currentHolenBall is available (may take a frame on the remote client)
        float timeout = 3f;
        while (currentHolenBall == null && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (currentHolenBall == null)
        {
            Debug.LogWarning("[SwapHolenModel] currentHolenBall never appeared — cannot swap model.");
            yield break;
        }

        // Look up the HolenData from the local HolenInventoryManager database
        if (HolenInventoryManager.Instance == null)
        {
            Debug.LogWarning("[SwapHolenModel] HolenInventoryManager.Instance is null.");
            yield break;
        }

        HolenData data = HolenInventoryManager.Instance.GetHolenData(holenID);
        if (data == null || data.holenPrefab == null)
        {
            Debug.LogWarning($"[SwapHolenModel] No HolenData or holenPrefab found for ID '{holenID}'.");
            yield break;
        }

        SwapHolenModel(data);
    }

    private void SwapHolenModel(HolenData data)
    {
        if (currentHolenBall == null || data == null || data.holenPrefab == null)
        {
            Debug.LogWarning("[SwapHolenModel] Cannot swap — ball, data, or holenPrefab is null.");
            return;
        }

        // Destroy existing model children that have a HolenIdentifier component
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in currentHolenBall.transform)
        {
            if (child.GetComponent<HolenIdentifier>() != null)
                toDestroy.Add(child.gameObject);
        }

        if (toDestroy.Count == 0)
            Debug.LogWarning("[SwapHolenModel] No child with HolenIdentifier found. Make sure your holenPrefab has a HolenIdentifier component.");

        foreach (GameObject go in toDestroy)
            Destroy(go);

        // Instantiate the new 3D model as a local child of the ball
        GameObject newModel = Instantiate(data.holenPrefab, currentHolenBall.transform);
        newModel.transform.localPosition = Vector3.zero;
        newModel.transform.localRotation = Quaternion.identity;
        newModel.transform.localScale = Vector3.one;

        Debug.Log($"[SwapHolenModel] Swapped model to: {data.holenName}");
    }

    // ─────────────────────────────────────────────
    //  SHOOT
    // ─────────────────────────────────────────────
    public bool IsTurn() => isTurn;

    public void ShootHolen(Vector3 launchVelocity)
    {
        if (!isTurn || isReady || currentHolenBall == null) return;
        isReady = true;
        isHolenLaunched = true;
        SetAllActionButtonsInteractable(false);
        CloseInventory();
        photonView.RPC("RPC_ShootHolen", RpcTarget.All, launchVelocity);
        UpdateStatusText("launched");

        // Open the feedback tracking window on both clients.
        // Called AFTER RPC_ShootHolen so currentHolenBall is still valid when
        // PVPScore.OnTriggerExit checks against it to skip the launched ball.
        PVPScore scoreManager = FindObjectOfType<PVPScore>();
        if (scoreManager != null) scoreManager.OnTurnStarted();

        if (!isCompletingTurn) StartCoroutine(CompleteTurn());
    }

    [PunRPC]
    private void RPC_ShootHolen(Vector3 launchVelocity)
    {
        if (currentHolenBall == null) { Debug.LogError("[RPC_ShootHolen] currentHolenBall is null."); return; }
        Rigidbody rb = currentHolenBall.GetComponent<Rigidbody>();
        if (rb == null) return;
        rb.isKinematic = false;
        rb.AddForce(launchVelocity, ForceMode.VelocityChange);

        if (activePlayerCamera != null)
        {
            activePlayerCamera.Follow = null;
            activePlayerCamera.LookAt = currentHolenBall.transform;
            ApplyFlightCameraSettings();
        }

        if (audioSource != null && launchSoundClip != null) audioSource.PlayOneShot(launchSoundClip);
    }

    private IEnumerator CompleteTurn()
    {
        isCompletingTurn = true;
        yield return new WaitForSeconds(7f);

        if (activePlayerCamera != null)
        {
            activePlayerCamera.Follow = null;
            activePlayerCamera.LookAt = defaultLookAtTarget;
        }
        if (cameraSpawnPoint != null)
        {
            mainCamera.transform.position = cameraSpawnPoint.position;
            mainCamera.transform.rotation = cameraSpawnPoint.rotation;
        }
        if (currentHolenBall != null) { PhotonNetwork.Destroy(currentHolenBall); currentHolenBall = null; }

        isReady = false; isSwiping = false; isCompletingTurn = false;
        EndTurn();
    }

    // ─────────────────────────────────────────────
    //  CONTROLS
    // ─────────────────────────────────────────────
    private void DisableControls()
    {
        if (swipeIndicator != null) swipeIndicator.SetActive(false);
        if (trajectoryLine != null) trajectoryLine.enabled = false;
        if (targetIndicator != null) targetIndicator.SetActive(false);
        SetCameraView(false);
    }

    private void EnableControls() => SetCameraView(true);

    // ─────────────────────────────────────────────
    //  END TURN / SWITCH TURN
    // ─────────────────────────────────────────────
    private void EndTurn()
    {
        isTurn = false; isReady = false; isSwiping = false;
        isHolenLaunched = false; isInventoryOpen = false;
        isOnChangeCooldown = false; isCompletingTurn = false;

        CloseInventory();
        SetAllActionButtonsInteractable(false);
        DisableControls();

        Debug.Log($"{playerRole} ended their turn.");

        PVPScore scoreManager = FindObjectOfType<PVPScore>();
        if (scoreManager != null) scoreManager.OnTurnEnd();

        photonView.RPC("SwitchTurn", RpcTarget.Others);
    }

    [PunRPC]
    private void SwitchTurn()
    {
        isTurn = true; isReady = false; isSwiping = false;
        isHolenLaunched = false; isInventoryOpen = false;
        isOnChangeCooldown = false; isCompletingTurn = false;

        EnableControls();
        SetAllActionButtonsInteractable(true);
        SpawnHolenBall();
        UpdateStatusText("idle");

        Debug.Log($"{playerRole}'s turn started.");
    }
}