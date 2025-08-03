using UnityEngine;
using System.Collections.Generic;

public class WagerManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject wagerContent; // The Content GameObject in the P1Wager Scroll View
    public GameObject holenUISlotPrefab; // Reference to the HolenUISlot prefab (same as in the inventory)
    public static WagerManager Instance { get; private set; } // Singleton instance

    private List<GameObject> selectedItems = new List<GameObject>(); // To keep track of selected items

    void Start()
    {
        // Optional: If you want to initialize with some pre-selected items, do it here.
    }

    public void HandleWagerItemClick(HolenData holenData, int quantity)
    {
        // Check if the item is already in the P1Wager content
        GameObject existingItem = selectedItems.Find(item => item.GetComponent<HolenSlotUI>().IsSameItem(holenData));

        if (existingItem != null)
        {
            // Item is already selected, so remove it
            selectedItems.Remove(existingItem);
            Destroy(existingItem);
        }
        else
        {
            // If we have less than 3 selected items, add it to the P1Wager content
            if (selectedItems.Count < 3)
            {
                GameObject newSlot = Instantiate(holenUISlotPrefab, wagerContent.transform);
                var holenUISlot = newSlot.GetComponent<HolenSlotUI>();
                if (holenUISlot != null)
                {
                    holenUISlot.SetSlot(holenData, quantity);
                    selectedItems.Add(newSlot);
                }
                else
                {
                    Debug.LogError("HolenSlotUI script missing on prefab.");
                }
            }
            else
            {
                Debug.LogWarning("Maximum of 3 items can be selected.");
            }
        }
    }
}
