using UnityEngine;

public class TestGachaKeyInput : MonoBehaviour
{
    public HolenInventoryManager inventoryManager;
    public InventoryUIManager inventoryUI; // 🔄 Updated name here

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            string randomID = GetRandomHolenID();
            inventoryManager.AddHolen(randomID);
            Invoke(nameof(RefreshInventoryUI), 0.1f);
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            string randomFromInventory = GetRandomOwnedHolenID();
            if (!string.IsNullOrEmpty(randomFromInventory))
            {
                inventoryManager.RemoveHolen(randomFromInventory, 1); // Explicit amount
                Invoke(nameof(RefreshInventoryUI), 0.1f);
            }
            else
            {
                Debug.Log("No Holens to remove from inventory!");
            }
        }
    }

    private void RefreshInventoryUI()
    {
        inventoryUI.RefreshUI();
    }
    private string GetRandomHolenID()
    {
        if (inventoryManager.allHolens.Count == 0)
        {
            Debug.LogWarning("No Holens in the database!");
            return null;
        }

        int index = Random.Range(0, inventoryManager.allHolens.Count);
        return inventoryManager.allHolens[index].holenID;
    }

    private string GetRandomOwnedHolenID()
    {
        var currentInventory = inventoryManager.inventory;
        if (currentInventory == null || currentInventory.Count == 0)
        {
            return null;
        }

        int index = Random.Range(0, currentInventory.Count);
        return currentInventory[index].holenID;
    }
}
