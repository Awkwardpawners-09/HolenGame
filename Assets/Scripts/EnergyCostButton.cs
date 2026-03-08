using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to buttons that cost energy to use.
/// Supports Arcade Mode (1 energy) and Multiplayer Mode (2 energy).
/// Will enable/show a GameObject when clicked if player has enough energy.
/// </summary>
[RequireComponent(typeof(Button))]
public class EnergyCostButton : MonoBehaviour
{
    [Header("Energy Cost")]
    [Tooltip("How much energy this button costs")]
    public ButtonType buttonType = ButtonType.Arcade;

    public enum ButtonType
    {
        Arcade = 2,      // Costs 1 energy
        Multiplayer = 0  // Costs 2 energy
    }

    [Header("Target GameObject")]
    [Tooltip("GameObject to enable/show when button is clicked (optional)")]
    public GameObject targetGameObject;

    [Header("Optional: Not Enough Energy Feedback")]
    [Tooltip("Text to show when player doesn't have enough energy (optional)")]
    public TMP_Text feedbackText;

    [Tooltip("Message to show when not enough energy")]
    public string notEnoughEnergyMessage = "Not enough energy!";

    [Tooltip("How long to show feedback message (seconds)")]
    public float feedbackDuration = 2f;

    [Header("Optional: Visual Feedback")]
    [Tooltip("Disable button visually when not enough energy")]
    public bool disableWhenNotEnoughEnergy = true;

    private Button button;
    private bool isShowingFeedback = false;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        // Add click listener
        button.onClick.AddListener(OnButtonClicked);

        // Update button state
        UpdateButtonState();
    }

    private void Update()
    {
        // Continuously update button state (in case energy regenerates)
        if (disableWhenNotEnoughEnergy)
        {
            UpdateButtonState();
        }
    }

    private void UpdateButtonState()
    {
        if (PlayerDataManager.Instance == null) return;

        // If a LevelUnlockButton has locked this stage, never override it.
        LevelUnlockButton levelLock = GetComponent<LevelUnlockButton>();
        if (levelLock != null)
        {
            int playerLevel = PlayerDataManager.Instance.playerData.level;
            bool stageIsLocked = playerLevel < levelLock.stageIndex;
            if (stageIsLocked)
            {
                button.interactable = false;
                return;
            }
        }

        int energyCost = (int)buttonType;
        bool hasEnoughEnergy = PlayerDataManager.Instance.HasEnergy(energyCost);

        if (disableWhenNotEnoughEnergy)
        {
            button.interactable = hasEnoughEnergy;
        }
    }

    private void OnButtonClicked()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[EnergyCostButton] PlayerDataManager not found!");
            return;
        }

        int energyCost = (int)buttonType;
        string modeText = buttonType == ButtonType.Arcade ? "Arcade" : "Multiplayer";

        // Try to spend energy
        if (PlayerDataManager.Instance.SpendEnergy(energyCost))
        {
            // Success! Enable target GameObject
            Debug.Log($"[EnergyCostButton] {modeText} mode started - spent {energyCost} energy");

            if (targetGameObject != null)
            {
                targetGameObject.SetActive(true);
                Debug.Log($"[EnergyCostButton] Enabled {targetGameObject.name}");
            }
        }
        else
        {
            // Not enough energy
            Debug.LogWarning($"[EnergyCostButton] Not enough energy for {modeText} mode (need {energyCost})");
            ShowNotEnoughEnergyFeedback();
        }
    }

    private void ShowNotEnoughEnergyFeedback()
    {
        if (feedbackText != null && !isShowingFeedback)
        {
            isShowingFeedback = true;

            // Show message
            feedbackText.text = notEnoughEnergyMessage;
            feedbackText.gameObject.SetActive(true);

            // Hide after duration
            Invoke(nameof(HideFeedback), feedbackDuration);
        }
    }

    private void HideFeedback()
    {
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
        isShowingFeedback = false;
    }

    // ===================== PUBLIC METHODS =====================

    /// <summary>
    /// Manually set the energy cost (if you need dynamic costs)
    /// </summary>
    public void SetEnergyCost(ButtonType type)
    {
        buttonType = type;
        UpdateButtonState();
    }

    /// <summary>
    /// Check if player has enough energy for this button
    /// </summary>
    public bool CanAfford()
    {
        if (PlayerDataManager.Instance == null) return false;
        return PlayerDataManager.Instance.HasEnergy((int)buttonType);
    }

    private void OnDestroy()
    {
        // Clean up
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}