using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the first-time player name setup UI.
/// Shows only once on first launch, then stays disabled forever.
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

    private void Start()
    {
        // Check if player already has a name
        var inventoryManager = HolenInventoryManager.Instance;

        if (inventoryManager == null)
        {
            Debug.LogError("[PlayerNameSetup] HolenInventoryManager not found!");
            return;
        }

        if (inventoryManager.HasPlayerName())
        {
            // Player already has a name - hide this UI permanently
            Debug.Log($"[PlayerNameSetup] Player already has name: {inventoryManager.PlayerName}");
            gameObject.SetActive(false);
            return;
        }

        // First time setup - show the UI
        Debug.Log("[PlayerNameSetup] First launch detected - showing name setup");
        SetupUI();
    }

    private void SetupUI()
    {
        // Make sure the GameObject is active
        gameObject.SetActive(true);

        // Clear any default text in input field
        if (nameInputField != null)
        {
            nameInputField.text = "";
            nameInputField.characterLimit = maxNameLength;

            // Add listener for input changes (for real-time validation)
            nameInputField.onValueChanged.AddListener(OnNameInputChanged);

            // Focus the input field so player can start typing immediately
            nameInputField.Select();
            nameInputField.ActivateInputField();
        }

        // Setup confirm button
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);

            // Disable button initially until valid name is entered
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

        // Update button interactability
        if (confirmButton != null)
        {
            confirmButton.interactable = isValid;
        }

        // Show/hide error message
        if (errorMessageText != null)
        {
            if (!isValid && !string.IsNullOrEmpty(newName))
            {
                errorMessageText.text = errorMessage;
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
        var inventoryManager = HolenInventoryManager.Instance;

        if (inventoryManager == null)
        {
            Debug.LogError("[PlayerNameSetup] HolenInventoryManager not found!");
            return;
        }

        Debug.Log($"[PlayerNameSetup] Saving player name: {playerName}");

        // Save the name to inventory manager
        inventoryManager.SetPlayerName(playerName);

        // Optional: Show welcome message
        if (welcomeText != null)
        {
            welcomeText.text = $"Welcome, {playerName}!";
            welcomeText.gameObject.SetActive(true);
        }

        // Hide this UI after a short delay (to show welcome message)
        Invoke(nameof(HideUI), 1.5f);
    }

    private void HideUI()
    {
        Debug.Log("[PlayerNameSetup] Name saved - hiding UI permanently");

        // Disable this GameObject permanently
        // Since player name is saved, this UI will never show again
        gameObject.SetActive(false);
    }

    // ===================== PUBLIC METHODS (Optional) =====================

    /// <summary>
    /// Manually trigger the name setup (for testing or if you want a "change name" button)
    /// </summary>
    public void ShowNameSetup()
    {
        SetupUI();
    }

    /// <summary>
    /// For testing: Reset player name to force the setup UI to appear again
    /// </summary>
    public void ResetPlayerNameForTesting()
    {
        var inventoryManager = HolenInventoryManager.Instance;
        if (inventoryManager != null)
        {
            // Clear the player name
            inventoryManager.SetPlayerName("");

            // Show the setup UI again
            SetupUI();

            Debug.Log("🧪 [TESTING] Player name reset - setup UI will show on next launch");
        }
    }
}