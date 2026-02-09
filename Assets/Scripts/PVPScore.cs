using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

/// <summary>
/// FIXED: Manages scoring, knockout tracking, and game over logic in PVP mode.
/// - Only MasterClient destroys GameObjects
/// - All clients update UI via RPC
/// </summary>
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
    public GameObject firstUIObject;
    public GameObject secondUIObject;
    public float firstUIDelay = 3f;
    public float secondUIDelay = 3f;
    public float sceneTransitionDelay = 2f;

    [Header("Scene Transition")]
    public string resultSceneName = "PVPResult";

    [Header("Bounds Detection")]
    [Tooltip("Trigger collider that defines the play area - holens leaving this trigger are knocked out")]
    public Collider playAreaTrigger;

    [Header("Settings")]
    public float noHolensWaitTime = 3f;
    public float checkInterval = 0.5f;

    [Header("Debug")]
    public bool showDebugInfo = true;

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
    private TurnManager turnManager;

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

        if (showDebugInfo)
            Debug.Log("[PVPScore] Created and persisting between scenes");
    }

    void Start()
    {
        holenController = FindObjectOfType<MultiplayerHolenController>();
        turnManager = FindObjectOfType<TurnManager>();

        if (firstUIObject != null)
            firstUIObject.SetActive(false);
        if (secondUIObject != null)
            secondUIObject.SetActive(false);

        // Start checking for out-of-bounds holens
        InvokeRepeating(nameof(CheckHolensInBounds), checkInterval, checkInterval);

        UpdateTurnDisplay();
    }

    void Update()
    {
        UpdateTurnDisplay();
    }

    /// <summary>
    /// Check for holens that have left the play area (Master Client only)
    /// </summary>
    private void CheckHolensInBounds()
    {
        // Only Master Client performs the check to avoid duplicate detections
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (playAreaTrigger == null)
        {
            playAreaTrigger = GetComponent<Collider>();
            if (playAreaTrigger == null || !playAreaTrigger.isTrigger)
            {
                Debug.LogError("[PVPScore] No play area trigger assigned or found!");
                return;
            }
        }

        // Find all holens with "Objective" tag
        GameObject[] allHolens = GameObject.FindGameObjectsWithTag("Objective");
        int holensInside = 0;

        foreach (GameObject holen in allHolens)
        {
            if (holen == null)
                continue;

            // Skip the current player's thrown ball (being actively controlled)
            if (holenController != null && holenController.currentHolenBall != null)
            {
                if (holen == holenController.currentHolenBall)
                    continue;
            }

            // Check if holen has a PhotonView
            PhotonView pv = holen.GetComponent<PhotonView>();
            if (pv == null)
                continue;

            // Skip if already processed
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
    /// Handle a holen being knocked out (MasterClient only)
    /// Determines which player gets credit based on ownership
    /// </summary>
    private void HandleHolenKnockedOut(GameObject holen, PhotonView holenPV)
    {
        // CRITICAL: Only MasterClient should call this
        if (!PhotonNetwork.IsMasterClient)
            return;

        // Mark as processed
        processedHolenViewIDs.Add(holenPV.ViewID);

        // Get HolenData
        HolenData holenData = GetHolenDataFromGameObject(holen);
        if (holenData == null)
        {
            Debug.LogWarning($"[PVPScore] Could not find HolenData for {holen.name}");
            // Still destroy the object even if we can't find data
            PhotonNetwork.Destroy(holen);
            return;
        }

        // Determine which player knocked it out based on ownership
        int knockoutPlayer = DetermineKnockoutPlayer(holenPV);

        if (knockoutPlayer == 0)
        {
            Debug.LogWarning($"[PVPScore] Could not determine knockout player for {holenData.holenName}");
            // Default to current turn player
            if (turnManager != null)
            {
                var currentPlayer = turnManager.GetCurrentPlayer();
                if (currentPlayer != null)
                {
                    knockoutPlayer = currentPlayer.ActorNumber;
                }
            }

            if (knockoutPlayer == 0)
                knockoutPlayer = 1; // Fallback to player 1
        }

        if (showDebugInfo)
            Debug.Log($"[PVPScore] {holenData.holenName} knocked out by Player {knockoutPlayer}");

        // Broadcast knockout to all clients BEFORE destroying
        photonView.RPC("RPC_HolenKnockedOut", RpcTarget.All, holenData.holenID, holenData.holenName, knockoutPlayer, holenPV.ViewID);

        // CRITICAL: Only MasterClient destroys the object
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(holen);
        }
    }

    /// <summary>
    /// Determine which player should get credit for the knockout
    /// </summary>
    private int DetermineKnockoutPlayer(PhotonView holenPV)
    {
        // Method 1: Check PhotonView ownership
        if (holenPV.Owner != null)
        {
            int actorNumber = holenPV.Owner.ActorNumber;

            if (showDebugInfo)
                Debug.Log($"[PVPScore] Holen owned by Actor {actorNumber}");

            // Actor 1 = Player 1, Actor 2 = Player 2
            return actorNumber <= 2 ? actorNumber : 0;
        }

        // Method 2: Use turn manager's current turn
        if (turnManager != null)
        {
            var currentPlayer = turnManager.GetCurrentPlayer();
            if (currentPlayer != null)
            {
                int actorNumber = currentPlayer.ActorNumber;

                if (showDebugInfo)
                    Debug.Log($"[PVPScore] Using current turn player: Actor {actorNumber}");

                return actorNumber <= 2 ? actorNumber : 0;
            }
        }

        // Method 3: Use holen controller's player number
        if (holenController != null)
        {
            int playerNum = holenController.isPlayer1 ? 1 : 2;

            if (showDebugInfo)
                Debug.Log($"[PVPScore] Using controller's player number: {playerNum}");

            return playerNum;
        }

        return 0;
    }

    /// <summary>
    /// RPC to notify all clients that a holen was knocked out
    /// </summary>
    [PunRPC]
    private void RPC_HolenKnockedOut(string holenID, string holenName, int knockoutPlayer, int viewID)
    {
        if (showDebugInfo)
            Debug.Log($"[PVPScore] RPC received - {holenName} knocked out by Player {knockoutPlayer}");

        // Add to appropriate list
        KnockedOutHolen knockout = new KnockedOutHolen(holenID, holenName, knockoutPlayer);

        if (knockoutPlayer == 1)
        {
            player1KnockedOut.Add(knockout);
            RefreshKnockoutPanel(1);
        }
        else if (knockoutPlayer == 2)
        {
            player2KnockedOut.Add(knockout);
            RefreshKnockoutPanel(2);
        }

        // Mark as processed locally too
        processedHolenViewIDs.Add(viewID);
    }

    /// <summary>
    /// Trigger game over sequence
    /// </summary>
    private void TriggerGameOver()
    {
        if (gameOverTriggered)
            return;

        gameOverTriggered = true;

        if (showDebugInfo)
            Debug.Log("[PVPScore] Game Over triggered!");

        // Only Master Client triggers the game over sequence
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_GameOver", RpcTarget.All);
        }
    }

    /// <summary>
    /// RPC to notify all clients of game over
    /// </summary>
    [PunRPC]
    private void RPC_GameOver()
    {
        if (showDebugInfo)
            Debug.Log("[PVPScore] Game Over!");

        StartCoroutine(GameOverSequence());
    }

    /// <summary>
    /// Game over UI sequence
    /// </summary>
    private IEnumerator GameOverSequence()
    {
        // Wait before showing first UI
        yield return new WaitForSeconds(firstUIDelay);

        if (firstUIObject != null)
            firstUIObject.SetActive(true);

        // Wait before showing second UI
        yield return new WaitForSeconds(secondUIDelay);

        if (secondUIObject != null)
            secondUIObject.SetActive(true);

        // Wait before scene transition
        yield return new WaitForSeconds(sceneTransitionDelay);

        // Load result scene
        if (!string.IsNullOrEmpty(resultSceneName))
        {
            SceneManager.LoadScene(resultSceneName);
        }
    }

    /// <summary>
    /// Get HolenData from a GameObject
    /// </summary>
    private HolenData GetHolenDataFromGameObject(GameObject holenObject)
    {
        if (holenObject == null)
            return null;

        // Method 1: Check HolenIdentifier component
        HolenIdentifier identifier = holenObject.GetComponent<HolenIdentifier>();
        if (identifier != null && identifier.holenData != null)
        {
            return identifier.holenData;
        }

        // Method 2: Search by name in WagerDataManager
        if (WagerDataManager.Instance != null)
        {
            var allWagers = WagerDataManager.Instance.GetAllWageredHolensIndividual();
            string objectName = holenObject.name.Replace("(Clone)", "").Trim();

            foreach (var wager in allWagers)
            {
                HolenData data = LoadHolenDataByID(wager.holenID);
                if (data != null && data.holenPrefab != null)
                {
                    if (data.holenPrefab.name == objectName)
                    {
                        return data;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Load HolenData by ID from Resources
    /// </summary>
    private HolenData LoadHolenDataByID(string holenID)
    {
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
    /// </summary>
    public void OnTurnEnd()
    {
        if (showDebugInfo)
            Debug.Log("[PVPScore] Turn ended");
    }

    /// <summary>
    /// Refresh the knockout panel for a specific player
    /// </summary>
    private void RefreshKnockoutPanel(int absolutePlayerNumber)
    {
        if (showDebugInfo)
            Debug.Log($"[PVPScore] RefreshKnockoutPanel for Player {absolutePlayerNumber}");

        // Map to local panel (local player vs opponent)
        int localPlayerNumber = GetLocalPlayerNumber();
        bool isLocalPlayer = (absolutePlayerNumber == localPlayerNumber);

        Transform panel = isLocalPlayer ? player1KnockoutPanel : player2KnockoutPanel;
        List<KnockedOutHolen> knockouts = (absolutePlayerNumber == 1) ? player1KnockedOut : player2KnockedOut;

        if (panel == null)
        {
            Debug.LogError($"[PVPScore] Knockout panel not assigned!");
            return;
        }

        if (holenSlotPrefab == null)
        {
            Debug.LogError("[PVPScore] holenSlotPrefab not assigned!");
            return;
        }

        // Clear existing slots
        foreach (Transform child in panel)
        {
            Destroy(child.gameObject);
        }

        // Create new slots
        foreach (var knockout in knockouts)
        {
            HolenData data = LoadHolenDataByID(knockout.holenID);
            if (data == null)
                continue;

            GameObject slotObj = Instantiate(holenSlotPrefab, panel);
            HolenSlotUI slotUI = slotObj.GetComponent<HolenSlotUI>();

            if (slotUI != null)
            {
                slotUI.SetSlot(data, 1);
            }
        }

        if (showDebugInfo)
            Debug.Log($"[PVPScore] Panel refreshed with {knockouts.Count} knockouts");
    }

    /// <summary>
    /// Update turn display UI
    /// </summary>
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
    /// Get local player number
    /// </summary>
    public int GetLocalPlayerNumber()
    {
        if (holenController == null)
            holenController = FindObjectOfType<MultiplayerHolenController>();

        if (holenController != null)
        {
            return holenController.isPlayer1 ? 1 : 2;
        }

        return 0;
    }

    /// <summary>
    /// Get knockouts for a specific player
    /// </summary>
    public List<KnockedOutHolen> GetPlayerKnockedOutHolens(int playerNumber)
    {
        if (playerNumber == 1)
            return new List<KnockedOutHolen>(player1KnockedOut);
        else if (playerNumber == 2)
            return new List<KnockedOutHolen>(player2KnockedOut);

        return new List<KnockedOutHolen>();
    }

    /// <summary>
    /// Get all knockouts
    /// </summary>
    public List<KnockedOutHolen> GetAllKnockedOutHolens()
    {
        List<KnockedOutHolen> allKnockedOut = new List<KnockedOutHolen>();
        allKnockedOut.AddRange(player1KnockedOut);
        allKnockedOut.AddRange(player2KnockedOut);
        return allKnockedOut;
    }

    /// <summary>
    /// Clear all data (for new game)
    /// </summary>
    public void ClearData()
    {
        player1KnockedOut.Clear();
        player2KnockedOut.Clear();
        processedHolenViewIDs.Clear();
        gameOverTriggered = false;
        noHolensTimer = 0f;

        if (showDebugInfo)
            Debug.Log("[PVPScore] Data cleared");
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        CancelInvoke();
    }
}