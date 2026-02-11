using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Linq;

public class MenuConnection : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject findingOpponentPanel;

    [Header("Inventory Check UI")]
    [Tooltip("Panel to show when inventory is empty (should be disabled by default)")]
    public GameObject noHolensPanel;

    [Header("Match Found UI")]
    [Tooltip("Panel to show when match is found (should be disabled by default)")]
    public GameObject matchFound;

    [Tooltip("TextMeshPro component to display the 'no holens' message")]
    public TextMeshProUGUI noHolensText;

    [Tooltip("Message to display when inventory is empty")]
    public string noHolensMessage = "You don't have any holens!";

    [Tooltip("Message to display when player doesn't have 5 different kinds")]
    public string notEnoughKindsMessage = "You need at least 5 different kinds of holens!";

    [Tooltip("How long to show the no holens message (in seconds)")]
    public float noHolensPanelDuration = 3f;

    [Tooltip("How long to show the match found panel (in seconds)")]
    public float matchFoundDuration = 3f;

    [Tooltip("Minimum number of different holen kinds required")]
    public int requiredHolenKinds = 5;

    [Header("Scene Transition")]
    [Tooltip("GameObject to enable before switching scenes (transition effect)")]
    public GameObject transitionObject;

    [Tooltip("How long to show transition before loading scene (in seconds)")]
    public float transitionDuration = 1f;

    [Header("Scene Settings")]
    [Tooltip("The Lobby scene where players wager their Holens")]
    public string lobbySceneName = "LobbyScene";

    private bool isSearching = false;

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        // Get player name from HolenInventoryManager
        if (HolenInventoryManager.Instance != null && HolenInventoryManager.Instance.HasPlayerName())
        {
            PhotonNetwork.NickName = HolenInventoryManager.Instance.PlayerName;
            Debug.Log($"[MENU] Player nickname set from saved name: {PhotonNetwork.NickName}");
        }
        else
        {
            // Fallback to random name if no saved name exists
            PhotonNetwork.NickName = "Player_" + Random.Range(1000, 9999);
            Debug.Log($"[MENU] No saved name found, using random nickname: {PhotonNetwork.NickName}");
        }

        // Ensure transition object is disabled at start
        if (transitionObject != null)
        {
            transitionObject.SetActive(false);
        }
    }

    // Called when the "Find Opponent" button is clicked
    public void OnMultiplayerClicked()
    {
        if (isSearching) return;

        // Check if player has any holens in inventory
        if (HolenInventoryManager.Instance == null)
        {
            Debug.LogError("[MENU] HolenInventoryManager not found!");
            return;
        }

        var inventory = HolenInventoryManager.Instance.GetAllHolens();

        // Check if inventory is empty
        if (inventory == null || inventory.Count == 0)
        {
            ShowNoHolensMessage(noHolensMessage);
            Debug.Log("[MENU] Cannot find opponent - inventory is empty!");
            return;
        }

        // Check if player has enough different kinds of holens
        int uniqueKinds = CountUniqueHolenKinds(inventory);
        Debug.Log($"[MENU] Player has {uniqueKinds} different kinds of holens (required: {requiredHolenKinds})");

        if (uniqueKinds < requiredHolenKinds)
        {
            ShowNoHolensMessage(notEnoughKindsMessage);
            Debug.Log($"[MENU] Cannot find opponent - only has {uniqueKinds} different kinds, needs {requiredHolenKinds}!");
            return;
        }

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

    /// <summary>
    /// Counts the number of unique holen kinds in the inventory
    /// </summary>
    private int CountUniqueHolenKinds(System.Collections.Generic.List<HolenInventoryEntry> inventory)
    {
        if (inventory == null || inventory.Count == 0)
            return 0;

        // Count distinct holenIDs (each ID represents a different kind of holen)
        var uniqueKinds = inventory.Select(entry => entry.holenID).Distinct().Count();

        return uniqueKinds;
    }

    // Show the "no holens" panel for a set duration with custom message
    private void ShowNoHolensMessage(string message)
    {
        if (noHolensPanel == null)
        {
            Debug.LogWarning("[MENU] No Holens Panel not assigned in Inspector!");
            return;
        }

        // Set the text if text component is assigned
        if (noHolensText != null)
        {
            noHolensText.text = message;
        }

        // Show the panel
        noHolensPanel.SetActive(true);

        // Start coroutine to hide it after duration
        StartCoroutine(HideNoHolensPanelAfterDelay());
    }

    private IEnumerator HideNoHolensPanelAfterDelay()
    {
        yield return new WaitForSeconds(noHolensPanelDuration);

        if (noHolensPanel != null)
        {
            noHolensPanel.SetActive(false);
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
            // Show match found panel, then transition
            StartCoroutine(ShowMatchFoundThenTransition());
        }
    }

    // Called when another player enters the room
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[MENU] Another player joined. Total: {PhotonNetwork.CurrentRoom.PlayerCount}/2");
        Debug.Log($"[MENU] New player ActorNumber: {newPlayer.ActorNumber}");

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && isSearching)
        {
            // Show match found panel, then transition
            StartCoroutine(ShowMatchFoundThenTransition());
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

    /// <summary>
    /// Shows the match found panel for 3 seconds, then enables transition
    /// </summary>
    private IEnumerator ShowMatchFoundThenTransition()
    {
        // Show match found panel
        if (matchFound != null)
        {
            matchFound.SetActive(true);
            Debug.Log($"[MENU] Match found panel shown for {PhotonNetwork.NickName}");
        }
        else
        {
            Debug.LogWarning("[MENU] Match found panel not assigned!");
        }

        // Wait for match found duration
        yield return new WaitForSeconds(matchFoundDuration);

        // Hide match found panel
        if (matchFound != null)
        {
            matchFound.SetActive(false);
        }

        // Enable transition for this player
        EnableTransition();

        // Only Master Client loads the scene
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(LoadLobbyAfterDelay());
        }
    }

    /// <summary>
    /// Enables the transition object for this player
    /// </summary>
    private void EnableTransition()
    {
        if (transitionObject != null)
        {
            transitionObject.SetActive(true);

            Debug.Log($"[MENU] Transition enabled for {PhotonNetwork.NickName}");
        }
        else
        {
            Debug.LogWarning("[MENU] Transition object not assigned!");
        }
    }

    /// <summary>
    /// Coroutine to load lobby after transition delay (Master Client only)
    /// </summary>
    private IEnumerator LoadLobbyAfterDelay()
    {
        Debug.Log($"[MENU] Master Client waiting {transitionDuration} seconds before loading scene...");
        Debug.Log($"[MENU] Player 1 (ActorNumber 1): {GetPlayerByActorNumber(1)?.NickName ?? "Not found"}");
        Debug.Log($"[MENU] Player 2 (ActorNumber 2): {GetPlayerByActorNumber(2)?.NickName ?? "Not found"}");

        yield return new WaitForSeconds(transitionDuration);

        Debug.Log("[MENU] Loading Lobby scene for both players...");
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