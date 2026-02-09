using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UPDATED: Attach this to each HolenSlotUI prefab in the inventory panel.
/// Handles player taps/clicks on inventory slots and forwards the selection
/// to MultiplayerHolenController for spawning the selected holen.
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

    [Header("Holen Data")]
    [Tooltip("The networked prefab that will be spawned when this slot is tapped. Must be in Resources folder.")]
    public GameObject holenPrefab;

    [Tooltip("Reference to HolenData for this slot (optional, for additional info)")]
    public HolenData holenData;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private MultiplayerHolenController controller;
    private bool isSelected = false;

    private void Start()
    {
        // Find the controller in the scene
        controller = FindObjectOfType<MultiplayerHolenController>();

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

        // Check if it's the player's turn
        if (!controller.IsTurn())
        {
            if (showDebugInfo)
                Debug.Log("[HolenSlotClickHandler] Not your turn - cannot select holen");
            return;
        }

        // Validate prefab
        if (holenPrefab == null)
        {
            Debug.LogWarning("[HolenSlotClickHandler] No holenPrefab assigned to this slot!");
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
        controller.OnHolenSelectedFromInventory(holenPrefab);

        if (showDebugInfo)
            Debug.Log($"[HolenSlotClickHandler] Selected: {holenPrefab.name}");
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

    private void OnDestroy()
    {
        // Clean up button listener
        if (slotButton != null)
        {
            slotButton.onClick.RemoveListener(OnSlotTapped);
        }
    }
}