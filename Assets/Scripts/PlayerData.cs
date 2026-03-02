using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Serializable player data class.
/// Handles saving/loading player information to/from PlayerPrefs.
/// INCLUDES: Level progression tracking + Time-based Energy System + Completed Levels + Avatar Index
/// </summary>
[Serializable]
public class PlayerData
{
    public bool isSoundEnabled = true;

    private const string KEY_SOUND_ENABLED = "SoundEnabled";

    public void ToggleSound()
    {
        isSoundEnabled = !isSoundEnabled;
        Save();
        Debug.Log($"[PlayerData] Sound {(isSoundEnabled ? "enabled" : "disabled")}");
    }

    public void SetSound(bool enabled)
    {
        isSoundEnabled = enabled;
        Save();
    }

    // Energy system constants
    public const int MAX_ENERGY = 10;
    public const int ENERGY_REGEN_MINUTES = 10;

    public string playerName = "PlayerName";
    public int coins = 5000;
    public int energy = 10;
    public int highestLevelUnlocked = 1;

    // ===================== AVATAR SYSTEM =====================
    // Stores the index of the selected avatar sprite.
    // The actual Sprite[] lives in PlayerDataManager (set in Inspector).
    public int selectedAvatarIndex = 0;

    private const string KEY_AVATAR_INDEX = "SelectedAvatarIndex";

    public void SetAvatarIndex(int index)
    {
        selectedAvatarIndex = index;
        Save();
        Debug.Log($"[PlayerData] Avatar index set to {index}");
    }
    // ===================== END AVATAR SYSTEM =====================

    // ===================== COMPLETED LEVELS TRACKING =====================
    public string completedLevelsData = "";
    private const string KEY_COMPLETED_LEVELS = "CompletedLevels";
    private HashSet<int> completedLevelsCache = null;

    private HashSet<int> GetCompletedLevels()
    {
        if (completedLevelsCache == null)
        {
            completedLevelsCache = new HashSet<int>();
            if (!string.IsNullOrEmpty(completedLevelsData))
            {
                foreach (string entry in completedLevelsData.Split(','))
                {
                    if (int.TryParse(entry.Trim(), out int idx))
                        completedLevelsCache.Add(idx);
                }
            }
        }
        return completedLevelsCache;
    }

    public bool IsLevelCompleted(int levelIndex) => GetCompletedLevels().Contains(levelIndex);

    public void MarkLevelCompleted(int levelIndex)
    {
        if (GetCompletedLevels().Add(levelIndex))
        {
            completedLevelsData = string.Join(",", completedLevelsCache);
            Save();
            Debug.Log($"[PlayerData] Level {levelIndex} marked as completed. All completed: {completedLevelsData}");
        }
    }
    // ===================== END COMPLETED LEVELS =====================

    public string lastEnergyUpdateTime = "";

    private const string KEY_PLAYER_NAME = "PlayerName";
    private const string KEY_COINS = "Coins";
    private const string KEY_ENERGY = "Energy";
    private const string KEY_HIGHEST_LEVEL = "HighestLevelUnlocked";
    private const string KEY_LAST_ENERGY_UPDATE = "LastEnergyUpdate";

    // ===================== COIN METHODS =====================

    public void AddCoins(int amount)
    {
        if (amount > 0) { coins += amount; Save(); }
    }

    public bool SpendCoins(int amount)
    {
        if (amount > 0 && coins >= amount) { coins -= amount; Save(); return true; }
        return false;
    }

    // ===================== ENERGY METHODS =====================

