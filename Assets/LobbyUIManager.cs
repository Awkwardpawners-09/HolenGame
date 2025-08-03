using UnityEngine;
using System.Collections.Generic;

public class LobbyUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject contentScrollView; // The Content GameObject in the Scroll View
    public GameObject holenUISlotPrefab; // Reference to the HolenUISlot prefab

    private HolenInventoryManager inventoryManager;
    public static LobbyUIManager Instance { get; private set; } // Singleton instance
    void Start()
    {
        // Find the InventoryManager in the scene
        inventoryManager = FindObjectOfType<HolenInventoryManager>();

        if (inventoryManager != null)
        {
            // Instantiate the inventory UI items
            InstantiateInventoryItems();
        }
        else
        {
            Debug.LogError("HolenInventoryManager not found in the scene!");
        }
    }

    void InstantiateInventoryItems()
    {
        // Clear any existing items in the scroll view
        foreach (Transform child in contentScrollView.transform)
        {
            Destroy(child.gameObject);
        }

        // Instantiate an item for each entry in the inventory
        foreach (var entry in inventoryManager.inventory)
        {
            // Create a new slot for each item
            GameObject slot = Instantiate(holenUISlotPrefab, contentScrollView.transform);
            // Set up the slot (for example, display the item name and quantity)
            var holenUISlot = slot.GetComponent<HolenSlotUI>();
            if (holenUISlot != null)
            {
                holenUISlot.SetSlot(inventoryManager.GetHolenData(entry.holenID), entry.quantity);
            }
            else
            {
                Debug.LogError("HolenSlotUI script missing on prefab.");
            }
        }
    }
}
