using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class HolenInventoryManager : MonoBehaviour
{
    [Header("Holen Database")]
    public List<HolenData> allHolens; // Assign in Inspector

    [SerializeField]
    public List<HolenInventoryEntry> inventory = new List<HolenInventoryEntry>();

    private string SavePath => Path.Combine(Application.persistentDataPath, "holen_inventory.json");

    public InventoryUIManager inventoryUI;

    public static HolenInventoryManager Instance; // ✅ Singleton for easy access

    private void Awake()
    {
        // ✅ Make this object persistent across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadInventory(); // Always load on start
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    private void Start()
    {
        // If inventory is empty (first run), give a random Holen
        if (inventory == null || inventory.Count == 0)
        {
            AddHolen(GetRandomHolenID());
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
        if (string.IsNullOrEmpty(holenID))
        {
            Debug.LogWarning("Tried to add null/empty Holen ID.");
            return;
        }

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

        if (inventoryUI != null)
            inventoryUI.RefreshUI(); // ✅ Auto update if UI is open
    }

    public void RemoveHolen(string holenID, int amount = 1)
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

            if (inventoryUI != null)
                inventoryUI.RefreshUI();
        }
        else
        {
            Debug.LogWarning($"Tried to remove Holen {holenID}, but it doesn't exist in inventory.");
        }
    }

    public void SaveInventory()
    {
        InventorySaveSystem.Save(inventory); // ✅ Uses your existing save system
        Debug.Log("✅ Inventory saved!");
    }

public void LoadInventory()
{
    var loaded = InventorySaveSystem.Load();
    if (loaded != null && loaded.Count > 0)
    {
        // Merge instead of overwrite
        foreach (var entry in loaded)
        {
            var existing = inventory.Find(e => e.holenID == entry.holenID);
            if (existing != null)
            {
                existing.quantity = entry.quantity; // keep saved quantity
            }
            else
            {
                inventory.Add(new HolenInventoryEntry(entry.holenID, entry.quantity));
            }
        }
    }

    Debug.Log("✅ Inventory merged from save!");
}


    // 🔍 Get HolenData by ID (for UI or instantiation)
    public HolenData GetHolenData(string holenID)
    {
        return allHolens.Find(h => h.holenID == holenID);
    }

    public void ResetInventory()
{
    inventory.Clear();
    SaveInventory();
    if (inventoryUI != null)
        inventoryUI.RefreshUI();
}

}
