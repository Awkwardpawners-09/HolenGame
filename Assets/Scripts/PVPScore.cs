using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class PVPScore : MonoBehaviourPunCallbacks
{
    public static PVPScore Instance { get; private set; }

    // ── Turn Display ──────────────────────────────
    // yourTurnObject and opponentTurnObject are handled by MultiplayerHolenControllerNew.
    // PVPScore does not need turn display fields.

    [Header("Knockout Count Display")]
    [Tooltip("TMP_Text for the LOCAL player's knockout count (always shown as Player 1)")]
    public TMP_Text player1KnockoutCountText;
    [Tooltip("TMP_Text for the OPPONENT's knockout count (always shown as Player 2)")]
    public TMP_Text player2KnockoutCountText;

    [Header("Knocked Out Holens Display")]
    [Tooltip("Prefab containing the HolenSlotUI component")]
    public GameObject holenSlotUIPrefab;
    [Tooltip("Panel for the LOCAL player's knocked-out holens (Player 1 UI slot)")]
    public Transform player1KnockedOutPanel;
    [Tooltip("Panel for the OPPONENT's knocked-out holens (Player 2 UI slot)")]
    public Transform player2KnockedOutPanel;

    [Header("Game Over UI")]
    public GameObject firstUIObject;
    public GameObject secondUIObject;
    public float firstUIDelay = 3f;
    public float secondUIDelay = 3f;
    public float sceneTransitionDelay = 2f;

    [Header("Scene Transition")]
    public string resultSceneName = "PVPResult";

    [Header("Settings")]
    public float noHolensWaitTime = 3f;

    [Header("Turn Feedback (Launch Result)")]
    [Tooltip("Shown on both screens when the active player knocks out NO wager holens this turn.")]
    public GameObject feedbackNoKnockout;
    [Tooltip("Shown on both screens when exactly 1 wager holen is knocked out.")]
    public GameObject feedback1Knockout;
    [Tooltip("Shown on both screens when exactly 2 wager holens are knocked out.")]
    public GameObject feedback2Knockout;
    [Tooltip("Shown on both screens when exactly 3 wager holens are knocked out.")]
    public GameObject feedback3Knockout;
    [Tooltip("Shown on both screens when exactly 4 wager holens are knocked out.")]
    public GameObject feedback4Knockout;
    [Tooltip("Shown on both screens when 5 or more wager holens are knocked out.")]
    public GameObject feedback5Knockout;
    [Tooltip("How long (seconds) the feedback object stays visible.")]
    public float feedbackDisplayDuration = 4f;

    [Header("Knocked-Out Holen Destruction")]
    [Tooltip("Seconds after a wager holen exits the field before it is destroyed.")]
    public float knockedOutDestroyDelay = 3f;
    [Tooltip("(Optional) Instantiated locally at each knocked-out holen's position before it is destroyed.")]
    public GameObject knockedOutVFXPrefab;
    [Tooltip("How long (seconds) the VFX object lives before being destroyed. 0 = permanent.")]
    public float vfxLifetime = 2f;

    // ── Private ──────────────────────────────────
    private Coroutine activeFeedbackCoroutine;
    private bool turnInProgress = false;
    private int holensKnockedOutThisTurn = 0;

    // The ABSOLUTE player number (1 or 2, based on Photon ActorNumber) of whoever is
    // currently taking their turn. Synced via RPC at turn-start so BOTH clients always
    // agree on who the scoring player is — this is the root fix for the wrong-player bug.
    private int activeTurnPlayerNumber = 0;

    [System.Serializable]
    public class KnockedOutHolen
    {
        public string holenID;
        public string holenName;
        public int playerNumber; // absolute: 1 = ActorNumber 1, 2 = ActorNumber 2

        public KnockedOutHolen(string id, string name, int player)
        {
            holenID = id;
            holenName = name;
            playerNumber = player;
        }
    }

    // Keyed by ABSOLUTE player number — identical on both clients.
    private List<KnockedOutHolen> player1KnockedOut = new List<KnockedOutHolen>();
    private List<KnockedOutHolen> player2KnockedOut = new List<KnockedOutHolen>();

    private float noHolensTimer = 0f;
    private bool gameOverTriggered = false;

    private MultiplayerHolenControllerNew holenController;
    private List<GameObject> holensToDestroy = new List<GameObject>();

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[PVPScore] Created and persisting between scenes");
    }

    void Start()
    {
        holenController = FindObjectOfType<MultiplayerHolenControllerNew>();

        if (firstUIObject != null) firstUIObject.SetActive(false);
        if (secondUIObject != null) secondUIObject.SetActive(false);

        DisableFeedbackObject(feedbackNoKnockout);
        DisableFeedbackObject(feedback1Knockout);
        DisableFeedbackObject(feedback2Knockout);
        DisableFeedbackObject(feedback3Knockout);
        DisableFeedbackObject(feedback4Knockout);
        DisableFeedbackObject(feedback5Knockout);

        UpdateKnockoutCountDisplay();
    }

    void Update()
    {
        if (holenController == null)
            holenController = FindObjectOfType<MultiplayerHolenControllerNew>();

        // Count only wager holens (not the launched ball) for the game-over check.
        GameObject[] allHolens = GameObject.FindGameObjectsWithTag("Objective");
        int holensInside = 0;
        foreach (GameObject holen in allHolens)
        {
            if (holenController != null && holen == holenController.currentHolenBall)
                continue;

            Collider col = holen.GetComponent<Collider>();
            if (col != null && IsInsideTrigger(col))
                holensInside++;
        }

        if (holensInside == 0)
        {
            noHolensTimer += Time.deltaTime;
            if (noHolensTimer >= noHolensWaitTime && !gameOverTriggered)
                TriggerGameOver();
        }
        else
        {
            noHolensTimer = 0f;
        }

    }

    private bool IsInsideTrigger(Collider otherCollider)
    {
        Collider thisTrigger = GetComponent<Collider>();
        if (thisTrigger != null && thisTrigger.isTrigger)
            return thisTrigger.bounds.Intersects(otherCollider.bounds);
        return false;
    }

    // ─────────────────────────────────────────────
    //  TRIGGER — wager holens only
    // ─────────────────────────────────────────────
    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Objective")) return;

        // Skip the ball the active player launched.
        if (holenController != null && holenController.currentHolenBall != null)
        {
            if (other.gameObject == holenController.currentHolenBall)
            {
                Debug.Log("[PVPScore] Launched ball exited field — skipped.");
                return;
            }
        }

        // Only the master client detects knockouts to prevent both clients double-firing.
        // The master broadcasts the result to ALL clients (including itself) via RPC so
        // every client's list and UI stay in sync.
        if (!PhotonNetwork.IsMasterClient) return;

        HolenData holenData = GetHolenDataFromGameObject(other.gameObject);
        if (holenData != null)
        {
            // Send the absolute player number — both clients will receive the same value.
            photonView.RPC("RPC_RecordKnockout", RpcTarget.All,
                holenData.holenID, holenData.holenName, activeTurnPlayerNumber);
        }
        else
        {
            Debug.LogWarning($"[PVPScore] Could not find HolenData for: {other.gameObject.name}");
        }

        // Queue for delayed destruction.
        if (!holensToDestroy.Contains(other.gameObject))
            holensToDestroy.Add(other.gameObject);

        // Broadcast feedback count to both screens.
        if (turnInProgress)
        {
            holensKnockedOutThisTurn++;
            Debug.Log($"[PVPScore] Wager holen knocked out #{holensKnockedOutThisTurn} — broadcasting feedback.");
            photonView.RPC("RPC_ShowTurnFeedback", RpcTarget.All, holensKnockedOutThisTurn);
        }
    }

    // ─────────────────────────────────────────────
    //  TURN START / END
    // ─────────────────────────────────────────────

    /// <summary>
    /// Called from MultiplayerHolenControllerNew.ShootHolen().
    /// Pass (holenController.isPlayer1 ? 1 : 2) so the absolute scorer is network-synced
    /// before any OnTriggerExit fires. This is what fixes the wrong-player score bug.
    /// </summary>
    public void OnTurnStarted(int shootingPlayerAbsoluteNumber)
    {
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC("RPC_OnTurnStarted", RpcTarget.All, shootingPlayerAbsoluteNumber);
    }

    [PunRPC]
    private void RPC_OnTurnStarted(int shootingPlayerAbsoluteNumber)
    {
        turnInProgress = true;
        holensKnockedOutThisTurn = 0;
        activeTurnPlayerNumber = shootingPlayerAbsoluteNumber;
        Debug.Log($"[PVPScore] Turn started — active player: {activeTurnPlayerNumber}");
    }

    /// <summary>
    /// Called from MultiplayerHolenControllerNew.EndTurn().
    /// </summary>
    public void OnTurnEnd()
    {
        if (holensToDestroy.Count > 0)
            StartCoroutine(DestroyQueuedHolens());

        if (PhotonNetwork.IsMasterClient)
            photonView.RPC("RPC_OnTurnEnded", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_OnTurnEnded()
    {
        turnInProgress = false;
        Debug.Log($"[PVPScore] Turn ended — total knockouts this turn: {holensKnockedOutThisTurn}");

        if (holensKnockedOutThisTurn == 0)
            ShowTurnFeedbackLocal(0);
    }

    // ─────────────────────────────────────────────
    //  KNOCKOUT RECORDING  (RPC — runs on ALL clients)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Runs on every client. absolutePlayerNumber is 1 or 2, identical on both machines.
    /// Each client independently remaps it to the correct local UI panel:
    ///   • local player's knockouts  → player1KnockedOutPanel (Player 1 UI slot)
    ///   • opponent's knockouts      → player2KnockedOutPanel (Player 2 UI slot)
    /// This ensures the local player always appears in the Player 1 UI position.
    /// </summary>
    [PunRPC]
    private void RPC_RecordKnockout(string holenID, string holenName, int absolutePlayerNumber)
    {
        var knockedOut = new KnockedOutHolen(holenID, holenName, absolutePlayerNumber);

        if (absolutePlayerNumber == 1) player1KnockedOut.Add(knockedOut);
        else player2KnockedOut.Add(knockedOut);

        HolenData data = LoadHolenDataByID(holenID);
        if (data != null)
            DisplayKnockedOutHolen(data, absolutePlayerNumber);
        else
            Debug.LogWarning($"[PVPScore] Could not load HolenData for ID: {holenID}");

        UpdateKnockoutCountDisplay();
        Debug.Log($"[PVPScore] Player {absolutePlayerNumber} knocked out: {holenName}");
    }

    // ─────────────────────────────────────────────
    //  DISPLAY  (per-client panel remapping)
    // ─────────────────────────────────────────────

    private void DisplayKnockedOutHolen(HolenData holenData, int absolutePlayerNumber)
    {
        if (holenSlotUIPrefab == null) { Debug.LogWarning("[PVPScore] HolenSlotUI prefab not assigned."); return; }

        // Remap: local player → player1KnockedOutPanel, opponent → player2KnockedOutPanel.
        bool isLocalPlayer = (absolutePlayerNumber == GetLocalPlayerNumber());
        Transform targetPanel = isLocalPlayer ? player1KnockedOutPanel : player2KnockedOutPanel;

        if (targetPanel == null) { Debug.LogWarning("[PVPScore] Target panel not assigned."); return; }

        GameObject slotInstance = Instantiate(holenSlotUIPrefab, targetPanel);
        HolenSlotUI slotUI = slotInstance.GetComponent<HolenSlotUI>();
        if (slotUI == null) { Debug.LogError("[PVPScore] HolenSlotUI not on prefab!"); Destroy(slotInstance); return; }

        slotUI.SetSlot(holenData, 1);
        Debug.Log($"[PVPScore] Displayed '{holenData.holenName}' in {(isLocalPlayer ? "local" : "opponent")}'s panel");
    }

    // ─────────────────────────────────────────────
    //  KNOCKOUT COUNT DISPLAY
    // ─────────────────────────────────────────────

    /// <summary>
    /// player1KnockoutCountText = local player's score.
    /// player2KnockoutCountText = opponent's score.
    /// </summary>
    private void UpdateKnockoutCountDisplay()
    {
        int localNumber = GetLocalPlayerNumber();

        int localCount = (localNumber == 1) ? player1KnockedOut.Count : player2KnockedOut.Count;
        int opponentCount = (localNumber == 1) ? player2KnockedOut.Count : player1KnockedOut.Count;

        if (player1KnockoutCountText != null) player1KnockoutCountText.text = localCount.ToString();
        if (player2KnockoutCountText != null) player2KnockoutCountText.text = opponentCount.ToString();
    }

    // ─────────────────────────────────────────────
    //  TURN FEEDBACK
    // ─────────────────────────────────────────────
    [PunRPC]
    private void RPC_ShowTurnFeedback(int knockedOut)
    {
        ShowTurnFeedbackLocal(knockedOut);
    }

    private void ShowTurnFeedbackLocal(int knockedOut)
    {
        if (gameOverTriggered) return;

        GameObject target = GetFeedbackObject(knockedOut);
        if (target == null) { Debug.Log($"[PVPScore] No feedback object for {knockedOut} knockout(s)."); return; }

        if (activeFeedbackCoroutine != null) StopCoroutine(activeFeedbackCoroutine);
        activeFeedbackCoroutine = StartCoroutine(DisplayFeedback(target));
    }

    private GameObject GetFeedbackObject(int knockedOut)
    {
        switch (knockedOut)
        {
            case 0: return feedbackNoKnockout;
            case 1: return feedback1Knockout;
            case 2: return feedback2Knockout;
            case 3: return feedback3Knockout;
            case 4: return feedback4Knockout;
            default: return feedback5Knockout;
        }
    }

    private IEnumerator DisplayFeedback(GameObject feedbackObj)
    {
        DisableFeedbackObject(feedbackNoKnockout);
        DisableFeedbackObject(feedback1Knockout);
        DisableFeedbackObject(feedback2Knockout);
        DisableFeedbackObject(feedback3Knockout);
        DisableFeedbackObject(feedback4Knockout);
        DisableFeedbackObject(feedback5Knockout);

        feedbackObj.SetActive(true);
        Debug.Log($"[PVPScore] Feedback shown: {feedbackObj.name}");

        yield return new WaitForSeconds(feedbackDisplayDuration);

        DisableFeedbackObject(feedbackObj);
        Debug.Log($"[PVPScore] Feedback hidden: {feedbackObj.name}");
        activeFeedbackCoroutine = null;
    }

    private void DisableFeedbackObject(GameObject obj)
    {
        if (obj != null && obj.activeSelf) obj.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  DELAYED DESTROY + OPTIONAL VFX
    // ─────────────────────────────────────────────
    private IEnumerator DestroyQueuedHolens()
    {
        var snapshot = new List<(GameObject go, Vector3 pos)>();
        foreach (GameObject holen in holensToDestroy)
            if (holen != null) snapshot.Add((holen, holen.transform.position));

        yield return new WaitForSeconds(knockedOutDestroyDelay);

        foreach (var (go, pos) in snapshot)
        {
            if (knockedOutVFXPrefab != null)
            {
                GameObject vfx = Instantiate(knockedOutVFXPrefab, pos, Quaternion.identity);
                if (vfxLifetime > 0f) Destroy(vfx, vfxLifetime);
                Debug.Log($"[PVPScore] VFX spawned at {pos}");
            }

            if (go != null && PhotonNetwork.IsMasterClient)
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null) PhotonNetwork.Destroy(go);
                else Destroy(go);
                Debug.Log($"[PVPScore] Knocked-out holen destroyed at {pos}");
            }
        }

        holensToDestroy.Clear();
    }

    // ─────────────────────────────────────────────
    //  GAME OVER
    // ─────────────────────────────────────────────
    private void TriggerGameOver()
    {
        gameOverTriggered = true;
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC("RPC_TriggerGameOver", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_TriggerGameOver()
    {
        StartCoroutine(ShowGameOverSequence());
    }

    private IEnumerator ShowGameOverSequence()
    {
        Debug.Log("[PVPScore] Game Over! No wager holens remaining.");

        yield return new WaitForSeconds(firstUIDelay);
        if (firstUIObject != null) firstUIObject.SetActive(true);

        yield return new WaitForSeconds(secondUIDelay);
        if (secondUIObject != null) secondUIObject.SetActive(true);

        LogFinalResults();

        yield return new WaitForSeconds(sceneTransitionDelay);
        LoadResultScene();
    }

    private void LoadResultScene()
    {
        int localPlayer = GetLocalPlayerNumber();

        var p1Data = new List<(string, string, int)>();
        foreach (var h in player1KnockedOut) p1Data.Add((h.holenID, h.holenName, h.playerNumber));

        var p2Data = new List<(string, string, int)>();
        foreach (var h in player2KnockedOut) p2Data.Add((h.holenID, h.holenName, h.playerNumber));

        PVPDataHolder.StoreMatchResults(p1Data, p2Data, localPlayer);

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(resultSceneName);
    }

    private void LogFinalResults()
    {
        Debug.Log("=== GAME RESULTS ===");
        Debug.Log($"Player 1 knocked out {player1KnockedOut.Count} holens:");
        foreach (var h in player1KnockedOut) Debug.Log($"  - {h.holenName} (ID: {h.holenID})");
        Debug.Log($"Player 2 knocked out {player2KnockedOut.Count} holens:");
        foreach (var h in player2KnockedOut) Debug.Log($"  - {h.holenName} (ID: {h.holenID})");
    }

    // ─────────────────────────────────────────────
    //  HOLEN DATA LOOKUP
    // ─────────────────────────────────────────────
    private HolenData GetHolenDataFromGameObject(GameObject holenObject)
    {
        HolenIdentifier identifier = holenObject.GetComponent<HolenIdentifier>();
        if (identifier != null && identifier.holenData != null) return identifier.holenData;

        if (WagerDataManager.Instance != null)
        {
            string objectName = holenObject.name.Replace("(Clone)", "").Trim();
            foreach (var wager in WagerDataManager.Instance.GetAllWageredHolensIndividual())
            {
                HolenData data = LoadHolenDataByID(wager.holenID);
                if (data != null && data.holenPrefab != null && data.holenPrefab.name == objectName)
                    return data;
            }
        }
        return null;
    }

    private HolenData LoadHolenDataByID(string holenID)
    {
        foreach (var data in Resources.LoadAll<HolenData>("HolenData"))
            if (data.holenID == holenID) return data;
        return null;
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns the absolute player number (1 or 2) for the local client.
    /// Matches the ActorNumber == 1 check MultiplayerHolenControllerNew uses.
    /// </summary>
    public int GetLocalPlayerNumber()
    {
        if (holenController == null)
            holenController = FindObjectOfType<MultiplayerHolenControllerNew>();
        if (holenController != null)
            return holenController.isPlayer1 ? 1 : 2;

        // Fallback if controller isn't ready yet.
        return PhotonNetwork.LocalPlayer.ActorNumber == 1 ? 1 : 2;
    }

    public List<KnockedOutHolen> GetPlayerKnockedOutHolens(int playerNumber)
    {
        if (playerNumber == 1) return new List<KnockedOutHolen>(player1KnockedOut);
        if (playerNumber == 2) return new List<KnockedOutHolen>(player2KnockedOut);
        return new List<KnockedOutHolen>();
    }

    public List<KnockedOutHolen> GetAllKnockedOutHolens()
    {
        var all = new List<KnockedOutHolen>(player1KnockedOut);
        all.AddRange(player2KnockedOut);
        return all;
    }

    private void ClearKnockedOutPanels()
    {
        if (player1KnockedOutPanel != null)
            foreach (Transform child in player1KnockedOutPanel) Destroy(child.gameObject);
        if (player2KnockedOutPanel != null)
            foreach (Transform child in player2KnockedOutPanel) Destroy(child.gameObject);
    }

    public void ClearData()
    {
        player1KnockedOut.Clear();
        player2KnockedOut.Clear();
        gameOverTriggered = false;
        noHolensTimer = 0f;
        ClearKnockedOutPanels();
        UpdateKnockoutCountDisplay();
        Debug.Log("[PVPScore] Data cleared");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}