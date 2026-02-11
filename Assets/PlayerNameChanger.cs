using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles changing the player's name using separate UI elements.
/// This is different from PlayerNameSetup which is for first-time setup.
/// Use this for a "Change Name" feature in settings or profile screen.
/// </summary>
public class PlayerNameChanger : MonoBehaviour
{
    [Header("Change Name UI References")]
    [Tooltip("The input field where player types their new name")]
    public TMP_InputField changeNameInputField;

    [Tooltip("The button player clicks to confirm name change")]
    public Button changeNameConfirmButton;

    [Header("Optional: Validation")]
    [Tooltip("Minimum character length for player name")]
    public int minNameLength = 3;

    [Tooltip("Maximum character length for player name")]
    public int maxNameLength = 20;

    [Header("Optional: Feedback")]
    [Tooltip("Text to show validation errors or success messages (optional)")]
    public TMP_Text feedbackText;

    [Tooltip("How long to show feedback message (seconds)")]
    public float feedbackDuration = 2f;

    [Header("Optional: Success Callback")]
    [Tooltip("GameObject to disable after successful name change (e.g., settings panel)")]
    public GameObject objectToDisableOnSuccess;

    private string originalName = "";
    private bool isShowingFeedback = false;

    private void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        // Setup input field
        if (changeNameInputField != null)
        {
            changeNameInputField.characterLimit = maxNameLength;
            changeNameInputField.onValueChanged.AddListener(OnNameInputChanged);

            // Load current name when UI is ready
            LoadCurrentName();
        }

        // Setup confirm button
        if (changeNameConfirmButton != null)
        {
            changeNameConfirmButton.onClick.AddListener(OnConfirmButtonClicked);
            changeNameConfirmButton.gameObject.SetActive(false); // Hidden initially until name changes
        }

        // Hide feedback initially
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // Reload current name every time this UI is shown
        LoadCurrentName();
    }

    private void LoadCurrentName()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("[PlayerNameChanger] PlayerDataManager not found, retrying...");
            Invoke(nameof(LoadCurrentName), 0.1f);
            return;
        }

        originalName = PlayerDataManager.Instance.GetPlayerName();

        if (changeNameInputField != null)
        {
            changeNameInputField.text = originalName;
        }

        // Hide button since name hasn't changed yet
        if (changeNameConfirmButton != null)
        {
            changeNameConfirmButton.gameObject.SetActive(false);
        }

        Debug.Log($"[PlayerNameChanger] Loaded current name: {originalName}");
    }

    private void OnNameInputChanged(string newName)
    {
        // Real-time validation as player types
        bool isValid = ValidateName(newName, out string errorMessage);
        bool hasChanged = newName.Trim() != originalName;

        // Show/hide button based on whether name is valid AND different
        // Button is shown only if:
        // 1. Name is valid
        // 2. Name is different from original
        if (changeNameConfirmButton != null)
        {
            bool shouldShowButton = isValid && hasChanged;
            changeNameConfirmButton.gameObject.SetActive(shouldShowButton);

            // Also make sure it's interactable when shown
            if (shouldShowButton)
            {
                changeNameConfirmButton.interactable = true;
            }
        }

        // Show/hide error message
        if (feedbackText != null && !isShowingFeedback)
        {
            if (!isValid && !string.IsNullOrEmpty(newName))
            {
                ShowFeedback(errorMessage, false);
            }
            else if (!hasChanged && !string.IsNullOrEmpty(newName))
            {
                ShowFeedback("Name is the same as before", false);
            }
            else
            {
                HideFeedback();
            }
        }
    }

    private void OnConfirmButtonClicked()
    {
        if (changeNameInputField == null)
        {
            Debug.LogError("[PlayerNameChanger] Name input field is null!");
            return;
        }

        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[PlayerNameChanger] PlayerDataManager not found!");
            ShowFeedback("Error: Cannot save name", false);
            return;
        }

        string newName = changeNameInputField.text.Trim();

        // Final validation
        bool isValid = ValidateName(newName, out string errorMessage);

        if (!isValid)
        {
            ShowFeedback(errorMessage, false);
            Debug.LogWarning($"[PlayerNameChanger] Invalid name: {errorMessage}");
            return;
        }

        // Check if name actually changed
        if (newName == originalName)
        {
            ShowFeedback("Name is the same as before", false);
            return;
        }

        // Save the new name
        SaveNewName(newName);
    }

    private bool ValidateName(string name, out string errorMessage)
    {
        errorMessage = "";

        // Check if empty
        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage = "Please enter a name";
            return false;
        }

        // Check minimum length
        if (name.Length < minNameLength)
        {
            errorMessage = $"Name must be at least {minNameLength} characters";
            return false;
        }

        // Check maximum length
        if (name.Length > maxNameLength)
        {
            errorMessage = $"Name must be {maxNameLength} characters or less";
            return false;
        }

        // Optional: Add custom character validation here
        // Example:
        /*
        foreach (char c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != ' ' && c != '_')
            {
                errorMessage = "Name can only contain letters, numbers, spaces, and underscores";
                return false;
            }
        }
        */

        return true;
    }

    private void SaveNewName(string newName)
    {
        Debug.Log($"[PlayerNameChanger] Changing name from '{originalName}' to '{newName}'");

        // Save via PlayerDataManager
        PlayerDataManager.Instance.SetPlayerName(newName);

        // Show success message
        ShowFeedback($"Name changed to {newName}!", true);

        // Update original name
        originalName = newName;

        // Hide button again since name is now saved
        if (changeNameConfirmButton != null)
        {
            changeNameConfirmButton.gameObject.SetActive(false);
        }

        // Optional: Disable parent object after delay
        if (objectToDisableOnSuccess != null)
        {
            Invoke(nameof(DisableTargetObject), feedbackDuration);
        }
    }

    private void DisableTargetObject()
    {
        if (objectToDisableOnSuccess != null)
        {
            objectToDisableOnSuccess.SetActive(false);
            Debug.Log($"[PlayerNameChanger] Disabled {objectToDisableOnSuccess.name}");
        }
    }

    private void ShowFeedback(string message, bool isSuccess)
    {
        if (feedbackText == null) return;

        isShowingFeedback = true;
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);

        // Optional: Change color based on success/error
        // feedbackText.color = isSuccess ? Color.green : Color.red;

        // Auto-hide after duration
        CancelInvoke(nameof(HideFeedback));
        Invoke(nameof(HideFeedback), feedbackDuration);
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
    /// Manually trigger opening the name change UI
    /// Call this from a "Change Name" button in your settings
    /// </summary>
    public void OpenNameChangeUI()
    {
        gameObject.SetActive(true);
        LoadCurrentName();

        if (changeNameInputField != null)
        {
            changeNameInputField.Select();
            changeNameInputField.ActivateInputField();
        }
    }

    /// <summary>
    /// Close/hide the name change UI without saving
    /// </summary>
    public void CloseNameChangeUI()
    {
        gameObject.SetActive(false);
        HideFeedback();
    }

    /// <summary>
    /// Reset the input field to the original name
    /// Useful for a "Cancel" button
    /// </summary>
    public void ResetToOriginalName()
    {
        if (changeNameInputField != null)
        {
            changeNameInputField.text = originalName;
        }

        if (changeNameConfirmButton != null)
        {
            changeNameConfirmButton.gameObject.SetActive(false);
        }

        HideFeedback();
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (changeNameInputField != null)
        {
            changeNameInputField.onValueChanged.RemoveListener(OnNameInputChanged);
        }

        if (changeNameConfirmButton != null)
        {
            changeNameConfirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        }
    }
}