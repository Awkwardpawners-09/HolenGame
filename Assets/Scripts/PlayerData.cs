using UnityEngine;
using System;

/// <summary>
/// Serializable player data class.
/// Handles saving/loading player information to/from PlayerPrefs.
/// </summary>
[Serializable]
public class PlayerData
{
    public string playerName = "PlayerName";
    public int coins = 0;
    public int energy = 100; // Default starting energy

    // PlayerPrefs keys
    private const string KEY_PLAYER_NAME = "PlayerName";
    private const string KEY_COINS = "Coins";
    private const string KEY_ENERGY = "Energy";

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

    // ===================== SAVE/LOAD METHODS =====================

    /// <summary>
    /// Save all player data to PlayerPrefs
    /// </summary>
    public void Save()
    {
        PlayerPrefs.SetString(KEY_PLAYER_NAME, playerName);
        PlayerPrefs.SetInt(KEY_COINS, coins);
        PlayerPrefs.SetInt(KEY_ENERGY, energy);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load player data from PlayerPrefs
    /// </summary>
    public static PlayerData Load()
    {
        PlayerData data = new PlayerData();
        data.playerName = PlayerPrefs.GetString(KEY_PLAYER_NAME, "");
        data.coins = PlayerPrefs.GetInt(KEY_COINS, 0);
        data.energy = PlayerPrefs.GetInt(KEY_ENERGY, 100); // Default to 100 if not set
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
        PlayerPrefs.Save();
        Debug.Log("[PlayerData] All player data deleted");
    }
}