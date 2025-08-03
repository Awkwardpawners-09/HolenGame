using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HolenSlotUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text quantityText;

    private Button button; // Reference to the Button component
    private HolenData holenData; // Store the HolenData for this slot

    public GameObject contentPrefab; // Reference to Content prefab
    public Transform inventoryView; // Reference to the InventoryView to place the Content GameObject

    void Start()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("Button component not found on HolenSlotUI.");
            return;
        }

        // Set the button's onClick listener
        button.onClick.AddListener(OnClick);
    }

    public void SetSlot(HolenData data, int quantity)
    {
        holenData = data; // Store the HolenData for this slot
        iconImage.sprite = data.holenIcon;
        nameText.text = data.holenName;
        quantityText.text = "x" + quantity.ToString();
    }

    // Button click logic
    public void OnClick()
    {
        // This will trigger the method to handle the click and pass the item data to the wager manager
        if (WagerManager.Instance != null) // Check if WagerManager is accessible
        {
            WagerManager.Instance.HandleWagerItemClick(holenData, 1); // Pass the data to WagerManager
        }
        else
        {
            Debug.LogError("WagerManager not found in the scene!");
        }
    }

    // Add this helper function to check if clicked slot is the same
    public bool IsSameItem(HolenData data)
    {
        return holenData.holenID == data.holenID; // Compare IDs
    }
}
