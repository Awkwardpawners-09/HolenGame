using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// Manages the player's Holen collection (discovery log).
/// Tracks which holens the player has ever obtained, separate from current inventory.
/// </summary>
public class HolenCollectionManager : MonoBehaviour
{
    [Header("Collection UI")]
    [Tooltip("Content transform with GridLayoutGroup to populate collected holens")]
    public Transform collectionContentView;

    [Tooltip("Text to display collection progress (e.g., '11/50')")]
    public TMPro.TMP_Text collectionProgressText;

    [Header("Prefabs")]
    [Tooltip("HolenSlotUI prefab to display each collected holen")]
    public GameObject holenSlotPrefab;

    [Header("Settings")]
    [Tooltip("Show silhouettes for uncollected holens (optional)")]
    public bool showUncollectedSlots = false;

    [Tooltip("Sprite to use for uncollected holens (optional)")]
    public Sprite uncollectedSprite;

    // List of collected holen IDs (persisted to disk)
    private HashSet<string> collectedHolenIDs = new HashSet<string>();

    private string SavePath => Path.Combine(Application.persistentDataPath, "holen_collection.json");

    public static HolenCollectionManager Instance;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadCollection();
    }

    private void Start()
    {
        // Subscribe to inventory changes to auto-update collection
        HolenInventoryManager.OnInventoryChanged += OnInventoryChanged;

        // Initial check of current inventory
        CheckInventoryForNewHolens();

        // Refresh the collection UI
        RefreshCollectionUI();

        // Force update progress text on start
        UpdateProgressText();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        HolenInventoryManager.OnInventoryChanged -= OnInventoryChanged;
    }

    /// <summary>
    /// Called when inventory changes - checks for newly collected holens
    /// </summary>
    private void OnInventoryChanged()
    {
        CheckInventoryForNewHolens();
        RefreshCollectionUI();
    }

    /// <summary>
    /// Checks current inventory and adds any new holens to collection
    /// </summary>
    private void CheckInventoryForNewHolens()
    {
        if (HolenInventoryManager.Instance == null) return;

        var inventory = HolenInventoryManager.Instance.GetAllHolens();
        bool hasNewHolens = false;

        foreach (var entry in inventory)
        {
            if (!collectedHolenIDs.Contains(entry.holenID))
            {
                AddToCollection(entry.holenID, false); // Don't save individually
                hasNewHolens = true;
                Debug.Log($"[Collection] New holen discovered: {entry.holenID}");
            }
        }

        if (hasNewHolens)
        {
            SaveCollection();
        }
    }

    /// <summary>
    /// Adds a holen to the collection by ID
    /// </summary>
    public void AddToCollection(string holenID, bool saveImmediately = true)
    {
        if (string.IsNullOrEmpty(holenID))
        {
            Debug.LogWarning("[Collection] Tried to add null/empty Holen ID to collection");
            return;
        }

        if (collectedHolenIDs.Contains(holenID))
        {
            // Already collected
            return;
        }

        collectedHolenIDs.Add(holenID);
        Debug.Log($"[Collection] Added {holenID} to collection!");

        if (saveImmediately)
        {
            SaveCollection();
        }
    }

    /// <summary>
    /// Checks if a holen has been collected
    /// </summary>
    public bool IsCollected(string holenID)
    {
        return collectedHolenIDs.Contains(holenID);
    }

    /// <summary>
    /// Gets the total number of unique holens collected
    /// </summary>
    public int GetCollectionCount()
    {
        return collectedHolenIDs.Count;
    }

    /// <summary>
    /// Gets the total number of holens available in the database
    /// </summary>
    public int GetTotalHolensCount()
    {
        if (HolenInventoryManager.Instance == null) return 0;
        return HolenInventoryManager.Instance.allHolens.Count;
    }

    /// <summary>
    /// Gets the collection completion percentage
    /// </summary>
    public float GetCollectionPercentage()
    {
        int total = GetTotalHolensCount();
        if (total == 0) return 0f;
        return (float)GetCollectionCount() / total * 100f;
    }

    /// <summary>
    /// Refreshes the collection UI by populating the content view
    /// </summary>
    public void RefreshCollectionUI()
    {
        if (collectionContentView == null)
        {
            Debug.LogWarning("[Collection] Collection content view not assigned!");
            return;
        }

        if (holenSlotPrefab == null)
        {
            Debug.LogError("[Collection] Holen slot prefab not assigned!");
            return;
        }

        if (HolenInventoryManager.Instance == null)
        {
            Debug.LogError("[Collection] HolenInventoryManager not found!");
            return;
        }

        // Clear existing slots
        foreach (Transform child in collectionContentView)
        {
            Destroy(child.gameObject);
        }

        var allHolens = HolenInventoryManager.Instance.allHolens;

        // Sort holens by rarity or name (optional)
        var sortedHolens = allHolens.OrderBy(h => GetRarityOrder(h.rarity)).ThenBy(h => h.holenName).ToList();

        // Create slots for all holens
        foreach (var holenData in sortedHolens)
        {
            if (holenData == null) continue;

            bool isCollected = collectedHolenIDs.Contains(holenData.holenID);

            // Skip uncollected if we're not showing them
            if (!isCollected && !showUncollectedSlots)
                continue;

            // Instantiate slot
            GameObject slotObj = Instantiate(holenSlotPrefab, collectionContentView);
            HolenSlotUI slotUI = slotObj.GetComponent<HolenSlotUI>();

            if (slotUI != null)
            {
                if (isCollected)
                {
                    // Show collected holen with no quantity
                    slotUI.SetSlot(holenData, 0); // Pass 0 quantity
                    HideQuantityText(slotUI); // Hide the quantity display
                }
                else
                {
                    // Show uncollected silhouette
                    SetupUncollectedSlot(slotUI, holenData);
                }
            }
        }

        // Update progress text
        UpdateProgressText();

        Debug.Log($"[Collection] UI refreshed. {collectedHolenIDs.Count}/{allHolens.Count} holens collected");
    }

    /// <summary>
    /// Updates the collection progress text (e.g., "11/50")
    /// </summary>
    private void UpdateProgressText()
    {
        if (collectionProgressText != null)
        {
            int collected = GetCollectionCount();
            int total = GetTotalHolensCount();
            collectionProgressText.text = $"{collected}/{total}";

            Debug.Log($"[Collection] Progress text updated: {collected}/{total}");
        }
    }

    /// <summary>
    /// Hides the quantity text on a HolenSlotUI
    /// </summary>
    private void HideQuantityText(HolenSlotUI slotUI)
    {
        if (slotUI.quantityText != null)
        {
            slotUI.quantityText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Sets up an uncollected holen slot (silhouette/locked)
    /// </summary>
    private void SetupUncollectedSlot(HolenSlotUI slotUI, HolenData holenData)
    {
        // Create a temporary HolenData for uncollected display
        HolenData uncollectedData = ScriptableObject.CreateInstance<HolenData>();
        uncollectedData.holenID = holenData.holenID;
        uncollectedData.holenName = "???";
        uncollectedData.holenIcon = uncollectedSprite != null ? uncollectedSprite : null;
        uncollectedData.rarity = holenData.rarity;

        slotUI.SetSlot(uncollectedData, 0);
        HideQuantityText(slotUI);

        // Make the icon darker/grayed out
        if (slotUI.iconImage != null)
        {
            slotUI.iconImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        }
    }

    /// <summary>
    /// Helper method to get rarity order for sorting
    /// </summary>
    private int GetRarityOrder(string rarity)
    {
        switch (rarity.ToLower())
        {
            case "common": return 1;
            case "uncommon": return 2;
            case "rare": return 3;
            case "epic": return 4;
            case "legendary": return 5;
            case "mythic": return 6;
            default: return 0;
        }
    }

    /// <summary>
    /// Saves the collection to disk
    /// </summary>
    public void SaveCollection()
    {
        try
        {
            CollectionSaveData saveData = new CollectionSaveData
            {
                collectedHolenIDs = new List<string>(collectedHolenIDs)
            };

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SavePath, json);

            Debug.Log($"[Collection] Saved {collectedHolenIDs.Count} collected holens to {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Collection] Failed to save collection: {e.Message}");
        }
    }

    /// <summary>
    /// Loads the collection from disk
    /// </summary>
    public void LoadCollection()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                CollectionSaveData saveData = JsonUtility.FromJson<CollectionSaveData>(json);

                if (saveData != null && saveData.collectedHolenIDs != null)
                {
                    collectedHolenIDs = new HashSet<string>(saveData.collectedHolenIDs);
                    Debug.Log($"[Collection] Loaded {collectedHolenIDs.Count} collected holens from save");
                }
            }
            else
            {
                Debug.Log("[Collection] No save file found. Starting fresh collection.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Collection] Failed to load collection: {e.Message}");
        }
    }

    /// <summary>
    /// Resets the entire collection (for testing)
    /// </summary>
    public void ResetCollection()
    {
        collectedHolenIDs.Clear();
        SaveCollection();
        RefreshCollectionUI();
        Debug.Log("[Collection] Collection reset!");
    }

    /// <summary>
    /// Unlocks all holens in the collection (for testing)
    /// </summary>
    public void UnlockAllForTesting()
    {
        if (HolenInventoryManager.Instance == null) return;

        var allHolens = HolenInventoryManager.Instance.allHolens;
        foreach (var holen in allHolens)
        {
            if (holen != null && !string.IsNullOrEmpty(holen.holenID))
            {
                AddToCollection(holen.holenID, false);
            }
        }

        SaveCollection();
        RefreshCollectionUI();
        Debug.Log($"[Collection] Unlocked all {collectedHolenIDs.Count} holens!");
    }

    /// <summary>
    /// Prints collection status to console (for debugging)
    /// </summary>
    public void PrintCollectionStatus()
    {
        Debug.Log("=== HOLEN COLLECTION STATUS ===");
        Debug.Log($"Collected: {GetCollectionCount()}/{GetTotalHolensCount()} ({GetCollectionPercentage():F1}%)");

        if (HolenInventoryManager.Instance != null)
        {
            foreach (var holen in HolenInventoryManager.Instance.allHolens)
            {
                if (holen == null) continue;
                bool collected = IsCollected(holen.holenID);
                Debug.Log($"{(collected ? "✅" : "❌")} {holen.holenName} ({holen.rarity}) - {holen.holenID}");
            }
        }

        Debug.Log("==============================");
    }
}

/// <summary>
/// Save data structure for the collection
/// </summary>
[System.Serializable]
public class CollectionSaveData
{
    public List<string> collectedHolenIDs;
}