using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component for displaying a holen slot.
/// Click handling is now done by LobbyNetworkManager, not internally.
/// </summary>
public class HolenSlotUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text quantityText;

    [Header("Rarity Border")]
    public Image itemBorder; // ✅ Reference to the ItemBorder Image

    private HolenData holenData; // Store the HolenData for this slot

    public GameObject contentPrefab; // Reference to Content prefab
    public Transform inventoryView; // Reference to the InventoryView to place the Content GameObject

    /// <summary>
    /// Sets up the slot display with holen data and quantity.
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

        // ✅ Set border color based on rarity
        if (itemBorder != null)
        {
            itemBorder.color = GetRarityColor(data.rarity);
        }
    }

    /// <summary>
    /// Returns a color based on the rarity string.
    /// Customize these colors to match your game's theme!
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
                return Color.white; // Default if rarity is unknown
        }
    }

    /// <summary>
    /// Gets the holen data associated with this slot.
    /// </summary>
    public HolenData GetHolenData()
    {
        return holenData;
    }

    /// <summary>
    /// Checks if this slot represents the same item.
    /// </summary>
    public bool IsSameItem(HolenData data)
    {
        return holenData != null && data != null && holenData.holenID == data.holenID;
    }

    /// <summary>
    /// Checks if this slot represents the same item by ID.
    /// </summary>
    public bool IsSameItem(string holenID)
    {
        return holenData != null && holenData.holenID == holenID;
    }
}