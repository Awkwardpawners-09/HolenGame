using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Serializable player data class.
/// Handles saving/loading player information to/from PlayerPrefs.
/// NOW INCLUDES: Level progression tracking
/// </summary>
[Serializable]
public class PlayerData
{
    public string playerName = "PlayerName";
    public int coins = 5000; // Default starting Coins
    public int energy = 100; // Default starting energy
    public int highestLevelUnlocked = 1; // Track progression

    // PlayerPrefs keys
    private const string KEY_PLAYER_NAME = "PlayerName";
    private const string KEY_COINS = "Coins";
    private const string KEY_ENERGY = "Energy";
    private const string KEY_HIGHEST_LEVEL = "HighestLevelUnlocked";

    // ===================== COIN METHODS =====================

    public void AddCoins(int amount)
    {
        if (amount > 0)
        {
            coins += amount;
            Save();
        }
    }

    public bool SpendCoins(int amount)
    {
        if (amount > 0 && coins >= amount)
        {
            coins -= amount;
            Save();
            return true;
        }
        return false;
    }

    // ===================== ENERGY METHODS =====================

    public void AddEnergy(int amount)
    {
        if (amount > 0)
        {
            energy += amount;
            Save();
        }
    }

    public bool SpendEnergy(int amount)
    {
        if (amount > 0 && energy >= amount)
        {
            energy -= amount;
            Save();
            return true;
        }
        return false;
    }

    // ===================== LEVEL PROGRESSION METHODS =====================

    /// <summary>
    /// Unlock a level by number. Will update highestLevelUnlocked if needed.
    /// </summary>
    /// <param name="levelNumber">The level number to unlock</param>
    public void UnlockLevel(int levelNumber)
    {
        if (levelNumber > highestLevelUnlocked)
        {
            highestLevelUnlocked = levelNumber;
            Save();
            Debug.Log($"[PlayerData] Level {levelNumber} unlocked! Highest level is now: {highestLevelUnlocked}");
        }
    }

    /// <summary>
    /// Check if a specific level is unlocked
    /// </summary>
    /// <param name="levelNumber">The level number to check</param>
    /// <returns>True if the level is unlocked</returns>
    public bool IsLevelUnlocked(int levelNumber)
    {
        return levelNumber <= highestLevelUnlocked;
    }

    /// <summary>
    /// Complete a level and unlock the next one
    /// </summary>
    /// <param name="completedLevel">The level that was just completed</param>
    public void CompleteLevel(int completedLevel)
    {
        UnlockLevel(completedLevel + 1);
    }

    // ===================== SAVE/LOAD METHODS =====================

    /// <summary>
    /// Save all player data to PlayerPrefs
    /// </summary>
    public void Save()
    {
        PlayerPrefs.SetString(KEY_PLAYER_NAME, playerName);
        PlayerPrefs.SetInt(KEY_COINS, coins);
        PlayerPrefs.SetInt(KEY_ENERGY, energy);
        PlayerPrefs.SetInt(KEY_HIGHEST_LEVEL, highestLevelUnlocked);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load player data from PlayerPrefs
    /// </summary>
    public static PlayerData Load()
    {
        PlayerData data = new PlayerData();
        data.playerName = PlayerPrefs.GetString(KEY_PLAYER_NAME, "Player");
        data.coins = PlayerPrefs.GetInt(KEY_COINS, 5000);
        data.energy = PlayerPrefs.GetInt(KEY_ENERGY, 100);
        data.highestLevelUnlocked = PlayerPrefs.GetInt(KEY_HIGHEST_LEVEL, 1); // Default to level 1 unlocked
        return data;
    }

    /// <summary>
    /// Delete all saved player data (for testing or reset)
    /// </summary>
    public static void DeleteAll()
    {
        PlayerPrefs.DeleteKey(KEY_PLAYER_NAME);
        PlayerPrefs.DeleteKey(KEY_COINS);
        PlayerPrefs.DeleteKey(KEY_ENERGY);
        PlayerPrefs.DeleteKey(KEY_HIGHEST_LEVEL);
        PlayerPrefs.Save();
        Debug.Log("[PlayerData] All player data deleted");
    }
}