using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Populates a panel with the player's currently owned Holens.
/// Mirrors HolenCollectionManager's instantiation pattern but only
/// displays Holens the player actually has in their inventory.
/// </summary>
public class HolenChangeInventory : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Content transform (e.g. with a GridLayoutGroup) to populate owned Holens into")]
    public Transform inventoryContentView;

    [Header("Prefabs")]
    [Tooltip("HolenSlotUI prefab used to display each owned Holen")]
    public GameObject holenSlotPrefab;

    private void Start()
    {
        HolenInventoryManager.OnInventoryChanged += OnInventoryChanged;
        RefreshInventoryUI();
    }

    private void OnDestroy()
    {
        HolenInventoryManager.OnInventoryChanged -= OnInventoryChanged;
    }

    /// <summary>
    /// Called automatically whenever the player's inventory changes.
    /// </summary>
    private void OnInventoryChanged()
    {
        RefreshInventoryUI();
    }

    /// <summary>
    /// Clears and re-populates the panel with every Holen the player currently owns.
    /// Sorted by rarity (Common → Mythic), then alphabetically by name.
    /// </summary>
    public void RefreshInventoryUI()
    {
        if (inventoryContentView == null)
        {
            Debug.LogWarning("[HolenChangeInventory] inventoryContentView is not assigned!");
            return;
        }

        if (holenSlotPrefab == null)
        {
            Debug.LogError("[HolenChangeInventory] holenSlotPrefab is not assigned!");
            return;
        }

        if (HolenInventoryManager.Instance == null)
        {
            Debug.LogError("[HolenChangeInventory] HolenInventoryManager instance not found!");
            return;
        }

        // Clear existing slots
        foreach (Transform child in inventoryContentView)
        {
            Destroy(child.gameObject);
        }

        // Grab every entry the player currently owns
        List<HolenInventoryEntry> inventory = HolenInventoryManager.Instance.GetAllHolens();

        // Resolve each entry to its full HolenData, filtering out anything invalid
        var resolvedEntries = new List<(HolenData data, int quantity)>();
        foreach (var entry in inventory)
        {
            HolenData data = HolenInventoryManager.Instance.GetHolenData(entry.holenID);
            if (data != null)
            {
                resolvedEntries.Add((data, entry.quantity));
            }
        }

        // Sort by rarity order, then alphabetically by name (matches CollectionManager pattern)
        var sorted = resolvedEntries
            .OrderBy(e => GetRarityOrder(e.data.rarity))
            .ThenBy(e => e.data.holenName)
            .ToList();

        // Instantiate a slot for each owned Holen
        foreach (var (holenData, quantity) in sorted)
        {
            GameObject slotObj = Instantiate(holenSlotPrefab, inventoryContentView);
            HolenSlotUI slotUI = slotObj.GetComponent<HolenSlotUI>();

            if (slotUI != null)
            {
                slotUI.SetSlot(holenData, quantity);
            }
        }

        Debug.Log($"[HolenChangeInventory] Panel refreshed — displaying {sorted.Count} owned Holens.");
    }

    /// <summary>
    /// Returns a numeric order for a rarity string so sorting works correctly.
    /// Matches the order used in HolenCollectionManager.
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
}