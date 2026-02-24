using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to a stage select button in your level select screen.
/// Set "Required Level" in the Inspector — the button will be locked
/// until the player's highestLevelUnlocked reaches that number.
///
/// HOW TO SET UP:
/// 1. Add this component to your stage button GameObject.
/// 2. Set "Required Level" to the minimum highestLevelUnlocked needed.
///    - Level 1 = always accessible (set requiredLevel to 1)
///    - Level 2 = accessible after clearing level 1 (set requiredLevel to 2)
///    - etc.
/// 3. (Optional) Assign a "Locked Overlay" child GameObject — shown when locked,
///    hidden when unlocked. Use this for a dark tint, lock icon, etc.
/// 4. (Optional) Assign a "Lock Label" TMP text to show e.g. "Locked (Lv.3)".
/// </summary>
public class LevelUnlockButton : MonoBehaviour
{
    [Header("Unlock Requirement")]
    [Tooltip("The player must have reached this level (highestLevelUnlocked) before this button becomes playable.\n\nExample: Set to 1 for Stage 1 (always open), 2 for Stage 2 (unlocked after clearing Stage 1), etc.")]
    public int requiredLevel = 1;

    [Header("Optional Visuals")]
    [Tooltip("A child GameObject to show while locked (e.g. dark panel + lock icon). Will be hidden when unlocked.")]
    public GameObject lockedOverlay;

    [Tooltip("Optional TMP text to display the lock requirement, e.g. 'Locked (Lv.2)'.")]
    public TextMeshProUGUI lockLabel;

    [Tooltip("Format for the lock label text. {0} is replaced with the required level number.")]
    public string lockLabelFormat = "Locked (Lv.{0})";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    /// <summary>
    /// Called every time this GameObject becomes active (e.g. when the level select screen opens).
    /// Refreshes the locked/unlocked visual state.
    /// </summary>
    private void OnEnable()
    {
        RefreshLockState();
    }

    /// <summary>
    /// Checks the player's current highestLevelUnlocked against the required level
    /// and updates the button and any visuals accordingly.
    /// </summary>
    public void RefreshLockState()
    {
        int playerLevel = GetHighestLevelUnlocked();
        bool isUnlocked = playerLevel >= requiredLevel;

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
                lockLabel.text = string.Format(lockLabelFormat, requiredLevel);
            }
        }

        Debug.Log($"[LevelUnlockButton] '{gameObject.name}' — Player level: {playerLevel}, Required: {requiredLevel}, Unlocked: {isUnlocked}");
    }

    /// <summary>
    /// Reads highestLevelUnlocked from PlayerDataManager (preferred) or directly from PlayerPrefs.
    /// </summary>
    private int GetHighestLevelUnlocked()
    {
        if (PlayerDataManager.Instance != null)
            return PlayerDataManager.Instance.playerData.highestLevelUnlocked;

        // Fallback if PlayerDataManager isn't in the scene
        return PlayerPrefs.GetInt("HighestLevelUnlocked", 1);
    }
}