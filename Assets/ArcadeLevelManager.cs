using UnityEngine;

/// <summary>
/// Manages level completion and progression.
/// Place this on a GameObject in each level scene.
/// Call CompleteLevel() when the player finishes the level.
/// </summary>
public class ArcadeLevelManager : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private int currentLevelNumber = 1;

    [Header("Rewards")]
    [SerializeField] private int coinReward = 100;
    [SerializeField] private int energyReward = 10;

    private PlayerData playerData;

    /// <summary>
    /// Call this method when the player successfully completes the level
    /// </summary>
public void CompleteLevel()
{
    if (PlayerDataManager.Instance == null)
    {
        Debug.LogError("❌ PlayerDataManager not found!");
        return;
    }

    PlayerDataManager.Instance.AddCoins(coinReward);
    PlayerDataManager.Instance.AddEnergy(energyReward);
    playerData.CompleteLevel(currentLevelNumber);

        Debug.Log($"[LevelManager] Level {currentLevelNumber} completed! Next level unlocked.");
        Debug.Log($"[LevelManager] Rewards: +{coinReward} coins, +{energyReward} energy");
    }

    /// <summary>
    /// Call this if you want to manually unlock the next level without rewards
    /// </summary>
    public void UnlockNextLevel()
    {
        playerData.UnlockLevel(currentLevelNumber + 1);
    }

    /// <summary>
    /// Check if the current level is unlocked (useful for debugging)
    /// </summary>
    public bool IsCurrentLevelUnlocked()
    {
        return playerData.IsLevelUnlocked(currentLevelNumber);
    }
}