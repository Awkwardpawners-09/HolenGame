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

    // ====================================================================
    // 🧪 TESTING METHOD - Give all Holens with 99 quantity
    // ====================================================================
    /// <summary>
    /// FOR TESTING ONLY: Clears inventory and gives all Holens from the database with 99 quantity each.
    /// Attach this to a button's OnClick event in the Inspector.
    /// </summary>
    public void GiveAllHolensForTesting()
    {
        Debug.Log("🧪 [TESTING] Giving all Holens with 99 quantity...");

        // Clear existing inventory
        inventory.Clear();

        // Check if we have any Holens in the database
        if (allHolens == null || allHolens.Count == 0)
        {
            Debug.LogError("🧪 [TESTING] No Holens found in allHolens database! Assign HolenData assets in Inspector.");
            return;
        }

        // Add all Holens with 99 quantity
        int addedCount = 0;
        foreach (HolenData holen in allHolens)
        {
            if (holen != null && !string.IsNullOrEmpty(holen.holenID))
            {
                inventory.Add(new HolenInventoryEntry(holen.holenID, 99));
                addedCount++;
                Debug.Log($"🧪 [TESTING] Added {holen.holenName} x99");
            }
            else
            {
                Debug.LogWarning("🧪 [TESTING] Skipped null or invalid Holen in database");
            }
        }

        // Save the inventory
        SaveInventory();

        // Refresh UI if available
        if (inventoryUI != null)
        {
            inventoryUI.RefreshUI();
            Debug.Log("🧪 [TESTING] Inventory UI refreshed");
        }

        Debug.Log($"🧪 [TESTING] Successfully added {addedCount} Holens with 99 quantity each!");
        Debug.Log($"🧪 [TESTING] Total inventory entries: {inventory.Count}");
    }

    // ====================================================================
    // 🧪 TESTING METHOD - Reset to empty inventory
    // ====================================================================
    /// <summary>
    /// FOR TESTING ONLY: Completely clears the inventory and saves.
    /// Useful for testing the "empty inventory" state.
    /// </summary>
    public void ClearInventoryForTesting()
    {
        Debug.Log("🧪 [TESTING] Clearing entire inventory...");

        inventory.Clear();
        SaveInventory();

        if (inventoryUI != null)
        {
            inventoryUI.RefreshUI();
        }

        Debug.Log("🧪 [TESTING] Inventory cleared and saved!");
    }

    // ====================================================================
    // 🧪 TESTING METHOD - Give random Holens for quick testing
    // ====================================================================
    /// <summary>
    /// FOR TESTING ONLY: Gives 5-10 random Holens with random quantities (1-20).
    /// Good for testing variety without giving everything.
    /// </summary>
    public void GiveRandomHolensForTesting()
    {
        Debug.Log("🧪 [TESTING] Giving random Holens...");

        if (allHolens == null || allHolens.Count == 0)
        {
            Debug.LogError("🧪 [TESTING] No Holens in database!");
            return;
        }

        inventory.Clear();

        // Give 5-10 random Holens
        int numToGive = Random.Range(5, Mathf.Min(11, allHolens.Count + 1));
        List<HolenData> shuffled = new List<HolenData>(allHolens);

        // Shuffle the list
        for (int i = 0; i < shuffled.Count; i++)
        {
            HolenData temp = shuffled[i];
            int randomIndex = Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        // Add the first numToGive Holens
        for (int i = 0; i < numToGive; i++)
        {
            if (shuffled[i] != null && !string.IsNullOrEmpty(shuffled[i].holenID))
            {
                int quantity = Random.Range(1, 21); // 1-20 quantity
                inventory.Add(new HolenInventoryEntry(shuffled[i].holenID, quantity));
                Debug.Log($"🧪 [TESTING] Added {shuffled[i].holenName} x{quantity}");
            }
        }

        SaveInventory();

        if (inventoryUI != null)
        {
            inventoryUI.RefreshUI();
        }

        Debug.Log($"🧪 [TESTING] Added {numToGive} random Holens!");
    }

    // ====================================================================
    // 🧪 TESTING METHOD - Print inventory to console
    // ====================================================================
    /// <summary>
    /// FOR TESTING ONLY: Prints current inventory to console for debugging.
    /// </summary>
    public void PrintInventoryForTesting()
    {
        Debug.Log("🧪 [TESTING] ===== CURRENT INVENTORY =====");
        Debug.Log($"🧪 [TESTING] Total entries: {inventory.Count}");

        if (inventory.Count == 0)
        {
            Debug.Log("🧪 [TESTING] Inventory is empty!");
        }
        else
        {
            foreach (var entry in inventory)
            {
                HolenData data = GetHolenData(entry.holenID);
                string name = data != null ? data.holenName : "UNKNOWN";
                string rarity = data != null ? data.rarity : "N/A";
                Debug.Log($"🧪 [TESTING] {name} ({rarity}) x{entry.quantity} [ID: {entry.holenID}]");
            }
        }

        Debug.Log("🧪 [TESTING] ==============================");
    }
}