using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject contentScrollView;
    public GameObject holenUISlotPrefab;

    private HolenInventoryManager inventoryManager;

    void Start()
    {
        inventoryManager = FindObjectOfType<HolenInventoryManager>();

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

        // Get the wager manager for this player
        WagerManager wagerManager = GetComponent<WagerManager>();
        if (wagerManager == null)
        {
            wagerManager = transform.parent.GetComponentInChildren<WagerManager>();
        }

        // Instantiate an item for each entry in the inventory
        foreach (var entry in inventoryManager.inventory)
        {
            GameObject slot = Instantiate(holenUISlotPrefab, contentScrollView.transform);
            var holenUISlot = slot.GetComponent<HolenSlotUI>();

            if (holenUISlot != null)
            {
                HolenData data = inventoryManager.GetHolenData(entry.holenID);
                holenUISlot.SetSlot(data, entry.quantity);

                // Make the slot clickable to add to wager
                Button slotButton = slot.GetComponent<Button>();
                if (slotButton == null)
                    slotButton = slot.AddComponent<Button>();

                // Capture variables for closure
                HolenData capturedData = data;
                int capturedQty = entry.quantity;

                slotButton.onClick.AddListener(() => {
                    if (wagerManager != null)
                    {
                        wagerManager.HandleWagerItemClick(capturedData, capturedQty);
                    }
                });
            }
            else
            {
                Debug.LogError("HolenSlotUI script missing on prefab.");
            }
        }
    }

    public void RefreshInventory()
    {
        InstantiateInventoryItems();
    }
}