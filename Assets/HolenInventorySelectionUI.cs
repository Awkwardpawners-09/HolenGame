using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Manager for selecting up to 3 Holens from the player's inventory
/// Uses HolenSlotUI prefab and handles open/close animations
/// </summary>
public class HolenInventorySelectionUI : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("GameObject with Animator that has 'Open' and 'Close' animation triggers")]
    public GameObject inventoryPanel;

    [Tooltip("Animator component (auto-found if not assigned)")]
    public Animator panelAnimator;

    [Header("UI References")]
    [Tooltip("Parent transform where HolenSlotUI items will be spawned")]
    public Transform inventoryContentParent;

    [Tooltip("HolenSlotUI prefab")]
    public GameObject holenSlotUIPrefab;

    [Header("Selection Display")]
    [Tooltip("Images showing the 3 currently selected Holens")]
    public Image selectedSlot1Image;
    public Image selectedSlot2Image;
    public Image selectedSlot3Image;

    [Tooltip("Text labels for selected slots (optional)")]
    public TMP_Text selectedSlot1Text;
    public TMP_Text selectedSlot2Text;
    public TMP_Text selectedSlot3Text;

    [Header("Buttons")]
    [Tooltip("Button to close the inventory panel (plays Close animation)")]
    public Button closeButton;

    [Tooltip("Button to confirm selection and close (optional)")]
    public Button confirmButton;

    // Event to notify HolenChanger when selection changes
    public event System.Action<HolenData, HolenData, HolenData> OnHolensSelected;

    // Currently selected Holens (up to 3)
    private HolenData selectedSlot1;
    private HolenData selectedSlot2;
    private HolenData selectedSlot3;

    // List of spawned HolenSlotUI items
    private List<HolenSlotUI> spawnedSlots = new List<HolenSlotUI>();

    // Track if inventory is open
    private bool isOpen = false;

    private void Awake()
    {
        // Auto-find animator if not assigned
        if (panelAnimator == null && inventoryPanel != null)
        {
            panelAnimator = inventoryPanel.GetComponent<Animator>();
        }

        // Make sure panel starts inactive (or with scale 0 depending on your animation)
        if (inventoryPanel != null)
        {
            // Don't set inactive if you're using animations - instead ensure the animator starts in closed state
            if (panelAnimator == null)
            {
                inventoryPanel.SetActive(false);
            }
        }
    }

    private void Start()
    {
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }

        // Setup confirm button (optional - auto-closes after confirming)
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmAndClose);
        }
    }

    // ===================== OPEN/CLOSE METHODS =====================

    /// <summary>
    /// Opens the inventory panel with animation
    /// Called by HolenChanger when inventory button is pressed
    /// </summary>
    public void OpenInventory()
    {
        if (isOpen) return;

        isOpen = true;

        // Activate panel if it was inactive
        if (inventoryPanel != null && !inventoryPanel.activeSelf)
        {
            inventoryPanel.SetActive(true);
        }

        // Play open animation
        if (panelAnimator != null)
        {
            panelAnimator.SetTrigger("Open");
        }

        Debug.Log("Inventory opened");
    }

    /// <summary>
    /// Closes the inventory panel with animation
    /// </summary>
    public void CloseInventory()
    {
        if (!isOpen) return;

        isOpen = false;

        // Play close animation
        if (panelAnimator != null)
        {
            panelAnimator.SetTrigger("Close");
        }

        // If no animator, just deactivate
        if (panelAnimator == null && inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }

        Debug.Log("Inventory closed");
    }

    /// <summary>
    /// Confirms selection and closes inventory
    /// </summary>
    private void ConfirmAndClose()
    {
        // Notify listeners about the selection
        OnHolensSelected?.Invoke(selectedSlot1, selectedSlot2, selectedSlot3);

        Debug.Log($"Selection confirmed: {selectedSlot1?.holenName}, {selectedSlot2?.holenName}, {selectedSlot3?.holenName}");

        // Close the inventory
        CloseInventory();
    }

    // ===================== INITIALIZATION =====================

    /// <summary>
    /// Initializes the inventory UI with the current slot selections
    /// Called by HolenChanger when opening the inventory
    /// </summary>
    public void InitializeWithCurrentSlots(HolenData slot1, HolenData slot2, HolenData slot3)
    {
        // Store current selections
        selectedSlot1 = slot1;
        selectedSlot2 = slot2;
        selectedSlot3 = slot3;

        // Refresh the inventory display
        RefreshInventory();

        // Update selection display
        UpdateSelectionDisplay();
    }

    /// <summary>
    /// Refreshes the inventory by destroying old slots and creating new ones
    /// </summary>
    private void RefreshInventory()
    {
        // Clear existing slots
        foreach (var slot in spawnedSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }
        spawnedSlots.Clear();

        // Get HolenInventoryManager instance (persists from previous scene)
        HolenInventoryManager inventoryManager = HolenInventoryManager.Instance;

        if (inventoryManager == null)
        {
            Debug.LogError("HolenInventoryManager not found! Make sure it exists and persists from the menu scene.");
            return;
        }

        // Get all Holens from inventory
        List<HolenInventoryEntry> allHolens = inventoryManager.GetAllHolens();

        if (allHolens == null || allHolens.Count == 0)
        {
            Debug.LogWarning("No Holens in inventory!");
            return;
        }

        // Create a HolenSlotUI for each Holen in inventory
        foreach (var entry in allHolens)
        {
            HolenData holenData = inventoryManager.GetHolenData(entry.holenID);

            if (holenData != null)
            {
                CreateHolenSlot(holenData, entry.quantity);
            }
        }

        Debug.Log($"Populated inventory with {spawnedSlots.Count} Holens");
    }

    /// <summary>
    /// Creates a HolenSlotUI for a single Holen
    /// </summary>
    private void CreateHolenSlot(HolenData holenData, int quantity)
    {
        if (holenSlotUIPrefab == null || inventoryContentParent == null)
        {
            Debug.LogError("HolenSlotUI prefab or content parent not assigned!");
            return;
        }

        // Instantiate the HolenSlotUI
        GameObject slotObj = Instantiate(holenSlotUIPrefab, inventoryContentParent);

        // Get the HolenSlotUI component
        HolenSlotUI slotUI = slotObj.GetComponent<HolenSlotUI>();

        if (slotUI == null)
        {
            Debug.LogError("HolenSlotUI prefab doesn't have HolenSlotUI component!");
            Destroy(slotObj);
            return;
        }

        // Set up the slot with data
        slotUI.SetSlot(holenData, quantity);

        // Add click listener to the slot
        Button slotButton = slotObj.GetComponent<Button>();
        if (slotButton == null)
        {
            slotButton = slotObj.AddComponent<Button>();
        }

        // Capture the holenData in a local variable for the lambda
        HolenData capturedData = holenData;
        slotButton.onClick.AddListener(() => OnHolenSlotClicked(capturedData, slotUI));

        // Update visual state based on selection
        UpdateSlotVisualState(slotUI, holenData);

        // Add to list
        spawnedSlots.Add(slotUI);
    }

    // ===================== SELECTION LOGIC =====================

    /// <summary>
    /// Called when a HolenSlotUI is clicked
    /// </summary>
    private void OnHolenSlotClicked(HolenData holenData, HolenSlotUI slotUI)
    {
        // Check if this Holen is already selected
        if (IsHolenSelected(holenData))
        {
            // Deselect it
            DeselectHolen(holenData);
        }
        else
        {
            // Try to select it in the first available slot
            if (selectedSlot1 == null)
            {
                selectedSlot1 = holenData;
                Debug.Log($"Selected slot 1: {holenData.holenName}");
            }
            else if (selectedSlot2 == null)
            {
                selectedSlot2 = holenData;
                Debug.Log($"Selected slot 2: {holenData.holenName}");
            }
            else if (selectedSlot3 == null)
            {
                selectedSlot3 = holenData;
                Debug.Log($"Selected slot 3: {holenData.holenName}");
            }
            else
            {
                Debug.Log("All 3 slots are full! Deselect a Holen first.");
                return;
            }
        }

        // Update visuals
        UpdateSelectionDisplay();
        UpdateAllSlotVisuals();

        // Automatically notify selection change (real-time update)
        OnHolensSelected?.Invoke(selectedSlot1, selectedSlot2, selectedSlot3);
    }

    /// <summary>
    /// Checks if a Holen is currently selected in any slot
    /// </summary>
    private bool IsHolenSelected(HolenData holenData)
    {
        if (holenData == null) return false;

        return (selectedSlot1 != null && selectedSlot1.holenID == holenData.holenID) ||
               (selectedSlot2 != null && selectedSlot2.holenID == holenData.holenID) ||
               (selectedSlot3 != null && selectedSlot3.holenID == holenData.holenID);
    }

    /// <summary>
    /// Deselects a Holen from whichever slot it's in
    /// </summary>
    private void DeselectHolen(HolenData holenData)
    {
        if (selectedSlot1 != null && selectedSlot1.holenID == holenData.holenID)
        {
            selectedSlot1 = null;
            Debug.Log($"Deselected from slot 1: {holenData.holenName}");
        }
        else if (selectedSlot2 != null && selectedSlot2.holenID == holenData.holenID)
        {
            selectedSlot2 = null;
            Debug.Log($"Deselected from slot 2: {holenData.holenName}");
        }
        else if (selectedSlot3 != null && selectedSlot3.holenID == holenData.holenID)
        {
            selectedSlot3 = null;
            Debug.Log($"Deselected from slot 3: {holenData.holenName}");
        }
    }

    // ===================== VISUAL UPDATES =====================

    /// <summary>
    /// Updates the selection display showing which 3 Holens are selected
    /// </summary>
    private void UpdateSelectionDisplay()
    {
        // Update Slot 1
        if (selectedSlot1Image != null)
        {
            if (selectedSlot1 != null)
            {
                selectedSlot1Image.sprite = selectedSlot1.holenIcon;
                selectedSlot1Image.color = Color.white;
            }
            else
            {
                selectedSlot1Image.sprite = null;
                selectedSlot1Image.color = new Color(1f, 1f, 1f, 0.2f);
            }
        }

        if (selectedSlot1Text != null)
        {
            selectedSlot1Text.text = selectedSlot1 != null ? selectedSlot1.holenName : "Empty";
        }

        // Update Slot 2
        if (selectedSlot2Image != null)
        {
            if (selectedSlot2 != null)
            {
                selectedSlot2Image.sprite = selectedSlot2.holenIcon;
                selectedSlot2Image.color = Color.white;
            }
            else
            {
                selectedSlot2Image.sprite = null;
                selectedSlot2Image.color = new Color(1f, 1f, 1f, 0.2f);
            }
        }

        if (selectedSlot2Text != null)
        {
            selectedSlot2Text.text = selectedSlot2 != null ? selectedSlot2.holenName : "Empty";
        }

        // Update Slot 3
        if (selectedSlot3Image != null)
        {
            if (selectedSlot3 != null)
            {
                selectedSlot3Image.sprite = selectedSlot3.holenIcon;
                selectedSlot3Image.color = Color.white;
            }
            else
            {
                selectedSlot3Image.sprite = null;
                selectedSlot3Image.color = new Color(1f, 1f, 1f, 0.2f);
            }
        }

        if (selectedSlot3Text != null)
        {
            selectedSlot3Text.text = selectedSlot3 != null ? selectedSlot3.holenName : "Empty";
        }
    }

    /// <summary>
    /// Updates all slot visuals to reflect selection state
    /// </summary>
    private void UpdateAllSlotVisuals()
    {
        foreach (var slot in spawnedSlots)
        {
            if (slot != null)
            {
                HolenData slotData = slot.GetHolenData();
                UpdateSlotVisualState(slot, slotData);
            }
        }
    }

    /// <summary>
    /// Updates a single slot's visual state (selected/unselected)
    /// </summary>
    private void UpdateSlotVisualState(HolenSlotUI slot, HolenData holenData)
    {
        if (slot == null || holenData == null) return;

        bool isSelected = IsHolenSelected(holenData);

        // Update the border to show selection (make it brighter/highlighted)
        if (slot.itemBorder != null)
        {
            Color borderColor = slot.itemBorder.color;

            if (isSelected)
            {
                // Make border brighter and add alpha to show selection
                slot.itemBorder.color = new Color(
                    Mathf.Min(borderColor.r * 1.5f, 1f),
                    Mathf.Min(borderColor.g * 1.5f, 1f),
                    Mathf.Min(borderColor.b * 1.5f, 1f),
                    1f
                );
            }
            else
            {
                // Use the rarity color (already set by SetSlot)
                // Just ensure it's not highlighted
                Color rarityColor = GetRarityColor(holenData.rarity);
                slot.itemBorder.color = rarityColor;
            }
        }

        // Optional: Add scale effect to selected items
        if (isSelected)
        {
            slot.transform.localScale = Vector3.one * 1.1f;
        }
        else
        {
            slot.transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Returns a color based on rarity (same as HolenSlotUI)
    /// </summary>
    private Color GetRarityColor(string rarity)
    {
        switch (rarity.ToLower())
        {
            case "common":
                return new Color(0.7f, 0.7f, 0.7f, 1f); // Gray
            case "uncommon":
                return new Color(0.2f, 1f, 0.2f, 1f); // Green
            case "rare":
                return new Color(0.3f, 0.5f, 1f, 1f); // Blue
            case "epic":
                return new Color(0.8f, 0.3f, 1f, 1f); // Purple
            case "legendary":
                return new Color(1f, 0.6f, 0f, 1f); // Orange/Gold
            case "mythic":
                return new Color(1f, 0.2f, 0.2f, 1f); // Red
            default:
                return Color.white;
        }
    }

    // ===================== PUBLIC UTILITY METHODS =====================

    /// <summary>
    /// Clears all selections
    /// </summary>
    public void ClearSelection()
    {
        selectedSlot1 = null;
        selectedSlot2 = null;
        selectedSlot3 = null;

        UpdateSelectionDisplay();
        UpdateAllSlotVisuals();
    }

    /// <summary>
    /// Checks if the inventory panel is currently open
    /// </summary>
    public bool IsOpen()
    {
        return isOpen;
    }
}