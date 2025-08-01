using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviourPunCallbacks
{
    public GameObject findingOpponentPanel; // Reference to the Finding Opponent panel
    private float searchTimeout = 10f; // Timeout for finding an opponent
    private bool isSearching = false;  // To track if we are currently searching

    // Trigger the multiplayer auto-matching process
    public void OnMultiplayerClicked()
    {
        if (isSearching) return; // Prevent multiple searches at the same time

        // Show the "Finding Opponent" panel
        findingOpponentPanel.SetActive(true);

        // Start the process of connecting to Photon and finding an opponent
        StartCoroutine(ConnectAndFindOpponent());
    }

    private IEnumerator ConnectAndFindOpponent()
    {
        // Connect to Photon first
        PhotonNetwork.ConnectUsingSettings(); // This will initiate the connection to Photon

        // Wait until the client is connected to the master server
        while (!PhotonNetwork.IsConnected)
        {
            yield return null; // Wait for connection to complete
        }

        // Once connected, proceed with matchmaking
        float timer = 0f;
        bool roomCreated = false;

        // First 5 seconds: Try to join a random room
        while (timer < 5f && !PhotonNetwork.InRoom)
        {
            PhotonNetwork.JoinRandomRoom();
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // If no room was found, create one
        if (!PhotonNetwork.InRoom)
        {
            Debug.Log("No room found, creating a new room...");
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 2, // Limit the room to 2 players (1v1)
                IsOpen = true,   // Open for other players to join
                IsVisible = true // Visible for others to find
            };
            PhotonNetwork.CreateRoom(null, roomOptions);
            roomCreated = true;
        }

        // Now wait for the next 5 seconds for a joiner
        timer = 0f;
        while (timer < 5f && PhotonNetwork.CountOfPlayers < 2)
        {
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // If no joiner after 10 seconds, disable the "Finding Opponent" panel and close the room
        if (PhotonNetwork.CountOfPlayers < 2)
        {
            Debug.Log("No joiner found, retrying...");

            // Close the room (make it inaccessible)
            if (roomCreated && PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false; // Close the room to prevent further joiners
                PhotonNetwork.CurrentRoom.IsVisible = false; // Hide the room
            }

            findingOpponentPanel.SetActive(false); // Hide the Finding Opponent panel
            isSearching = false; // Reset the search flag
        }
        else
        {
            // Both players are connected, move to the game scene
            SceneManager.LoadScene("GameScene"); // Replace with your game scene name
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master server");

        // Proceed to find a random room after successfully connecting to the master server
        if (findingOpponentPanel.activeSelf)
        {
            PhotonNetwork.JoinRandomRoom(); // Now it's safe to attempt joining
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // If no available room is found, create a new room for 1v1 match
        Debug.Log("No rooms found, creating new room...");
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2, // Limit the room to 2 players (1v1)
            IsOpen = true,
            IsVisible = true
        };
        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room, waiting for the other player");

        // Once both players are connected, load the game scene
        if (PhotonNetwork.CountOfPlayers == 2)
        {
            SceneManager.LoadScene("GameScene"); // Replace with your game scene name
        }
    }
}
