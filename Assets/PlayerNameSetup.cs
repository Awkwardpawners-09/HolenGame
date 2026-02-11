using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the first-time player name setup UI.
/// Shows only once on first launch, then stays disabled forever.
/// Now fully integrated with PlayerDataManager.
/// ALSO handles name changes when player wants to update their name.
/// </summary>
public class PlayerNameSetup : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The input field where player types their name")]
    public TMP_InputField nameInputField;

    [Tooltip("The button player clicks to confirm their name")]
    public Button confirmButton;

    [Header("Optional: Validation")]
    [Tooltip("Minimum character length for player name")]
    public int minNameLength = 3;

    [Tooltip("Maximum character length for player name")]
    public int maxNameLength = 20;

    [Header("Optional: Feedback")]
    [Tooltip("Text to show validation errors (optional)")]
    public TMP_Text errorMessageText;

    [Header("Optional: Welcome Message")]
    [Tooltip("Text to personalize welcome message (optional)")]
    public TMP_Text welcomeText;

    [Header("Name Change Mode")]
    [Tooltip("Is this UI being used for changing existing name? (Set via code)")]
    private bool isNameChangeMode = false;
    private string originalName = "";

    private bool isChecking = false;

    private void Start()
    {
        // Start checking for PlayerDataManager
        CheckPlayerDataManager();
    }

    private void CheckPlayerDataManager()
    {
        if (isChecking) return;
        isChecking = true;

        // Try to find PlayerDataManager (required)
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("[PlayerNameSetup] PlayerDataManager not ready yet, retrying...");
            // Retry after a short delay
            Invoke(nameof(RetryCheck), 0.1f);
            return;
        }

        // Check if player already has a name
        if (PlayerDataManager.Instance.HasPlayerName())
        {
            // Player already has a name - hide this UI permanently
            Debug.Log($"[PlayerNameSetup] Player already has name: {PlayerDataManager.Instance.GetPlayerName()}");
            gameObject.SetActive(false);
            return;
        }

        // First time setup - show the UI
        Debug.Log("[PlayerNameSetup] First launch detected - showing name setup");
        SetupUI();
    }

    private void RetryCheck()
    {
        isChecking = false;
        CheckPlayerDataManager();
    }

    private void SetupUI()
    {
        // Make sure the GameObject is active
        gameObject.SetActive(true);

        // Clear any default text in input field
        if (nameInputField != null)
        {
            // If in name change mode, show current name
            if (isNameChangeMode && !string.IsNullOrEmpty(originalName))
            {
                nameInputField.text = originalName;
            }
            else
            {
                nameInputField.text = "";
            }

            nameInputField.characterLimit = maxNameLength;

            // Add listener for input changes (for real-time validation)
            nameInputField.onValueChanged.RemoveAllListeners();
            nameInputField.onValueChanged.AddListener(OnNameInputChanged);

            // Focus the input field so player can start typing immediately
            nameInputField.Select();
            nameInputField.ActivateInputField();
        }

        // Setup confirm button
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);

            // In name change mode, button is disabled until name changes
            // In first-time setup, button is disabled until valid name is entered
            confirmButton.interactable = false;
        }

        // Hide error message initially
        if (errorMessageText != null)
        {
            errorMessageText.gameObject.SetActive(false);
        }
    }

    private void OnNameInputChanged(string newName)
    {
        // Real-time validation as player types
        bool isValid = ValidateName(newName, out string errorMessage);

        // Check if name has changed (for name change mode)
        bool hasChanged = true;
        if (isNameChangeMode)
        {
            hasChanged = newName.Trim() != originalName;
        }

        // Update button interactability
        if (confirmButton != null)
        {
            // Button is enabled only if:
            // 1. Name is valid
            // 2. In name change mode: name must be different from original
            confirmButton.interactable = isValid && hasChanged;
        }

        // Show/hide error message
        if (errorMessageText != null)
        {
            if (!isValid && !string.IsNullOrEmpty(newName))
            {
                errorMessageText.text = errorMessage;
                errorMessageText.gameObject.SetActive(true);
            }
            else if (isNameChangeMode && !hasChanged && !string.IsNullOrEmpty(newName))
            {
                errorMessageText.text = "Name is the same as before";
                errorMessageText.gameObject.SetActive(true);
            }
            else
            {
                errorMessageText.gameObject.SetActive(false);
            }
        }
    }

    private void OnConfirmButtonClicked()
    {
        if (nameInputField == null)
        {
            Debug.LogError("[PlayerNameSetup] Name input field is null!");
            return;
        }

        string playerName = nameInputField.text.Trim();

        // Final validation
        bool isValid = ValidateName(playerName, out string errorMessage);

        if (!isValid)
        {
            // Show error message
            if (errorMessageText != null)
            {
                errorMessageText.text = errorMessage;
                errorMessageText.gameObject.SetActive(true);
            }
            Debug.LogWarning($"[PlayerNameSetup] Invalid name: {errorMessage}");
            return;
        }

        // Check if name actually changed (for name change mode)
        if (isNameChangeMode && playerName == originalName)
        {
            if (errorMessageText != null)
            {
                errorMessageText.text = "Name is the same as before";
                errorMessageText.gameObject.SetActive(true);
            }
            return;
        }

        // Name is valid - save it
        SavePlayerName(playerName);
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

        // Optional: Check for invalid characters
        // You can add more validation rules here
        // For example, only allow letters and numbers:
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

    private void SavePlayerName(string playerName)
    {
        Debug.Log($"[PlayerNameSetup] Saving player name: {playerName}");

        // ✅ Use PlayerDataManager to save the name (this is the correct way!)
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetPlayerName(playerName);
        }
        else
        {
            Debug.LogError("[PlayerNameSetup] PlayerDataManager not found! Cannot save name.");
            return;
        }

        // Also update HolenInventoryManager if it exists (optional compatibility)
        var inventoryManager = HolenInventoryManager.Instance;
        if (inventoryManager != null)
        {
            inventoryManager.SetPlayerName(playerName);
        }

        // Optional: Show welcome/confirmation message
        if (welcomeText != null)
        {
            if (isNameChangeMode)
            {
                welcomeText.text = $"Name changed to {playerName}!";
            }
            else
            {
                welcomeText.text = $"Welcome, {playerName}!";
            }
            welcomeText.gameObject.SetActive(true);
        }

        // Hide this UI after a short delay (to show message)
        Invoke(nameof(HideUI), 1.5f);
    }

    private void HideUI()
    {
        if (isNameChangeMode)
        {
            Debug.Log("[PlayerNameSetup] Name changed - hiding UI");
        }
        else
        {
            Debug.Log("[PlayerNameSetup] Name saved - hiding UI permanently");
        }

        // Reset name change mode
        isNameChangeMode = false;
        originalName = "";

        // Disable this GameObject
        gameObject.SetActive(false);
    }

    // ===================== PUBLIC METHODS =====================

    /// <summary>
    /// Manually trigger the name setup (for testing or if you want a "change name" button)
    /// </summary>
    public void ShowNameSetup()
    {
        isNameChangeMode = false;
        originalName = "";
        SetupUI();
    }

    /// <summary>
    /// Show UI for changing existing player name.
    /// Call this method when player taps the input field to change their name.
    /// The confirm button will only be enabled when the name is different from current name.
    /// </summary>
    public void ShowNameChange()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[PlayerNameSetup] PlayerDataManager not found!");
            return;
        }

        // Get current name
        originalName = PlayerDataManager.Instance.GetPlayerName();

        if (string.IsNullOrEmpty(originalName))
        {
            Debug.LogWarning("[PlayerNameSetup] No existing name found. Using first-time setup mode.");
            isNameChangeMode = false;
        }
        else
        {
            Debug.Log($"[PlayerNameSetup] Showing name change UI (current name: {originalName})");
            isNameChangeMode = true;
        }

        SetupUI();
    }

    /// <summary>
    /// For testing: Reset player name to force the setup UI to appear again
    /// </summary>
    public void ResetPlayerNameForTesting()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetPlayerName("");
            Debug.Log("🧪 [TESTING] Player name reset via PlayerDataManager - setup UI will show on next launch");
        }

        // Show the setup UI again
        isNameChangeMode = false;
        originalName = "";
        SetupUI();
    }
}