using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

/// <summary>
/// ENHANCED VERSION: Displays detailed information about a selected Holen.
/// This version checks for optional fields (backgroundSprite, detailImage, description)
/// and uses them if available, but works fine with the original HolenData too!
/// 
/// Setup:
/// 1. Create a UI panel with background image, selected holen image, and text fields
/// 2. Attach this script to that panel or a manager GameObject
/// 3. Assign the UI references in the Inspector
/// 4. This script automatically finds all HolenSlotClickHandler instances and subscribes to their clicks
/// </summary>
public class HolenDetailDisplayEnhanced : MonoBehaviour
{
    [Header("UI Elements to Update")]
    [Tooltip("Background image to update (uses backgroundSprite from HolenData if available)")]
    public Image backgroundImage;

    [Tooltip("Main display image for the selected Holen (uses detailImage or InventoryImage)")]
    public Image selectedHolenImage;

    [Tooltip("Text field for Holen name")]
    public TMP_Text holenNameText;

    [Tooltip("Text field for Holen rarity (optional)")]
    public TMP_Text holenRarityText;

    [Tooltip("Text field for description (optional)")]
    public TMP_Text holenDescriptionText;

    [Header("Settings")]
    [Tooltip("Auto-find and subscribe to all slot click handlers on Start")]
    public bool autoSubscribeToSlots = true;

    [Tooltip("Parent transform containing all HolenSlotUI instances (optional - will search entire scene if null)")]
    public Transform slotsContainer;

    [Header("Default Display")]
    [Tooltip("Show a default/placeholder when no Holen is selected")]
    public bool useDefaultDisplay = true;

    [Tooltip("Default sprite to show when nothing is selected")]
    public Sprite defaultHolenSprite;

    [Tooltip("Default background sprite")]
    public Sprite defaultBackgroundSprite;

    [Tooltip("Default text when nothing is selected")]
    public string defaultText = "Select a Holen";

    [Header("Animation (Optional)")]
    [Tooltip("Animate the display when showing new Holen")]
    public bool useAnimation = true;

    [Tooltip("Animation duration in seconds")]
    public float animationDuration = 0.3f;

    private HolenData currentlyDisplayedHolen;
    private bool isAnimating = false;

    private void Start()
    {
        if (autoSubscribeToSlots)
        {
            SubscribeToAllSlots();
        }

        // Show default display on start
        if (useDefaultDisplay)
        {
            ShowDefaultDisplay();
        }
    }

    private void OnEnable()
    {
        // Re-subscribe when enabled (in case slots were created while disabled)
        if (autoSubscribeToSlots)
        {
            SubscribeToAllSlots();
        }
    }

    /// <summary>
    /// Finds all HolenSlotClickHandler instances and subscribes to their Button clicks
    /// </summary>
    public void SubscribeToAllSlots()
    {
        HolenSlotClickHandler[] allSlots;

        // Search in container or entire scene
        if (slotsContainer != null)
        {
            allSlots = slotsContainer.GetComponentsInChildren<HolenSlotClickHandler>(true);
        }
        else
        {
            allSlots = FindObjectsOfType<HolenSlotClickHandler>(true);
        }

        foreach (HolenSlotClickHandler slotHandler in allSlots)
        {
            if (slotHandler.slotButton != null)
            {
                // Remove listener first to avoid duplicates
                slotHandler.slotButton.onClick.RemoveListener(() => OnSlotClicked(slotHandler));
                // Add listener
                slotHandler.slotButton.onClick.AddListener(() => OnSlotClicked(slotHandler));
            }
        }

        Debug.Log($"[HolenDetailDisplay] Subscribed to {allSlots.Length} Holen slots");
    }

    /// <summary>
    /// Called when any Holen slot is clicked
    /// </summary>
    private void OnSlotClicked(HolenSlotClickHandler clickedSlot)
    {
        HolenData holenData = clickedSlot.GetHolenData();

        if (holenData != null)
        {
            DisplayHolenDetails(holenData);
        }
        else
        {
            Debug.LogWarning("[HolenDetailDisplay] Clicked slot has no HolenData!");
        }
    }

    /// <summary>
    /// Updates all UI elements with the selected Holen's information
    /// Uses reflection to check for optional enhanced fields
    /// </summary>
    public void DisplayHolenDetails(HolenData holenData)
    {
        if (holenData == null)
        {
            Debug.LogWarning("[HolenDetailDisplay] Tried to display null HolenData");
            return;
        }

        currentlyDisplayedHolen = holenData;

        if (useAnimation && !isAnimating)
        {
            StartCoroutine(AnimateDisplay(holenData));
        }
        else
        {
            UpdateDisplayImmediate(holenData);
        }
    }

