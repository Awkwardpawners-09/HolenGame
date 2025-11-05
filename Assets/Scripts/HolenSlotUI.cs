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