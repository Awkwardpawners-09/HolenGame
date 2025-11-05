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

    private WagerManager player1Wager;
    private WagerManager player2Wager;

    private bool isPlayer1Ready = false;
    private bool isPlayer2Ready = false;
    private bool isInitialized = false;

    private const byte READY_STATE_EVENT = 1;
    private const byte WAGER_UPDATE_EVENT = 2;
    private const byte WAGER_SELECTION_EVENT = 3;
    private const byte INVENTORY_SYNC_EVENT = 4;

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
        SetupLocalPlayerUI();
        SetupOpponentUI();
        SetupInventories();

        Debug.Log($"[INIT] Lobby initialization complete for Player {localPlayerNumber}");

        Invoke(nameof(SendInventoryToOpponent), 0.5f);
    }

    private void SetupLocalPlayerUI()
    {
        if (localPlayerNumber == 1)
        {
            Debug.Log("[SETUP] Setting up Player 1 (local) UI");

            player1Wager = CreateWagerManager(
                player1WagerContent,
                player1ActionButton,
                player1StateText,
                player1CountdownText,
                player1PointsText,
                true
            );

            if (player2ActionButton != null)
            {
                player2ActionButton.interactable = false;
            }
        }
        else if (localPlayerNumber == 2)
        {
            Debug.Log("[SETUP] Setting up Player 2 (local) UI");

            player2Wager = CreateWagerManager(
                player2WagerContent,
                player2ActionButton,
                player2StateText,
                player2CountdownText,
                player2PointsText,
                true
            );

            if (player1ActionButton != null)
            {
                player1ActionButton.interactable = false;
            }
        }
    }

    private void SetupOpponentUI()
    {
        if (localPlayerNumber == 1)
        {
            Debug.Log("[SETUP] Setting up Player 2 (opponent) UI - read-only");

            player2Wager = CreateWagerManager(
                player2WagerContent,
                player2ActionButton,
                player2StateText,
                player2CountdownText,
                player2PointsText,
                false
            );
        }
        else if (localPlayerNumber == 2)
        {
            Debug.Log("[SETUP] Setting up Player 1 (opponent) UI - read-only");

            player1Wager = CreateWagerManager(
                player1WagerContent,
                player1ActionButton,
                player1StateText,
                player1CountdownText,
                player1PointsText,
                false
            );
        }
    }

    private void SetupInventories()
    {
        // Setup local player's inventory with click handlers
        GameObject localInventoryContent = localPlayerNumber == 1 ? player1InventoryContent : player2InventoryContent;
        var inv = FindObjectOfType<HolenInventoryManager>();

        if (inv == null || localInventoryContent == null) return;

        var allHolens = inv.GetAllHolens(); // Returns List<HolenInventoryEntry>

        foreach (var inventoryEntry in allHolens)
        {
            // Get the actual HolenData from the inventory entry's ID
            HolenData holenData = inv.GetHolenData(inventoryEntry.holenID);
            if (holenData == null)
            {
                Debug.LogWarning($"[INVENTORY] Could not find HolenData for ID: {inventoryEntry.holenID}");
                continue;
            }

            GameObject newSlot = Instantiate(holenUISlotPrefab, localInventoryContent.transform);
            var holenUISlot = newSlot.GetComponent<HolenSlotUI>();

            if (holenUISlot != null)
            {
                holenUISlot.SetSlot(holenData, inventoryEntry.quantity);

                // Add click handler
                Button btn = newSlot.GetComponent<Button>();
                if (btn != null)
                {
                    HolenData capturedData = holenData;
                    int capturedQty = inventoryEntry.quantity;
                    btn.onClick.AddListener(() => OnLocalInventoryItemClicked(capturedData, capturedQty));
                }
            }
        }

        Debug.Log($"[INVENTORY] Setup {allHolens.Count} holens for local player");
    }

    private void OnLocalInventoryItemClicked(HolenData holenData, int quantity)
    {
        WagerManager localWager = GetLocalWagerManager();
        if (localWager == null) return;

        bool isSelected = localWager.IsHolenSelected(holenData.holenID);

        if (isSelected)
        {
            // Remove from wager
            if (localWager.RemoveHolen(holenData.holenID))
            {
                RefreshLocalWagerDisplay();
                Debug.Log($"[WAGER] Removed {holenData.holenName} from wager");
            }
        }
        else
        {
            // Add to wager
            if (localWager.AddOrUpdateHolen(holenData.holenID, quantity, holenData))
            {
                RefreshLocalWagerDisplay();
                Debug.Log($"[WAGER] Added {holenData.holenName} to wager");
            }
        }
    }

    private void RefreshLocalWagerDisplay()
    {
        WagerManager localWager = GetLocalWagerManager();
        GameObject wagerContent = localPlayerNumber == 1 ? player1WagerContent : player2WagerContent;

        if (localWager == null || wagerContent == null) return;

        // Clear existing display
        foreach (Transform child in wagerContent.transform)
        {
            Destroy(child.gameObject);
        }

        // Get inventory manager
        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null) return;

        // Display all selected holens
        var selectedHolens = localWager.GetSelectedHolensCopy();
        foreach (var record in selectedHolens)
        {
            HolenData data = inv.GetHolenData(record.holenID);
            if (data != null)
            {
                GameObject newSlot = Instantiate(holenUISlotPrefab, wagerContent.transform);
                var holenUISlot = newSlot.GetComponent<HolenSlotUI>();

                if (holenUISlot != null)
                {
                    holenUISlot.SetSlot(data, record.quantity);

                    // Make it clickable to remove
                    Button btn = newSlot.GetComponent<Button>();
                    if (btn != null)
                    {
                        string capturedID = record.holenID;
                        btn.onClick.AddListener(() => OnWagerItemClicked(capturedID));
                    }
                }
            }
        }
    }

    private void OnWagerItemClicked(string holenID)
    {
        WagerManager localWager = GetLocalWagerManager();
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
        manager.actionButton = actionButton;
        manager.stateText = stateText;
        manager.countdownText = countdownText;
        manager.player1PointsText = pointsText;

        Debug.Log($"[CREATE] Created WagerManager ({(isLocal ? "Local" : "Remote")})");

        if (isLocal && actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => OnLocalPlayerReady(manager));

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

        var allHolens = inv.GetAllHolens(); // Returns List<HolenInventoryEntry>
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
        WagerManager localWager = GetLocalWagerManager();
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

    private WagerManager GetLocalWagerManager()
    {
        return localPlayerNumber == 1 ? player1Wager : player2Wager;
    }

    private WagerManager GetOpponentWagerManager()
    {
        return localPlayerNumber == 1 ? player2Wager : player1Wager;
    }

    private void UpdatePlayerNames()
    {
        Player[] players = PhotonNetwork.PlayerList;

        Player p1 = System.Array.Find(players, p => p.ActorNumber == 1);
        Player p2 = System.Array.Find(players, p => p.ActorNumber == 2);

        if (p1 != null && player1NameText != null)
        {
            string displayName = string.IsNullOrEmpty(p1.NickName) ? $"Player {p1.ActorNumber}" : p1.NickName;
            player1NameText.text = displayName;
        }

        if (p2 != null && player2NameText != null)
        {
            string displayName = string.IsNullOrEmpty(p2.NickName) ? $"Player {p2.ActorNumber}" : p2.NickName;
            player2NameText.text = displayName;
        }
    }

    private void OnLocalPlayerReady(WagerManager wagerManager)
    {
        bool currentReadyState = GetPlayerReadyState(localPlayerNumber);
        bool newReadyState = !currentReadyState;

        Debug.Log($"[READY] Player {localPlayerNumber} toggling ready: {currentReadyState} -> {newReadyState}");

        wagerManager.OnActionButtonPressed();
        SetPlayerReadyState(localPlayerNumber, newReadyState);

        object[] content = new object[] { localPlayerNumber, newReadyState };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = SendOptions.SendReliable;

        PhotonNetwork.RaiseEvent(READY_STATE_EVENT, content, raiseEventOptions, sendOptions);

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
    }

    private void CheckBothPlayersReady()
    {
        Debug.Log($"[CHECK] Ready states - P1: {isPlayer1Ready}, P2: {isPlayer2Ready}");

        if (isPlayer1Ready && isPlayer2Ready)
        {
            Debug.Log("[GAME START] Both players ready! Starting game...");

            if (PhotonNetwork.IsMasterClient)
            {
                Invoke(nameof(LoadGameScene), 2f);
            }
        }
    }

    private void LoadGameScene()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[LOADING] Master client loading scene: {gameSceneName}");

            if (WagerDataManager.Instance == null)
            {
                GameObject wagerDataObj = new GameObject("WagerDataManager");
                wagerDataObj.AddComponent<WagerDataManager>();
            }

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

            Debug.Log($"[NETWORK] Received ready state - Player {playerNum}: {readyState}");

            SetPlayerReadyState(playerNum, readyState);
            UpdateOpponentStateText(playerNum, readyState);
            CheckBothPlayersReady();
        }
        else if (eventCode == WAGER_UPDATE_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            int playerNum = (int)data[0];
            int points = (int)data[1];

            Debug.Log($"[NETWORK] Player {playerNum} updated points to: {points}");
            UpdateOpponentPoints(playerNum, points);
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
                UpdateOpponentWagerDisplay(playerNum, holenIDs, quantities);
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
                UpdateOpponentInventoryDisplay(playerNum, holenIDs, quantities);
            }
        }
    }

    private void UpdateOpponentInventoryDisplay(int playerNum, string[] holenIDs, int[] quantities)
    {
        GameObject inventoryContent = playerNum == 1 ? player1InventoryContent : player2InventoryContent;

        if (inventoryContent == null) return;

        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null) return;

        // Clear existing inventory
        foreach (Transform child in inventoryContent.transform)
        {
            Destroy(child.gameObject);
        }

        // Display each holen
        for (int i = 0; i < holenIDs.Length; i++)
        {
            HolenData data = inv.GetHolenData(holenIDs[i]);
            if (data != null)
            {
                GameObject newSlot = Instantiate(holenUISlotPrefab, inventoryContent.transform);
                var holenUISlot = newSlot.GetComponent<HolenSlotUI>();

                if (holenUISlot != null)
                {
                    holenUISlot.SetSlot(data, quantities[i]);

                    // Disable button for opponent
                    Button btn = newSlot.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = false;
                    }
                }
            }
        }

        Debug.Log($"[INVENTORY SYNC] ✅ Updated Player {playerNum}'s inventory with {holenIDs.Length} items");
    }

    private void UpdateOpponentStateText(int playerNum, bool isReady)
    {
        string stateLabel = isReady ? "READY" : "CANCEL";

        if (playerNum == 1 && player1StateText != null)
        {
            player1StateText.text = stateLabel;
        }
        else if (playerNum == 2 && player2StateText != null)
        {
            player2StateText.text = stateLabel;
        }
    }

    private void UpdateOpponentPoints(int playerNum, int points)
    {
        if (playerNum == 1 && player1PointsText != null)
        {
            player1PointsText.text = $"{points}";
        }
        else if (playerNum == 2 && player2PointsText != null)
        {
            player2PointsText.text = $"{points}";
        }
    }

    private void UpdateOpponentWagerDisplay(int playerNum, string[] holenIDs, int[] quantities)
    {
        GameObject wagerContent = playerNum == 1 ? player1WagerContent : player2WagerContent;

        if (wagerContent == null) return;

        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null) return;

        // Clear existing wager
        foreach (Transform child in wagerContent.transform)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"[WAGER SYNC] Displaying {holenIDs.Length} holens for Player {playerNum}");

        // Display each selected holen
        for (int i = 0; i < holenIDs.Length; i++)
        {
            HolenData data = inv.GetHolenData(holenIDs[i]);
            if (data != null)
            {
                GameObject newSlot = Instantiate(holenUISlotPrefab, wagerContent.transform);
                var holenUISlot = newSlot.GetComponent<HolenSlotUI>();

                if (holenUISlot != null)
                {
                    holenUISlot.SetSlot(data, quantities[i]);

                    // Disable button for opponent
                    Button btn = newSlot.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = false;
                    }
                }
            }
        }

        Debug.Log($"[WAGER SYNC] ✅ Updated Player {playerNum}'s wager with {holenIDs.Length} items");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogWarning($"[DISCONNECT] Opponent left the room!");
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[DISCONNECT] Left room, returning to menu...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"[DISCONNECT] Disconnected from Photon: {cause}");
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }
}