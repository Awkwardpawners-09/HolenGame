using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this to level selection buttons in your main menu.
/// Automatically enables/disables the button based on player progression.
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private string sceneName = "Level1";

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private GameObject lockedIcon;
    [SerializeField] private GameObject unlockedIcon;
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color unlockedColor = Color.white;

    private Button button;
    private Image buttonImage;
    private PlayerData playerData;

    private void Start()
    {
        // Get components
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();

        // Load player data
        playerData = PlayerData.Load();

        // Update button state
        UpdateButtonState();

        // Add click listener
        button.onClick.AddListener(OnButtonClick);
    }

    private void UpdateButtonState()
    {
        bool isUnlocked = playerData.IsLevelUnlocked(levelNumber);

        // Set button interactability
        button.interactable = isUnlocked;

        // Update visual feedback
        if (buttonImage != null)
        {
            buttonImage.color = isUnlocked ? unlockedColor : lockedColor;
        }

        if (lockedIcon != null)
        {
            lockedIcon.SetActive(!isUnlocked);
        }

        if (unlockedIcon != null)
        {
            unlockedIcon.SetActive(isUnlocked);
        }

        // Debug info
        Debug.Log($"[LevelButton] Level {levelNumber} - {(isUnlocked ? "UNLOCKED" : "LOCKED")}");
    }

    private void OnButtonClick()
    {
        // Load the level scene
        //SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Call this to refresh the button state (useful if data changes)
    /// </summary>
    public void RefreshState()
    {
        playerData = PlayerData.Load();
        UpdateButtonState();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
}