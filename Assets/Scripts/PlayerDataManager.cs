using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages all player data and provides event-based updates for UI elements.
/// Singleton pattern with DontDestroyOnLoad for persistence across scenes.
/// Combines coin management, energy tracking, and player name management.
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    [Header("Player Data")]
    public PlayerData playerData;

    [Header("Energy Regeneration")]
    [Tooltip("How often to check for energy regeneration (in seconds)")]
    public float energyCheckInterval = 1f;
    private float energyCheckTimer = 0f;

    [Header("UI Element Lists - Add UI components here")]
    [Tooltip("Add all UI elements that should display player name")]
    public List<TMPro.TextMeshProUGUI> playerNameUIElements = new List<TMPro.TextMeshProUGUI>();

    [Tooltip("Add all UI elements that should display coin count")]
    public List<TMPro.TextMeshProUGUI> coinUIElements = new List<TMPro.TextMeshProUGUI>();

    [Tooltip("Add all UI elements that should display energy count")]
    public List<TMPro.TextMeshProUGUI> energyUIElements = new List<TMPro.TextMeshProUGUI>();

    // 🔔 Events for custom UI updates (if you need more control)
    public static event Action<int> OnCoinsChanged;
    public static event Action<int> OnEnergyChanged;
    public static event Action<string> OnPlayerNameChanged;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load player data from disk
            playerData = PlayerData.Load();

            Debug.Log($"[PlayerDataManager] Loaded - Name: {playerData.playerName}, Coins: {playerData.coins}, Energy: {playerData.energy}/{PlayerData.MAX_ENERGY}");

            // Notify all UI elements of initial values after a short delay
            // This ensures all UI elements are ready
            Invoke(nameof(NotifyInitialValues), 0.1f);
        }
        else
        {
            // Destroy duplicate instances
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Check for energy regeneration periodically
        energyCheckTimer += Time.deltaTime;
        if (energyCheckTimer >= energyCheckInterval)
        {
            energyCheckTimer = 0f;
            CheckEnergyRegeneration();
        }
    }

    private void CheckEnergyRegeneration()
    {
        int oldEnergy = playerData.energy;
        playerData.RegenerateEnergy();

        // If energy changed, update UI
        if (oldEnergy != playerData.energy)
        {
            UpdateEnergyUI();
            OnEnergyChanged?.Invoke(playerData.energy);
        }
    }

    private void NotifyInitialValues()
    {
        // Update all registered UI elements
        UpdateAllUI();

        // Trigger events for any custom listeners
        OnPlayerNameChanged?.Invoke(playerData.playerName);
        OnCoinsChanged?.Invoke(playerData.coins);
        OnEnergyChanged?.Invoke(playerData.energy);

        Debug.Log($"[PlayerDataManager] Initial UI update complete - Name: {playerData.playerName}, Coins: {playerData.coins}, Energy: {playerData.energy}");
    }

    // ===================== UI UPDATE METHODS =====================

    /// <summary>
    /// Updates all registered UI elements with current values
    /// </summary>
    public void UpdateAllUI()
    {
        UpdatePlayerNameUI();
        UpdateCoinUI();
        UpdateEnergyUI();
    }

    /// <summary>
    /// Updates all player name UI elements
    /// </summary>
    private void UpdatePlayerNameUI()
    {
        // Remove any null references (destroyed UI elements)
        playerNameUIElements.RemoveAll(item => item == null);

        // Update each UI element
        foreach (var uiElement in playerNameUIElements)
        {
            if (uiElement != null)
            {
                uiElement.text = playerData.playerName;
            }
        }
    }

    /// <summary>
    /// Updates all coin UI elements
    /// </summary>
    private void UpdateCoinUI()
    {
        // Remove any null references
        coinUIElements.RemoveAll(item => item == null);

        // Update each UI element
        foreach (var uiElement in coinUIElements)
        {
            if (uiElement != null)
            {
                uiElement.text = playerData.coins.ToString();
            }
        }
    }

    /// <summary>
    /// Updates all energy UI elements
    /// </summary>
    private void UpdateEnergyUI()
    {
        // Remove any null references
        energyUIElements.RemoveAll(item => item == null);

        // Update each UI element with "X/10" format
        foreach (var uiElement in energyUIElements)
        {
            if (uiElement != null)
            {
                uiElement.text = $"{playerData.energy}/{PlayerData.MAX_ENERGY}";
            }
        }
    }

    // ===================== RUNTIME UI REGISTRATION =====================

    /// <summary>
    /// Register a UI element for player name updates at runtime
    /// </summary>
    public void RegisterPlayerNameUI(TMPro.TextMeshProUGUI uiElement)
    {
        if (uiElement != null && !playerNameUIElements.Contains(uiElement))
        {
            playerNameUIElements.Add(uiElement);
            uiElement.text = playerData.playerName; // Update immediately
            Debug.Log($"[PlayerDataManager] Registered player name UI element: {uiElement.gameObject.name}");
        }
    }

    /// <summary>
    /// Register a UI element for coin updates at runtime
    /// </summary>
    public void RegisterCoinUI(TMPro.TextMeshProUGUI uiElement)
    {
        if (uiElement != null && !coinUIElements.Contains(uiElement))
        {
            coinUIElements.Add(uiElement);
            uiElement.text = playerData.coins.ToString(); // Update immediately
            Debug.Log($"[PlayerDataManager] Registered coin UI element: {uiElement.gameObject.name}");
        }
    }

    /// <summary>
    /// Register a UI element for energy updates at runtime
    /// </summary>
    public void RegisterEnergyUI(TMPro.TextMeshProUGUI uiElement)
    {
        if (uiElement != null && !energyUIElements.Contains(uiElement))
        {
            energyUIElements.Add(uiElement);
            uiElement.text = $"{playerData.energy}/{PlayerData.MAX_ENERGY}"; // Update immediately with format
            Debug.Log($"[PlayerDataManager] Registered energy UI element: {uiElement.gameObject.name}");
        }
    }

    /// <summary>
    /// Unregister UI elements (useful when destroying UI)
    /// </summary>
    public void UnregisterPlayerNameUI(TMPro.TextMeshProUGUI uiElement)
    {
        playerNameUIElements.Remove(uiElement);
    }

    public void UnregisterCoinUI(TMPro.TextMeshProUGUI uiElement)
    {
        coinUIElements.Remove(uiElement);
    }

    public void UnregisterEnergyUI(TMPro.TextMeshProUGUI uiElement)
    {
        energyUIElements.Remove(uiElement);
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

            // Update UI
            UpdateCoinUI();
            OnCoinsChanged?.Invoke(playerData.coins);

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

        // Update UI
        UpdateCoinUI();
        OnCoinsChanged?.Invoke(playerData.coins);
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

        // Update UI
        UpdateCoinUI();
        OnCoinsChanged?.Invoke(playerData.coins);
    }

    public int GetCoins()
    {
        return playerData.coins;
    }

    // ===================== ENERGY METHODS =====================

    public bool SpendEnergy(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[PlayerDataManager] Attempted to spend non-positive energy amount: {amount}");
            return false;
        }

        int oldAmount = playerData.energy;

        if (playerData.SpendEnergy(amount))
        {
            Debug.Log($"[PlayerDataManager] Spent {amount} energy ({oldAmount} → {playerData.energy})");

            // Update UI
            UpdateEnergyUI();
            OnEnergyChanged?.Invoke(playerData.energy);

            return true;
        }

        Debug.LogWarning($"[PlayerDataManager] Failed to spend {amount} energy (only have {playerData.energy})");
        return false;
    }

    public void AddEnergy(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[PlayerDataManager] Attempted to add non-positive energy amount: {amount}");
            return;
        }

        int oldAmount = playerData.energy;
        playerData.AddEnergy(amount);

        Debug.Log($"[PlayerDataManager] Added {amount} energy ({oldAmount} → {playerData.energy})");

        // Update UI
        UpdateEnergyUI();
        OnEnergyChanged?.Invoke(playerData.energy);
    }

    public void SetEnergy(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"[PlayerDataManager] Attempted to set negative energy: {amount}");
            amount = 0;
        }

        if (amount > PlayerData.MAX_ENERGY)
        {
            Debug.LogWarning($"[PlayerDataManager] Attempted to set energy above max ({amount}), capping at {PlayerData.MAX_ENERGY}");
            amount = PlayerData.MAX_ENERGY;
        }

        int oldAmount = playerData.energy;
        playerData.energy = amount;
        playerData.Save();

        Debug.Log($"[PlayerDataManager] Set energy to {amount}/{PlayerData.MAX_ENERGY} (was {oldAmount})");

        // Update UI
        UpdateEnergyUI();
        OnEnergyChanged?.Invoke(playerData.energy);
    }

    public int GetEnergy()
    {
        return playerData.energy;
    }

    /// <summary>
    /// Get seconds until next energy point regenerates
    /// </summary>
    public int GetSecondsUntilNextEnergy()
    {
        return playerData.GetSecondsUntilNextEnergy();
    }

    /// <summary>
    /// Check if player has enough energy
    /// </summary>
    public bool HasEnergy(int amount)
    {
        return playerData.energy >= amount;
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

        // Update UI
        UpdatePlayerNameUI();
        OnPlayerNameChanged?.Invoke(playerData.playerName);
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
        UpdateAllUI();

        // Trigger events
        OnPlayerNameChanged?.Invoke(playerData.playerName);
        OnCoinsChanged?.Invoke(playerData.coins);
        OnEnergyChanged?.Invoke(playerData.energy);

        Debug.Log($"[PlayerDataManager] Manually refreshed all UI - Name: {playerData.playerName}, Coins: {playerData.coins}, Energy: {playerData.energy}");
    }

    /// <summary>
    /// Reload player data from disk and refresh all UI
    /// </summary>
    public void ReloadDataFromDisk()
    {
        playerData = PlayerData.Load();
        RefreshAllUI();
        Debug.Log($"[PlayerDataManager] Reloaded data from disk - Name: {playerData.playerName}, Coins: {playerData.coins}, Energy: {playerData.energy}");
    }

    /// <summary>
    /// Save current player data to disk
    /// </summary>
    public void SaveData()
    {
        playerData.Save();
        Debug.Log($"[PlayerDataManager] Data saved - Name: {playerData.playerName}, Coins: {playerData.coins}, Energy: {playerData.energy}");
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

    public void AddEnergyForTesting(int amount)
    {
        AddEnergy(amount);
        Debug.Log($"🧪 [TESTING] Added {amount} energy (Total: {playerData.energy})");
    }

    public void ResetEnergyForTesting()
    {
        SetEnergy(0);
        Debug.Log("🧪 [TESTING] Reset energy to 0");
    }

    public void GiveStartingEnergyForTesting(int amount = 100)
    {
        SetEnergy(amount);
        Debug.Log($"🧪 [TESTING] Set starting energy to {amount}");
    }

    public void PrintDataForTesting()
    {
        Debug.Log("🧪 [TESTING] ===== PLAYER DATA =====");
        Debug.Log($"🧪 [TESTING] Player Name: {(string.IsNullOrEmpty(playerData.playerName) ? "NOT SET" : playerData.playerName)}");
        Debug.Log($"🧪 [TESTING] Coins: {playerData.coins}");
        Debug.Log($"🧪 [TESTING] Energy: {playerData.energy}");
        Debug.Log($"🧪 [TESTING] Registered Name UIs: {playerNameUIElements.Count}");
        Debug.Log($"🧪 [TESTING] Registered Coin UIs: {coinUIElements.Count}");
        Debug.Log($"🧪 [TESTING] Registered Energy UIs: {energyUIElements.Count}");
        Debug.Log("🧪 [TESTING] ========================");
    }

    public void ResetAllDataForTesting()
    {
        playerData.playerName = "";
        playerData.coins = 0;
        playerData.energy = 0;
        playerData.Save();
        RefreshAllUI();
        Debug.Log("🧪 [TESTING] Reset all player data");
    }
}