    /// <summary>
    /// Immediately updates the display without animation
    /// </summary>
    private void UpdateDisplayImmediate(HolenData holenData)
    {
        // Update background image
        if (backgroundImage != null)
        {
            Sprite bgSprite = GetFieldValue<Sprite>(holenData, "backgroundSprite");

            if (bgSprite != null)
            {
                backgroundImage.sprite = bgSprite;
            }
            else
            {
                // Fallback to InventoryImage or holenIcon
                backgroundImage.sprite = holenData.InventoryImage != null ? holenData.InventoryImage : holenData.holenIcon;
            }
            backgroundImage.enabled = true;
        }

        // Update selected Holen image
        if (selectedHolenImage != null)
        {
            // Try to get detailImage field (from enhanced version)
            Sprite detailSprite = GetFieldValue<Sprite>(holenData, "detailImage");

            if (detailSprite != null)
            {
                selectedHolenImage.sprite = detailSprite;
            }
            else
            {
                // Fallback to InventoryImage or holenIcon
                selectedHolenImage.sprite = holenData.InventoryImage != null ? holenData.InventoryImage : holenData.holenIcon;
            }
            selectedHolenImage.enabled = true;
        }

        // Update name text
        if (holenNameText != null)
        {
            holenNameText.text = holenData.holenName;
        }

        // Update rarity text
        if (holenRarityText != null)
        {
            holenRarityText.text = holenData.rarity;
            holenRarityText.color = GetRarityColor(holenData.rarity);
        }

        // Update description
        if (holenDescriptionText != null)
        {
            // Try to get description field (from enhanced version)
            string description = GetFieldValue<string>(holenData, "description");

            if (!string.IsNullOrEmpty(description))
            {
                holenDescriptionText.text = description;
            }
            else
            {
                // Fallback to showing ID and max stack
                holenDescriptionText.text = $"ID: {holenData.holenID}\nMax Stack: {holenData.maxStack}";
            }
        }

        Debug.Log($"[HolenDetailDisplay] Now displaying: {holenData.holenName}");
    }

    /// <summary>
    /// Animates the display when showing a new Holen
    /// </summary>
    private System.Collections.IEnumerator AnimateDisplay(HolenData holenData)
    {
        isAnimating = true;

        // Fade out current display
        float elapsed = 0f;
        while (elapsed < animationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / (animationDuration / 2));
            SetImageAlpha(selectedHolenImage, alpha);
            yield return null;
        }

        // Update the content
        UpdateDisplayImmediate(holenData);

        // Fade in new display
        elapsed = 0f;
        while (elapsed < animationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float alpha = elapsed / (animationDuration / 2);
            SetImageAlpha(selectedHolenImage, alpha);
            yield return null;
        }

        SetImageAlpha(selectedHolenImage, 1f);
        isAnimating = false;
    }

    /// <summary>
    /// Helper to set image alpha
    /// </summary>
    private void SetImageAlpha(Image image, float alpha)
    {
        if (image != null)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    /// <summary>
    /// Uses reflection to safely get a field value from HolenData
    /// Returns null/default if field doesn't exist (works with original HolenData)
    /// </summary>
    private T GetFieldValue<T>(HolenData holenData, string fieldName)
    {
        FieldInfo field = holenData.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);

        if (field != null && field.FieldType == typeof(T))
        {
            return (T)field.GetValue(holenData);
        }

        return default(T);
    }

    /// <summary>
    /// Shows the default/placeholder display
    /// </summary>
    public void ShowDefaultDisplay()
    {
        currentlyDisplayedHolen = null;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = defaultBackgroundSprite;
            backgroundImage.enabled = defaultBackgroundSprite != null;
        }

        if (selectedHolenImage != null)
        {
            selectedHolenImage.sprite = defaultHolenSprite;
            selectedHolenImage.enabled = defaultHolenSprite != null;
            SetImageAlpha(selectedHolenImage, 1f);
        }

        if (holenNameText != null)
        {
            holenNameText.text = defaultText;
        }

        if (holenRarityText != null)
        {
            holenRarityText.text = "";
        }

        if (holenDescriptionText != null)
        {
            holenDescriptionText.text = "";
        }
    }

    /// <summary>
    /// Clears the display
    /// </summary>
    public void ClearDisplay()
    {
        ShowDefaultDisplay();
    }

    /// <summary>
    /// Gets the currently displayed Holen data
    /// </summary>
    public HolenData GetCurrentlyDisplayedHolen()
    {
        return currentlyDisplayedHolen;
    }

    /// <summary>
    /// Returns a color based on rarity
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

    /// <summary>
    /// Call this if slots are dynamically created after Start (e.g., inventory refresh)
    /// </summary>
    public void RefreshSlotSubscriptions()
    {
        SubscribeToAllSlots();
    }

    private void OnDestroy()
    {
        // Clean up listeners
        HolenSlotClickHandler[] allSlots;

        if (slotsContainer != null)
        {
            allSlots = slotsContainer.GetComponentsInChildren<HolenSlotClickHandler>(true);
        }
        else
        {
            allSlots = FindObjectsOfType<HolenSlotClickHandler>(true);
        }

        foreach (HolenSlotClickHandler slotHandler in allSlots)
        {
            if (slotHandler.slotButton != null)
            {
                slotHandler.slotButton.onClick.RemoveListener(() => OnSlotClicked(slotHandler));
            }
        }
    }
}