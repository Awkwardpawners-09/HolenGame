using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to a "Stage Clear" / "Next Level" button inside a stage scene.
///
/// When the player presses the button:
///   - If they have NOT cleared this level before → highestLevelUnlocked increases by 1,
///     this level is permanently marked as completed, and rewards are given.
///   - If they HAVE already cleared this level → nothing changes. No free level farming!
///
/// HOW TO SET UP:
/// 1. Add this component to your completion/clear button.
/// 2. Set "This Level Index" to match the 0-based index of this stage
///    (same numbering used in ArcadeModeManager.stageScenes, e.g. Stage1 = 0, Stage2 = 1).
/// 3. Wire the Button's onClick event → call CompleteThisLevel() on this component.
/// 4. (Optional) Assign "Already Cleared Label" — shown when the player has done this before.
/// 5. (Optional) Assign "Level Up Label" — shown briefly when a new level is awarded.
/// </summary>
public class LevelCompleteButton : MonoBehaviour
{
    [Header("This Stage's Identity")]
    [Tooltip("The 0-based index of this stage, matching ArcadeModeManager.stageScenes.\nStage 1 = 0, Stage 2 = 1, Stage 3 = 2, etc.")]
    public int thisLevelIndex = 0;

    [Header("Rewards")]
    public int coinReward = 100;
    public int energyReward = 10;

    [Header("Optional Feedback Labels")]
    [Tooltip("Shown when the player presses the button but has already cleared this level before.")]
    public TextMeshProUGUI alreadyClearedLabel;

    [Tooltip("Text to display in the Already Cleared label.")]
    public string alreadyClearedText = "Already cleared! No bonus this time.";

    [Tooltip("Shown briefly when the player earns a new level for the first time.")]
    public TextMeshProUGUI levelUpLabel;

    [Tooltip("Text to display in the Level Up label. {0} = new highestLevelUnlocked value.")]
    public string levelUpText = "Stage cleared! Level {0} unlocked!";

    private void OnEnable()
    {
        // Refresh feedback labels whenever this screen appears
        RefreshLabels();
    }

    /// <summary>
    /// Call this from the Button's onClick event.
    /// Awards coins/energy and +1 to highestLevelUnlocked if this level hasn't been completed before.
    /// </summary>
    public void CompleteThisLevel()
    {

         if (thisLevelIndex == 1)
{
    Debug.Log($"[ArcadeLevel1Quest] Marking complete. thisLevelIndex={thisLevelIndex}");
    PlayerDataManager.Instance.playerData.arcadeLevel1QuestCompleted = true;
    PlayerDataManager.Instance.playerData.Save();

    foreach (var q in FindObjectsOfType<ArcadeLevel1Quest>())
    {
        Debug.Log($"[ArcadeLevel1Quest] Found and refreshing: {q.gameObject.name}");
        q.RefreshUI();
    }

    Debug.Log($"[ArcadeLevel1Quest] Total found: {FindObjectsOfType<ArcadeLevel1Quest>().Length}");
}

        if (IsAlreadyCleared())
        {
            // Player has already beaten this level — no reward
            Debug.Log($"[LevelCompleteButton] Level {thisLevelIndex} was already cleared. No reward.");
            ShowAlreadyClearedFeedback();
            return;
        }

        // First time clearing — give rewards
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddCoins(coinReward);
            PlayerDataManager.Instance.AddEnergy(energyReward);
        }

        MarkAsCleared();
        int newHighest = AwardLevelUp();

        Debug.Log($"[LevelCompleteButton] Level {thisLevelIndex} cleared for the first time! +{coinReward} coins, +{energyReward} energy. highestLevelUnlocked is now {newHighest}.");
        ShowLevelUpFeedback(newHighest);

    }

    // ─────────────────────────────────────────────
    //  Internal helpers
    // ─────────────────────────────────────────────

    private bool IsAlreadyCleared()
    {
        if (PlayerDataManager.Instance != null)
            return PlayerDataManager.Instance.playerData.IsLevelCompleted(thisLevelIndex);

        // Fallback
        return PlayerPrefs.GetInt($"LevelCompleted_{thisLevelIndex}", 0) == 1;
    }

    private void MarkAsCleared()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.playerData.MarkLevelCompleted(thisLevelIndex);
        }
        else
        {
            // Fallback
            PlayerPrefs.SetInt($"LevelCompleted_{thisLevelIndex}", 1);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Increments highestLevelUnlocked by 1 (so the NEXT stage becomes accessible)
    /// and returns the new value.
    /// </summary>
    private int AwardLevelUp()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerData data = PlayerDataManager.Instance.playerData;
            int nextLevel = data.highestLevelUnlocked + 1;
            data.UnlockLevel(nextLevel);
            return data.highestLevelUnlocked;
        }

        // Fallback
        int current = PlayerPrefs.GetInt("HighestLevelUnlocked", 1);
        int newLevel = current + 1;
        PlayerPrefs.SetInt("HighestLevelUnlocked", newLevel);
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

    private void ShowLevelUpFeedback(int newHighest)
    {
        if (levelUpLabel != null)
        {
            levelUpLabel.text = string.Format(levelUpText, newHighest);
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