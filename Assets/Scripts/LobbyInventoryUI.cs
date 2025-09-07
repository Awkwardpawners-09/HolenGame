using UnityEngine;
using UnityEngine.UI;

public class LobbyInventoryUI : MonoBehaviour
{
    public static LobbyInventoryUI Instance; // Singleton instance

    public LobbyInventoryUI inventoryUIManager; // Reference to InventoryUIManager
    public HolenInventoryManager holenInventoryManager; // Reference to HolenInventoryManager

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instance
        }
        else
        {
            Instance = this; // Set this instance as the singleton
            // Remove DontDestroyOnLoad so it can be instantiated only in TestGame Scene
            // DontDestroyOnLoad(gameObject); 
        }
    }

    private void Start()
    {
        // Ensure the InventoryUIManager and HolenInventoryManager are properly referenced
        if (inventoryUIManager == null)
        {
            inventoryUIManager = FindObjectOfType<LobbyInventoryUI>(); // Automatically find InventoryUIManager
            if (inventoryUIManager == null)
            {
                Debug.LogError("InventoryUIManager is not assigned and cannot be found in the scene.");
            }
        }

        if (holenInventoryManager == null)
        {
            holenInventoryManager = FindObjectOfType<HolenInventoryManager>(); // Automatically find HolenInventoryManager
            if (holenInventoryManager == null)
            {
                Debug.LogError("HolenInventoryManager is not assigned and cannot be found in the scene.");
            }
        }

        // Refresh the inventory UI to match the actual inventory data
        RefreshInventoryUI();
    }

    // Refresh the Inventory UI based on the current inventory data
    public void RefreshInventoryUI()
    {
        if (inventoryUIManager != null && holenInventoryManager != null)
        {
            inventoryUIManager.RefreshInventoryUI(); // Call RefreshUI from InventoryUIManager
        }
        else
        {
            Debug.LogError("InventoryUIManager or HolenInventoryManager is not properly assigned.");
        }
    }

    // Method to call ToggleWagerItem from InventoryUIManager
    public void ToggleWagerItemInUI(HolenData holenData)
    {
        if (inventoryUIManager != null)
        {
            inventoryUIManager.ToggleWagerItemInUI(holenData); // Call ToggleWagerItem from InventoryUIManager
        }
    }
}
