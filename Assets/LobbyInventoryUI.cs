using Photon.Pun.Demo.PunBasics;
using UnityEngine;

public class LobbyInventoryUI : MonoBehaviour
{
    public GameManager inventoryUIManager; // Reference to the InventoryUIManager
    public HolenInventoryManager holenInventoryManager; // Reference to the HolenInventoryManager

    private void Start()
    {
        // Ensure the InventoryUIManager and HolenInventoryManager are properly referenced
        if (inventoryUIManager == null)
        {
            inventoryUIManager = FindObjectOfType<GameManager>(); // Fix: reference to InventoryUIManager, not itself
        }

        if (holenInventoryManager == null)
        {
            holenInventoryManager = FindObjectOfType<HolenInventoryManager>();
        }

        // Refresh the inventory UI to match the actual inventory data
        RefreshInventoryUI();
    }

    // Refresh the Inventory UI based on the current inventory data
    public void RefreshInventoryUI()
    {
        if (inventoryUIManager != null && holenInventoryManager != null)
        {
            inventoryUIManager.RefreshUI(); // Call RefreshUI from InventoryUIManager
        }
        else
        {
            Debug.LogError("InventoryUIManager or HolenInventoryManager is not properly assigned.");
        }
    }
}
