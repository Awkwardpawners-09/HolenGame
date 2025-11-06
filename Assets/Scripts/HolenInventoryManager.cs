using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class HolenInventoryManager : MonoBehaviour
{
    [Header("Holen Database")]
    public List<HolenData> allHolens; // Assign in Inspector

    [SerializeField]
    public List<HolenInventoryEntry> inventory = new List<HolenInventoryEntry>();

    [Header("Player Name")]
    [SerializeField] private string playerName = "";
    public string PlayerName => playerName; // Read-only access

    [Header("Player Name Display UI")]
    [Tooltip("TMP Text in menu where player name is displayed")]
    [SerializeField] public TMPro.TMP_Text playerNameDisplayText;

    /// <summary>
    /// Manually refresh the player name display (call this if UI doesn't auto-update)
    /// </summary>
    public void RefreshPlayerNameDisplay()
    {
        UpdatePlayerNameDisplay();
    }

    private string SavePath => Path.Combine(Application.persistentDataPath, "holen_inventory.json");

    public InventoryUIManager inventoryUI;

    public static HolenInventoryManager Instance; // ✅ Singleton for easy access

    // ✅ Events
    public static event System.Action OnInventoryChanged;
    public static event System.Action<string> OnPlayerNameChanged;

    private void Awake()
    {
        // ✅ Make this object persistent across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadInventory(); // Load inventory
            LoadPlayerData(); // Load player name
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

        // Update player name display
        UpdatePlayerNameDisplay();
    }

    // ===================== PLAYER NAME METHODS =====================

    /// <summary>
    /// Sets the player name and saves it to disk
    /// </summary>
    public void SetPlayerName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("Cannot set empty player name");
            return;
        }

        playerName = name;
        SavePlayerData();
        UpdatePlayerNameDisplay();

        // Notify listeners (e.g., UI elements)
        OnPlayerNameChanged?.Invoke(playerName);

        Debug.Log($"✅ Player name set to: {playerName}");
    }

    /// <summary>
    /// Checks if player has set their name
    /// </summary>
    public bool HasPlayerName()
    {
        return !string.IsNullOrEmpty(playerName);
    }

    /// <summary>
    /// Updates the player name display text in menu
    /// </summary>
    private void UpdatePlayerNameDisplay()
    {
        if (playerNameDisplayText != null && !string.IsNullOrEmpty(playerName))
        {
            playerNameDisplayText.text = playerName;
        }
    }

    /// <summary>
    /// Saves player name to persistent storage (using PlayerData)
    /// </summary>
    private void SavePlayerData()
    {
        // Load existing player data (to preserve coins)
        PlayerData data = PlayerData.Load();

        // Update the player name
        data.playerName = this.playerName;

        // Save back to file
        data.Save();

        Debug.Log($"✅ Player name saved: {playerName}");
    }

    /// <summary>
    /// Loads player name from persistent storage
    /// </summary>
    private void LoadPlayerData()
    {
        PlayerData data = PlayerData.Load();

        if (!string.IsNullOrEmpty(data.playerName))
        {
            playerName = data.playerName;
            Debug.Log($"✅ Loaded player name: {playerName}");
        }
        else
        {
            Debug.Log("No player name found (first launch)");
        }
    }

    // ===================== HOLEN INVENTORY METHODS =====================

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
            inventoryUI.RefreshUI();

        OnInventoryChanged?.Invoke();
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

            OnInventoryChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"Tried to remove Holen {holenID}, but it doesn't exist in inventory.");
        }
    }

    public void SaveInventory()
    {
        InventorySaveSystem.Save(inventory);
        Debug.Log("✅ Inventory saved!");
    }

    public void LoadInventory()
    {
        var loaded = InventorySaveSystem.Load();
        if (loaded != null && loaded.Count > 0)
        {
            foreach (var entry in loaded)
            {
                var existing = inventory.Find(e => e.holenID == entry.holenID);
                if (existing != null)
                {
                    existing.quantity = entry.quantity;
                }
                else
                {
                    inventory.Add(new HolenInventoryEntry(entry.holenID, entry.quantity));
                }
            }
        }

        Debug.Log("✅ Inventory merged from save!");
    }

    public HolenData GetHolenData(string holenID)
    {
        return allHolens.Find(h => h.holenID == holenID);
    }

    public List<HolenInventoryEntry> GetAllHolens()
    {
        return new List<HolenInventoryEntry>(inventory);
    }

    public void ResetInventory()
    {
        inventory.Clear();
        SaveInventory();
        if (inventoryUI != null)
            inventoryUI.RefreshUI();

        OnInventoryChanged?.Invoke();
    }

    // ===================== TESTING METHODS =====================

    public void GiveAllHolensForTesting()
    {
        Debug.Log("🧪 [TESTING] Giving all Holens with 99 quantity...");

        inventory.Clear();

        if (allHolens == null || allHolens.Count == 0)
        {
            Debug.LogError("🧪 [TESTING] No Holens found in allHolens database!");
            return;
        }

        int addedCount = 0;
        foreach (HolenData holen in allHolens)
        {
            if (holen != null && !string.IsNullOrEmpty(holen.holenID))
            {
                inventory.Add(new HolenInventoryEntry(holen.holenID, 99));
                addedCount++;
                Debug.Log($"🧪 [TESTING] Added {holen.holenName} x99");
            }
        }

        SaveInventory();

        if (inventoryUI != null)
        {
            inventoryUI.RefreshUI();
        }

        OnInventoryChanged?.Invoke();

        Debug.Log($"🧪 [TESTING] Successfully added {addedCount} Holens with 99 quantity each!");
    }

    public void ClearInventoryForTesting()
    {
        Debug.Log("🧪 [TESTING] Clearing entire inventory...");

        inventory.Clear();
        SaveInventory();

        if (inventoryUI != null)
        {
            inventoryUI.RefreshUI();
        }

        OnInventoryChanged?.Invoke();

        Debug.Log("🧪 [TESTING] Inventory cleared and saved!");
    }

    public void GiveRandomHolensForTesting()
    {
        Debug.Log("🧪 [TESTING] Giving random Holens...");

        if (allHolens == null || allHolens.Count == 0)
        {
            Debug.LogError("🧪 [TESTING] No Holens in database!");
            return;
        }

        inventory.Clear();

        int numToGive = Random.Range(5, Mathf.Min(11, allHolens.Count + 1));
        List<HolenData> shuffled = new List<HolenData>(allHolens);

        for (int i = 0; i < shuffled.Count; i++)
        {
            HolenData temp = shuffled[i];
            int randomIndex = Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        for (int i = 0; i < numToGive; i++)
        {
            if (shuffled[i] != null && !string.IsNullOrEmpty(shuffled[i].holenID))
            {
                int quantity = Random.Range(1, 21);
                inventory.Add(new HolenInventoryEntry(shuffled[i].holenID, quantity));
                Debug.Log($"🧪 [TESTING] Added {shuffled[i].holenName} x{quantity}");
            }
        }

        SaveInventory();

        if (inventoryUI != null)
        {
            inventoryUI.RefreshUI();
        }

        OnInventoryChanged?.Invoke();

        Debug.Log($"🧪 [TESTING] Added {numToGive} random Holens!");
    }

    public void PrintInventoryForTesting()
    {
        Debug.Log("🧪 [TESTING] ===== CURRENT INVENTORY =====");
        Debug.Log($"🧪 [TESTING] Player Name: {(string.IsNullOrEmpty(playerName) ? "NOT SET" : playerName)}");
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