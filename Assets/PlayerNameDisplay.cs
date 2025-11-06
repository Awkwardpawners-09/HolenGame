using UnityEngine;
using TMPro;

/// <summary>
/// Attach this script to the TextMeshPro GameObject that displays the player name.
/// It will automatically get the player name from HolenInventoryManager and display it.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class PlayerNameDisplay : MonoBehaviour
{
    private TextMeshProUGUI playerNameText;

    [Header("Settings")]
    [Tooltip("Update the name every frame (useful if name can change during gameplay)")]
    public bool updateContinuously = false;

    [Tooltip("Default text to show if no player name is set")]
    public string defaultText = "Player";

    private void Awake()
    {
        // Get the TextMeshProUGUI component on this GameObject
        playerNameText = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        // Update the player name display
        UpdatePlayerName();

        // Subscribe to player name changes
        HolenInventoryManager.OnPlayerNameChanged += OnPlayerNameChanged;
    }

    private void Update()
    {
        // Continuously update if enabled (useful for testing or dynamic changes)
        if (updateContinuously)
        {
            UpdatePlayerName();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from event to prevent memory leaks
        HolenInventoryManager.OnPlayerNameChanged -= OnPlayerNameChanged;
    }

    /// <summary>
    /// Called when player name changes in HolenInventoryManager
    /// </summary>
    private void OnPlayerNameChanged(string newName)
    {
        UpdatePlayerName();
    }

    /// <summary>
    /// Gets the player name from HolenInventoryManager and updates the text
    /// </summary>
    private void UpdatePlayerName()
    {
        if (playerNameText == null)
        {
            Debug.LogWarning("[PlayerNameDisplay] TextMeshProUGUI component not found!");
            return;
        }

        // Check if HolenInventoryManager exists
        if (HolenInventoryManager.Instance == null)
        {
            Debug.LogWarning("[PlayerNameDisplay] HolenInventoryManager not found! Using default text.");
            playerNameText.text = defaultText;
            return;
        }

        // Get the player name
        string playerName = HolenInventoryManager.Instance.PlayerName;

        // Display the name or default text if name is empty
        if (string.IsNullOrEmpty(playerName))
        {
            playerNameText.text = defaultText;
        }
        else
        {
            playerNameText.text = playerName;
        }
    }

}