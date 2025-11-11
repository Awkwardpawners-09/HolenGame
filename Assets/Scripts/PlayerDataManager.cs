using UnityEngine;
using System;

/// <summary>
/// Manages player data and provides event-based updates.
/// Singleton pattern with DontDestroyOnLoad.
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    public PlayerData playerData;

    // 🔔 Events for UI updates
    public static event Action<int> OnCoinsChanged;
    public static event Action<string> OnPlayerNameChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            playerData = PlayerData.Load();

            Debug.Log($"[PlayerDataManager] Loaded - Coins: {playerData.coins}, Name: {playerData.playerName}");

            // 🔔 Notify all UI elements of initial values on startup
            // This ensures all CoinUI instances update immediately
            Invoke(nameof(NotifyInitialValues), 0.1f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void NotifyInitialValues()
    {
        // Notify all listeners of the loaded values
        OnCoinsChanged?.Invoke(playerData.coins);
        OnPlayerNameChanged?.Invoke(playerData.playerName);
        Debug.Log($"[PlayerDataManager] Notified UI - Coins: {playerData.coins}");
    }

    // ===================== COIN METHODS =====================

    public bool SpendCoins(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[PlayerDataManager] Attempted to spend non-positive amount: {amount}");
            return false;
        }

        int oldAmount = playerData.coins;

        if (playerData.SpendCoins(amount))
        {
            Debug.Log($"[PlayerDataManager] Spent {amount} coins ({oldAmount} → {playerData.coins})");
            OnCoinsChanged?.Invoke(playerData.coins); // 🔔 notify UI
            return true;
        }

        Debug.LogWarning($"[PlayerDataManager] Failed to spend {amount} coins (only have {playerData.coins})");
        return false;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[PlayerDataManager] Attempted to add non-positive amount: {amount}");
            return;
        }

        int oldAmount = playerData.coins;
        playerData.AddCoins(amount);

        Debug.Log($"[PlayerDataManager] Added {amount} coins ({oldAmount} → {playerData.coins})");
        OnCoinsChanged?.Invoke(playerData.coins); // 🔔 notify UI
    }

    public void SetCoins(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"[PlayerDataManager] Attempted to set negative coins: {amount}");
            amount = 0;
        }

        int oldAmount = playerData.coins;
        playerData.coins = amount;
        playerData.Save();

        Debug.Log($"[PlayerDataManager] Set coins to {amount} (was {oldAmount})");
        OnCoinsChanged?.Invoke(playerData.coins); // 🔔 notify UI
    }

    public int GetCoins()
    {
        return playerData.coins;
    }

    // ===================== PLAYER NAME METHODS =====================

    public void SetPlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning("[PlayerDataManager] Attempted to set empty player name");
            return;
        }

        string oldName = playerData.playerName;
        playerData.playerName = name;
        playerData.Save();

        Debug.Log($"[PlayerDataManager] Player name changed: '{oldName}' → '{name}'");
        OnPlayerNameChanged?.Invoke(playerData.playerName); // 🔔 notify UI
    }

    public string GetPlayerName()
    {
        return playerData.playerName;
    }

    public bool HasPlayerName()
    {
        return !string.IsNullOrWhiteSpace(playerData.playerName);
    }

    // ===================== UTILITY METHODS =====================

    /// <summary>
    /// Manually refresh all UI elements (useful after loading save data externally)
    /// </summary>
    public void RefreshAllUI()
    {
        OnCoinsChanged?.Invoke(playerData.coins);
        OnPlayerNameChanged?.Invoke(playerData.playerName);
        Debug.Log($"[PlayerDataManager] Manually refreshed all UI - Coins: {playerData.coins}");
    }

    /// <summary>
    /// Reload player data from disk and refresh all UI
    /// </summary>
    public void ReloadDataFromDisk()
    {
        playerData = PlayerData.Load();
        RefreshAllUI();
        Debug.Log($"[PlayerDataManager] Reloaded data from disk - Coins: {playerData.coins}, Name: {playerData.playerName}");
    }

    // ===================== TESTING METHODS =====================

    public void AddCoinsForTesting(int amount)
    {
        AddCoins(amount);
        Debug.Log($"🧪 [TESTING] Added {amount} coins (Total: {playerData.coins})");
    }

    public void ResetCoinsForTesting()
    {
        SetCoins(0);
        Debug.Log("🧪 [TESTING] Reset coins to 0");
    }

    public void GiveStartingCoinsForTesting(int amount = 1000)
    {
        SetCoins(amount);
        Debug.Log($"🧪 [TESTING] Set starting coins to {amount}");
    }

    public void PrintDataForTesting()
    {
        Debug.Log("🧪 [TESTING] ===== PLAYER DATA =====");
        Debug.Log($"🧪 [TESTING] Player Name: {(string.IsNullOrEmpty(playerData.playerName) ? "NOT SET" : playerData.playerName)}");
        Debug.Log($"🧪 [TESTING] Coins: {playerData.coins}");
        Debug.Log("🧪 [TESTING] ========================");
    }
}