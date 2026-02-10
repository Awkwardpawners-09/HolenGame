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

    [Header("Game Over UI")]
    public GameObject firstUIObject;  // Enable after no holens detected
    public GameObject secondUIObject; // Enable after first UI
    public float firstUIDelay = 3f;   // Time to wait before showing first UI
    public float secondUIDelay = 3f;  // Time to wait between first and second UI
    public float sceneTransitionDelay = 2f; // Time to wait before switching scenes

    [Header("Scene Transition")]
    public string resultSceneName = "PVPResult"; // Scene to load after game over

    [Header("Settings")]
    public float noHolensWaitTime = 3f; // Time to wait if no holens remain before game over

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
    private List<GameObject> holensToDestroy = new List<GameObject>();

    // Track which holens have already been processed to avoid duplicates
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

        UpdateTurnDisplay();
    }

    void Update()
    {
        // Only Master Client checks for game over condition
        if (!PhotonNetwork.IsMasterClient)
        {
            UpdateTurnDisplay();
            return;
        }

        GameObject[] allHolens = GameObject.FindGameObjectsWithTag("Objective");
        int holensInside = 0;

        foreach (GameObject holen in allHolens)
        {
            Collider holenCollider = holen.GetComponent<Collider>();
            if (holenCollider != null && IsInsideTrigger(holenCollider))
            {
                holensInside++;
            }
        }

        if (holensInside == 0)
        {
            noHolensTimer += Time.deltaTime;

            if (noHolensTimer >= noHolensWaitTime && !gameOverTriggered)
            {
                TriggerGameOver();
            }
        }
        else
        {
            noHolensTimer = 0f;
        }

        UpdateTurnDisplay();
    }

    private bool IsInsideTrigger(Collider otherCollider)
    {
        Collider thisTrigger = GetComponent<Collider>();
        if (thisTrigger != null && thisTrigger.isTrigger)
        {
            return thisTrigger.bounds.Intersects(otherCollider.bounds);
        }
        return false;
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

    void OnTriggerExit(Collider other)
    {
        // CRITICAL: Only Master Client detects knockouts to prevent duplicates
        if (!PhotonNetwork.IsMasterClient) return;

        if (other.CompareTag("Objective"))
        {
            // Don't count the current player's own ball
            if (holenController != null && holenController.currentHolenBall != null)
            {
                if (other.gameObject == holenController.currentHolenBall)
                {
                    return;
                }
            }

            // Check if we've already processed this holen
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && processedHolenViewIDs.Contains(pv.ViewID))
            {
                Debug.Log($"[PVPScore] Holen {other.gameObject.name} already processed, skipping");
                return;
            }

            // Try to get HolenData from the knocked out holen
            HolenData holenData = GetHolenDataFromGameObject(other.gameObject);

            if (holenData != null)
            {
                RecordKnockedOutHolen(holenData);

                // Mark as processed
                if (pv != null)
                {
                    processedHolenViewIDs.Add(pv.ViewID);
                }
            }
            else
            {
                Debug.LogWarning($"Could not find HolenData for knocked out object: {other.gameObject.name}");
            }

            // Queue for destruction
            if (!holensToDestroy.Contains(other.gameObject))
            {
                holensToDestroy.Add(other.gameObject);
            }
        }
    }

    private HolenData GetHolenDataFromGameObject(GameObject holenObject)
    {
        // Method 1: Check if there's a HolenIdentifier component (recommended approach)
        HolenIdentifier identifier = holenObject.GetComponent<HolenIdentifier>();
        if (identifier != null && identifier.holenData != null)
        {
            return identifier.holenData;
        }

        // Method 2: Try to find matching HolenData from WagerDataManager by name
        if (WagerDataManager.Instance != null)
        {
            var allWagers = WagerDataManager.Instance.GetAllWageredHolensIndividual();

            // Try to match by prefab name (assumes GameObject name matches HolenData prefab name)
            string objectName = holenObject.name.Replace("(Clone)", "").Trim();

            foreach (var wager in allWagers)
            {
                HolenData data = LoadHolenDataByID(wager.holenID);
                if (data != null && data.holenPrefab != null)
                {
                    string prefabName = data.holenPrefab.name;
                    if (prefabName == objectName)
                    {
                        return data;
                    }
                }
            }
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

    private void RecordKnockedOutHolen(HolenData holenData)
    {
        if (holenController == null) return;

        int currentPlayer = holenController.isPlayer1 ? 1 : 2;

        // Only record during the current player's turn
        if (holenController.IsTurn())
        {
            // Master Client records and broadcasts to all
            photonView.RPC("RPC_RecordKnockout", RpcTarget.All, holenData.holenID, holenData.holenName, currentPlayer);
        }
    }

    [PunRPC]
    private void RPC_RecordKnockout(string holenID, string holenName, int playerNumber)
    {
        KnockedOutHolen knockedOut = new KnockedOutHolen(holenID, holenName, playerNumber);

        if (playerNumber == 1)
        {
            player1KnockedOut.Add(knockedOut);
            Debug.Log($"[PVPScore] Player 1 knocked out: {holenName} (ID: {holenID})");
        }
        else if (playerNumber == 2)
        {
            player2KnockedOut.Add(knockedOut);
            Debug.Log($"[PVPScore] Player 2 knocked out: {holenName} (ID: {holenID})");
        }
    }

    public void OnTurnEnd()
    {
        // Only Master Client destroys objects
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(DestroyQueuedHolens());
        }
    }

    private IEnumerator DestroyQueuedHolens()
    {
        yield return new WaitForSeconds(0.5f);

        foreach (GameObject holen in holensToDestroy)
        {
            if (holen != null)
            {
                PhotonView pv = holen.GetComponent<PhotonView>();
                if (pv != null)
                {
                    Debug.Log($"[PVPScore] Master Client destroying holen: {holen.name}");
                    PhotonNetwork.Destroy(holen);
                }
                else
                {
                    Destroy(holen);
                }
            }
        }

        holensToDestroy.Clear();
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
        gameOverTriggered = false;
        noHolensTimer = 0f;
        processedHolenViewIDs.Clear();
        Debug.Log("[PVPScore] Data cleared");
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}