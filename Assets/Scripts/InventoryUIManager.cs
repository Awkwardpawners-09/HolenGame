using System.Collections.Generic;
using UnityEngine;

public class InventoryUIManager : MonoBehaviour
{
    public Transform slotParent; // Your Grid Layout Group
    public GameObject slotPrefab; // HolenSlotUI prefab
    private HolenInventoryManager inventoryManager;

    private void OnEnable()
    {
        // 🔧 Grab the singleton instance of HolenInventoryManager
        inventoryManager = HolenInventoryManager.Instance;

        if (inventoryManager != null)
        {
            inventoryManager.inventoryUI = this;

            // ✅ Always reload latest data when opening inventory
            inventoryManager.LoadInventory();
            RefreshUI();
        }
        else
        {
            Debug.LogError("No HolenInventoryManager found in scene!");
        }
    }

    public void RefreshUI()
    {
        Debug.Log("Refreshing Inventory UI...");

        // Clear existing slots
        foreach (Transform child in slotParent)
        {
            Destroy(child.gameObject);
        }

        if (inventoryManager == null) return;

        Debug.Log($"Inventory count: {inventoryManager.inventory.Count}");

        foreach (var entry in inventoryManager.inventory)
        {
            var holenData = inventoryManager.GetHolenData(entry.holenID);
            if (holenData == null)
            {
                Debug.LogWarning($"Missing HolenData for ID: {entry.holenID}");
                continue;
            }

            GameObject slotGO = Instantiate(slotPrefab, slotParent);
            Debug.Log($"Instantiated slot for {holenData.name}");

            var slotUI = slotGO.GetComponent<HolenSlotUI>();
            if (slotUI == null)
            {
                Debug.LogError("Missing HolenSlotUI component on slotPrefab!");
            }
            else
            {
                slotUI.SetSlot(holenData, entry.quantity);
            }
        }
    }
}