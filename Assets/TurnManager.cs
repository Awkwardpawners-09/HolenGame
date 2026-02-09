using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

/// <summary>
/// Manages turn-based gameplay in multiplayer.
/// Coordinates whose turn it is and ensures proper turn transitions.
/// </summary>
public class TurnManager : MonoBehaviourPunCallbacks
{
    [Header("Turn Settings")]
    [Tooltip("Maximum time per turn (0 = unlimited)")]
    public float turnTimeLimit = 60f;

    [Tooltip("Automatically start game when all players ready")]
    public bool autoStart = true;

    [Header("UI References")]
    [Tooltip("UI element to display current turn info")]
    public UnityEngine.UI.Text turnInfoText;

    [Tooltip("UI element to display timer")]
    public UnityEngine.UI.Text timerText;

    [Header("Debug")]
    public bool showDebugInfo = true;

    // State
    private int currentPlayerIndex = 0;
    private bool gameStarted = false;
    private bool turnInProgress = false;
    private float turnTimer = 0f;
    private Player[] players;

    void Start()
    {
        // Get all players in room
        players = PhotonNetwork.PlayerList;

        if (showDebugInfo)
        {
            Debug.Log($"[TurnManager] Initialized with {players.Length} players");
            foreach (var p in players)
            {
                Debug.Log($"[TurnManager] - Player: {p.NickName} (Actor: {p.ActorNumber})");
            }
        }

        // Wait a bit for holens to spawn
        if (autoStart && PhotonNetwork.IsMasterClient)
        {
            Invoke(nameof(StartGame), 2f);
        }
    }

    void Update()
    {
        if (!gameStarted || !turnInProgress)
            return;

        // Update turn timer
        if (turnTimeLimit > 0)
        {
            turnTimer -= Time.deltaTime;

            if (timerText != null)
            {
                timerText.text = $"Time: {Mathf.CeilToInt(turnTimer)}s";
            }

            // Time's up!
            if (turnTimer <= 0)
            {
                if (showDebugInfo)
                    Debug.Log("[TurnManager] Turn time expired!");

                OnPlayerTurnComplete();
            }
        }

        UpdateTurnUI();
    }

    /// <summary>
    /// Start the game (Master Client only)
    /// </summary>
    [PunRPC]
    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (showDebugInfo)
            Debug.Log("[TurnManager] Starting game...");

