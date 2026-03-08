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

    /// <summary>
    /// The player's current level. Starts at 1.
    /// Stage buttons with a matching index are unlocked when the player reaches that level.
    /// Example: level=1 unlocks stage index 1 only. level=2 unlocks stage index 1 and 2, etc.
    /// </summary>
    public int level = 1;

    // ===================== AVATAR SYSTEM =====================
    public int selectedAvatarIndex = 0;

    private const string KEY_AVATAR_INDEX = "SelectedAvatarIndex";

    public void SetAvatarIndex(int index)
    {
        selectedAvatarIndex = index;
        Save();
        Debug.Log($"[PlayerData] Avatar index set to {index}");
    }
    // ===================== END AVATAR SYSTEM =====================

    // ===================== QUEST SYSTEM =====================
    public bool gachaQuestCompleted = false;
    public bool gachaQuestClaimed = false;

    public const string KEY_GACHA_QUEST_COMPLETED = "GachaQuestCompleted";
    public const string KEY_GACHA_QUEST_CLAIMED = "GachaQuestClaimed";

    public bool loginQuestClaimed = false;
    public const string KEY_LOGIN_QUEST_CLAIMED = "LoginQuestClaimed";

    public bool arcadeLevel1QuestCompleted = false;
    public bool arcadeLevel1QuestClaimed = false;
    public const string KEY_ARCADE_LEVEL1_QUEST_COMPLETED = "ArcadeLevel1QuestCompleted";
    public const string KEY_ARCADE_LEVEL1_QUEST_CLAIMED = "ArcadeLevel1QuestClaimed";

    public bool gacha1xQuestCompleted = false;
    public bool gacha1xQuestClaimed = false;
    public const string KEY_GACHA1X_QUEST_COMPLETED = "Gacha1xQuestCompleted";
    public const string KEY_GACHA1X_QUEST_CLAIMED = "Gacha1xQuestClaimed";

    public bool allQuestsClaimed = false;
    public const string KEY_ALL_QUESTS_CLAIMED = "AllQuestsClaimed";
    // ===================== END QUEST SYSTEM =====================

    // ===================== ACHIEVEMENT SYSTEM =====================
    public bool kalyeStageAchievementCompleted = false;
    public bool kalyeStageAchievementClaimed = false;
    public const string KEY_KALYE_ACHIEVEMENT_COMPLETED = "KalyeAchievementCompleted";
    public const string KEY_KALYE_ACHIEVEMENT_CLAIMED = "KalyeAchievementClaimed";

    public bool rareHolenAchievementCompleted = false;
    public bool rareHolenAchievementClaimed = false;
    public const string KEY_RARE_HOLEN_ACHIEVEMENT_COMPLETED = "RareHolenAchievementCompleted";
    public const string KEY_RARE_HOLEN_ACHIEVEMENT_CLAIMED = "RareHolenAchievementClaimed";

    public bool collect10HolensAchievementCompleted = false;
    public bool collect10HolensAchievementClaimed = false;
    public int totalHolensCollected = 0;
    public const string KEY_COLLECT10_ACHIEVEMENT_COMPLETED = "Collect10AchievementCompleted";
    public const string KEY_COLLECT10_ACHIEVEMENT_CLAIMED = "Collect10AchievementClaimed";
    public const string KEY_TOTAL_HOLENS_COLLECTED = "TotalHolensCollected";
    // ===================== END ACHIEVEMENT SYSTEM =====================

    // ===================== COMPLETED LEVELS TRACKING =====================
    public string completedLevelsData = "";
    private const string KEY_COMPLETED_LEVELS = "CompletedLevels";
    private HashSet<int> completedLevelsCache = null;

    // ===================== FIRST CLEAR TRACKING =====================
    // Stores stage IDs that have already been first-cleared, as a comma-separated string.
    // e.g. "Stage1,Stage2,Stage3"
    public string firstClearedStages = "";
    private const string KEY_FIRST_CLEARED_STAGES = "FirstClearedStages";
    private HashSet<string> firstClearedCache = null;

    private HashSet<string> GetFirstClearedStages()
    {
        if (firstClearedCache == null)
        {
            firstClearedCache = new HashSet<string>();
            if (!string.IsNullOrEmpty(firstClearedStages))
            {
                foreach (string id in firstClearedStages.Split(','))
                {
                    string trimmed = id.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        firstClearedCache.Add(trimmed);
                }
            }
        }
        return firstClearedCache;
    }

    /// <summary>Returns true if this stage has never been first-cleared before.</summary>
    public bool IsFirstClear(string stageID) => !GetFirstClearedStages().Contains(stageID);

    /// <summary>Marks a stage as first-cleared so the bonus never triggers again.</summary>
    public void MarkFirstCleared(string stageID)
    {
        if (GetFirstClearedStages().Add(stageID))
        {
            firstClearedStages = string.Join(",", firstClearedCache);
            Save();
            Debug.Log($"[PlayerData] Stage '{stageID}' marked as first-cleared.");
        }
    }
    // ===================== END FIRST CLEAR TRACKING =====================

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
    private const string KEY_LEVEL = "PlayerLevel";
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

    // ===================== LEVEL METHODS =====================

    /// <summary>
    /// Increments the player's level by 1. Only call this once per stage clear.
    /// The guard against double-incrementing is handled by IsLevelCompleted() in LevelCompleteButton.
    /// </summary>
    public void IncrementLevel()
    {
        level += 1;
        Save();
        Debug.Log($"[PlayerData] Player leveled up! Current level: {level}");
    }

    public bool IsStageUnlocked(int stageIndex) => stageIndex <= level;

    // ── Compatibility shims ────────────────────────────────────────────────────
    // These keep existing scripts (ArcadeLevelManager, LevelButton, etc.) compiling
    // without modification. They delegate to the new level field.

    /// <summary>Unlocks the level if it is higher than the current level. Legacy-compatible.</summary>
    public void UnlockLevel(int levelNumber)
    {
        if (levelNumber > level)
        {
            level = levelNumber;
            Save();
            Debug.Log($"[PlayerData] UnlockLevel({levelNumber}) called — level is now {level}");
        }
    }

    /// <summary>Marks stageIndex+1 as unlocked. Legacy-compatible.</summary>
    public void CompleteLevel(int completedStageIndex) => UnlockLevel(completedStageIndex + 1);

    /// <summary>Returns true if the given level number is <= the player's current level. Legacy-compatible.</summary>
    public bool IsLevelUnlocked(int levelNumber) => levelNumber <= level;

    // ── End compatibility shims ────────────────────────────────────────────────

    // ===================== SAVE/LOAD METHODS =====================

    public void Save()
    {
        PlayerPrefs.SetString(KEY_PLAYER_NAME, playerName);
        PlayerPrefs.SetInt(KEY_COINS, coins);
        PlayerPrefs.SetInt(KEY_ENERGY, energy);
        PlayerPrefs.SetInt(KEY_LEVEL, level);
        PlayerPrefs.SetString(KEY_LAST_ENERGY_UPDATE, lastEnergyUpdateTime);
        PlayerPrefs.SetInt(KEY_SOUND_ENABLED, isSoundEnabled ? 1 : 0);
        PlayerPrefs.SetString(KEY_COMPLETED_LEVELS, completedLevelsData);
        PlayerPrefs.SetString(KEY_FIRST_CLEARED_STAGES, firstClearedStages);
        PlayerPrefs.SetInt(KEY_AVATAR_INDEX, selectedAvatarIndex);
        PlayerPrefs.SetInt(KEY_GACHA_QUEST_COMPLETED, gachaQuestCompleted ? 1 : 0);
        PlayerPrefs.SetInt(KEY_GACHA_QUEST_CLAIMED, gachaQuestClaimed ? 1 : 0);
        PlayerPrefs.SetInt(KEY_LOGIN_QUEST_CLAIMED, loginQuestClaimed ? 1 : 0);
        PlayerPrefs.SetInt(KEY_ARCADE_LEVEL1_QUEST_COMPLETED, arcadeLevel1QuestCompleted ? 1 : 0);
        PlayerPrefs.SetInt(KEY_ARCADE_LEVEL1_QUEST_CLAIMED, arcadeLevel1QuestClaimed ? 1 : 0);
        PlayerPrefs.SetInt(KEY_GACHA1X_QUEST_COMPLETED, gacha1xQuestCompleted ? 1 : 0);
        PlayerPrefs.SetInt(KEY_GACHA1X_QUEST_CLAIMED, gacha1xQuestClaimed ? 1 : 0);
        PlayerPrefs.SetInt(KEY_ALL_QUESTS_CLAIMED, allQuestsClaimed ? 1 : 0);
        PlayerPrefs.SetInt(KEY_KALYE_ACHIEVEMENT_COMPLETED, kalyeStageAchievementCompleted ? 1 : 0);
        PlayerPrefs.SetInt(KEY_KALYE_ACHIEVEMENT_CLAIMED, kalyeStageAchievementClaimed ? 1 : 0);
        PlayerPrefs.SetInt(KEY_RARE_HOLEN_ACHIEVEMENT_COMPLETED, rareHolenAchievementCompleted ? 1 : 0);
        PlayerPrefs.SetInt(KEY_RARE_HOLEN_ACHIEVEMENT_CLAIMED, rareHolenAchievementClaimed ? 1 : 0);
        PlayerPrefs.SetInt(KEY_COLLECT10_ACHIEVEMENT_COMPLETED, collect10HolensAchievementCompleted ? 1 : 0);
        PlayerPrefs.SetInt(KEY_COLLECT10_ACHIEVEMENT_CLAIMED, collect10HolensAchievementClaimed ? 1 : 0);
        PlayerPrefs.SetInt(KEY_TOTAL_HOLENS_COLLECTED, totalHolensCollected);
        PlayerPrefs.Save();
    }

    public static PlayerData Load()
    {
        PlayerData data = new PlayerData();
        data.playerName = PlayerPrefs.GetString(KEY_PLAYER_NAME, "");
        data.coins = PlayerPrefs.GetInt(KEY_COINS, 5000);
        data.energy = PlayerPrefs.GetInt(KEY_ENERGY, 10);
        data.level = PlayerPrefs.GetInt(KEY_LEVEL, 1);
        data.lastEnergyUpdateTime = PlayerPrefs.GetString(KEY_LAST_ENERGY_UPDATE, DateTime.Now.ToString("o"));
        data.isSoundEnabled = PlayerPrefs.GetInt(KEY_SOUND_ENABLED, 1) == 1;
        data.completedLevelsData = PlayerPrefs.GetString(KEY_COMPLETED_LEVELS, "");
        data.firstClearedStages = PlayerPrefs.GetString(KEY_FIRST_CLEARED_STAGES, "");
        data.selectedAvatarIndex = PlayerPrefs.GetInt(KEY_AVATAR_INDEX, 0);
        data.gachaQuestCompleted = PlayerPrefs.GetInt(KEY_GACHA_QUEST_COMPLETED, 0) == 1;
        data.gachaQuestClaimed = PlayerPrefs.GetInt(KEY_GACHA_QUEST_CLAIMED, 0) == 1;
        data.loginQuestClaimed = PlayerPrefs.GetInt(KEY_LOGIN_QUEST_CLAIMED, 0) == 1;
        data.arcadeLevel1QuestCompleted = PlayerPrefs.GetInt(KEY_ARCADE_LEVEL1_QUEST_COMPLETED, 0) == 1;
        data.arcadeLevel1QuestClaimed = PlayerPrefs.GetInt(KEY_ARCADE_LEVEL1_QUEST_CLAIMED, 0) == 1;
        data.gacha1xQuestCompleted = PlayerPrefs.GetInt(KEY_GACHA1X_QUEST_COMPLETED, 0) == 1;
        data.gacha1xQuestClaimed = PlayerPrefs.GetInt(KEY_GACHA1X_QUEST_CLAIMED, 0) == 1;
        data.allQuestsClaimed = PlayerPrefs.GetInt(KEY_ALL_QUESTS_CLAIMED, 0) == 1;
        data.kalyeStageAchievementCompleted = PlayerPrefs.GetInt(KEY_KALYE_ACHIEVEMENT_COMPLETED, 0) == 1;
        data.kalyeStageAchievementClaimed = PlayerPrefs.GetInt(KEY_KALYE_ACHIEVEMENT_CLAIMED, 0) == 1;
        data.rareHolenAchievementCompleted = PlayerPrefs.GetInt(KEY_RARE_HOLEN_ACHIEVEMENT_COMPLETED, 0) == 1;
        data.rareHolenAchievementClaimed = PlayerPrefs.GetInt(KEY_RARE_HOLEN_ACHIEVEMENT_CLAIMED, 0) == 1;
        data.collect10HolensAchievementCompleted = PlayerPrefs.GetInt(KEY_COLLECT10_ACHIEVEMENT_COMPLETED, 0) == 1;
        data.collect10HolensAchievementClaimed = PlayerPrefs.GetInt(KEY_COLLECT10_ACHIEVEMENT_CLAIMED, 0) == 1;
        data.totalHolensCollected = PlayerPrefs.GetInt(KEY_TOTAL_HOLENS_COLLECTED, 0);

        data.RegenerateEnergy();
        return data;
    }

    public static void DeleteAll()
    {
        PlayerPrefs.DeleteKey(KEY_PLAYER_NAME);
        PlayerPrefs.DeleteKey(KEY_COINS);
        PlayerPrefs.DeleteKey(KEY_ENERGY);
        PlayerPrefs.DeleteKey(KEY_LEVEL);
        PlayerPrefs.DeleteKey(KEY_LAST_ENERGY_UPDATE);
        PlayerPrefs.DeleteKey(KEY_COMPLETED_LEVELS);
        PlayerPrefs.DeleteKey(KEY_FIRST_CLEARED_STAGES);
        PlayerPrefs.DeleteKey(KEY_AVATAR_INDEX);
        PlayerPrefs.DeleteKey(KEY_GACHA_QUEST_COMPLETED);
        PlayerPrefs.DeleteKey(KEY_GACHA_QUEST_CLAIMED);
        PlayerPrefs.DeleteKey(KEY_LOGIN_QUEST_CLAIMED);
        PlayerPrefs.DeleteKey(KEY_ARCADE_LEVEL1_QUEST_COMPLETED);
        PlayerPrefs.DeleteKey(KEY_ARCADE_LEVEL1_QUEST_CLAIMED);
        PlayerPrefs.DeleteKey(KEY_GACHA1X_QUEST_COMPLETED);
        PlayerPrefs.DeleteKey(KEY_GACHA1X_QUEST_CLAIMED);
        PlayerPrefs.DeleteKey(KEY_ALL_QUESTS_CLAIMED);
        PlayerPrefs.DeleteKey(KEY_KALYE_ACHIEVEMENT_COMPLETED);
        PlayerPrefs.DeleteKey(KEY_KALYE_ACHIEVEMENT_CLAIMED);
        PlayerPrefs.DeleteKey(KEY_RARE_HOLEN_ACHIEVEMENT_COMPLETED);
        PlayerPrefs.DeleteKey(KEY_RARE_HOLEN_ACHIEVEMENT_CLAIMED);
        PlayerPrefs.DeleteKey(KEY_COLLECT10_ACHIEVEMENT_COMPLETED);
        PlayerPrefs.DeleteKey(KEY_COLLECT10_ACHIEVEMENT_CLAIMED);
        PlayerPrefs.DeleteKey(KEY_TOTAL_HOLENS_COLLECTED);
        PlayerPrefs.Save();
        Debug.Log("[PlayerData] All player data deleted");
    }

    // Legacy key migration: if old "HighestLevelUnlocked" key exists, migrate it once.
    private const string KEY_HIGHEST_LEVEL_LEGACY = "HighestLevelUnlocked";
    public static void MigrateLegacyKeys()
    {
        if (PlayerPrefs.HasKey(KEY_HIGHEST_LEVEL_LEGACY))
        {
            int legacy = PlayerPrefs.GetInt(KEY_HIGHEST_LEVEL_LEGACY, 1);
            if (!PlayerPrefs.HasKey(KEY_LEVEL))
                PlayerPrefs.SetInt(KEY_LEVEL, legacy);
            PlayerPrefs.DeleteKey(KEY_HIGHEST_LEVEL_LEGACY);
            PlayerPrefs.Save();
            Debug.Log($"[PlayerData] Migrated legacy HighestLevelUnlocked ({legacy}) → PlayerLevel");
        }
    }
}