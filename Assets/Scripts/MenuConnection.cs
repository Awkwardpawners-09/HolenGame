using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuConnection : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject findingOpponentPanel;

    [Header("Scene Settings")]
    [Tooltip("The Lobby scene where players wager their Holens")]
    public string lobbySceneName = "LobbyScene"; // CHANGED: Load Lobby first, not Game

    private bool isSearching = false;

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        // Set a default nickname if not set
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Player_" + Random.Range(1000, 9999);
        }

        Debug.Log($"[MENU] Player nickname: {PhotonNetwork.NickName}");
    }

    // Called when the "Find Opponent" button is clicked
    public void OnMultiplayerClicked()
    {
        if (isSearching) return;
        isSearching = true;

        findingOpponentPanel.SetActive(true);

        // Check if already connected to Photon
        if (PhotonNetwork.IsConnected)
        {
            // Already connected, go straight to finding a room
            PhotonNetwork.JoinRandomRoom();
            Debug.Log("[MENU] Already connected to Photon, joining random room...");
        }
        else
        {
            // Not connected, connect first
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("[MENU] Connecting to Photon...");
        }
    }

    // Called when the "Cancel" button is clicked
    public void OnCancelFindingOpponent()
    {
        isSearching = false;
        findingOpponentPanel.SetActive(false);

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            Debug.Log("[MENU] Left room");
        }
        // Don't disconnect from Photon - stay connected for faster rejoining

        Debug.Log("[MENU] Cancelled matchmaking.");
    }

    // Called when connected to the Photon Master server
    public override void OnConnectedToMaster()
    {
        Debug.Log("[MENU] Connected to Photon Master server");

        // Only join room if user is still searching
        if (isSearching)
        {
            PhotonNetwork.JoinRandomRoom();
        }
    }

    // Called when no room is found and we need to create one
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // Only create room if still searching (user didn't cancel)
        if (!isSearching)
        {
            Debug.Log("[MENU] Join random failed but user cancelled search");
            return;
        }

        Debug.Log("[MENU] No rooms found, creating new room...");
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsOpen = true,
            IsVisible = true,
            EmptyRoomTtl = 0, // Delete empty rooms immediately
            PlayerTtl = 10000 // Keep disconnected player's slot for 10 seconds
        };
        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    // Called when successfully joined a room
    public override void OnJoinedRoom()
    {
        Debug.Log($"[MENU] Joined a room. Players: {PhotonNetwork.CurrentRoom.PlayerCount}/2");
        Debug.Log($"[MENU] My ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");
        Debug.Log($"[MENU] Is Master Client: {PhotonNetwork.IsMasterClient}");

        // Check if user cancelled during connection
        if (!isSearching)
        {
            Debug.Log("[MENU] Joined room but user cancelled, leaving...");
            PhotonNetwork.LeaveRoom();
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            StartLobby();
        }
    }

    // Called when another player enters the room
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[MENU] Another player joined. Total: {PhotonNetwork.CurrentRoom.PlayerCount}/2");
        Debug.Log($"[MENU] New player ActorNumber: {newPlayer.ActorNumber}");

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && isSearching)
        {
            StartLobby();
        }
    }

    // Called when leaving a room
    public override void OnLeftRoom()
    {
        Debug.Log("[MENU] Left room successfully");
    }

    // Called when disconnected from Photon
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[MENU] Disconnected from Photon: {cause}");
        isSearching = false;
        findingOpponentPanel.SetActive(false);
    }

    // Start the lobby when both players are ready
    private void StartLobby()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("[MENU] Both players connected. Loading Lobby scene...");
        Debug.Log($"[MENU] Player 1 (ActorNumber 1): {GetPlayerByActorNumber(1)?.NickName ?? "Not found"}");
        Debug.Log($"[MENU] Player 2 (ActorNumber 2): {GetPlayerByActorNumber(2)?.NickName ?? "Not found"}");

        // Load the LOBBY scene (wager selection happens there)
        PhotonNetwork.LoadLevel(lobbySceneName);
    }

    // Helper method to get player by ActorNumber
    private Player GetPlayerByActorNumber(int actorNumber)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber == actorNumber)
                return player;
        }
        return null;
    }

    // Optional: Add this to handle network errors gracefully
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[MENU] Create room failed: {message}");

        if (isSearching)
        {
            // Try joining random room again as fallback
            Debug.Log("[MENU] Retrying with JoinRandomRoom...");
            PhotonNetwork.JoinRandomRoom();
        }
    }

    // Debug helper - press 'D' in menu to see Photon status
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("=== PHOTON DEBUG INFO ===");
            Debug.Log($"Connected: {PhotonNetwork.IsConnected}");
            Debug.Log($"In Room: {PhotonNetwork.InRoom}");
            Debug.Log($"Room Name: {PhotonNetwork.CurrentRoom?.Name ?? "None"}");
            Debug.Log($"Players in Room: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0}");
            Debug.Log($"Is Master: {PhotonNetwork.IsMasterClient}");
            Debug.Log($"My ActorNumber: {PhotonNetwork.LocalPlayer?.ActorNumber ?? 0}");
            Debug.Log($"My Nickname: {PhotonNetwork.NickName}");
            Debug.Log("========================");
        }
    }
}