        // Sync game start to all clients
        photonView.RPC("RPC_GameStarted", RpcTarget.All);
    }

    /// <summary>
    /// RPC to notify all clients that game has started
    /// </summary>
    [PunRPC]
    private void RPC_GameStarted()
    {
        gameStarted = true;

        if (showDebugInfo)
            Debug.Log("[TurnManager] Game started!");

        // Master Client starts first turn
        if (PhotonNetwork.IsMasterClient)
        {
            StartNextTurn();
        }
    }

    /// <summary>
    /// Start the next player's turn (Master Client only)
    /// </summary>
    private void StartNextTurn()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        // Ensure all holens have stopped before starting next turn
        if (!HolenPhysicsSync.AreAllHolensStopped())
        {
            if (showDebugInfo)
                Debug.Log("[TurnManager] Waiting for holens to stop before next turn...");

            StartCoroutine(WaitForHolensToStopThenStartTurn());
            return;
        }

        // Get next player
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Length;
        Player currentPlayer = players[currentPlayerIndex];

        if (showDebugInfo)
            Debug.Log($"[TurnManager] Starting turn for {currentPlayer.NickName}");

        // Reset timer
        turnTimer = turnTimeLimit;
        turnInProgress = true;

        // Sync turn start to all clients
        photonView.RPC("RPC_TurnStarted", RpcTarget.All, currentPlayer.ActorNumber);
    }

    /// <summary>
    /// Wait for holens to stop, then start next turn
    /// </summary>
    private IEnumerator WaitForHolensToStopThenStartTurn()
    {
        float timeout = 30f;
        float elapsed = 0f;

        while (!HolenPhysicsSync.AreAllHolensStopped() && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning("[TurnManager] Timeout waiting for holens to stop!");
        }

        StartNextTurn();
    }

    /// <summary>
    /// RPC to notify all clients whose turn it is
    /// </summary>
    [PunRPC]
    private void RPC_TurnStarted(int playerActorNumber)
    {
        Player currentPlayer = GetPlayerByActorNumber(playerActorNumber);

        if (currentPlayer == null)
        {
            Debug.LogError($"[TurnManager] Could not find player with ActorNumber {playerActorNumber}");
            return;
        }

        if (showDebugInfo)
            Debug.Log($"[TurnManager] Turn started for {currentPlayer.NickName}");

        // Enable/disable controller based on whose turn it is
        var controller = FindObjectOfType<MultiplayerHolenController>();
        if (controller != null)
        {
            if (currentPlayer == PhotonNetwork.LocalPlayer)
            {
                controller.StartTurn();
            }
            else
            {
                controller.EndTurn();
            }
        }

        UpdateTurnUI();
    }

    /// <summary>
    /// Called when a player completes their turn
    /// </summary>
    public void OnPlayerTurnComplete()
    {
        if (!gameStarted)
            return;

        turnInProgress = false;

        if (showDebugInfo)
            Debug.Log("[TurnManager] Player turn complete");

        // Only Master Client advances the turn
        if (PhotonNetwork.IsMasterClient)
        {
            // Small delay before next turn
            Invoke(nameof(StartNextTurn), 1f);
        }
    }

    /// <summary>
    /// Update turn UI
    /// </summary>
    private void UpdateTurnUI()
    {
        if (turnInfoText == null)
            return;

        if (!gameStarted)
        {
            turnInfoText.text = "Waiting for game to start...";
            return;
        }

        Player currentPlayer = players[currentPlayerIndex];
        bool isMyTurn = currentPlayer == PhotonNetwork.LocalPlayer;

        if (isMyTurn)
        {
            turnInfoText.text = $"YOUR TURN";
            turnInfoText.color = Color.green;
        }
        else
        {
            turnInfoText.text = $"{currentPlayer.NickName}'s Turn";
            turnInfoText.color = Color.yellow;
        }
    }

    /// <summary>
    /// Get player by ActorNumber
    /// </summary>
    private Player GetPlayerByActorNumber(int actorNumber)
    {
        foreach (var player in players)
        {
            if (player.ActorNumber == actorNumber)
                return player;
        }
        return null;
    }

    /// <summary>
    /// Get the current player whose turn it is
    /// </summary>
    public Player GetCurrentPlayer()
    {
        if (!gameStarted || players == null || players.Length == 0)
            return null;

        return players[currentPlayerIndex];
    }

    /// <summary>
    /// Check if it's the local player's turn
    /// </summary>
    public bool IsMyTurn()
    {
        if (!gameStarted)
            return false;

        return GetCurrentPlayer() == PhotonNetwork.LocalPlayer;
    }

    /// <summary>
    /// Manually advance to next turn (for testing or admin)
    /// </summary>
    public void ForceNextTurn()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            OnPlayerTurnComplete();
        }
    }

    /// <summary>
    /// End the game
    /// </summary>
    public void EndGame()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        photonView.RPC("RPC_GameEnded", RpcTarget.All);
    }

    /// <summary>
    /// RPC to notify all clients that game has ended
    /// </summary>
    [PunRPC]
    private void RPC_GameEnded()
    {
        gameStarted = false;
        turnInProgress = false;

        if (showDebugInfo)
            Debug.Log("[TurnManager] Game ended");

        // Disable all controllers
        var controller = FindObjectOfType<MultiplayerHolenController>();
        if (controller != null)
        {
            controller.EndTurn();
        }

        if (turnInfoText != null)
        {
            turnInfoText.text = "Game Over";
        }
    }

    /// <summary>
    /// Handle player leaving during game
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (showDebugInfo)
            Debug.Log($"[TurnManager] Player left: {otherPlayer.NickName}");

        // Update player list
        players = PhotonNetwork.PlayerList;

        // If current player left, advance turn
        if (otherPlayer.ActorNumber == players[currentPlayerIndex].ActorNumber)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                OnPlayerTurnComplete();
            }
        }
    }
}