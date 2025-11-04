using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuConnection : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject findingOpponentPanel;
    public string gameSceneName = "GameScene";

    private bool isSearching = false;

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
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
            Debug.Log("Already connected to Photon, joining random room...");
        }
        else
        {
            // Not connected, connect first
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Connecting to Photon...");
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
            Debug.Log("Left room");
        }
        // Don't disconnect from Photon - stay connected for faster rejoining

        Debug.Log("Cancelled matchmaking.");
    }

    // Called when connected to the Photon Master server
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master server");

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
            Debug.Log("Join random failed but user cancelled search");
            return;
        }

        Debug.Log("No rooms found, creating new room...");
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsOpen = true,
            IsVisible = true
        };
        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    // Called when successfully joined a room
    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined a room. Players: {PhotonNetwork.CurrentRoom.PlayerCount}/2");

        // Check if user cancelled during connection
        if (!isSearching)
        {
            Debug.Log("Joined room but user cancelled, leaving...");
            PhotonNetwork.LeaveRoom();
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            StartGame();
        }
    }

    // Called when another player enters the room
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Another player joined. Total: {PhotonNetwork.CurrentRoom.PlayerCount}/2");

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && isSearching)
        {
            StartGame();
        }
    }

    // Called when leaving a room
    public override void OnLeftRoom()
    {
        Debug.Log("Left room successfully");
    }

    // Called when disconnected from Photon
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Disconnected from Photon: {cause}");
        isSearching = false;
        findingOpponentPanel.SetActive(false);
    }

    // Start the game when both players are ready
    private void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("Both players connected. Starting game...");

        // Load the game scene (roles will be assigned in the game scene based on ActorNumber)
        PhotonNetwork.LoadLevel(gameSceneName);
    }

    // Optional: Add this to handle network errors gracefully
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Create room failed: {message}");

        if (isSearching)
        {
            // Try joining random room again as fallback
            PhotonNetwork.JoinRandomRoom();
        }
    }
}