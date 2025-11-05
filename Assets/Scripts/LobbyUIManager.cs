using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject contentScrollView;
    public GameObject holenUISlotPrefab;

    private HolenInventoryManager inventoryManager;
    private WagerManager wagerManager; // Cache the wager manager reference

    void Start()
    {
        // Find inventory manager
        inventoryManager = FindObjectOfType<HolenInventoryManager>();

        // Get the wager manager for this player (sibling component)
        wagerManager = GetComponent<WagerManager>();
        if (wagerManager == null)
        {
            wagerManager = transform.parent.GetComponentInChildren<WagerManager>();
        }

        if (inventoryManager != null)
        {
            InstantiateInventoryItems();
        }
        else
        {
            Debug.LogError("HolenInventoryManager not found in the scene!");
        }
    }

    void InstantiateInventoryItems()
    {
        // Clear any existing items
        foreach (Transform child in contentScrollView.transform)
        {
            Destroy(child.gameObject);
        }

        if (inventoryManager == null || inventoryManager.inventory == null)
        {
            Debug.LogWarning("Inventory is null or empty!");
            return;
        }

        // Instantiate an item for each entry in the inventory
        foreach (var entry in inventoryManager.inventory)
        {
            GameObject slot = Instantiate(holenUISlotPrefab, contentScrollView.transform);
            var holenUISlot = slot.GetComponent<HolenSlotUI>();

            if (holenUISlot != null)
            {
                HolenData data = inventoryManager.GetHolenData(entry.holenID);

                // Set the slot data (visual only, no button listener in HolenSlotUI)
                holenUISlot.SetSlot(data, entry.quantity);

                // Add button component if it doesn't exist
                Button slotButton = slot.GetComponent<Button>();
                if (slotButton == null)
                    slotButton = slot.AddComponent<Button>();

                // Clear any existing listeners from the prefab
                slotButton.onClick.RemoveAllListeners();

                // Capture variables for closure
                HolenData capturedData = data;
                int capturedQty = entry.quantity;

                // Add click listener that connects to THIS player's wager manager
                slotButton.onClick.AddListener(() => {
                    if (wagerManager != null)
                    {
                        wagerManager.HandleWagerItemClick(capturedData, capturedQty);
                    }
                    else
                    {
                        Debug.LogError("WagerManager not found for this player!");
                    }
                });
            }
            else
            {
                Debug.LogError("HolenSlotUI script missing on prefab.");
            }
        }

        Debug.Log($"Loaded {inventoryManager.inventory.Count} items into inventory UI");
    }

    public void RefreshInventory()
    {
        InstantiateInventoryItems();
    }
}