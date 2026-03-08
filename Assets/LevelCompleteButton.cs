using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to a "Stage Clear" / "Next Level" button inside a stage scene.
///
/// When the player presses the button:
///   - If they have NOT cleared this stage before → player.level increases by 1,
///     this stage is permanently marked as completed, and rewards are given.
///   - If they HAVE already cleared this stage → nothing changes. No free level farming!
///
/// HOW TO SET UP:
/// 1. Add this component to your completion/clear button.
/// 2. Set "This Stage Index" to match this stage's number
///    (Stage 1 = 1, Stage 2 = 2, etc. — must match LevelUnlockButton's stageIndex).
/// 3. Wire the Button's onClick event → call CompleteThisLevel() on this component.
/// 4. (Optional) Assign "Already Cleared Label" — shown when the player has done this before.
/// 5. (Optional) Assign "Level Up Label" — shown briefly when a new level is awarded.
/// </summary>
public class LevelCompleteButton : MonoBehaviour
{
    [Header("This Stage's Identity")]
    [Tooltip("The index of this stage (Stage 1 = 1, Stage 2 = 2, etc.).\nMust match the stageIndex set on the corresponding LevelUnlockButton.")]
    public int thisStageIndex = 1;

    [Header("Rewards")]
    public int coinReward = 100;
    public int energyReward = 10;

    [Header("Optional Feedback Labels")]
    [Tooltip("Shown when the player presses the button but has already cleared this stage before.")]
    public TextMeshProUGUI alreadyClearedLabel;

    [Tooltip("Text to display in the Already Cleared label.")]
    public string alreadyClearedText = "Already cleared! No bonus this time.";

    [Tooltip("Shown briefly when the player earns a new level for the first time.")]
    public TextMeshProUGUI levelUpLabel;

    [Tooltip("Text to display in the Level Up label. {0} = new player level value.")]
    public string levelUpText = "Stage cleared! You are now Level {0}!";

    private void OnEnable()
    {
        RefreshLabels();
    }

    /// <summary>
    /// Call this from the Button's onClick event.
    /// Awards coins/energy and +1 to player level if this stage hasn't been completed before.
    /// This is safe to call multiple times — repeat clears give no reward.
    /// </summary>
    public void CompleteThisLevel()
    {
        // ── Quest: Arcade Level 1 ──────────────────────────────────────────────
        // Runs regardless of whether this is a repeat clear.
        if (thisStageIndex == 1 && PlayerDataManager.Instance != null)
        {
            Debug.Log($"[LevelCompleteButton] Marking ArcadeLevel1Quest complete. thisStageIndex={thisStageIndex}");
            PlayerDataManager.Instance.playerData.arcadeLevel1QuestCompleted = true;
            PlayerDataManager.Instance.playerData.Save();

            foreach (var q in FindObjectsOfType<ArcadeLevel1Quest>())
            {
                Debug.Log($"[LevelCompleteButton] Refreshing ArcadeLevel1Quest on: {q.gameObject.name}");
                q.RefreshUI();
            }
        }

        // ── Achievement: Kalye Stage ───────────────────────────────────────────
        // Runs regardless of whether this is a repeat clear.
        if (thisStageIndex == 4 && PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.playerData.kalyeStageAchievementCompleted = true;
            PlayerDataManager.Instance.playerData.Save();

            foreach (var a in FindObjectsOfType<KalyeStageAchievement>())
                a.RefreshUI();
        }

        // ── Guard: already cleared → no reward ────────────────────────────────
        if (IsAlreadyCleared())
        {
            Debug.Log($"[LevelCompleteButton] Stage {thisStageIndex} was already cleared. No reward.");
            ShowAlreadyClearedFeedback();
            return;
        }

        // ── First-time clear: give rewards ────────────────────────────────────
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddCoins(coinReward);
            PlayerDataManager.Instance.AddEnergy(energyReward);
        }

        MarkAsCleared();
        int newLevel = AwardLevelUp();

        Debug.Log($"[LevelCompleteButton] Stage {thisStageIndex} cleared for the first time! +{coinReward} coins, +{energyReward} energy. Player is now level {newLevel}.");
        ShowLevelUpFeedback(newLevel);
    }

    // ─────────────────────────────────────────────
    //  Internal helpers
    // ─────────────────────────────────────────────

    private bool IsAlreadyCleared()
    {
        if (PlayerDataManager.Instance != null)
            return PlayerDataManager.Instance.playerData.IsLevelCompleted(thisStageIndex);

        // Fallback
        return PlayerPrefs.GetInt($"LevelCompleted_{thisStageIndex}", 0) == 1;
    }

    private void MarkAsCleared()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.playerData.MarkLevelCompleted(thisStageIndex);
        }
        else
        {
            // Fallback
            PlayerPrefs.SetInt($"LevelCompleted_{thisStageIndex}", 1);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Increments the player's level by 1 (unlocking the next stage) and returns the new level.
    /// </summary>
    private int AwardLevelUp()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerData data = PlayerDataManager.Instance.playerData;
            data.IncrementLevel();
            return data.level;
        }

        // Fallback
        int current = PlayerPrefs.GetInt("PlayerLevel", 1);
        int newLevel = current + 1;
        PlayerPrefs.SetInt("PlayerLevel", newLevel);
        PlayerPrefs.Save();
        return newLevel;
    }

    private void ShowAlreadyClearedFeedback()
    {
        if (alreadyClearedLabel != null)
        {
            alreadyClearedLabel.text = alreadyClearedText;
            alreadyClearedLabel.gameObject.SetActive(true);
        }

        if (levelUpLabel != null)
            levelUpLabel.gameObject.SetActive(false);
    }

    private void ShowLevelUpFeedback(int newLevel)
    {
        if (levelUpLabel != null)
        {
            levelUpLabel.text = string.Format(levelUpText, newLevel);
            levelUpLabel.gameObject.SetActive(true);
        }

        if (alreadyClearedLabel != null)
            alreadyClearedLabel.gameObject.SetActive(false);
    }

    private void RefreshLabels()
    {
        bool cleared = IsAlreadyCleared();

        if (alreadyClearedLabel != null)
        {
            alreadyClearedLabel.gameObject.SetActive(cleared);
            if (cleared)
                alreadyClearedLabel.text = alreadyClearedText;
        }

        if (levelUpLabel != null)
            levelUpLabel.gameObject.SetActive(false);
    }
}