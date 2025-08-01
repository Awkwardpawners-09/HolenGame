using System.Collections.Generic;
using UnityEngine;
using System.IO;



public class HolenInventoryManager : MonoBehaviour
{
    [Header("Holen Database")]
    public List<HolenData> allHolens; // Assign manually or load at runtime

    [SerializeField]
    public List<HolenInventoryEntry> inventory = new List<HolenInventoryEntry>();

    private string SavePath => Path.Combine(Application.persistentDataPath, "holen_inventory.json");

    public InventoryUIManager inventoryUI;

    private void Start()
    {
        DontDestroyOnLoad(gameObject); // Ensures the inventory manager persists across scenes
        LoadInventory(); // Load the saved inventory data
        if (inventory == null || inventory.Count == 0)
        {
            AddHolen(GetRandomHolenID()); // Add random Holen if inventory is empty
        }
    }


    private string GetRandomHolenID()
    {
        if (allHolens.Count == 0)
        {
            Debug.LogWarning("No Holens in the database!");
            return null;
        }

        int index = Random.Range(0, allHolens.Count);
        return allHolens[index].holenID;
    }

    // 📦 Add a Holen by ID
    public void AddHolen(string holenID, int amount = 1)
    {
        var entry = inventory.Find(e => e.holenID == holenID);
        if (entry != null)
        {
            entry.quantity += amount;
        }
        else
        {
            inventory.Add(new HolenInventoryEntry(holenID, amount));
        }

        SaveInventory();
    }

    public void RemoveHolen(string holenID, int amount = -1)
    {
        var entry = inventory.Find(e => e.holenID == holenID);
        if (entry != null)
        {
            entry.quantity -= amount;

            if (entry.quantity <= 0)
            {
                inventory.Remove(entry);
            }

            SaveInventory();
            inventoryUI.RefreshUI();
        }
        else
        {
            Debug.LogWarning($"Tried to remove Holen {holenID}, but it doesn't exist in inventory.");
        }
    }

    public void SaveInventory()
    {
        InventorySaveSystem.Save(inventory);
    }

    public void LoadInventory()
    {
        inventory = InventorySaveSystem.Load();
    }

    // 🔍 Get HolenData by ID (for UI or instantiation)
    public HolenData GetHolenData(string holenID)
    {
        return allHolens.Find(h => h.holenID == holenID);
    }

    // ✅ Wrapper class for JSON compatibility
    [System.Serializable]
    private class InventoryWrapper
    {
        public List<HolenInventoryEntry> entries;
    }
}
