using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component for displaying a single holen slot in the inventory grid.
///
/// Click handling is done externally by HolenInventoryPanel — this script
/// only handles display (icon, name, quantity, rarity border color).
///
/// SETUP:
///  - Attach to your HolenSlotUI prefab.
///  - Assign iconImage, nameText, quantityText, and itemBorder in the Inspector.
///  - Make sure the prefab also has a Button component (HolenInventoryPanel will
///    add one automatically if missing, but it's cleaner to have it pre-added).
/// </summary>
public class HolenSlotUI : MonoBehaviour
{
    [Header("Slot Display")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text quantityText;

    [Header("Rarity Border")]
    [Tooltip("The Image used as the rarity-coloured border. Also used for selection highlight.")]
    public Image itemBorder;

    // Internal data reference
    private HolenData holenData;

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Populates the slot with holen data and quantity.
    /// Called by HolenInventoryPanel when building the grid.
    /// </summary>
    public void SetSlot(HolenData data, int quantity)
    {
        holenData = data;

        if (iconImage != null)
            iconImage.sprite = data.holenIcon;

        if (nameText != null)
            nameText.text = data.holenName;

        if (quantityText != null)
            quantityText.text = quantity.ToString();

        // Set border to rarity color
        if (itemBorder != null)
            itemBorder.color = GetRarityColor(data.rarity);
    }

    /// <summary>
    /// Returns the HolenData associated with this slot.
    /// </summary>
    public HolenData GetHolenData() => holenData;

    /// <summary>
    /// Returns true if this slot represents the same item as the given data.
    /// </summary>
    public bool IsSameItem(HolenData data) =>
        holenData != null && data != null && holenData.holenID == data.holenID;

    /// <summary>
    /// Returns true if this slot represents the item with the given ID.
    /// </summary>
    public bool IsSameItem(string id) =>
        holenData != null && holenData.holenID == id;

    // ─────────────────────────────────────────────
    //  RARITY COLOR
    // ─────────────────────────────────────────────

    private Color GetRarityColor(string rarity)
    {
        if (string.IsNullOrEmpty(rarity)) return Color.white;

        switch (rarity.ToLower())
        {
            case "common": return new Color(0.7f, 0.7f, 0.7f, 1f); // Gray
            case "uncommon": return new Color(0.2f, 1f, 0.2f, 1f); // Green
            case "rare": return new Color(0.3f, 0.5f, 1f, 1f); // Blue
            case "epic": return new Color(0.8f, 0.3f, 1f, 1f); // Purple
            case "legendary": return new Color(1f, 0.6f, 0f, 1f); // Orange/Gold
            case "mythic": return new Color(1f, 0.2f, 0.2f, 1f); // Red
            default: return Color.white;
        }
    }
}