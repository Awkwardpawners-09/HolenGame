using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class LobbyNetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Player Assignment")]
    public int localPlayerNumber = 0;

    [Header("Top Player UI References (Opponent)")]
    public GameObject topPlayerInventoryContent;
    public GameObject topPlayerWagerContent;
    public TMP_Text topPlayerNameText;
    public TMP_Text topPlayerPointsText;
    public TMP_Text topPlayerStateText;
    public TMP_Text topPlayerInventoryStateText;
    public TMP_Text topPlayerCountdownText;

    [Header("Bottom Player UI References (Local Player)")]
    public GameObject bottomPlayerInventoryContent;
    public GameObject bottomPlayerWagerContent;
    public TMP_Text bottomPlayerNameText;
    public TMP_Text bottomPlayerPointsText;
    public TMP_Text bottomPlayerStateText;
    public TMP_Text bottomPlayerInventoryStateText;
    public TMP_Text bottomPlayerCountdownText;

    [Header("Shared UI References")]
    public Button sharedReadyButton; // Single ready button for local player
    public TMP_Text sharedReadyButtonText; // Optional: to change button text

    [Header("Prefab References")]
    public GameObject holenUISlotPrefab;

    [Header("Settings")]
    public string gameSceneName = "GameScene";

    [Header("Waiting UI (Optional)")]
    public GameObject waitingForPlayerPanel;

    [Header("Transition object")]
    public GameObject transitionObject;

    private WagerManager localWager;
    private WagerManager opponentWager;

    private bool isPlayer1Ready = false;
    private bool isPlayer2Ready = false;
    private bool isInitialized = false;

    private const byte READY_STATE_EVENT = 1;
    private const byte WAGER_UPDATE_EVENT = 2;
    private const byte WAGER_SELECTION_EVENT = 3;
    private const byte INVENTORY_SYNC_EVENT = 4;
    private const byte SAVE_WAGER_EVENT = 5;

    void Start()
    {
        Debug.Log($"LobbyNetworkManager Start. Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");

        if (waitingForPlayerPanel != null)
        {
            waitingForPlayerPanel.SetActive(PhotonNetwork.CurrentRoom.PlayerCount < 2);
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Invoke(nameof(InitializeLobby), 0.5f);
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player entered room. Total players: {PhotonNetwork.CurrentRoom.PlayerCount}");

        if (waitingForPlayerPanel != null)
        {
            waitingForPlayerPanel.SetActive(false);
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && !isInitialized)
        {
            Invoke(nameof(InitializeLobby), 0.5f);
        }
    }

    private void InitializeLobby()
    {
        if (isInitialized)
        {
            Debug.LogWarning("Lobby already initialized, skipping...");
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            Debug.LogWarning("Cannot initialize lobby with less than 2 players");
            return;
        }

        isInitialized = true;
        localPlayerNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        Debug.Log($"[INIT] Local player assigned as Player {localPlayerNumber}");

        UpdatePlayerNames();
        UpdateInventoryStateLabels();
        SetupLocalPlayerUI();
        SetupOpponentUI();
        SetupInventories();
        SetupSharedReadyButton();

        Debug.Log($"[INIT] Lobby initialization complete for Player {localPlayerNumber}");

        Invoke(nameof(SendInventoryToOpponent), 0.5f);
    }

    private void SetupSharedReadyButton()
    {
        if (sharedReadyButton != null)
        {
            sharedReadyButton.onClick.RemoveAllListeners();
            sharedReadyButton.onClick.AddListener(OnSharedReadyButtonPressed);

            // Set initial button text
            UpdateSharedReadyButtonText(false);

            Debug.Log($"[SETUP] Shared ready button configured for Player {localPlayerNumber}");
        }
    }

    private void OnSharedReadyButtonPressed()
    {
        if (localWager == null) return;

        bool currentReadyState = GetPlayerReadyState(localPlayerNumber);
        bool newReadyState = !currentReadyState;

        Debug.Log($"[READY] Player {localPlayerNumber} toggling ready: {currentReadyState} -> {newReadyState}");

        // Update local wager manager state
        localWager.OnActionButtonPressed();
        SetPlayerReadyState(localPlayerNumber, newReadyState);

        // Update button appearance
        UpdateSharedReadyButtonText(newReadyState);

        // Update local player's state text (always bottom)
        UpdateLocalPlayerStateText(newReadyState);

        // Send ready state to opponent
        object[] content = new object[] { localPlayerNumber, newReadyState };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = SendOptions.SendReliable;
        PhotonNetwork.RaiseEvent(READY_STATE_EVENT, content, raiseEventOptions, sendOptions);

        // Save wager data
        SaveLocalWagerToManager();

        CheckBothPlayersReady();
    }

    private void UpdateSharedReadyButtonText(bool isReady)
    {
        if (sharedReadyButtonText != null)
        {
            sharedReadyButtonText.text = isReady ? "CANCEL" : "READY";
        }
        else if (sharedReadyButton != null)
        {
            // Fallback: update button's own text component if sharedReadyButtonText not assigned
            TMP_Text buttonText = sharedReadyButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = isReady ? "CANCEL" : "READY";
            }
        }
    }

    private void UpdateLocalPlayerStateText(bool isReady)
    {
        string stateLabel = isReady ? "READY" : "CANCEL";

        // Local player is always on bottom
        if (bottomPlayerStateText != null)
        {
            bottomPlayerStateText.text = stateLabel;
        }
    }

    private void UpdateInventoryStateLabels()
    {
        // Bottom is always local player
        if (bottomPlayerInventoryStateText != null)
            bottomPlayerInventoryStateText.text = "Your Holens";

        // Top is always opponent
        if (topPlayerInventoryStateText != null)
            topPlayerInventoryStateText.text = "Opponent's Holens";

        Debug.Log($"[LABELS] Updated inventory state labels (Local always on bottom)");
    }

    private void SetupLocalPlayerUI()
    {
        Debug.Log("[SETUP] Setting up Local Player UI (Bottom)");

        // Local player is always displayed on bottom UI
        localWager = CreateWagerManager(
            bottomPlayerWagerContent,
            null, // No individual action button
            bottomPlayerStateText,
            bottomPlayerCountdownText,
            bottomPlayerPointsText,
            true
        );
    }

    private void SetupOpponentUI()
    {
        Debug.Log("[SETUP] Setting up Opponent UI (Top) - read-only");

        // Opponent is always displayed on top UI
        opponentWager = CreateWagerManager(
            topPlayerWagerContent,
            null, // No individual action button
            topPlayerStateText,
            topPlayerCountdownText,
            topPlayerPointsText,
            false
        );
    }

    private void SetupInventories()
    {
        // Local player inventory always goes to bottom
        var inv = FindObjectOfType<HolenInventoryManager>();

        if (inv == null || bottomPlayerInventoryContent == null) return;

        var allHolens = inv.GetAllHolens();

        foreach (var inventoryEntry in allHolens)
        {
            HolenData holenData = inv.GetHolenData(inventoryEntry.holenID);
            if (holenData == null)
            {
                Debug.LogWarning($"[INVENTORY] Could not find HolenData for ID: {inventoryEntry.holenID}");
                continue;
            }

            GameObject newSlot = Instantiate(holenUISlotPrefab, bottomPlayerInventoryContent.transform);
            var holenUISlot = newSlot.GetComponent<HolenSlotUI>();

            if (holenUISlot != null)
            {
                holenUISlot.SetSlot(holenData, inventoryEntry.quantity);

                Button btn = newSlot.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();

                    HolenData capturedData = holenData;
                    int capturedQty = inventoryEntry.quantity;
                    btn.onClick.AddListener(() => OnLocalInventoryItemClicked(capturedData, capturedQty));
                }
            }
        }

        Debug.Log($"[INVENTORY] Setup {allHolens.Count} holens for local player (bottom)");
    }

    private void OnLocalInventoryItemClicked(HolenData holenData, int quantity)
    {
        if (localWager == null) return;

        bool isSelected = localWager.IsHolenSelected(holenData.holenID);

        if (isSelected)
        {
            if (localWager.RemoveHolen(holenData.holenID))
            {
                RefreshLocalWagerDisplay();
                Debug.Log($"[WAGER] Removed {holenData.holenName} from wager");
            }
        }
        else
        {
            if (localWager.AddOrUpdateHolen(holenData.holenID, quantity, holenData))
            {
                RefreshLocalWagerDisplay();
                Debug.Log($"[WAGER] Added {holenData.holenName} to wager");
            }
        }
    }

    private void RefreshLocalWagerDisplay()
    {
        // Local player wager is always on bottom
        if (localWager == null || bottomPlayerWagerContent == null) return;

        foreach (Transform child in bottomPlayerWagerContent.transform)
        {
            Destroy(child.gameObject);
        }

        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null) return;

        var selectedHolens = localWager.GetSelectedHolensCopy();
        foreach (var record in selectedHolens)
        {
            HolenData data = inv.GetHolenData(record.holenID);
            if (data != null)
            {
                GameObject newSlot = Instantiate(holenUISlotPrefab, bottomPlayerWagerContent.transform);
                var holenUISlot = newSlot.GetComponent<HolenSlotUI>();

                if (holenUISlot != null)
                {
                    holenUISlot.SetSlot(data, record.quantity);

                    Button btn = newSlot.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        string capturedID = record.holenID;
                        btn.onClick.AddListener(() => OnWagerItemClicked(capturedID));
                    }
                }
            }
        }
    }

    private void OnWagerItemClicked(string holenID)
    {
        if (localWager == null) return;

        if (localWager.RemoveHolen(holenID))
        {
            RefreshLocalWagerDisplay();
        }
    }

    private WagerManager CreateWagerManager(
        GameObject wagerContent,
        Button actionButton,
        TMP_Text stateText,
        TMP_Text countdownText,
        TMP_Text pointsText,
        bool isLocal)
    {
        GameObject managerObj = new GameObject($"WagerManager_P{localPlayerNumber}_{(isLocal ? "Local" : "Remote")}");
        managerObj.transform.SetParent(transform);

        WagerManager manager = managerObj.AddComponent<WagerManager>();
        manager.wagerContent = wagerContent;
        manager.holenUISlotPrefab = holenUISlotPrefab;
        manager.actionButton = actionButton; // Can be null now
        manager.stateText = stateText;
        manager.countdownText = countdownText;
        manager.player1PointsText = pointsText;

        Debug.Log($"[CREATE] Created WagerManager ({(isLocal ? "Local" : "Remote")})");

        if (isLocal)
        {
            manager.OnPointsChanged = (points) => OnLocalPlayerPointsChanged(points);
            manager.OnWagerSelectionChanged = () => SendWagerSelectionToOpponent();

            Debug.Log($"[CALLBACK] All callbacks connected for Player {localPlayerNumber}");
        }

        return manager;
    }

    private void SendInventoryToOpponent()
    {
        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null) return;

        var allHolens = inv.GetAllHolens();
        if (allHolens == null || allHolens.Count == 0) return;

        List<string> holenIDs = new List<string>();
        List<int> quantities = new List<int>();

        foreach (var inventoryEntry in allHolens)
        {
            holenIDs.Add(inventoryEntry.holenID);
            quantities.Add(inventoryEntry.quantity);
        }

        Debug.Log($"[INVENTORY SYNC] 📤 Sending {holenIDs.Count} holens to opponent");

        object[] content = new object[] { localPlayerNumber, holenIDs.ToArray(), quantities.ToArray() };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = SendOptions.SendReliable;

        PhotonNetwork.RaiseEvent(INVENTORY_SYNC_EVENT, content, raiseEventOptions, sendOptions);
    }

    private void OnLocalPlayerPointsChanged(int newPoints)
    {
        Debug.Log($"[POINTS] Player {localPlayerNumber} points changed to: {newPoints}");

        object[] content = new object[] { localPlayerNumber, newPoints };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = SendOptions.SendReliable;

        PhotonNetwork.RaiseEvent(WAGER_UPDATE_EVENT, content, raiseEventOptions, sendOptions);
    }

    private void SendWagerSelectionToOpponent()
    {
        if (localWager == null) return;

        var selectedHolens = localWager.GetSelectedHolensCopy();

        List<string> holenIDs = new List<string>();
        List<int> quantities = new List<int>();

        foreach (var record in selectedHolens)
        {
            holenIDs.Add(record.holenID);
            quantities.Add(record.quantity);
        }

        Debug.Log($"[WAGER SYNC] 📤 Sending {holenIDs.Count} selected holens to opponent");

        object[] content = new object[] { localPlayerNumber, holenIDs.ToArray(), quantities.ToArray() };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = SendOptions.SendReliable;

        PhotonNetwork.RaiseEvent(WAGER_SELECTION_EVENT, content, raiseEventOptions, sendOptions);
    }

    private void UpdatePlayerNames()
    {
        Player[] players = PhotonNetwork.PlayerList;

        Player localPlayer = PhotonNetwork.LocalPlayer;
        Player opponentPlayer = System.Array.Find(players, p => p.ActorNumber != localPlayerNumber);

        // Local player name always on bottom
        if (localPlayer != null && bottomPlayerNameText != null)
        {
            string displayName = string.IsNullOrEmpty(localPlayer.NickName) ? $"Player {localPlayer.ActorNumber}" : localPlayer.NickName;
            bottomPlayerNameText.text = displayName;
            Debug.Log($"[NAMES] Local player (bottom) name set to: {displayName}");
        }

        // Opponent name always on top
        if (opponentPlayer != null && topPlayerNameText != null)
        {
            string displayName = string.IsNullOrEmpty(opponentPlayer.NickName) ? $"Player {opponentPlayer.ActorNumber}" : opponentPlayer.NickName;
            topPlayerNameText.text = displayName;
            Debug.Log($"[NAMES] Opponent (top) name set to: {displayName}");
        }
    }

    private void SaveLocalWagerToManager()
    {
        if (WagerDataManager.Instance == null)
        {
            GameObject wagerDataObj = new GameObject("WagerDataManager");
            wagerDataObj.AddComponent<WagerDataManager>();
        }

        if (localWager != null)
        {
            var wagers = localWager.GetSelectedHolensCopy();
            WagerDataManager.Instance.SetPlayerWager(localPlayerNumber, wagers);
            Debug.Log($"[SAVE WAGER] 💾 Saved Player {localPlayerNumber} wager locally: {wagers.Count} holens");

            List<string> holenIDs = new List<string>();
            List<int> quantities = new List<int>();

            foreach (var record in wagers)
            {
                holenIDs.Add(record.holenID);
                quantities.Add(record.quantity);
            }

            object[] content = new object[] { localPlayerNumber, holenIDs.ToArray(), quantities.ToArray() };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendOptions sendOptions = SendOptions.SendReliable;

            PhotonNetwork.RaiseEvent(SAVE_WAGER_EVENT, content, raiseEventOptions, sendOptions);
            Debug.Log($"[SAVE WAGER] 📤 Sent wager data to opponent for saving");
        }
    }

    private bool GetPlayerReadyState(int playerNum)
    {
        return playerNum == 1 ? isPlayer1Ready : isPlayer2Ready;
    }

    private void SetPlayerReadyState(int playerNum, bool ready)
    {
        if (playerNum == 1)
            isPlayer1Ready = ready;
        else
            isPlayer2Ready = ready;
    }

    private void CheckBothPlayersReady()
    {
        Debug.Log($"[CHECK] Ready states - P1: {isPlayer1Ready}, P2: {isPlayer2Ready}");

        if (isPlayer1Ready && isPlayer2Ready)
        {
            Debug.Log("[GAME START] Both players ready! Starting game...");

            if (PhotonNetwork.IsMasterClient)
            {
                transitionObject.SetActive(true);
                Invoke(nameof(LoadGameScene), 2f);
            }
        }
    }

    private void LoadGameScene()
    {
        // IMPORTANT: Deduct holens BEFORE scene transition for ALL players
        DeductWageredHolensFromInventory();

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[LOADING] Master client loading scene: {gameSceneName}");

            if (WagerDataManager.Instance != null)
            {
                var p1Wagers = WagerDataManager.Instance.GetPlayerWager(1);
                var p2Wagers = WagerDataManager.Instance.GetPlayerWager(2);

                Debug.Log($"[LOADING] 🎯 Final wager check - P1: {p1Wagers.Count} holens, P2: {p2Wagers.Count} holens");
            }
            else
            {
                Debug.LogError("[LOADING] ❌ WagerDataManager not found!");
            }

            PhotonNetwork.LoadLevel(gameSceneName);
        }
    }

    /// <summary>
    /// Deducts the wagered holens from the local player's inventory.
    /// Called by BOTH players before scene transition.
    /// </summary>
    private void DeductWageredHolensFromInventory()
    {
        var inventoryManager = HolenInventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("[DEDUCT] ❌ HolenInventoryManager not found!");
            return;
        }

        if (localWager == null)
        {
            Debug.LogError("[DEDUCT] ❌ Local WagerManager not found!");
            return;
        }

        var wageredHolens = localWager.GetSelectedHolensCopy();
        if (wageredHolens == null || wageredHolens.Count == 0)
        {
            Debug.LogWarning("[DEDUCT] ⚠️ No holens wagered by local player");
            return;
        }

        Debug.Log($"[DEDUCT] 💰 Starting to deduct {wageredHolens.Count} wagered holens from Player {localPlayerNumber}'s inventory");

        foreach (var wager in wageredHolens)
        {
            // Deduct 1 of each wagered holen from inventory
            inventoryManager.RemoveHolen(wager.holenID, 1);

            HolenData data = inventoryManager.GetHolenData(wager.holenID);
            string holenName = data != null ? data.holenName : wager.holenID;

            Debug.Log($"[DEDUCT] ✅ Removed 1x {holenName} from inventory");
        }

        // Save the updated inventory
        inventoryManager.SaveInventory();

        Debug.Log($"[DEDUCT] 💾 Player {localPlayerNumber}'s inventory updated and saved");
    }

    private void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;

        if (eventCode == READY_STATE_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            int playerNum = (int)data[0];
            bool readyState = (bool)data[1];

            Debug.Log($"[NETWORK] Received ready state - Player {playerNum}: {readyState}");

            SetPlayerReadyState(playerNum, readyState);
            UpdateOpponentStateText(readyState);
            CheckBothPlayersReady();
        }
        else if (eventCode == WAGER_UPDATE_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            int playerNum = (int)data[0];
            int points = (int)data[1];

            Debug.Log($"[NETWORK] Player {playerNum} updated points to: {points}");
            UpdateOpponentPoints(points);
        }
        else if (eventCode == WAGER_SELECTION_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            int playerNum = (int)data[0];
            string[] holenIDs = (string[])data[1];
            int[] quantities = (int[])data[2];

            Debug.Log($"[NETWORK] ⭐ Received wager selection from Player {playerNum}: {holenIDs.Length} holens");

            if (playerNum != localPlayerNumber)
            {
                UpdateOpponentWagerDisplay(holenIDs, quantities);
            }
        }
        else if (eventCode == INVENTORY_SYNC_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            int playerNum = (int)data[0];
            string[] holenIDs = (string[])data[1];
            int[] quantities = (int[])data[2];

            Debug.Log($"[NETWORK] 📥 Received inventory from Player {playerNum}: {holenIDs.Length} holens");

            if (playerNum != localPlayerNumber)
            {
                UpdateOpponentInventoryDisplay(holenIDs, quantities);
            }
        }
        else if (eventCode == SAVE_WAGER_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            int playerNum = (int)data[0];
            string[] holenIDs = (string[])data[1];
            int[] quantities = (int[])data[2];

            Debug.Log($"[NETWORK] 💾 Received wager save request from Player {playerNum}: {holenIDs.Length} holens");

            if (WagerDataManager.Instance == null)
            {
                GameObject wagerDataObj = new GameObject("WagerDataManager");
                wagerDataObj.AddComponent<WagerDataManager>();
            }

            List<WagerManager.SelectedHolenRecord> wagers = new List<WagerManager.SelectedHolenRecord>();
            for (int i = 0; i < holenIDs.Length; i++)
            {
                wagers.Add(new WagerManager.SelectedHolenRecord(holenIDs[i], quantities[i]));
            }

            WagerDataManager.Instance.SetPlayerWager(playerNum, wagers);
            Debug.Log($"[NETWORK] ✅ Saved Player {playerNum}'s wager to WagerDataManager: {wagers.Count} holens");
        }
    }

    private void UpdateOpponentInventoryDisplay(string[] holenIDs, int[] quantities)
    {
        // Opponent inventory always goes to top
        if (topPlayerInventoryContent == null) return;

        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null) return;

        foreach (Transform child in topPlayerInventoryContent.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < holenIDs.Length; i++)
        {
            HolenData data = inv.GetHolenData(holenIDs[i]);
            if (data != null)
            {
                GameObject newSlot = Instantiate(holenUISlotPrefab, topPlayerInventoryContent.transform);
                var holenUISlot = newSlot.GetComponent<HolenSlotUI>();

                if (holenUISlot != null)
                {
                    holenUISlot.SetSlot(data, quantities[i]);

                    Button btn = newSlot.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = false;
                    }
                }
            }
        }

        Debug.Log($"[INVENTORY SYNC] ✅ Updated opponent's inventory (top) with {holenIDs.Length} items");
    }

    private void UpdateOpponentStateText(bool isReady)
    {
        string stateLabel = isReady ? "READY" : "CANCEL";

        // Opponent state always on top
        if (topPlayerStateText != null)
        {
            topPlayerStateText.text = stateLabel;
        }
    }

    private void UpdateOpponentPoints(int points)
    {
        // Opponent points always on top
        if (topPlayerPointsText != null)
        {
            topPlayerPointsText.text = $"{points}";
        }
    }

    private void UpdateOpponentWagerDisplay(string[] holenIDs, int[] quantities)
    {
        // Opponent wager always on top
        if (topPlayerWagerContent == null) return;

        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null) return;

        foreach (Transform child in topPlayerWagerContent.transform)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"[WAGER SYNC] Displaying {holenIDs.Length} holens for opponent (top)");

        for (int i = 0; i < holenIDs.Length; i++)
        {
            HolenData data = inv.GetHolenData(holenIDs[i]);
            if (data != null)
            {
                GameObject newSlot = Instantiate(holenUISlotPrefab, topPlayerWagerContent.transform);
                var holenUISlot = newSlot.GetComponent<HolenSlotUI>();

                if (holenUISlot != null)
                {
                    holenUISlot.SetSlot(data, quantities[i]);

                    Button btn = newSlot.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = false;
                    }
                }
            }
        }

        Debug.Log($"[WAGER SYNC] ✅ Updated opponent's wager (top) with {holenIDs.Length} items");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogWarning($"[DISCONNECT] Opponent left the room!");
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[DISCONNECT] Left room, returning to menu...");

        // Clean up WagerDataManager before returning to menu
        CleanupWagerDataManager();

        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"[DISCONNECT] Disconnected from Photon: {cause}");

        // Clean up WagerDataManager before returning to menu
        CleanupWagerDataManager();

        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }

    /// <summary>
    /// Cleans up the WagerDataManager singleton to prevent it from persisting
    /// </summary>
    private void CleanupWagerDataManager()
    {
        if (WagerDataManager.Instance != null)
        {
            Debug.Log("[CLEANUP] Destroying WagerDataManager instance");
            Destroy(WagerDataManager.Instance.gameObject);
        }
    }
}