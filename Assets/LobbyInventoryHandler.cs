using Photon.Pun.Demo.PunBasics;
using UnityEngine;
using UnityEngine.UI;

public class LobbyInventoryHandler : MonoBehaviour
{
    public GameManager inventoryUIManager; // Reference to InventoryUIManager
    public HolenInventoryManager holenInventoryManager; // Reference to HolenInventoryManager
    public Transform player1SlotParent; // Parent container for Player 1's inventory slots
    public GameObject slotPrefab; // The prefab for displaying an individual item slot

    private void Start()
    {
        // Ensure the HolenInventoryManager and InventoryUIManager are properly assigned
        if (holenInventoryManager == null)
        {
            holenInventoryManager = FindObjectOfType<HolenInventoryManager>();
        }

        if (inventoryUIManager == null)
        {
            inventoryUIManager = FindObjectOfType<GameManager>();
        }

        if (player1SlotParent == null || slotPrefab == null)
        {
            Debug.LogError("Player1SlotParent or SlotPrefab not assigned in the inspector!");
            return;
        }

        // Load and populate Player 1's inventory
        PopulateInventoryUIForPlayer1();
    }

    // Populate Player 1's inventory UI slots
    private void PopulateInventoryUIForPlayer1()
    {
        if (holenInventoryManager == null || inventoryUIManager == null)
        {
            return;
        }

        Debug.Log("Populating Player 1's Inventory UI...");

        // Clear the current inventory view
        foreach (Transform child in player1SlotParent)
        {
            Destroy(child.gameObject); // Destroy any existing slots
        }

        // Display all Holens in Player 1's inventory
        foreach (var entry in holenInventoryManager.inventory)
        {
            var holenData = holenInventoryManager.GetHolenData(entry.holenID);
            if (holenData == null)
            {
                Debug.LogWarning($"Missing HolenData for ID: {entry.holenID}");
                continue;
            }

            // Create a new inventory slot
            GameObject slotGO = Instantiate(slotPrefab, player1SlotParent);
            var slotUI = slotGO.GetComponent<HolenSlotUI>();
            if (slotUI != null)
            {
                slotUI.SetSlot(holenData, entry.quantity);
            }
            else
            {
                Debug.LogError("Missing HolenSlotUI component on slotPrefab!");
            }
        }
    }
}
