using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ExitGames.Client.Photon;

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

        // Set up the appropriate UI managers for this player
        SetupLocalPlayerUI();

        // Set up opponent's UI (read-only)
        SetupOpponentUI();

        Debug.Log($"[INIT] Lobby initialization complete for Player {localPlayerNumber}");
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
        // Create read-only UI for opponent (no inventory, just wager display)
        if (localPlayerNumber == 1)
        {
            Debug.Log("[SETUP] Setting up Player 2 (opponent) UI - read-only");
            // Opponent is Player 2 - we don't create inventory UI for them
            // Just create an empty wager manager for display purposes if needed
        }
        else if (localPlayerNumber == 2)
        {
            Debug.Log("[SETUP] Setting up Player 1 (opponent) UI - read-only");
            // Opponent is Player 1 - we don't create inventory UI for them
        }

        // Note: Opponent's wager content can be synced via WAGER_UPDATE_EVENT if you want to show their selections
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

        // If this is the local player's wager manager, hook up the button
        if (isLocal && actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners(); // Clear any existing listeners
            actionButton.onClick.AddListener(() => OnLocalPlayerReady(manager));
            Debug.Log($"[BUTTON] Ready button connected for Player {localPlayerNumber}");
        }

        return manager;
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
            CheckBothPlayersReady();
        }
        else if (eventCode == WAGER_UPDATE_EVENT)
        {
            // Handle wager updates from opponent if needed
            object[] data = (object[])photonEvent.CustomData;
            int playerNum = (int)data[0];
            Debug.Log($"[NETWORK] Player {playerNum} updated their wager");

            // TODO: Update opponent's wager display if you want to show their selections
        }
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