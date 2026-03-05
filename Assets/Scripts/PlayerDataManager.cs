using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages all player data and provides event-based updates for UI elements.
/// Singleton pattern with DontDestroyOnLoad for persistence across scenes.
/// INCLUDES: Coin, Energy, Player Name management + Avatar System
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

    // ===================== AVATAR SYSTEM =====================
    [Header("Avatar System")]
    [Tooltip("All available avatar sprites, ordered by index. Index 0 = default.")]
    public Sprite[] avatarSprites;

    [Tooltip("All Image components that should always display the current avatar (e.g. profile picture on HUD).")]
    public List<Image> avatarDisplayImages = new List<Image>();

    /// <summary>Fired whenever the avatar changes. Passes the new Sprite.</summary>
    public static event Action<Sprite> OnAvatarChanged;

    /// <summary>Returns the Sprite for the currently selected avatar index, or null if not set up.</summary>
    public Sprite GetCurrentAvatarSprite()
    {
        if (avatarSprites == null || avatarSprites.Length == 0) return null;
        int idx = Mathf.Clamp(playerData.selectedAvatarIndex, 0, avatarSprites.Length - 1);
        return avatarSprites[idx];
    }

    /// <summary>
    /// Call this from AvatarSelector (or any script) to change and persist the avatar.
    /// </summary>
    public void SetAvatar(int index)
    {
        if (avatarSprites == null || avatarSprites.Length == 0)
        {
            Debug.LogWarning("[PlayerDataManager] avatarSprites array is empty — assign sprites in the Inspector.");
            return;
        }

        index = Mathf.Clamp(index, 0, avatarSprites.Length - 1);
        playerData.SetAvatarIndex(index);

        UpdateAvatarUI();
        OnAvatarChanged?.Invoke(avatarSprites[index]);

        Debug.Log($"[PlayerDataManager] Avatar changed to index {index}");
    }

    private void UpdateAvatarUI()
    {
        avatarDisplayImages.RemoveAll(img => img == null);
        Sprite current = GetCurrentAvatarSprite();
        if (current == null) return;

        foreach (var img in avatarDisplayImages)
            if (img != null) img.sprite = current;
    }

    /// <summary>Register an Image at runtime to always mirror the current avatar.</summary>
    public void RegisterAvatarDisplay(Image image)
    {
        if (image != null && !avatarDisplayImages.Contains(image))
        {
            avatarDisplayImages.Add(image);
            image.sprite = GetCurrentAvatarSprite();
        }
    }

    public void UnregisterAvatarDisplay(Image image) => avatarDisplayImages.Remove(image);
    // ===================== END AVATAR SYSTEM =====================

    // Events for custom UI updates
    public static event Action<int> OnCoinsChanged;
    public static event Action<int> OnEnergyChanged;
    public static event Action<string> OnPlayerNameChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            playerData = PlayerData.Load();
            Debug.Log($"[PlayerDataManager] gachaQuestCompleted={playerData.gachaQuestCompleted}, gachaQuestClaimed={playerData.gachaQuestClaimed}");

            Debug.Log($"[PlayerDataManager] Loaded - Name: {playerData.playerName}, Coins: {playerData.coins}, Energy: {playerData.energy}/{PlayerData.MAX_ENERGY}, Avatar: {playerData.selectedAvatarIndex}");

            Invoke(nameof(NotifyInitialValues), 0.1f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
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

        if (oldEnergy != playerData.energy)
        {
            UpdateEnergyUI();
            OnEnergyChanged?.Invoke(playerData.energy);
        }
    }

    private void NotifyInitialValues()
    {
        UpdateAllUI();
        OnPlayerNameChanged?.Invoke(playerData.playerName);
        OnCoinsChanged?.Invoke(playerData.coins);
        OnEnergyChanged?.Invoke(playerData.energy);
        OnAvatarChanged?.Invoke(GetCurrentAvatarSprite());

        Debug.Log($"[PlayerDataManager] Initial UI update complete.");
    }

    // ===================== UI UPDATE METHODS =====================

    public void UpdateAllUI()
    {
        UpdatePlayerNameUI();
        UpdateCoinUI();
        UpdateEnergyUI();
        UpdateAvatarUI();
    }

    private void UpdatePlayerNameUI()
    {
        playerNameUIElements.RemoveAll(item => item == null);
        foreach (var ui in playerNameUIElements)
            if (ui != null) ui.text = playerData.playerName;
    }

    private void UpdateCoinUI()
    {
        coinUIElements.RemoveAll(item => item == null);
        foreach (var ui in coinUIElements)
            if (ui != null) ui.text = playerData.coins.ToString();
    }

    private void UpdateEnergyUI()
    {
        energyUIElements.RemoveAll(item => item == null);
        foreach (var ui in energyUIElements)
            if (ui != null) ui.text = $"{playerData.energy}/{PlayerData.MAX_ENERGY}";
    }

    // ===================== RUNTIME UI REGISTRATION =====================

    public void RegisterPlayerNameUI(TMPro.TextMeshProUGUI uiElement)
    {
        if (uiElement != null && !playerNameUIElements.Contains(uiElement))
        {
            playerNameUIElements.Add(uiElement);
            uiElement.text = playerData.playerName;
        }
    }

    public void RegisterCoinUI(TMPro.TextMeshProUGUI uiElement)
    {
        if (uiElement != null && !coinUIElements.Contains(uiElement))
        {
            coinUIElements.Add(uiElement);
            uiElement.text = playerData.coins.ToString();
        }
    }

    public void RegisterEnergyUI(TMPro.TextMeshProUGUI uiElement)
    {
        if (uiElement != null && !energyUIElements.Contains(uiElement))
        {
            energyUIElements.Add(uiElement);
            uiElement.text = $"{playerData.energy}/{PlayerData.MAX_ENERGY}";
        }
    }

    public void UnregisterPlayerNameUI(TMPro.TextMeshProUGUI uiElement) => playerNameUIElements.Remove(uiElement);
    public void UnregisterCoinUI(TMPro.TextMeshProUGUI uiElement) => coinUIElements.Remove(uiElement);
    public void UnregisterEnergyUI(TMPro.TextMeshProUGUI uiElement) => energyUIElements.Remove(uiElement);

    // ===================== COIN METHODS =====================

    public bool SpendCoins(int amount)
    {
        if (amount <= 0) { Debug.LogWarning($"[PlayerDataManager] Attempted to spend non-positive amount: {amount}"); return false; }

        if (playerData.SpendCoins(amount))
        {
            UpdateCoinUI();
            OnCoinsChanged?.Invoke(playerData.coins);
            return true;
        }

        Debug.LogWarning($"[PlayerDataManager] Failed to spend {amount} coins (only have {playerData.coins})");
        return false;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        playerData.AddCoins(amount);
        UpdateCoinUI();
        OnCoinsChanged?.Invoke(playerData.coins);
    }

    public void SetCoins(int amount)
    {
        if (amount < 0) amount = 0;
        playerData.coins = amount;
        playerData.Save();
        UpdateCoinUI();
        OnCoinsChanged?.Invoke(playerData.coins);
    }

    public int GetCoins() => playerData.coins;

    // ===================== ENERGY METHODS =====================

    public bool SpendEnergy(int amount)
    {
        if (amount <= 0) return false;

        if (playerData.SpendEnergy(amount))
        {
            UpdateEnergyUI();
            OnEnergyChanged?.Invoke(playerData.energy);
            return true;
        }

        Debug.LogWarning($"[PlayerDataManager] Failed to spend {amount} energy (only have {playerData.energy})");
        return false;
    }

    public void AddEnergy(int amount)
    {
        if (amount <= 0) return;
        playerData.AddEnergy(amount);
        UpdateEnergyUI();
        OnEnergyChanged?.Invoke(playerData.energy);
    }

    public void SetEnergy(int amount)
    {
        amount = Mathf.Clamp(amount, 0, PlayerData.MAX_ENERGY);
        playerData.energy = amount;
        playerData.Save();
        UpdateEnergyUI();
        OnEnergyChanged?.Invoke(playerData.energy);
    }

    public int GetEnergy() => playerData.energy;
    public int GetSecondsUntilNextEnergy() => playerData.GetSecondsUntilNextEnergy();
    public bool HasEnergy(int amount) => playerData.energy >= amount;

    // ===================== PLAYER NAME METHODS =====================

    public void SetPlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) { Debug.LogWarning("[PlayerDataManager] Attempted to set empty player name"); return; }
        playerData.playerName = name;
        playerData.Save();
        UpdatePlayerNameUI();
        OnPlayerNameChanged?.Invoke(playerData.playerName);
    }

    public string GetPlayerName() => playerData.playerName;
    public bool HasPlayerName() => !string.IsNullOrWhiteSpace(playerData.playerName);

    // ===================== UTILITY METHODS =====================

    public void RefreshAllUI()
    {
        UpdateAllUI();
        OnPlayerNameChanged?.Invoke(playerData.playerName);
        OnCoinsChanged?.Invoke(playerData.coins);
        OnEnergyChanged?.Invoke(playerData.energy);
        OnAvatarChanged?.Invoke(GetCurrentAvatarSprite());
    }

    public void ReloadDataFromDisk()
    {
        playerData = PlayerData.Load();
        RefreshAllUI();
    }

    public void SaveData() => playerData.Save();

    private void OnApplicationQuit()
{
    playerData.Save();
    Debug.Log("[PlayerDataManager] Data saved on quit.");
}

    // ===================== TESTING METHODS =====================

    public void AddCoinsForTesting(int amount) => AddCoins(amount);
    public void ResetCoinsForTesting() => SetCoins(0);
    public void GiveStartingCoinsForTesting(int amount = 1000) => SetCoins(amount);
    public void AddEnergyForTesting(int amount) => AddEnergy(amount);
    public void ResetEnergyForTesting() => SetEnergy(0);
    public void GiveStartingEnergyForTesting(int amount = 100) => SetEnergy(amount);

    public void PrintDataForTesting()
    {
        Debug.Log("🧪 [TESTING] ===== PLAYER DATA =====");
        Debug.Log($"🧪 Player Name: {playerData.playerName}");
        Debug.Log($"🧪 Coins: {playerData.coins}");
        Debug.Log($"🧪 Energy: {playerData.energy}");
        Debug.Log($"🧪 Avatar Index: {playerData.selectedAvatarIndex}");
        Debug.Log("🧪 [TESTING] ========================");
    }

    public void ResetAllDataForTesting()
    {
        playerData.playerName = "";
        playerData.coins = 0;
        playerData.energy = 0;
        playerData.selectedAvatarIndex = 0;
        playerData.Save();
        RefreshAllUI();
    }
}