    public void AddEnergy(int amount)
    {
        if (amount > 0)
        {
            energy += amount;
            if (energy > MAX_ENERGY) energy = MAX_ENERGY;
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

    private void UpdateEnergyTimestamp()
    {
        lastEnergyUpdateTime = DateTime.Now.ToString("o");
    }

    public void RegenerateEnergy()
    {
        if (energy >= MAX_ENERGY) { energy = MAX_ENERGY; UpdateEnergyTimestamp(); return; }

        DateTime lastUpdate;
        if (string.IsNullOrEmpty(lastEnergyUpdateTime) || !DateTime.TryParse(lastEnergyUpdateTime, out lastUpdate))
        {
            UpdateEnergyTimestamp();
            return;
        }

        DateTime now = DateTime.Now;
        TimeSpan timePassed = now - lastUpdate;
        double minutesPassed = timePassed.TotalMinutes;
        int energyToAdd = (int)(minutesPassed / ENERGY_REGEN_MINUTES);

        if (energyToAdd > 0)
        {
            energy += energyToAdd;
            if (energy > MAX_ENERGY) energy = MAX_ENERGY;
            double remainderMinutes = minutesPassed % ENERGY_REGEN_MINUTES;
            lastEnergyUpdateTime = now.AddMinutes(-remainderMinutes).ToString("o");
            Save();
            Debug.Log($"[PlayerData] Regenerated {energyToAdd} energy. Current: {energy}/{MAX_ENERGY}");
        }
    }

    public int GetSecondsUntilNextEnergy()
    {
        if (energy >= MAX_ENERGY) return 0;

        DateTime lastUpdate;
        if (string.IsNullOrEmpty(lastEnergyUpdateTime) || !DateTime.TryParse(lastEnergyUpdateTime, out lastUpdate))
            return ENERGY_REGEN_MINUTES * 60;

        TimeSpan timePassed = DateTime.Now - lastUpdate;
        double secondsPassed = timePassed.TotalSeconds;
        double secondsPerEnergy = ENERGY_REGEN_MINUTES * 60;
        double secondsUntilNext = secondsPerEnergy - (secondsPassed % secondsPerEnergy);
        return Mathf.CeilToInt((float)secondsUntilNext);
    }

    // ===================== LEVEL PROGRESSION METHODS =====================

    public void UnlockLevel(int levelNumber)
    {
        if (levelNumber > highestLevelUnlocked)
        {
            highestLevelUnlocked = levelNumber;
            Save();
            Debug.Log($"[PlayerData] Level {levelNumber} unlocked! Highest: {highestLevelUnlocked}");
        }
    }

    public bool IsLevelUnlocked(int levelNumber) => levelNumber <= highestLevelUnlocked;

    public void CompleteLevel(int completedLevel) => UnlockLevel(completedLevel + 1);

    // ===================== SAVE/LOAD METHODS =====================

    public void Save()
    {
        PlayerPrefs.SetString(KEY_PLAYER_NAME, playerName);
        PlayerPrefs.SetInt(KEY_COINS, coins);
        PlayerPrefs.SetInt(KEY_ENERGY, energy);
        PlayerPrefs.SetInt(KEY_HIGHEST_LEVEL, highestLevelUnlocked);
        PlayerPrefs.SetString(KEY_LAST_ENERGY_UPDATE, lastEnergyUpdateTime);
        PlayerPrefs.SetInt(KEY_SOUND_ENABLED, isSoundEnabled ? 1 : 0);
        PlayerPrefs.SetString(KEY_COMPLETED_LEVELS, completedLevelsData);
        PlayerPrefs.SetInt(KEY_AVATAR_INDEX, selectedAvatarIndex); // AVATAR
        PlayerPrefs.Save();
    }

    public static PlayerData Load()
    {
        PlayerData data = new PlayerData();
        data.playerName = PlayerPrefs.GetString(KEY_PLAYER_NAME, "");
        data.coins = PlayerPrefs.GetInt(KEY_COINS, 5000);
        data.energy = PlayerPrefs.GetInt(KEY_ENERGY, 10);
        data.highestLevelUnlocked = PlayerPrefs.GetInt(KEY_HIGHEST_LEVEL, 1);
        data.lastEnergyUpdateTime = PlayerPrefs.GetString(KEY_LAST_ENERGY_UPDATE, DateTime.Now.ToString("o"));
        data.isSoundEnabled = PlayerPrefs.GetInt(KEY_SOUND_ENABLED, 1) == 1;
        data.completedLevelsData = PlayerPrefs.GetString(KEY_COMPLETED_LEVELS, "");
        data.selectedAvatarIndex = PlayerPrefs.GetInt(KEY_AVATAR_INDEX, 0); // AVATAR

        data.RegenerateEnergy();
        return data;
    }

    public static void DeleteAll()
    {
        PlayerPrefs.DeleteKey(KEY_PLAYER_NAME);
        PlayerPrefs.DeleteKey(KEY_COINS);
        PlayerPrefs.DeleteKey(KEY_ENERGY);
        PlayerPrefs.DeleteKey(KEY_HIGHEST_LEVEL);
        PlayerPrefs.DeleteKey(KEY_LAST_ENERGY_UPDATE);
        PlayerPrefs.DeleteKey(KEY_COMPLETED_LEVELS);
        PlayerPrefs.DeleteKey(KEY_AVATAR_INDEX); // AVATAR
        PlayerPrefs.Save();
        Debug.Log("[PlayerData] All player data deleted");
    }
}