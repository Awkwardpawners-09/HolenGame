using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to a stage select button in your level select screen.
///
/// HOW THE UNLOCK SYSTEM WORKS:
/// - The player starts at level 1.
/// - Each stage button has a "Stage Index" that matches its position (Stage 1 = index 1, Stage 2 = index 2, etc.)
/// - A stage is unlocked when the player's level is >= the stage's index.
/// - Example: Player level 1 → only Stage 1 (index 1) is accessible.
///            Player level 2 → Stage 1 and Stage 2 are accessible, etc.
///
/// HOW TO SET UP:
/// 1. Add this component to your stage button GameObject.
/// 2. Set "Stage Index" to match this stage's number (Stage 1 = 1, Stage 2 = 2, etc.).
/// 3. (Optional) Assign a "Locked Overlay" child GameObject — shown when locked,
///    hidden when unlocked. Use this for a dark tint, lock icon, etc.
/// 4. (Optional) Assign a "Lock Label" TMP text to show e.g. "Locked".
/// </summary>
public class LevelUnlockButton : MonoBehaviour
{
    [Header("Stage Identity")]
    [Tooltip("The index of this stage (Stage 1 = 1, Stage 2 = 2, etc.).\nThe player's level must be >= this number for the button to be unlocked.\n\nExample: Stage 1 button = index 1 (always open at level 1).\nStage 2 button = index 2 (opens when player reaches level 2).")]
    public int stageIndex = 1;

    [Header("Optional Visuals")]
    [Tooltip("A child GameObject to show while locked (e.g. dark panel + lock icon). Will be hidden when unlocked.")]
    public GameObject lockedOverlay;

    [Tooltip("Optional TMP text to display when the stage is locked.")]
    public TextMeshProUGUI lockLabel;

    [Tooltip("Format for the lock label text. {0} = required player level to unlock.")]
    public string lockLabelFormat = "Locked (Lv.{0})";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    /// <summary>
    /// Called every time this GameObject becomes active (e.g. when the level select screen opens).
    /// Refreshes the locked/unlocked visual state based on current player level.
    /// </summary>
    private void OnEnable()
    {
        RefreshLockState();
    }

    /// <summary>
    /// Checks the player's current level against this stage's index and updates visuals.
    /// A stage is unlocked when playerLevel >= stageIndex.
    /// </summary>
    public void RefreshLockState()
    {
        int playerLevel = GetPlayerLevel();

        // Stage is accessible when the player's level is at least equal to the stage index.
        // Stage 1 (index 1) is always open for a level-1 player.
        // Stage 2 (index 2) requires the player to be level 2, etc.
        bool isUnlocked = playerLevel >= stageIndex;

        // Enable or disable button interaction
        if (button != null)
            button.interactable = isUnlocked;

        // Show/hide the locked overlay
        if (lockedOverlay != null)
            lockedOverlay.SetActive(!isUnlocked);

        // Update the lock label
        if (lockLabel != null)
        {
            if (isUnlocked)
            {
                lockLabel.gameObject.SetActive(false);
            }
            else
            {
                lockLabel.gameObject.SetActive(true);
                lockLabel.text = string.Format(lockLabelFormat, stageIndex);
            }
        }

        Debug.Log($"[LevelUnlockButton] '{gameObject.name}' — Player level: {playerLevel}, Stage index: {stageIndex}, Unlocked: {isUnlocked}");
    }

    /// <summary>
    /// Reads the player's level from PlayerDataManager (preferred) or directly from PlayerPrefs.
    /// </summary>
    private int GetPlayerLevel()
    {
        if (PlayerDataManager.Instance != null)
            return PlayerDataManager.Instance.playerData.level;

        // Fallback if PlayerDataManager isn't in the scene
        return PlayerPrefs.GetInt("PlayerLevel", 1);
    }
}