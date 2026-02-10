using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// UPDATED FOR MASTER CLIENT AUTHORITY:
/// Attach this to each HolenSlotUI prefab in the inventory panel.
/// Handles player taps/clicks on inventory slots and forwards the selection
/// to MultiplayerHolenController for spawning the selected holen.
/// 
/// NEW: Works with Master Client authority - selection works regardless of which client is Master.
/// 
/// Setup:
/// 1. Add a Button component to the slot's root GameObject
/// 2. Assign the Button to the 'slotButton' field
/// 3. The HolenSlotUI script should call SetHolenPrefab() after loading the holen data
/// </summary>
public class HolenSlotClickHandler : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Button component on this slot that the player taps")]
    public Button slotButton;

    [Header("Visual Feedback")]
    [Tooltip("Image to highlight when selected (optional)")]
    public Image selectionHighlight;

    [Tooltip("Color for selection highlight")]
    public Color highlightColor = new Color(1f, 1f, 0f, 0.5f);

    [Tooltip("Color when slot is disabled (not your turn)")]
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    [Header("Holen Data")]
    [Tooltip("The networked prefab that will be spawned when this slot is tapped. Must be in Resources folder.")]
    public GameObject holenPrefab;

    [Tooltip("Reference to HolenData for this slot (optional, for additional info)")]
    public HolenData holenData;

    [Header("Turn-Based Interaction")]
    [Tooltip("If true, slots are only interactable during the player's turn")]
    public bool requirePlayerTurn = true;

    [Tooltip("Update interval for checking turn state (seconds)")]
    public float turnCheckInterval = 0.5f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private MultiplayerHolenController controller;
    private bool isSelected = false;
    private float lastTurnCheck = 0f;

    private void Start()
    {
        // Find the controller in the scene
        controller = FindObjectOfType<MultiplayerHolenController>();

        if (controller == null)
        {
            Debug.LogWarning("[HolenSlotClickHandler] MultiplayerHolenController not found in scene at Start. Will search again on interaction.");
        }

        // Setup button listener
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotTapped);
        }
        else
        {
            Debug.LogWarning("[HolenSlotClickHandler] No Button component assigned! Add a Button to this slot.");
        }

        // Hide highlight initially
        if (selectionHighlight != null)
        {
            selectionHighlight.enabled = false;
        }

        // Initial interactability update
        UpdateInteractability();
    }

    private void Update()
    {
        // Periodically check if it's the player's turn and update button interactability
        if (requirePlayerTurn && Time.time - lastTurnCheck > turnCheckInterval)
        {
            UpdateInteractability();
            lastTurnCheck = Time.time;
        }
    }

    /// <summary>
    /// Updates the button's interactability based on whether it's the player's turn
    /// </summary>
    private void UpdateInteractability()
    {
        if (!requirePlayerTurn)
        {
            // Always interactable if turn checking is disabled
            SetInteractable(true);
            return;
        }

        // Find controller if not already cached
        if (controller == null)
        {
            controller = FindObjectOfType<MultiplayerHolenController>();
        }

        // Check if it's the player's turn
        bool isPlayerTurn = controller != null && controller.IsTurn();

        // Update button interactability
        SetInteractable(isPlayerTurn);

        // Visual feedback for disabled state
        if (!isPlayerTurn && selectionHighlight != null && !isSelected)
        {
            // Show a subtle indicator that it's not your turn (optional)
            // You can customize this behavior
        }
    }

    /// <summary>
    /// Set the holen prefab for this slot.
    /// Call this from HolenSlotUI.SetSlot() after loading the HolenData.
    /// </summary>
    public void SetHolenPrefab(GameObject prefab)
    {
        holenPrefab = prefab;

        if (showDebugInfo && prefab != null)
            Debug.Log($"[HolenSlotClickHandler] Prefab set: {prefab.name}");
    }

    /// <summary>
    /// Set the holen data for this slot (optional, for additional functionality)
    /// </summary>
    public void SetHolenData(HolenData data)
    {
        holenData = data;

        // Also set the prefab from data if available
        if (data != null && data.holenPrefab != null)
        {
            SetHolenPrefab(data.holenPrefab);
        }

        if (showDebugInfo && data != null)
            Debug.Log($"[HolenSlotClickHandler] Data set: {data.holenName}");
    }

    /// <summary>
    /// Called when the player taps/clicks this slot
    /// </summary>
    private void OnSlotTapped()
    {
        // Validate controller
        if (controller == null)
        {
            controller = FindObjectOfType<MultiplayerHolenController>();

            if (controller == null)
            {
                Debug.LogWarning("[HolenSlotClickHandler] MultiplayerHolenController not found in scene!");
                return;
            }
        }

        // Check if it's the player's turn (if required)
        if (requirePlayerTurn && !controller.IsTurn())
        {
            if (showDebugInfo)
                Debug.Log("[HolenSlotClickHandler] Not your turn - cannot select holen");

            // Optional: Show feedback to user
            ShowNotYourTurnFeedback();
            return;
        }

        // Validate prefab
        if (holenPrefab == null)
        {
            Debug.LogWarning("[HolenSlotClickHandler] No holenPrefab assigned to this slot!");
            return;
        }

        // IMPORTANT: Verify the prefab exists in Resources folder for PhotonNetwork.Instantiate
        if (!VerifyPrefabInResources())
        {
            Debug.LogError($"[HolenSlotClickHandler] Prefab '{holenPrefab.name}' must be in a Resources folder for multiplayer spawning!");
            return;
        }

        // Deselect all other slots
        DeselectAllSlots();

        // Select this slot
        isSelected = true;
        if (selectionHighlight != null)
        {
            selectionHighlight.enabled = true;
            selectionHighlight.color = highlightColor;
        }

        // Notify controller of selection
        // The controller will handle spawning with proper Master Client authority
        controller.OnHolenSelectedFromInventory(holenPrefab);

        if (showDebugInfo)
            Debug.Log($"[HolenSlotClickHandler] Selected: {holenPrefab.name}");
    }

    /// <summary>
    /// Verifies that the holen prefab is in a Resources folder (required for PhotonNetwork.Instantiate)
    /// </summary>
    private bool VerifyPrefabInResources()
    {
        if (holenPrefab == null) return false;

        // Try to load the prefab from Resources
        GameObject testLoad = Resources.Load<GameObject>(holenPrefab.name);

        if (testLoad == null)
        {
            Debug.LogWarning($"[HolenSlotClickHandler] Prefab '{holenPrefab.name}' not found in Resources folder! " +
                           $"For multiplayer, all spawnable prefabs must be in a Resources folder.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Show visual/audio feedback when player tries to select during opponent's turn
    /// </summary>
    private void ShowNotYourTurnFeedback()
    {
        // Optional: Add visual feedback here
        // Examples:
        // - Flash the slot briefly
        // - Show a "Not your turn" message
        // - Play a sound effect

        if (showDebugInfo)
            Debug.Log("[HolenSlotClickHandler] Player attempted to select during opponent's turn");
    }

    /// <summary>
    /// Deselect all other slots in the same parent
    /// </summary>
    private void DeselectAllSlots()
    {
        // Find all slot handlers in parent
        if (transform.parent != null)
        {
            HolenSlotClickHandler[] allSlots = transform.parent.GetComponentsInChildren<HolenSlotClickHandler>();

            foreach (var slot in allSlots)
            {
                slot.Deselect();
            }
        }
    }

    /// <summary>
    /// Deselect this slot
    /// </summary>
    public void Deselect()
    {
        isSelected = false;

        if (selectionHighlight != null)
        {
            selectionHighlight.enabled = false;
        }
    }

    /// <summary>
    /// Check if this slot is currently selected
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }

    /// <summary>
    /// Enable or disable interaction with this slot
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (slotButton != null)
        {
            slotButton.interactable = interactable;
        }

        // Optional: Visual feedback for disabled state
        if (!interactable && selectionHighlight != null && !isSelected)
        {
            // Could show a dimmed overlay or change color
        }
    }

    /// <summary>
    /// Get the prefab assigned to this slot
    /// </summary>
    public GameObject GetHolenPrefab()
    {
        return holenPrefab;
    }

    /// <summary>
    /// Get the holen data assigned to this slot
    /// </summary>
    public HolenData GetHolenData()
    {
        return holenData;
    }

    /// <summary>
    /// Force refresh the controller reference (useful after scene changes)
    /// </summary>
    public void RefreshController()
    {
        controller = FindObjectOfType<MultiplayerHolenController>();
        UpdateInteractability();
    }

    private void OnDestroy()
    {
        // Clean up button listener
        if (slotButton != null)
        {
            slotButton.onClick.RemoveListener(OnSlotTapped);
        }
    }
}