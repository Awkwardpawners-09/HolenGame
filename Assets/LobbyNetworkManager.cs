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
    public int localPlayerNumber = 0; // 1 or 2, assigned when both players connect

    [Header("Player 1 UI References")]
    public GameObject player1InventoryContent;
    public GameObject player1WagerContent;
    public TMP_Text player1NameText;
    public TMP_Text player1PointsText;
    public TMP_Text player1StateText;
    public Button player1ActionButton;
    public TMP_Text player1CountdownText;

    [Header("Player 2 UI References")]
    public GameObject player2InventoryContent;
    public GameObject player2WagerContent;
    public TMP_Text player2NameText;
    public TMP_Text player2PointsText;
    public TMP_Text player2StateText;
    public Button player2ActionButton;
    public TMP_Text player2CountdownText;

    [Header("Prefab References")]
    public GameObject holenUISlotPrefab;

    [Header("Settings")]
    public string gameSceneName = "GameScene";

    [Header("Waiting UI (Optional)")]
    public GameObject waitingForPlayerPanel;

    private LobbyUIManager player1LobbyUI;
    private LobbyUIManager player2LobbyUI;
    private WagerManager player1Wager;
    private WagerManager player2Wager;

    private bool isPlayer1Ready = false;
    private bool isPlayer2Ready = false;
    private bool isInitialized = false;

    private const byte READY_STATE_EVENT = 1;
    private const byte WAGER_UPDATE_EVENT = 2;
    private const byte WAGER_SELECTION_EVENT = 3;
    private const byte INVENTORY_SYNC_EVENT = 4; // NEW: For syncing inventory

    void Start()
    {
        Debug.Log($"LobbyNetworkManager Start. Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");

        // Show waiting panel if only 1 player
        if (waitingForPlayerPanel != null)
        {
            waitingForPlayerPanel.SetActive(PhotonNetwork.CurrentRoom.PlayerCount < 2);
        }

        // Only initialize if both players are present
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            // Small delay to ensure both clients have loaded the scene
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

        // Hide waiting panel
        if (waitingForPlayerPanel != null)
        {
            waitingForPlayerPanel.SetActive(false);
        }

        // Initialize when second player joins
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && !isInitialized)
        {
            // Small delay to ensure both clients are ready
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

        // Assign player numbers based on ActorNumber
        localPlayerNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        Debug.Log($"[INIT] Local player assigned as Player {localPlayerNumber} (ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber})");

        // Display player names first
        UpdatePlayerNames();

        // Set up BOTH players' UI managers (local and opponent)
        SetupLocalPlayerUI();
        SetupOpponentUI();

        Debug.Log($"[INIT] Lobby initialization complete for Player {localPlayerNumber}");

        // Send inventory to opponent after a short delay to ensure they're ready
        Invoke(nameof(SendInventoryToOpponent), 0.5f);
    }

    private void SetupLocalPlayerUI()
    {
        if (localPlayerNumber == 1)
        {
            Debug.Log("[SETUP] Setting up Player 1 (local) UI");

            // This player controls Player 1 UI (top section)
            player1LobbyUI = CreateLobbyUIManager(player1InventoryContent, true);
            player1Wager = CreateWagerManager(
                player1WagerContent,
                player1ActionButton,
                player1StateText,
                player1CountdownText,
                player1PointsText,
                true
            );

            // Disable Player 2 button (opponent)
            if (player2ActionButton != null)
            {
                player2ActionButton.interactable = false;
                Debug.Log("[SETUP] Player 2 button disabled (opponent)");
            }
        }
        else if (localPlayerNumber == 2)
        {
            Debug.Log("[SETUP] Setting up Player 2 (local) UI");

            // This player controls Player 2 UI (bottom section)
            player2LobbyUI = CreateLobbyUIManager(player2InventoryContent, true);
            player2Wager = CreateWagerManager(
                player2WagerContent,
                player2ActionButton,
                player2StateText,
                player2CountdownText,
                player2PointsText,
                true
            );

            // Disable Player 1 button (opponent)
            if (player1ActionButton != null)
            {
                player1ActionButton.interactable = false;
                Debug.Log("[SETUP] Player 1 button disabled (opponent)");
            }
        }
    }

    private void SetupOpponentUI()
    {
        // Create LobbyUIManager and WagerManager for opponent's display
        if (localPlayerNumber == 1)
        {
            Debug.Log("[SETUP] Setting up Player 2 (opponent) UI - read-only");

            // Create opponent's inventory display
            player2LobbyUI = CreateLobbyUIManager(player2InventoryContent, false);

            // Create opponent's wager manager for display purposes
            player2Wager = CreateWagerManager(
                player2WagerContent,
                player2ActionButton,
                player2StateText,
                player2CountdownText,
                player2PointsText,
                false // Not local, read-only
            );
        }
        else if (localPlayerNumber == 2)
        {
            Debug.Log("[SETUP] Setting up Player 1 (opponent) UI - read-only");

            // Create opponent's inventory display
            player1LobbyUI = CreateLobbyUIManager(player1InventoryContent, false);

            // Create opponent's wager manager for display purposes
            player1Wager = CreateWagerManager(
                player1WagerContent,
                player1ActionButton,
                player1StateText,
                player1CountdownText,
                player1PointsText,
                false // Not local, read-only
            );
        }
    }

    private LobbyUIManager CreateLobbyUIManager(GameObject contentScrollView, bool isLocal)
    {
        GameObject managerObj = new GameObject($"LobbyUIManager_P{localPlayerNumber}_{(isLocal ? "Local" : "Remote")}");
        managerObj.transform.SetParent(transform);

        LobbyUIManager manager = managerObj.AddComponent<LobbyUIManager>();
        manager.contentScrollView = contentScrollView;
        manager.holenUISlotPrefab = holenUISlotPrefab;

        Debug.Log($"[CREATE] Created LobbyUIManager for Player {localPlayerNumber} ({(isLocal ? "Local" : "Remote")})");

        return manager;
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
        manager.actionButton = actionButton;
        manager.stateText = stateText;
        manager.countdownText = countdownText;
        manager.player1PointsText = pointsText;

        Debug.Log($"[CREATE] Created WagerManager for Player {localPlayerNumber} ({(isLocal ? "Local" : "Remote")})");

        // If this is the local player's wager manager, hook up the button and callbacks
        if (isLocal && actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners(); // Clear any existing listeners
            actionButton.onClick.AddListener(() => OnLocalPlayerReady(manager));
            Debug.Log($"[BUTTON] Ready button connected for Player {localPlayerNumber}");

            // Hook up points change callback for network sync
            manager.OnPointsChanged = (points) => OnLocalPlayerPointsChanged(points);
            Debug.Log($"[CALLBACK] Points change callback connected for Player {localPlayerNumber}");
        }

        return manager;
    }

    private void SendInventoryToOpponent()
    {
        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null)
        {
            Debug.LogError("[INVENTORY SYNC] HolenInventoryManager not found");
            return;
        }

        // Get all holens from inventory
        var allHolens = inv.GetAllHolens(); // You may need to add this method to HolenInventoryManager
        if (allHolens == null || allHolens.Count == 0)
        {
            Debug.LogWarning("[INVENTORY SYNC] No holens in inventory to send");
            return;
        }

        // Convert to serializable format
        List<string> holenIDs = new List<string>();
        List<int> quantities = new List<int>();

        foreach (var holen in allHolens)
        {
            holenIDs.Add(holen.holenID);
            quantities.Add(holen.quantity);
        }

        Debug.Log($"[INVENTORY SYNC] 📤 Sending {holenIDs.Count} holens to opponent");

        // Send the inventory data
        object[] content = new object[] { localPlayerNumber, holenIDs.ToArray(), quantities.ToArray() };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = SendOptions.SendReliable;

        bool success = PhotonNetwork.RaiseEvent(INVENTORY_SYNC_EVENT, content, raiseEventOptions, sendOptions);
        Debug.Log($"[INVENTORY SYNC] Event send result: {success}");
    }

    private void OnLocalPlayerPointsChanged(int newPoints)
    {
        Debug.Log($"[POINTS] Player {localPlayerNumber} points changed to: {newPoints}");

        // Send points update to opponent
        object[] content = new object[] { localPlayerNumber, newPoints };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = SendOptions.SendReliable;

        PhotonNetwork.RaiseEvent(WAGER_UPDATE_EVENT, content, raiseEventOptions, sendOptions);

        // Also send the full wager selection
        SendWagerSelectionToOpponent();
    }

    private void SendWagerSelectionToOpponent()
    {
        WagerManager localWager = GetLocalWagerManager();
        if (localWager == null)
        {
            Debug.LogWarning("[WAGER SYNC] Local wager manager not found");
            return;
        }

        // Get the selected holens
        var selectedHolens = localWager.GetSelectedHolensCopy();

        // Convert to serializable format
        List<string> holenIDs = new List<string>();
        List<int> quantities = new List<int>();

        foreach (var record in selectedHolens)
        {
            holenIDs.Add(record.holenID);
            quantities.Add(record.quantity);
        }

        Debug.Log($"[WAGER SYNC] 📤 Sending {holenIDs.Count} selected holens to opponent");

        // Send the wager data
        object[] content = new object[] { localPlayerNumber, holenIDs.ToArray(), quantities.ToArray() };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = SendOptions.SendReliable;

        bool success = PhotonNetwork.RaiseEvent(WAGER_SELECTION_EVENT, content, raiseEventOptions, sendOptions);
        Debug.Log($"[WAGER SYNC] Event send result: {success}");
    }

    private WagerManager GetLocalWagerManager()
    {
        return localPlayerNumber == 1 ? player1Wager : player2Wager;
    }

    private WagerManager GetOpponentWagerManager()
    {
        return localPlayerNumber == 1 ? player2Wager : player1Wager;
    }

    private LobbyUIManager GetOpponentLobbyUIManager()
    {
        return localPlayerNumber == 1 ? player2LobbyUI : player1LobbyUI;
    }

    private void UpdatePlayerNames()
    {
        Player[] players = PhotonNetwork.PlayerList;

        Debug.Log($"[NAMES] Updating player names. Total players: {players.Length}");

        // Find players by ActorNumber
        Player p1 = System.Array.Find(players, p => p.ActorNumber == 1);
        Player p2 = System.Array.Find(players, p => p.ActorNumber == 2);

        if (p1 != null && player1NameText != null)
        {
            string displayName = string.IsNullOrEmpty(p1.NickName) ? $"Player {p1.ActorNumber}" : p1.NickName;
            player1NameText.text = displayName;
            Debug.Log($"[NAMES] Player 1 name set to: {displayName}");
        }

        if (p2 != null && player2NameText != null)
        {
            string displayName = string.IsNullOrEmpty(p2.NickName) ? $"Player {p2.ActorNumber}" : p2.NickName;
            player2NameText.text = displayName;
            Debug.Log($"[NAMES] Player 2 name set to: {displayName}");
        }
    }

    private void OnLocalPlayerReady(WagerManager wagerManager)
    {
        // Get current ready state
        bool currentReadyState = GetPlayerReadyState(localPlayerNumber);
        bool newReadyState = !currentReadyState;

        Debug.Log($"[READY] Player {localPlayerNumber} toggling ready: {currentReadyState} -> {newReadyState}");

        // Update the wager manager's internal state
        wagerManager.OnActionButtonPressed();

        // Update local state
        SetPlayerReadyState(localPlayerNumber, newReadyState);

        // Send ready state to other player via Photon
        object[] content = new object[] { localPlayerNumber, newReadyState };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = SendOptions.SendReliable;

        bool success = PhotonNetwork.RaiseEvent(READY_STATE_EVENT, content, raiseEventOptions, sendOptions);
        Debug.Log($"[NETWORK] Ready state event sent: {success}");

        // Check if both players are ready
        CheckBothPlayersReady();
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

        Debug.Log($"[STATE] Player {playerNum} ready state: {ready}");
    }

    private void CheckBothPlayersReady()
    {
        Debug.Log($"[CHECK] Ready states - P1: {isPlayer1Ready}, P2: {isPlayer2Ready}");

        if (isPlayer1Ready && isPlayer2Ready)
        {
            Debug.Log("[GAME START] Both players ready! Starting game...");

            // Only master client loads the scene to prevent duplicate loads
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[MASTER] Master client will load game scene in 2 seconds...");
                Invoke(nameof(LoadGameScene), 2f);
            }
            else
            {
                Debug.Log("[CLIENT] Waiting for master client to load scene...");
            }
        }
    }

    private void LoadGameScene()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[LOADING] Master client loading scene: {gameSceneName}");

            // Create WagerDataManager if it doesn't exist
            if (WagerDataManager.Instance == null)
            {
                GameObject wagerDataObj = new GameObject("WagerDataManager");
                wagerDataObj.AddComponent<WagerDataManager>();
            }

            // Store both players' wager data
            if (player1Wager != null)
            {
                var p1Wagers = player1Wager.GetSelectedHolensCopy();
                WagerDataManager.Instance.SetPlayerWager(1, p1Wagers);
                Debug.Log($"[LOADING] Saved Player 1 wager: {p1Wagers.Count} holens");
            }

            if (player2Wager != null)
            {
                var p2Wagers = player2Wager.GetSelectedHolensCopy();
                WagerDataManager.Instance.SetPlayerWager(2, p2Wagers);
                Debug.Log($"[LOADING] Saved Player 2 wager: {p2Wagers.Count} holens");
            }

            PhotonNetwork.LoadLevel(gameSceneName);
        }
    }

    private void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;

        if (eventCode == READY_STATE_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            int playerNum = (int)data[0];
            bool readyState = (bool)data[1];

            Debug.Log($"[NETWORK] Received ready state event - Player {playerNum}: {readyState}");

            SetPlayerReadyState(playerNum, readyState);

            // Update opponent's state text
            UpdateOpponentStateText(playerNum, readyState);

            CheckBothPlayersReady();
        }
        else if (eventCode == WAGER_UPDATE_EVENT)
        {
            // Handle points updates from opponent
            object[] data = (object[])photonEvent.CustomData;
            int playerNum = (int)data[0];
            int points = (int)data[1];

            Debug.Log($"[NETWORK] Player {playerNum} updated points to: {points}");

            // Update opponent's points display
            UpdateOpponentPoints(playerNum, points);
        }
        else if (eventCode == WAGER_SELECTION_EVENT)
        {
            // Handle wager selection updates from opponent
            object[] data = (object[])photonEvent.CustomData;
            int playerNum = (int)data[0];
            string[] holenIDs = (string[])data[1];
            int[] quantities = (int[])data[2];

            Debug.Log($"[NETWORK] ⭐ Received wager selection from Player {playerNum}: {holenIDs.Length} holens");

            if (playerNum != localPlayerNumber)
            {
                UpdateOpponentWagerDisplay(playerNum, holenIDs, quantities);
            }
        }
        else if (eventCode == INVENTORY_SYNC_EVENT)
        {
            // Handle inventory sync from opponent
            object[] data = (object[])photonEvent.CustomData;
            int playerNum = (int)data[0];
            string[] holenIDs = (string[])data[1];
            int[] quantities = (int[])data[2];

            Debug.Log($"[NETWORK] 📥 Received inventory from Player {playerNum}: {holenIDs.Length} holens");

            if (playerNum != localPlayerNumber)
            {
                UpdateOpponentInventoryDisplay(playerNum, holenIDs, quantities);
            }
        }
    }

    private void UpdateOpponentInventoryDisplay(int playerNum, string[] holenIDs, int[] quantities)
    {
        LobbyUIManager opponentLobbyUI = GetOpponentLobbyUIManager();
        if (opponentLobbyUI == null)
        {
            Debug.LogWarning("[INVENTORY SYNC] Opponent LobbyUI manager not found");
            return;
        }

        // Get inventory manager to look up holen data
        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null)
        {
            Debug.LogError("[INVENTORY SYNC] HolenInventoryManager not found");
            return;
        }

        // Get opponent's inventory content
        GameObject inventoryContent = opponentLobbyUI.contentScrollView;
        if (inventoryContent == null)
        {
            Debug.LogError("[INVENTORY SYNC] Opponent inventory content is NULL!");
            return;
        }

        Debug.Log($"[INVENTORY SYNC] Clearing opponent's inventory (had {inventoryContent.transform.childCount} items)");

        // Clear existing inventory
        foreach (Transform child in inventoryContent.transform)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"[INVENTORY SYNC] Displaying {holenIDs.Length} holens for Player {playerNum}");

        // Display each holen
        for (int i = 0; i < holenIDs.Length; i++)
        {
            string holenID = holenIDs[i];
            int quantity = quantities[i];

            HolenData data = inv.GetHolenData(holenID);
            if (data != null)
            {
                // Create UI slot for opponent's inventory
                GameObject newSlot = Instantiate(holenUISlotPrefab, inventoryContent.transform);
                var holenUISlot = newSlot.GetComponent<HolenSlotUI>();
                if (holenUISlot != null)
                {
                    holenUISlot.SetSlot(data, quantity);

                    // Disable the button on opponent's slots
                    Button btn = newSlot.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = false;
                    }

                    Debug.Log($"[INVENTORY SYNC] ✅ Displayed opponent's holen: {data.holenName} x{quantity}");
                }
            }
            else
            {
                Debug.LogWarning($"[INVENTORY SYNC] Could not find HolenData for ID: {holenID}");
            }
        }

        Debug.Log($"[INVENTORY SYNC] ✅ Successfully updated Player {playerNum}'s inventory with {holenIDs.Length} items");
    }

    private void UpdateOpponentStateText(int playerNum, bool isReady)
    {
        string stateLabel = isReady ? "READY" : "CANCEL";

        if (playerNum == 1 && player1StateText != null)
        {
            player1StateText.text = stateLabel;
            Debug.Log($"[UI UPDATE] Updated Player 1 state text to: {stateLabel}");
        }
        else if (playerNum == 2 && player2StateText != null)
        {
            player2StateText.text = stateLabel;
            Debug.Log($"[UI UPDATE] Updated Player 2 state text to: {stateLabel}");
        }
    }

    private void UpdateOpponentPoints(int playerNum, int points)
    {
        if (playerNum == 1 && player1PointsText != null)
        {
            player1PointsText.text = $"{points}";
            Debug.Log($"[UI UPDATE] Updated Player 1 points to: {points}");
        }
        else if (playerNum == 2 && player2PointsText != null)
        {
            player2PointsText.text = $"{points}";
            Debug.Log($"[UI UPDATE] Updated Player 2 points to: {points}");
        }
    }

    private void UpdateOpponentWagerDisplay(int playerNum, string[] holenIDs, int[] quantities)
    {
        WagerManager opponentWager = GetOpponentWagerManager();
        if (opponentWager == null)
        {
            Debug.LogWarning("[WAGER SYNC] Opponent wager manager not found");
            return;
        }

        // Get inventory manager to look up holen data
        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null)
        {
            Debug.LogError("[WAGER SYNC] HolenInventoryManager not found");
            return;
        }

        // Clear existing wager content for opponent
        GameObject wagerContent = opponentWager.wagerContent;
        if (wagerContent == null)
        {
            Debug.LogError("[WAGER SYNC] Opponent wager content is NULL!");
            return;
        }

        Debug.Log($"[WAGER SYNC] Clearing opponent's wager display (had {wagerContent.transform.childCount} items)");

        // Clear all children
        foreach (Transform child in wagerContent.transform)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"[WAGER SYNC] Displaying {holenIDs.Length} holens for Player {playerNum}");

        // Display each selected holen
        for (int i = 0; i < holenIDs.Length; i++)
        {
            string holenID = holenIDs[i];
            int quantity = quantities[i];

            HolenData data = inv.GetHolenData(holenID);
            if (data != null)
            {
                // Create UI slot for opponent's selection
                GameObject newSlot = Instantiate(holenUISlotPrefab, wagerContent.transform);
                var holenUISlot = newSlot.GetComponent<HolenSlotUI>();
                if (holenUISlot != null)
                {
                    holenUISlot.SetSlot(data, quantity);

                    // Disable the button on opponent's slots
                    Button btn = newSlot.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = false;
                    }

                    Debug.Log($"[WAGER SYNC] ✅ Displayed opponent's holen: {data.holenName} x{quantity}");
                }
                else
                {
                    Debug.LogError($"[WAGER SYNC] HolenSlotUI component missing on prefab!");
                }
            }
            else
            {
                Debug.LogWarning($"[WAGER SYNC] Could not find HolenData for ID: {holenID}");
            }
        }

        Debug.Log($"[WAGER SYNC] ✅ Successfully updated Player {playerNum}'s wager display with {holenIDs.Length} items");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogWarning($"[DISCONNECT] Opponent left the room! {otherPlayer.NickName} (Actor {otherPlayer.ActorNumber})");

        // Show a message or return to menu
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[DISCONNECT] Left room, returning to menu...");
        // Return to menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"[DISCONNECT] Disconnected from Photon: {cause}");
        // Return to menu
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }
}