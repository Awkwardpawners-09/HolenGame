using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Serializable player data class.
/// Handles saving/loading player information to/from PlayerPrefs.
/// NOW INCLUDES: Level progression tracking + Time-based Energy System
/// </summary>
[Serializable]
public class PlayerData
{
    // Add with other fields (around line 11)
    public bool isSoundEnabled = true; // Default: sound ON

    // Add to PlayerPrefs keys section (around line 20)
    private const string KEY_SOUND_ENABLED = "SoundEnabled";

    // ===================== SETTINGS METHODS =====================

    /// <summary>
    /// Toggle sound on/off and save
    /// </summary>
    public void ToggleSound()
    {
        isSoundEnabled = !isSoundEnabled;
        Save();
        Debug.Log($"[PlayerData] Sound {(isSoundEnabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Set sound state directly
    /// </summary>
    public void SetSound(bool enabled)
    {
        isSoundEnabled = enabled;
        Save();
    }

    // Energy system constants
    public const int MAX_ENERGY = 10;
    public const int ENERGY_REGEN_MINUTES = 10; // Regenerate 1 energy every 10 minutes

    public string playerName = "PlayerName";
    public int coins = 5000; // Default starting Coins
    public int energy = 10; // Default starting energy (max)
    public int highestLevelUnlocked = 1; // Track progression

    // Time tracking for energy regeneration
    public string lastEnergyUpdateTime = ""; // Stored as ISO 8601 string

    // PlayerPrefs keys
    private const string KEY_PLAYER_NAME = "PlayerName";
    private const string KEY_COINS = "Coins";
    private const string KEY_ENERGY = "Energy";
    private const string KEY_HIGHEST_LEVEL = "HighestLevelUnlocked";
    private const string KEY_LAST_ENERGY_UPDATE = "LastEnergyUpdate";

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
            if (energy > MAX_ENERGY)
            {
                energy = MAX_ENERGY;
            }
            UpdateEnergyTimestamp();
            Save();
        }
    }

    public bool SpendEnergy(int amount)
    {
        if (amount > 0 && energy >= amount)
        {
            energy -= amount;
            UpdateEnergyTimestamp();
            Save();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Updates the energy timestamp to current time
    /// </summary>
    private void UpdateEnergyTimestamp()
    {
        lastEnergyUpdateTime = DateTime.Now.ToString("o"); // ISO 8601 format
    }

    /// <summary>
    /// Calculate and apply energy regeneration based on time passed
    /// Call this when loading game or checking energy
    /// </summary>
    public void RegenerateEnergy()
    {
        // Don't regenerate if already at max
        if (energy >= MAX_ENERGY)
        {
            energy = MAX_ENERGY;
            UpdateEnergyTimestamp();
            return;
        }

        // Parse last update time
        DateTime lastUpdate;
        if (string.IsNullOrEmpty(lastEnergyUpdateTime) || !DateTime.TryParse(lastEnergyUpdateTime, out lastUpdate))
        {
            // No valid timestamp, set to now
            UpdateEnergyTimestamp();
            return;
        }

        // Calculate time passed
        DateTime now = DateTime.Now;
        TimeSpan timePassed = now - lastUpdate;
        double minutesPassed = timePassed.TotalMinutes;

        // Calculate energy to regenerate (1 energy per 10 minutes)
        int energyToAdd = (int)(minutesPassed / ENERGY_REGEN_MINUTES);

        if (energyToAdd > 0)
        {
            energy += energyToAdd;
            if (energy > MAX_ENERGY)
            {
                energy = MAX_ENERGY;
            }

            // Update timestamp to account for the regenerated energy
            // We subtract the "used" time and keep the remainder
            double remainderMinutes = minutesPassed % ENERGY_REGEN_MINUTES;
            lastEnergyUpdateTime = now.AddMinutes(-remainderMinutes).ToString("o");

            Save();
            Debug.Log($"[PlayerData] Regenerated {energyToAdd} energy. Current: {energy}/{MAX_ENERGY}");
        }
    }

    /// <summary>
    /// Get time until next energy point regenerates (in seconds)
    /// Returns 0 if energy is full
    /// </summary>
    public int GetSecondsUntilNextEnergy()
    {
        if (energy >= MAX_ENERGY)
            return 0;

        DateTime lastUpdate;
        if (string.IsNullOrEmpty(lastEnergyUpdateTime) || !DateTime.TryParse(lastEnergyUpdateTime, out lastUpdate))
        {
            return ENERGY_REGEN_MINUTES * 60; // Full cooldown if no timestamp
        }

        TimeSpan timePassed = DateTime.Now - lastUpdate;
        double secondsPassed = timePassed.TotalSeconds;
        double secondsPerEnergy = ENERGY_REGEN_MINUTES * 60;

        double secondsUntilNext = secondsPerEnergy - (secondsPassed % secondsPerEnergy);
        return Mathf.CeilToInt((float)secondsUntilNext);
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
        PlayerPrefs.SetString(KEY_LAST_ENERGY_UPDATE, lastEnergyUpdateTime);
        PlayerPrefs.SetInt(KEY_SOUND_ENABLED, isSoundEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load player data from PlayerPrefs
    /// </summary>
    public static PlayerData Load()
    {
        PlayerData data = new PlayerData();
        data.playerName = PlayerPrefs.GetString(KEY_PLAYER_NAME, ""); // Empty string = no name set yet
        data.coins = PlayerPrefs.GetInt(KEY_COINS, 5000);
        data.energy = PlayerPrefs.GetInt(KEY_ENERGY, 10); // Start with max energy
        data.highestLevelUnlocked = PlayerPrefs.GetInt(KEY_HIGHEST_LEVEL, 1); // Default to level 1 unlocked
        data.lastEnergyUpdateTime = PlayerPrefs.GetString(KEY_LAST_ENERGY_UPDATE, DateTime.Now.ToString("o"));
        data.isSoundEnabled = PlayerPrefs.GetInt(KEY_SOUND_ENABLED, 1) == 1; // Default ON


        // Regenerate energy based on time passed
        data.RegenerateEnergy();

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
        PlayerPrefs.DeleteKey(KEY_LAST_ENERGY_UPDATE);
        PlayerPrefs.Save();
        Debug.Log("[PlayerData] All player data deleted");
    }
}