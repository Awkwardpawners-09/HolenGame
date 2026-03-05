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
    PlayerDataManager.Instance.playerData.CompleteLevel(currentLevelNumber); // ✅ use the singleton

    Debug.Log($"[LevelManager] Level {currentLevelNumber} completed!");
}

public void UnlockNextLevel()
{
    PlayerDataManager.Instance.playerData.UnlockLevel(currentLevelNumber + 1); // ✅ same fix
}

public bool IsCurrentLevelUnlocked()
{
    return PlayerDataManager.Instance.playerData.IsLevelUnlocked(currentLevelNumber); // ✅ same fix
}
}