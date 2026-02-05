using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class PVPScore : MonoBehaviourPunCallbacks
{
    public static PVPScore Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text turnDisplayText; // Shows whose turn it is

    [Header("Knockout Panels")]
    [Tooltip("Content transform for LOCAL PLAYER'S knockouts (always shows YOUR knockouts)")]
    public Transform player1KnockoutPanel;
    [Tooltip("Content transform for OPPONENT'S knockouts (always shows THEIR knockouts)")]
    public Transform player2KnockoutPanel;
    [Tooltip("HolenSlotUI prefab used to display each knocked-out Holen in the panels")]
    public GameObject holenSlotPrefab;

    [Header("Game Over UI")]
    public GameObject firstUIObject;  // Enable after no holens detected
    public GameObject secondUIObject; // Enable after first UI
    public float firstUIDelay = 3f;   // Time to wait before showing first UI
    public float secondUIDelay = 3f;  // Time to wait between first and second UI
    public float sceneTransitionDelay = 2f; // Time to wait before switching scenes

    [Header("Scene Transition")]
    public string resultSceneName = "PVPResult"; // Scene to load after game over

    [Header("Bounds Detection")]
    [Tooltip("Trigger collider that defines the play area - holens leaving this trigger are knocked out")]
    public Collider playAreaTrigger;

    [Header("Settings")]
    public float noHolensWaitTime = 3f; // Time to wait if no holens remain before game over
    public float checkInterval = 0.5f; // How often to check for out-of-bounds holens (in seconds)

    // Track holens knocked out by each player
    [System.Serializable]
    public class KnockedOutHolen
    {
        public string holenID;
        public string holenName;
        public int playerNumber; // 1 or 2

        public KnockedOutHolen(string id, string name, int player)
        {
            holenID = id;
            holenName = name;
            playerNumber = player;
        }
    }

    private List<KnockedOutHolen> player1KnockedOut = new List<KnockedOutHolen>();
    private List<KnockedOutHolen> player2KnockedOut = new List<KnockedOutHolen>();

    private float noHolensTimer = 0f;
    private bool gameOverTriggered = false;

    private MultiplayerHolenController holenController;

    // Track holens we've already processed to prevent duplicates
    private HashSet<int> processedHolenViewIDs = new HashSet<int>();

    void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[PVPScore] Created and persisting between scenes");
    }

    void Start()
    {
        holenController = FindObjectOfType<MultiplayerHolenController>();

        if (firstUIObject != null)
            firstUIObject.SetActive(false);
        if (secondUIObject != null)
            secondUIObject.SetActive(false);

        // Use InvokeRepeating instead of Update for better performance
        InvokeRepeating(nameof(CheckHolensInBounds), checkInterval, checkInterval);

        UpdateTurnDisplay();
    }

    void Update()
    {
        // Only update turn display in Update - bounds checking is now in InvokeRepeating
        UpdateTurnDisplay();
    }

    /// <summary>
    /// IMPROVED: Only Master Client checks bounds, then broadcasts knockouts via RPC
    /// This ensures perfect synchronization between clients
    /// </summary>
    private void CheckHolensInBounds()
    {
        // Only Master Client performs the check to avoid duplicate detections
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (playAreaTrigger == null)
        {
            // Fallback: use this object's trigger if no specific trigger assigned
            playAreaTrigger = GetComponent<Collider>();
            if (playAreaTrigger == null || !playAreaTrigger.isTrigger)
            {
                Debug.LogError("[PVPScore] No play area trigger assigned or found!");
                return;
            }
        }

        GameObject[] allHolens = GameObject.FindGameObjectsWithTag("Objective");
        int holensInside = 0;

        foreach (GameObject holen in allHolens)
        {
            if (holen == null)
                continue;

            // Skip the current player's thrown ball
            if (holenController != null && holenController.currentHolenBall != null)
            {
                if (holen == holenController.currentHolenBall)
                    continue;
            }

            // Check if holen has a PhotonView
            PhotonView pv = holen.GetComponent<PhotonView>();
            if (pv == null)
                continue;

            // Skip if we've already processed this holen
            if (processedHolenViewIDs.Contains(pv.ViewID))
                continue;

            Collider holenCollider = holen.GetComponent<Collider>();
            if (holenCollider != null)
            {
                // Check if holen is inside bounds
                bool isInside = playAreaTrigger.bounds.Intersects(holenCollider.bounds);

                if (isInside)
                {
                    holensInside++;
                }
                else
                {
                    // Holen is out of bounds - knock it out!
                    HandleHolenKnockedOut(holen, pv);
                }
            }
        }

        // Check for game over condition
        if (holensInside == 0)
        {
            noHolensTimer += checkInterval;

            if (noHolensTimer >= noHolensWaitTime && !gameOverTriggered)
            {
                TriggerGameOver();
            }
        }
        else
        {
            noHolensTimer = 0f;
        }
    }

    /// <summary>
    /// IMPROVED: Handles a holen being knocked out
    /// Attributes the knockout to the PhotonView owner (last player to hit it)
    /// </summary>
    private void HandleHolenKnockedOut(GameObject holen, PhotonView holenPV)
    {
        // Mark as processed to prevent duplicate knockouts
        processedHolenViewIDs.Add(holenPV.ViewID);

        // Get HolenData
        HolenData holenData = GetHolenDataFromGameObject(holen);
        if (holenData == null)
        {
            Debug.LogWarning($"[PVPScore] Could not find HolenData for {holen.name}");
            return;
        }

        // CRITICAL: Determine which player knocked it out based on PhotonView ownership
        // If the holen is owned by a player, they're the one who hit it last
        int knockoutPlayer = 0;

        if (holenPV.Owner != null)
        {
            // The owner is the player who hit it
            knockoutPlayer = holenPV.Owner.ActorNumber;
            Debug.Log($"[PVPScore] Holen {holenData.holenName} is owned by Actor {knockoutPlayer}");
        }
        else
        {
            // No owner (Master Client owns it) - attribute to current turn player
            if (holenController != null)
            {
                knockoutPlayer = holenController.isPlayer1 ? 1 : 2;
                Debug.Log($"[PVPScore] Holen {holenData.holenName} has no owner, attributing to current turn player: {knockoutPlayer}");
            }
        }

        if (knockoutPlayer == 0)
        {
            Debug.LogWarning($"[PVPScore] Could not determine knockout player for {holenData.holenName}");
            return;
        }

        // Broadcast the knockout to all clients
        photonView.RPC("RPC_RecordKnockout", RpcTarget.All, holenData.holenID, holenData.holenName, knockoutPlayer, holenPV.ViewID);

        Debug.Log($"[PVPScore] Player {knockoutPlayer} knocked out {holenData.holenName}");
    }

    /// <summary>
    /// IMPROVED: Now includes ViewID for synchronized destruction
    /// </summary>
    [PunRPC]
    private void RPC_RecordKnockout(string holenID, string holenName, int playerNumber, int holenViewID)
    {
        KnockedOutHolen knockedOut = new KnockedOutHolen(holenID, holenName, playerNumber);

        if (playerNumber == 1)
        {
            player1KnockedOut.Add(knockedOut);
            Debug.Log($"[PVPScore] Player 1 knocked out {holenName} - Total: {player1KnockedOut.Count}");
        }
        else if (playerNumber == 2)
        {
            player2KnockedOut.Add(knockedOut);
            Debug.Log($"[PVPScore] Player 2 knocked out {holenName} - Total: {player2KnockedOut.Count}");
        }

        RefreshKnockoutPanel(playerNumber);

        // Queue the holen for destruction
        StartCoroutine(DestroyHolenAfterDelay(holenViewID, 0.5f));
    }

    /// <summary>
    /// NEW: Safely destroys a holen by ViewID with a delay
    /// </summary>
    private IEnumerator DestroyHolenAfterDelay(int viewID, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Only Master Client actually destroys
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonView pv = PhotonView.Find(viewID);
            if (pv != null && pv.gameObject != null)
            {
                Debug.Log($"[PVPScore] Destroying holen with ViewID {viewID}");
                PhotonNetwork.Destroy(pv.gameObject);
            }
        }
    }

    private void TriggerGameOver()
    {
        gameOverTriggered = true;

        // Only master client triggers the sequence to ensure sync
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_TriggerGameOver", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_TriggerGameOver()
    {
        StartCoroutine(ShowGameOverSequence());
    }

    private IEnumerator ShowGameOverSequence()
    {
        Debug.Log("Game Over! No holens remaining in play field.");

        // Wait for first UI delay
        yield return new WaitForSeconds(firstUIDelay);

        // Show first UI object
        if (firstUIObject != null)
        {
            firstUIObject.SetActive(true);
            Debug.Log("First UI object displayed");
        }

        // Wait for second UI delay
        yield return new WaitForSeconds(secondUIDelay);

        // Show second UI object
        if (secondUIObject != null)
        {
            secondUIObject.SetActive(true);
            Debug.Log("Second UI object displayed");
        }

        // Log final results
        LogFinalResults();

        // Wait before scene transition
        yield return new WaitForSeconds(sceneTransitionDelay);

        // Load result scene
        LoadResultScene();
    }

    private void LoadResultScene()
    {
        Debug.Log($"Loading result scene: {resultSceneName}");

        // Store match data in static holder before scene transition
        int localPlayer = GetLocalPlayerNumber();

        // Convert to tuples to avoid type dependency
        var p1Data = new List<(string, string, int)>();
        foreach (var holen in player1KnockedOut)
        {
            p1Data.Add((holen.holenID, holen.holenName, holen.playerNumber));
        }

        var p2Data = new List<(string, string, int)>();
        foreach (var holen in player2KnockedOut)
        {
            p2Data.Add((holen.holenID, holen.holenName, holen.playerNumber));
        }

        PVPDataHolder.StoreMatchResults(p1Data, p2Data, localPlayer);

        if (PhotonNetwork.IsMasterClient)
        {
            // Master client loads the scene for all players
            PhotonNetwork.LoadLevel(resultSceneName);
        }
    }

    private void LogFinalResults()
    {
        Debug.Log($"=== GAME RESULTS ===");
        Debug.Log($"Player 1 knocked out {player1KnockedOut.Count} holens:");
        foreach (var holen in player1KnockedOut)
        {
            Debug.Log($"  - {holen.holenName} (ID: {holen.holenID})");
        }

        Debug.Log($"Player 2 knocked out {player2KnockedOut.Count} holens:");
        foreach (var holen in player2KnockedOut)
        {
            Debug.Log($"  - {holen.holenName} (ID: {holen.holenID})");
        }
    }

    private HolenData GetHolenDataFromGameObject(GameObject holenObject)
    {
        if (holenObject == null)
            return null;

        Debug.Log($"[PVPScore] GetHolenDataFromGameObject called for: {holenObject.name}");

        // Method 1: Check if there's a HolenIdentifier component (recommended approach)
        HolenIdentifier identifier = holenObject.GetComponent<HolenIdentifier>();
        if (identifier != null)
        {
            Debug.Log($"[PVPScore] Found HolenIdentifier component on {holenObject.name}");
            if (identifier.holenData != null)
            {
                Debug.Log($"[PVPScore] HolenIdentifier has holenData: {identifier.holenData.holenName} (ID: {identifier.holenData.holenID})");
                return identifier.holenData;
            }
            else
            {
                Debug.LogWarning($"[PVPScore] HolenIdentifier found but holenData is null on {holenObject.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[PVPScore] No HolenIdentifier component found on {holenObject.name}");
        }

        // Method 2: Try to find matching HolenData from WagerDataManager by name
        Debug.Log($"[PVPScore] Attempting fallback method - searching WagerDataManager");
        if (WagerDataManager.Instance != null)
        {
            var allWagers = WagerDataManager.Instance.GetAllWageredHolensIndividual();
            Debug.Log($"[PVPScore] Found {allWagers.Count} wagered holens in WagerDataManager");

            // Try to match by prefab name (assumes GameObject name matches HolenData prefab name)
            string objectName = holenObject.name.Replace("(Clone)", "").Trim();
            Debug.Log($"[PVPScore] Cleaned object name: {objectName}");

            foreach (var wager in allWagers)
            {
                HolenData data = LoadHolenDataByID(wager.holenID);
                if (data != null && data.holenPrefab != null)
                {
                    string prefabName = data.holenPrefab.name;
                    Debug.Log($"[PVPScore] Comparing '{objectName}' with '{prefabName}'");
                    if (prefabName == objectName)
                    {
                        Debug.Log($"[PVPScore] Match found! Using HolenData: {data.holenName}");
                        return data;
                    }
                }
            }
            Debug.LogWarning($"[PVPScore] No match found in WagerDataManager for {objectName}");
        }
        else
        {
            Debug.LogWarning($"[PVPScore] WagerDataManager.Instance is null!");
        }

        return null;
    }

    private HolenData LoadHolenDataByID(string holenID)
    {
        // Load all HolenData from Resources folder
        HolenData[] allHolenData = Resources.LoadAll<HolenData>("HolenData");

        foreach (var data in allHolenData)
        {
            if (data.holenID == holenID)
            {
                return data;
            }
        }

        return null;
    }

    /// <summary>
    /// Called by MultiplayerHolenController when a turn ends
    /// No longer needed for destruction since we handle it immediately
    /// </summary>
    public void OnTurnEnd()
    {
        Debug.Log("[PVPScore] Turn ended");
    }

    /// <summary>
    /// Clears and re-populates the specified player's knockout panel
    /// with a HolenSlotUI for every Holen they have knocked out so far.
    /// Uses local player mapping: the local device always sees their own knockouts
    /// in player1KnockoutPanel and opponent knockouts in player2KnockoutPanel.
    /// </summary>
    private void RefreshKnockoutPanel(int absolutePlayerNumber)
    {
        Debug.Log($"[PVPScore] RefreshKnockoutPanel called for absolute player {absolutePlayerNumber}");

        // Map absolute player number to local panel
        // If this device IS absolutePlayerNumber, show in "my" panel (player1KnockoutPanel)
        // Otherwise show in opponent panel (player2KnockoutPanel)
        int localPlayerNumber = GetLocalPlayerNumber();
        bool isLocalPlayer = (absolutePlayerNumber == localPlayerNumber);

        Debug.Log($"[PVPScore] Local player number: {localPlayerNumber}, Is local player: {isLocalPlayer}");

        Transform panel = isLocalPlayer ? player1KnockoutPanel : player2KnockoutPanel;
        List<KnockedOutHolen> knockouts = (absolutePlayerNumber == 1) ? player1KnockedOut : player2KnockedOut;

        Debug.Log($"[PVPScore] Using panel: {(panel != null ? panel.name : "NULL")}, Knockouts count: {knockouts.Count}");

        if (panel == null)
        {
            Debug.LogError($"[PVPScore] Knockout panel for {(isLocalPlayer ? "local player" : "opponent")} is not assigned!");
            return;
        }

        if (holenSlotPrefab == null)
        {
            Debug.LogError("[PVPScore] holenSlotPrefab is not assigned!");
            return;
        }

        // Clear existing slots
        int childCount = panel.childCount;
        Debug.Log($"[PVPScore] Clearing {childCount} existing children from panel");
        foreach (Transform child in panel)
        {
            Destroy(child.gameObject);
        }

        // Instantiate a slot for each knocked-out Holen
        Debug.Log($"[PVPScore] Creating {knockouts.Count} slot(s)");
        int slotsCreated = 0;
        foreach (var knockout in knockouts)
        {
            Debug.Log($"[PVPScore] Loading HolenData for knockout: {knockout.holenName} (ID: {knockout.holenID})");
            HolenData data = LoadHolenDataByID(knockout.holenID);
            if (data == null)
            {
                Debug.LogWarning($"[PVPScore] Could not load HolenData for ID: {knockout.holenID}");
                continue;
            }

            Debug.Log($"[PVPScore] Instantiating slot for: {data.holenName}");
            GameObject slotObj = Instantiate(holenSlotPrefab, panel);
            HolenSlotUI slotUI = slotObj.GetComponent<HolenSlotUI>();

            if (slotUI != null)
            {
                Debug.Log($"[PVPScore] Setting slot data for: {data.holenName}");
                slotUI.SetSlot(data, 1);
                slotsCreated++;
            }
            else
            {
                Debug.LogError($"[PVPScore] HolenSlotUI component not found on instantiated prefab!");
            }
        }

        Debug.Log($"[PVPScore] {(isLocalPlayer ? "Local player" : "Opponent")} knockout panel refreshed – {slotsCreated}/{knockouts.Count} slot(s) successfully created.");
    }

    private void UpdateTurnDisplay()
    {
        if (turnDisplayText != null && holenController != null)
        {
            if (holenController.IsTurn())
            {
                turnDisplayText.text = "Your Turn";
            }
            else
            {
                turnDisplayText.text = "Opponent's Turn";
            }
        }
    }

    /// <summary>
    /// Returns the local player's number (1 or 2).
    /// Returns 0 if unable to determine.
    /// </summary>
    public int GetLocalPlayerNumber()
    {
        if (holenController == null)
        {
            holenController = FindObjectOfType<MultiplayerHolenController>();
        }

        if (holenController != null)
        {
            return holenController.isPlayer1 ? 1 : 2;
        }

        return 0;
    }

    public List<KnockedOutHolen> GetPlayerKnockedOutHolens(int playerNumber)
    {
        if (playerNumber == 1)
            return new List<KnockedOutHolen>(player1KnockedOut);
        else if (playerNumber == 2)
            return new List<KnockedOutHolen>(player2KnockedOut);

        return new List<KnockedOutHolen>();
    }

    public List<KnockedOutHolen> GetAllKnockedOutHolens()
    {
        List<KnockedOutHolen> allKnockedOut = new List<KnockedOutHolen>();
        allKnockedOut.AddRange(player1KnockedOut);
        allKnockedOut.AddRange(player2KnockedOut);
        return allKnockedOut;
    }

    public void ClearData()
    {
        player1KnockedOut.Clear();
        player2KnockedOut.Clear();
        processedHolenViewIDs.Clear();
        gameOverTriggered = false;
        noHolensTimer = 0f;
        Debug.Log("[PVPScore] Data cleared");
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        // Clean up InvokeRepeating
        CancelInvoke();
    }
}