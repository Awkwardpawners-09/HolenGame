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
    public GameObject swipeIndicator;
    public LineRenderer trajectoryLine;

    [Header("Holen Change UI")]
    [Tooltip("The button that toggles the inventory panel open/closed")]
    public Button changeHolenButton;
    [Tooltip("The GameObject containing HolenChangeInventory's panel — toggled by the button")]
    public GameObject inventoryPanel;
    [Tooltip("Text visible to BOTH players showing whose turn it is and what they are doing")]
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

    [Header("Physics Authority")]
    [Tooltip("If true, only Master Client simulates physics. Other clients just display results.")]
    public bool useMasterClientAuthority = true;

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

    // --- Holen Change state ---
    private bool isInventoryOpen = false;
    private bool isHolenLaunched = false;
    private bool isOnChangeCooldown = false;

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

        // --- Holen Change init ---
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (changeHolenButton != null)
            changeHolenButton.onClick.AddListener(OnChangeHolenButtonPressed);

        SetChangeHolenButtonInteractable(false);
        // ---

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

        if (isPlayer1)
        {
            isTurn = true;
            EnableControls();
            SetChangeHolenButtonInteractable(true);       // can change holen before launch
            SpawnHolenBall();
            UpdateStatusText("idle");                     // synced to both clients
            Debug.Log("Player 1's turn has started.");
        }
        else
        {
            // Player 2 waits — status text will be set when Player 1's RPC arrives
            UpdateLocalStatusText("idle", "Player 1");
        }
    }

    // ─────────────────────────────────────────────
    // HOLEN CHANGE LOGIC
    // ─────────────────────────────────────────────

    /// <summary>
    /// Toggles the inventory panel open or closed.
    /// Only works before the holen is launched.
    /// </summary>
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
        Debug.Log($"{playerRole} opened the Holen inventory.");
    }

    private void CloseInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        isInventoryOpen = false;
        Debug.Log($"{playerRole} closed the Holen inventory.");
    }

    /// <summary>
    /// Call this from HolenSlotUI (or wherever a slot tap is handled)
    /// when the player taps a Holen in the inventory panel.
    /// It swaps holenBallPrefab and respawns the ball with a cooldown guard.
    /// </summary>
    public void OnHolenSelectedFromInventory(GameObject newHolenPrefab)
    {
        if (isHolenLaunched || !isTurn || isOnChangeCooldown) return;
        if (newHolenPrefab == null) return;

        // Start cooldown — blocks further taps for 1 second
        StartCoroutine(ChangeCooldown());

        // Destroy the current ball (network-safe: only the owner destroys)
        if (currentHolenBall != null)
        {
            PhotonNetwork.Destroy(currentHolenBall);
            currentHolenBall = null;
        }

        // Swap the prefab and respawn
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

    /// <summary>
    /// Enable or disable the change-holen button's interactability.
    /// </summary>
    private void SetChangeHolenButtonInteractable(bool value)
    {
        if (changeHolenButton != null)
            changeHolenButton.interactable = value;
    }

    // ─────────────────────────────────────────────
    // STATUS TEXT (visible to both players via RPC)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Sends the current status to ALL clients so both players see it.
    /// <param name="state">One of: "idle", "changing", "launched"</param>
    /// </summary>
    private void UpdateStatusText(string state)
    {
        // Determine which player name to show (the one whose turn it is)
        string activeName = isTurn ? playerRole : GetOpponentName();
        photonView.RPC("RPC_UpdateStatusText", RpcTarget.All, state, activeName);
    }

    /// <summary>
    /// Updates the local status text without an RPC (used during init before RPC is reliable).
    /// </summary>
    private void UpdateLocalStatusText(string state, string activeName)
    {
        if (statusText == null) return;
        statusText.text = BuildStatusString(state, activeName);
    }

    [PunRPC]
    private void RPC_UpdateStatusText(string state, string activeName)
    {
        if (statusText == null) return;
        statusText.text = BuildStatusString(state, activeName);
    }

    private string BuildStatusString(string state, string activeName)
    {
        switch (state)
        {
            case "idle":
                return $"{activeName}: Choosing...";
            case "changing":
                return $"{activeName}: Changing holen...";
            case "launched":
                return $"{activeName}: Attacks!";
            default:
                return $"{activeName}: ...";
        }
    }

    private string GetOpponentName()
    {
        return isPlayer1 ? "Player 2" : "Player 1";
    }

    // ─────────────────────────────────────────────
    // INPUT & SWIPE
    // ─────────────────────────────────────────────

    void Update()
    {
        if (!isTurn || isReady || currentHolenBall == null)
            return;

        HandleSwipeInput();
    }

    private void HandleSwipeInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 touchPos = Input.mousePosition;

            if (requireTouchOnBall && !IsTouchOnBall(touchPos))
            {
                Debug.Log("Touch not on ball, ignoring");
                return;
            }

            swipeStartPos = touchPos;
            swipeStartTime = Time.time;
            isSwiping = true;

            Ray ray = mainCamera.ScreenPointToRay(touchPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                swipeWorldStart = hit.point;
            }
            else
            {
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                float enter;
                if (groundPlane.Raycast(ray, out enter))
                {
                    swipeWorldStart = ray.GetPoint(enter);
                }
            }

            if (swipeIndicator != null)
                swipeIndicator.SetActive(true);

            Debug.Log($"Swipe started at: {swipeStartPos}");
        }

        if (isSwiping && Input.GetMouseButton(0))
        {
            swipeEndPos = Input.mousePosition;

            Ray ray = mainCamera.ScreenPointToRay(swipeEndPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                swipeWorldEnd = hit.point;
            }
            else
            {
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                float enter;
                if (groundPlane.Raycast(ray, out enter))
                {
                    swipeWorldEnd = ray.GetPoint(enter);
                }
            }

            UpdateTrajectoryLine();
        }

        if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            swipeEndPos = Input.mousePosition;
            float swipeTime = Time.time - swipeStartTime;

            if (swipeTime > swipeTimeWindow)
            {
                CancelSwipe();
                return;
            }

            ProcessSwipe(swipeTime);
            isSwiping = false;

            if (swipeIndicator != null)
                swipeIndicator.SetActive(false);

            if (trajectoryLine != null)
                trajectoryLine.enabled = false;
        }
    }

    private bool IsTouchOnBall(Vector2 touchPos)
    {
        if (currentHolenBall == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(touchPos);
        RaycastHit hit;
        int layerMask = 1 << ballLayer;

        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            if (hit.collider.gameObject == currentHolenBall)
            {
                Debug.Log("Touch detected on ball");
                return true;
            }
        }

        return false;
    }

    private void UpdateTrajectoryLine()
    {
        if (trajectoryLine == null || currentHolenBall == null)
            return;

        Vector3 swipeWorldDir = (swipeWorldEnd - swipeWorldStart).normalized;
        float swipeDistance = Vector2.Distance(swipeStartPos, swipeEndPos);

        float force;
        if (useSpeedForce)
        {
            float swipeTime = Time.time - swipeStartTime;
            if (swipeTime <= 0) swipeTime = 0.01f;
            float swipeSpeed = swipeDistance / swipeTime;
            force = swipeSpeed * speedMultiplier;
            force = Mathf.Clamp(force, minLaunchForce, maxLaunchForce);
        }
        else
        {
            force = CalculateLaunchForce(swipeDistance);
        }

        trajectoryLine.enabled = true;
        trajectoryLine.positionCount = 20;

        Vector3 velocity = swipeWorldDir * force;
        Vector3 position = currentHolenBall.transform.position;

        for (int i = 0; i < 20; i++)
        {
            float t = i * 0.1f;
            Vector3 point = position + velocity * t + 0.5f * Physics.gravity * t * t;
            trajectoryLine.SetPosition(i, point);
        }
    }

    private void ProcessSwipe(float swipeTime)
    {
        float swipeDistance = Vector2.Distance(swipeStartPos, swipeEndPos);

        if (swipeDistance < swipeDeadZone)
        {
            Debug.Log($"Swipe too small: {swipeDistance} < {swipeDeadZone}");
            return;
        }

        if (swipeDistance >= minSwipeDistance)
        {
            Vector3 swipeDirection = (swipeWorldEnd - swipeWorldStart).normalized;
            swipeDirection.y = 0;

            if (swipeDirection.magnitude < 0.1f)
            {
                Debug.Log("Invalid swipe direction (too vertical or zero)");
                return;
            }

            swipeDirection = swipeDirection.normalized;

            float force;

            if (useSpeedForce)
            {
                float swipeSpeed = swipeDistance / swipeTime;
                force = swipeSpeed * speedMultiplier;
                force = Mathf.Clamp(force, minLaunchForce, maxLaunchForce);

                Debug.Log($"SHOOTING! Speed={swipeSpeed:F2} px/s, Force={force:F2}, Direction={swipeDirection}");
            }
            else
            {
                force = CalculateLaunchForce(swipeDistance);

                float speed = swipeDistance / swipeTime;
                float speedBonus = Mathf.Clamp01(speed / 2000f);
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
        float normalizedDistance = Mathf.InverseLerp(minSwipeDistance, maxSwipeDistance, swipeDistance);
        return Mathf.Lerp(minLaunchForce, maxLaunchForce, normalizedDistance);
    }

    // ─────────────────────────────────────────────
    // CAMERA
    // ─────────────────────────────────────────────

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

    // ─────────────────────────────────────────────
    // HOLEN BALL SPAWN / SHOOT / TURN
    // ─────────────────────────────────────────────

    private void SpawnHolenBall()
    {
        currentHolenBall = PhotonNetwork.Instantiate(holenBallPrefab.name, ballSpawnPoint.position, Quaternion.identity);

        if (ballLayer != -1)
        {
            currentHolenBall.layer = ballLayer;
        }

        // CRITICAL FIX: Request ownership of the ball we just spawned
        PhotonView pv = currentHolenBall.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine)
        {
            pv.RequestOwnership();
            Debug.Log($"{playerRole} requested ownership of shooting ball: {holenBallPrefab.name}");
        }

        Rigidbody rb = currentHolenBall.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            Debug.Log($"{playerRole} spawned ball as kinematic: {holenBallPrefab.name}");
        }

        // NOTE: Player's shooting ball does NOT use Master Client authority
        // It needs normal physics on both clients since it's owned by the player
        // Only objective holens (spawned by WagerSpawnPlacer) use Master Client authority

        if (activePlayerCamera != null && currentHolenBall != null && isTurn)
        {
            activePlayerCamera.Follow = currentHolenBall.transform;
            activePlayerCamera.LookAt = currentHolenBall.transform;
            AdjustCameraPosition();
        }

        Debug.Log($"{playerRole} spawned Holen Ball: {holenBallPrefab.name}");
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
            isHolenLaunched = true;

            // Lock out the change-holen button the moment we launch
            SetChangeHolenButtonInteractable(false);
            CloseInventory();

            // CRITICAL FIX: Ensure we have ownership before shooting
            PhotonView pv = currentHolenBall.GetComponent<PhotonView>();
            if (pv != null && !pv.IsMine)
            {
                pv.RequestOwnership();
                Debug.Log($"{playerRole} ensuring ownership before launch");
            }

            // Player's ball uses standard RPC - both clients apply physics
            // (Master Client authority only applies to objective holens on the field)
            photonView.RPC("RPC_ShootHolen", RpcTarget.All, direction, force);

            // Broadcast "Attacks!" status to both players
            UpdateStatusText("launched");

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

            if (rb != null)
            {
                // Player's shooting ball uses normal physics on all clients
                // (Master Client authority only applies to objective holens on the field)
                rb.isKinematic = false;
                rb.AddForce(direction * force, ForceMode.Impulse);

                Debug.Log($"[{playerRole}] Applied force to ball: {force}, isKinematic: {rb.isKinematic}");
            }

            if (activePlayerCamera != null && isTurn)
            {
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

    private void EndTurn()
    {
        isTurn = false;
        isReady = false;
        isSwiping = false;
        isHolenLaunched = false;
        isInventoryOpen = false;
        isOnChangeCooldown = false;

        // Close & disable inventory for this client
        CloseInventory();
        SetChangeHolenButtonInteractable(false);

        DisableControls();

        Debug.Log($"{playerRole} ended their turn. Switching to other player.");

        PVPScore scoreManager = FindObjectOfType<PVPScore>();
        if (scoreManager != null)
        {
            scoreManager.OnTurnEnd();
        }

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
        SetChangeHolenButtonInteractable(true);         // new turn — can change holen again
        SpawnHolenBall();
        UpdateStatusText("idle");                       // synced to both clients

        Debug.Log($"{playerRole}'s turn started");
    }

    // ─────────────────────────────────────────────
    // PHYSICS AUTHORITY (for spawned holens)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Call this when a new holen is spawned to set up physics authority
    /// </summary>
    public void RegisterNewHolen(GameObject holen)
    {
        if (!useMasterClientAuthority) return;

        // CRITICAL: Only apply physics authority in multiplayer mode
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[MultiplayerHolenController] Not connected to Photon - skipping physics authority for new holen (single-player mode)");
            return;
        }

        Rigidbody rb = holen.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Note: For the player's ball, it starts kinematic and becomes non-kinematic when launched
            // Physics authority is applied at launch time in RPC_ShootHolen
            Debug.Log($"[MultiplayerHolenController] Registered new holen {holen.name}");
        }
    }
}