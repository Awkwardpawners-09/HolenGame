using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persists wager selections between Lobby and Game scenes.
/// Created by LobbyNetworkManager before loading the game scene.
/// </summary>
public class WagerDataManager : MonoBehaviour
{
    public static WagerDataManager Instance { get; private set; }

    [System.Serializable]
    public class PlayerWagerData
    {
        public int playerNumber; // 1 or 2
        public List<WagerManager.SelectedHolenRecord> selectedHolens;

        public PlayerWagerData(int playerNum, List<WagerManager.SelectedHolenRecord> holens)
        {
            playerNumber = playerNum;
            selectedHolens = new List<WagerManager.SelectedHolenRecord>(holens);
        }
    }

    [Header("Wager Data")]
    [SerializeField] private PlayerWagerData player1Wager;
    [SerializeField] private PlayerWagerData player2Wager;

    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[WagerDataManager] Created and persisting between scenes");
    }

    /// <summary>
    /// Stores a player's wager selections.
    /// </summary>
    public void SetPlayerWager(int playerNumber, List<WagerManager.SelectedHolenRecord> holens)
    {
        if (playerNumber == 1)
        {
            player1Wager = new PlayerWagerData(1, holens);
            Debug.Log($"[WagerDataManager] Stored Player 1 wager: {holens.Count} holens");
        }
        else if (playerNumber == 2)
        {
            player2Wager = new PlayerWagerData(2, holens);
            Debug.Log($"[WagerDataManager] Stored Player 2 wager: {holens.Count} holens");
        }
    }

    /// <summary>
    /// Gets a player's wager selections.
    /// </summary>
    public List<WagerManager.SelectedHolenRecord> GetPlayerWager(int playerNumber)
    {
        if (playerNumber == 1 && player1Wager != null)
        {
            Debug.Log($"[WagerDataManager] Retrieved Player 1 wager: {player1Wager.selectedHolens.Count} holens");
            return player1Wager.selectedHolens;
        }
        else if (playerNumber == 2 && player2Wager != null)
        {
            Debug.Log($"[WagerDataManager] Retrieved Player 2 wager: {player2Wager.selectedHolens.Count} holens");
            return player2Wager.selectedHolens;
        }

        Debug.LogWarning($"[WagerDataManager] No wager data found for Player {playerNumber}");
        return new List<WagerManager.SelectedHolenRecord>();
    }

    /// <summary>
    /// Gets all holens from both players as individual spawn entries.
    /// Each player's selection is treated as a separate spawn, even if they selected the same holen.
    /// </summary>
    public List<WagerManager.SelectedHolenRecord> GetAllWageredHolensIndividual()
    {
        List<WagerManager.SelectedHolenRecord> allHolens = new List<WagerManager.SelectedHolenRecord>();

        // Add Player 1's wagers
        if (player1Wager != null && player1Wager.selectedHolens != null)
        {
            foreach (var holen in player1Wager.selectedHolens)
            {
                allHolens.Add(new WagerManager.SelectedHolenRecord(holen.holenID, holen.quantity));
            }
            Debug.Log($"[WagerDataManager] Added {player1Wager.selectedHolens.Count} holens from Player 1");
        }

        // Add Player 2's wagers
        if (player2Wager != null && player2Wager.selectedHolens != null)
        {
            foreach (var holen in player2Wager.selectedHolens)
            {
                allHolens.Add(new WagerManager.SelectedHolenRecord(holen.holenID, holen.quantity));
            }
            Debug.Log($"[WagerDataManager] Added {player2Wager.selectedHolens.Count} holens from Player 2");
        }

        Debug.Log($"[WagerDataManager] Retrieved all wagers: {allHolens.Count} total holen selections (duplicates included)");
        return allHolens;
    }

    /// <summary>
    /// Clears all wager data (call when returning to menu).
    /// </summary>
    public void ClearAllWagers()
    {
        player1Wager = null;
        player2Wager = null;
        Debug.Log("[WagerDataManager] Cleared all wager data");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}