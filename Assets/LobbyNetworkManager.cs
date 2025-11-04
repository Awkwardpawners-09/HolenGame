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

    private LobbyUIManager player1LobbyUI;
    private LobbyUIManager player2LobbyUI;
    private WagerManager player1Wager;
    private WagerManager player2Wager;

    private bool isPlayer1Ready = false;
    private bool isPlayer2Ready = false;

    private const byte READY_STATE_EVENT = 1;
    private const byte WAGER_UPDATE_EVENT = 2;

    void Start()
    {
        // Wait for both players to be in the room
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            Debug.Log("Waiting for second player...");
        }
        else
        {
            InitializeLobby();
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

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            InitializeLobby();
        }
    }

    private void InitializeLobby()
    {
        // Assign player numbers based on ActorNumber
        // Player with ActorNumber 1 becomes Player 1, ActorNumber 2 becomes Player 2
        localPlayerNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        Debug.Log($"Local player assigned as Player {localPlayerNumber}");

        // Set up the appropriate UI managers for this player
        SetupLocalPlayerUI();

        // Set up opponent's UI (read-only)
        SetupOpponentUI();

        // Display player names
        UpdatePlayerNames();
    }

    private void SetupLocalPlayerUI()
    {
        if (localPlayerNumber == 1)
        {
            // This player controls Player 1 UI
            player1LobbyUI = CreateLobbyUIManager(player1InventoryContent, true);
            player1Wager = CreateWagerManager(
                player1WagerContent,
                player1ActionButton,
                player1StateText,
                player1CountdownText,
                player1PointsText,
                true
            );

            // Disable Player 2 interaction
            if (player2ActionButton != null)
                player2ActionButton.interactable = false;
        }
        else if (localPlayerNumber == 2)
        {
            // This player controls Player 2 UI
            player2LobbyUI = CreateLobbyUIManager(player2InventoryContent, true);
            player2Wager = CreateWagerManager(
                player2WagerContent,
                player2ActionButton,
                player2StateText,
                player2CountdownText,
                player2PointsText,
                true
            );

            // Disable Player 1 interaction
            if (player1ActionButton != null)
                player1ActionButton.interactable = false;
        }
    }

    private void SetupOpponentUI()
    {
        // The opponent's UI will be updated via network events
        // We just need to make sure their UI is visible but non-interactive
        if (localPlayerNumber == 1)
        {
            // Create read-only managers for Player 2 (opponent)
            player2LobbyUI = CreateLobbyUIManager(player2InventoryContent, false);
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
            // Create read-only managers for Player 1 (opponent)
            player1LobbyUI = CreateLobbyUIManager(player1InventoryContent, false);
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

    private LobbyUIManager CreateLobbyUIManager(GameObject contentScrollView, bool isLocal)
    {
        GameObject managerObj = new GameObject($"LobbyUIManager_P{(isLocal ? localPlayerNumber : (3 - localPlayerNumber))}");
        managerObj.transform.SetParent(transform);

        LobbyUIManager manager = managerObj.AddComponent<LobbyUIManager>();
        manager.contentScrollView = contentScrollView;
        manager.holenUISlotPrefab = holenUISlotPrefab;

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
        GameObject managerObj = new GameObject($"WagerManager_P{(isLocal ? localPlayerNumber : (3 - localPlayerNumber))}");
        managerObj.transform.SetParent(transform);

        WagerManager manager = managerObj.AddComponent<WagerManager>();
        manager.wagerContent = wagerContent;
        manager.holenUISlotPrefab = holenUISlotPrefab;
        manager.actionButton = actionButton;
        manager.stateText = stateText;
        manager.countdownText = countdownText;
        manager.player1PointsText = pointsText;

        // If this is the local player's wager manager, hook up the button
        if (isLocal && actionButton != null)
        {
            actionButton.onClick.AddListener(() => OnLocalPlayerReady(manager));
        }

        return manager;
    }

    private void UpdatePlayerNames()
    {
        Player[] players = PhotonNetwork.PlayerList;

        if (players.Length >= 1 && player1NameText != null)
        {
            Player p1 = System.Array.Find(players, p => p.ActorNumber == 1);
            if (p1 != null)
                player1NameText.text = p1.NickName;
        }

        if (players.Length >= 2 && player2NameText != null)
        {
            Player p2 = System.Array.Find(players, p => p.ActorNumber == 2);
            if (p2 != null)
                player2NameText.text = p2.NickName;
        }
    }

    private void OnLocalPlayerReady(WagerManager wagerManager)
    {
        // Toggle the ready state
        bool newReadyState = !GetPlayerReadyState(localPlayerNumber);

        // Update the wager manager's internal state
        wagerManager.OnActionButtonPressed();

        // Send ready state to other player
        object[] content = new object[] { localPlayerNumber, newReadyState };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(READY_STATE_EVENT, content, raiseEventOptions, SendOptions.SendReliable);

        // Update local state
        SetPlayerReadyState(localPlayerNumber, newReadyState);

        Debug.Log($"Player {localPlayerNumber} ready state: {newReadyState}");

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
    }

    private void CheckBothPlayersReady()
    {
        if (isPlayer1Ready && isPlayer2Ready)
        {
            Debug.Log("Both players ready! Starting game...");

            // Only master client loads the scene
            if (PhotonNetwork.IsMasterClient)
            {
                Invoke(nameof(LoadGameScene), 2f); // Small delay for dramatic effect
            }
        }
    }

    private void LoadGameScene()
    {
        if (PhotonNetwork.IsMasterClient)
        {
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

            Debug.Log($"Received ready state for Player {playerNum}: {readyState}");

            SetPlayerReadyState(playerNum, readyState);
            CheckBothPlayersReady();
        }
        else if (eventCode == WAGER_UPDATE_EVENT)
        {
            // Handle wager updates from opponent if needed
            object[] data = (object[])photonEvent.CustomData;
            int playerNum = (int)data[0];
            // Update opponent's wager display
            Debug.Log($"Player {playerNum} updated their wager");
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Opponent left the room!");
        // Handle disconnect - maybe return to menu
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        // Return to menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }
}