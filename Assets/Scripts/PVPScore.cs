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
    public TMP_Text turnDisplayText;

    [Header("Knocked Out Holens Display")]
    [Tooltip("Prefab containing the HolenSlotUI component")]
    public GameObject holenSlotUIPrefab;

    [Tooltip("Panel where Player 1's knocked out holens will be displayed")]
    public Transform player1KnockedOutPanel;

    [Tooltip("Panel where Player 2's knocked out holens will be displayed")]
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

    [System.Serializable]
    public class KnockedOutHolen
    {
        public string holenID;
        public string holenName;
        public int playerNumber;

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

    private MultiplayerHolenControllerNew holenController;
    private List<GameObject> holensToDestroy = new List<GameObject>();

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────
    void Awake()
    {
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
        holenController = FindObjectOfType<MultiplayerHolenControllerNew>();

        if (firstUIObject != null) firstUIObject.SetActive(false);
        if (secondUIObject != null) secondUIObject.SetActive(false);

        DisableFeedbackObject(feedbackNoKnockout);
        DisableFeedbackObject(feedback1Knockout);
        DisableFeedbackObject(feedback2Knockout);
        DisableFeedbackObject(feedback3Knockout);
        DisableFeedbackObject(feedback4Knockout);
        DisableFeedbackObject(feedback5Knockout);

        UpdateTurnDisplay();
    }

    void Update()
    {
        if (holenController == null)
            holenController = FindObjectOfType<MultiplayerHolenControllerNew>();

        // Count only wager holens (not the launched ball) for the game-over check
        GameObject[] allHolens = GameObject.FindGameObjectsWithTag("Objective");
        int holensInside = 0;
        foreach (GameObject holen in allHolens)
        {
            if (holenController != null && holen == holenController.currentHolenBall)
                continue; // skip the launched ball

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

        UpdateTurnDisplay();
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

        // ── CRITICAL: skip the ball the active player launched ──
        // currentHolenBall is the ball shot by MultiplayerHolenControllerNew.
        // It is tagged "Objective" so we must explicitly ignore it here.
        if (holenController != null && holenController.currentHolenBall != null)
        {
            if (other.gameObject == holenController.currentHolenBall)
            {
                Debug.Log("[PVPScore] Launched ball exited field — skipped.");
                return;
            }
        }

        // It's a wager holen that was knocked out. ─────────────────

        // Record the score
        HolenData holenData = GetHolenDataFromGameObject(other.gameObject);
        if (holenData != null)
            RecordKnockedOutHolen(holenData);
        else
            Debug.LogWarning($"[PVPScore] Could not find HolenData for: {other.gameObject.name}");

        // Queue for delayed destruction
        if (!holensToDestroy.Contains(other.gameObject))
            holensToDestroy.Add(other.gameObject);

        // Update counter and broadcast feedback to both screens.
        // Master client is the single authority so feedback fires exactly once.
        if (turnInProgress && PhotonNetwork.IsMasterClient)
        {
            holensKnockedOutThisTurn++;
            Debug.Log($"[PVPScore] Wager holen knocked out #{holensKnockedOutThisTurn} — broadcasting feedback.");
            photonView.RPC("RPC_ShowTurnFeedback", RpcTarget.All, holensKnockedOutThisTurn);
        }
    }

    // ─────────────────────────────────────────────
    //  TURN FEEDBACK
    // ─────────────────────────────────────────────

    /// <summary>
    /// Call from MultiplayerHolenControllerNew.ShootHolen() right when the ball is launched.
    /// Opens the window so wager-holen exits are counted toward this turn's feedback.
    /// </summary>
    public void OnTurnStarted()
    {
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC("RPC_OnTurnStarted", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_OnTurnStarted()
    {
        turnInProgress = true;
        holensKnockedOutThisTurn = 0;
        Debug.Log("[PVPScore] Turn started — tracking wager holen knockouts.");
    }

    /// <summary>
    /// Called by MultiplayerHolenControllerNew.EndTurn() → scoreManager.OnTurnEnd().
    /// Closes the tracking window, shows no-knockout feedback if needed, and destroys
    /// queued holens after the configured delay.
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

        // Knockout feedback (1–5+) fires immediately in OnTriggerExit.
        // No-knockout feedback only fires here, once the turn is fully over.
        if (holensKnockedOutThisTurn == 0)
            ShowTurnFeedbackLocal(0);
    }

    [PunRPC]
    private void RPC_ShowTurnFeedback(int knockedOut)
    {
        ShowTurnFeedbackLocal(knockedOut);
    }

    private void ShowTurnFeedbackLocal(int knockedOut)
    {
        if (gameOverTriggered) return;

        GameObject target = GetFeedbackObject(knockedOut);
        if (target == null)
        {
            Debug.Log($"[PVPScore] No feedback object assigned for {knockedOut} knockout(s).");
            return;
        }

        if (activeFeedbackCoroutine != null)
            StopCoroutine(activeFeedbackCoroutine);

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
        if (obj != null && obj.activeSelf)
            obj.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  DELAYED DESTROY + OPTIONAL VFX
    // ─────────────────────────────────────────────

    private IEnumerator DestroyQueuedHolens()
    {
        // Snapshot positions now; the objects may move or be destroyed remotely before the delay ends
        var snapshot = new List<(GameObject go, Vector3 pos)>();
        foreach (GameObject holen in holensToDestroy)
            if (holen != null)
                snapshot.Add((holen, holen.transform.position));

        yield return new WaitForSeconds(knockedOutDestroyDelay);

        foreach (var (go, pos) in snapshot)
        {
            // VFX is purely visual — spawn locally on every client, no network object needed
            if (knockedOutVFXPrefab != null)
            {
                GameObject vfx = Instantiate(knockedOutVFXPrefab, pos, Quaternion.identity);
                if (vfxLifetime > 0f)
                    Destroy(vfx, vfxLifetime);
                Debug.Log($"[PVPScore] VFX spawned at {pos}");
            }

            // Only master client destroys the networked holen
            if (go != null && PhotonNetwork.IsMasterClient)
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null)
                    PhotonNetwork.Destroy(go);
                else
                    Destroy(go);

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
        foreach (var h in player1KnockedOut)
            p1Data.Add((h.holenID, h.holenName, h.playerNumber));

        var p2Data = new List<(string, string, int)>();
        foreach (var h in player2KnockedOut)
            p2Data.Add((h.holenID, h.holenName, h.playerNumber));

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
    //  KNOCKOUT RECORDING
    // ─────────────────────────────────────────────
    private HolenData GetHolenDataFromGameObject(GameObject holenObject)
    {
        HolenIdentifier identifier = holenObject.GetComponent<HolenIdentifier>();
        if (identifier != null && identifier.holenData != null)
            return identifier.holenData;

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

    private void RecordKnockedOutHolen(HolenData holenData)
    {
        if (holenController == null || !holenController.IsTurn()) return;

        int currentPlayer = holenController.isPlayer1 ? 1 : 2;
        var knockedOut = new KnockedOutHolen(holenData.holenID, holenData.holenName, currentPlayer);

        if (currentPlayer == 1)
        {
            player1KnockedOut.Add(knockedOut);
            DisplayKnockedOutHolen(holenData, true);
            photonView.RPC("RPC_RecordKnockout", RpcTarget.Others, holenData.holenID, holenData.holenName, 1);
        }
        else
        {
            player2KnockedOut.Add(knockedOut);
            DisplayKnockedOutHolen(holenData, true);
            photonView.RPC("RPC_RecordKnockout", RpcTarget.Others, holenData.holenID, holenData.holenName, 2);
        }

        Debug.Log($"[PVPScore] Player {currentPlayer} knocked out: {holenData.holenName}");
    }

    [PunRPC]
    private void RPC_RecordKnockout(string holenID, string holenName, int playerNumber)
    {
        var knockedOut = new KnockedOutHolen(holenID, holenName, playerNumber);
        if (playerNumber == 1) player1KnockedOut.Add(knockedOut);
        else if (playerNumber == 2) player2KnockedOut.Add(knockedOut);

        HolenData data = LoadHolenDataByID(holenID);
        if (data != null) DisplayKnockedOutHolen(data, false);
        else Debug.LogWarning($"[PVPScore] Could not load HolenData for synced knockout. ID: {holenID}");
    }

    private void DisplayKnockedOutHolen(HolenData holenData, bool isLocalPlayer)
    {
        if (holenSlotUIPrefab == null) { Debug.LogWarning("[PVPScore] HolenSlotUI prefab not assigned."); return; }

        Transform targetPanel = isLocalPlayer ? player1KnockedOutPanel : player2KnockedOutPanel;
        if (targetPanel == null) { Debug.LogWarning("[PVPScore] Target panel not assigned."); return; }

        GameObject slotInstance = Instantiate(holenSlotUIPrefab, targetPanel);
        HolenSlotUI slotUI = slotInstance.GetComponent<HolenSlotUI>();
        if (slotUI == null) { Debug.LogError("[PVPScore] HolenSlotUI not on prefab!"); Destroy(slotInstance); return; }

        slotUI.SetSlot(holenData, 1);
        Debug.Log($"[PVPScore] Displayed '{holenData.holenName}' in {(isLocalPlayer ? "local" : "opponent")}'s panel");
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────
    private void UpdateTurnDisplay()
    {
        if (turnDisplayText != null && holenController != null)
            turnDisplayText.text = holenController.IsTurn() ? "Your Turn" : "Opponent's Turn";
    }

    public int GetLocalPlayerNumber()
    {
        if (holenController == null)
            holenController = FindObjectOfType<MultiplayerHolenControllerNew>();
        return holenController != null ? (holenController.isPlayer1 ? 1 : 2) : 0;
    }

    public List<KnockedOutHolen> GetPlayerKnockedOutHolens(int playerNumber)
    {
        if (playerNumber == 1) return new List<KnockedOutHolen>(player1KnockedOut);
        if (playerNumber == 2) return new List<KnockedOutHolen>(player2KnockedOut);
        return new List<KnockedOutHolen>();
    }

    public List<KnockedOutHolen> GetAllKnockedOutHolens()
    {
        var all = new List<KnockedOutHolen>();
        all.AddRange(player1KnockedOut);
        all.AddRange(player2KnockedOut);
        return all;
    }

    private void ClearKnockedOutPanels()
    {
        if (player1KnockedOutPanel != null)
            foreach (Transform child in player1KnockedOutPanel)
                Destroy(child.gameObject);

        if (player2KnockedOutPanel != null)
            foreach (Transform child in player2KnockedOutPanel)
                Destroy(child.gameObject);
    }

    public void ClearData()
    {
        player1KnockedOut.Clear();
        player2KnockedOut.Clear();
        gameOverTriggered = false;
        noHolensTimer = 0f;
        ClearKnockedOutPanels();
        Debug.Log("[PVPScore] Data cleared");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}