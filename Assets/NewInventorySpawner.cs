using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns inventory prefabs on this GameObject based on the player's current inventory.
///
/// For each unique Holen type in the player's inventory, this spawner instantiates
/// up to MAX_INSTANCES of that Holen's inventoryPrefab as children of this transform.
/// The total instance count is capped at MAX_INSTANCES (9) across ALL Holen types.
///
/// Example: Player has 99 China Holens → spawns 9 instances.
///          Player has 3 Fire Holens + 4 Water Holens → spawns 3 + 4 = 7 instances.
///          Player has 99 Fire Holens + 99 Water Holens → spawns 9 + 9 = 18 instances.
///
/// SETUP:
///  - Attach this script to any GameObject that will act as the spawner/parent.
///  - HolenInventoryManager must exist in the scene (singleton).
///  - Each HolenData asset must have its inventoryPrefab assigned.
///  - Call RefreshSpawner() manually or it auto-refreshes on OnEnable and inventory changes.
/// </summary>
public class NewInventorySpawner : MonoBehaviour
{
    private const int MAX_INSTANCES = 9;

    [Header("Spawn Settings")]
    [Tooltip("Optional: offset applied between each spawned instance (local space).")]
    public Vector3 spawnOffset = Vector3.zero;

    [Tooltip("If true, instances are destroyed and re-spawned on each refresh. If false, only adds/removes as needed.")]
    public bool fullRebuildOnRefresh = true;

    // Currently spawned instance GameObjects
    private readonly List<GameObject> spawnedInstances = new List<GameObject>();

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    private void OnEnable()
    {
        // Subscribe to inventory changes
        HolenInventoryManager.OnInventoryChanged += RefreshSpawner;

        // Initial spawn
        RefreshSpawner();
    }

    private void OnDisable()
    {
        HolenInventoryManager.OnInventoryChanged -= RefreshSpawner;
    }

    // ─────────────────────────────────────────────
    //  CORE SPAWNER LOGIC
    // ─────────────────────────────────────────────

    /// <summary>
    /// Clears all existing spawned instances and re-spawns based on current inventory.
    /// </summary>
    public void RefreshSpawner()
    {
        if (fullRebuildOnRefresh)
        {
            ClearSpawnedInstances();
            SpawnFromInventory();
        }
        else
        {
            SmartRefresh();
        }
    }

    private void SpawnFromInventory()
    {
        HolenInventoryManager manager = HolenInventoryManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[NewInventorySpawner] HolenInventoryManager.Instance is null. Cannot spawn.");
            return;
        }

        List<HolenInventoryEntry> inventoryEntries = manager.GetAllHolens();
        if (inventoryEntries == null || inventoryEntries.Count == 0)
        {
            Debug.Log("[NewInventorySpawner] Inventory is empty. Nothing to spawn.");
            return;
        }

        int totalSpawned = 0;

        foreach (HolenInventoryEntry entry in inventoryEntries)
        {
            HolenData holenData = manager.GetHolenData(entry.holenID);
            if (holenData == null)
            {
                Debug.LogWarning($"[NewInventorySpawner] No HolenData found for ID: {entry.holenID}. Skipping.");
                continue;
            }

            if (holenData.inventoryPrefab == null)
            {
                Debug.LogWarning($"[NewInventorySpawner] HolenData '{holenData.holenName}' has no inventoryPrefab assigned. Skipping.");
                continue;
            }

            // Cap per Holen type, not globally
            int countToSpawn = Mathf.Min(entry.quantity, MAX_INSTANCES);

            for (int i = 0; i < countToSpawn; i++)
            {
                Vector3 localPos = spawnOffset * totalSpawned;
                GameObject instance = Instantiate(holenData.inventoryPrefab, transform);
                instance.transform.localPosition = localPos;
                instance.name = $"{holenData.holenName}_Instance_{i}";

                spawnedInstances.Add(instance);
                totalSpawned++;
            }

            Debug.Log($"[NewInventorySpawner] Spawned {countToSpawn}x '{holenData.holenName}' (quantity in inventory: {entry.quantity})");
        }

        Debug.Log($"[NewInventorySpawner] Total instances spawned: {totalSpawned}");
    }

    /// <summary>
    /// Smarter refresh: removes excess instances and adds missing ones without full rebuild.
    /// Useful if you want to preserve instance state (e.g., physics, animations).
    /// </summary>
    private void SmartRefresh()
    {
        HolenInventoryManager manager = HolenInventoryManager.Instance;
        if (manager == null) return;

        // Calculate desired total (9 per type, no global cap)
        List<HolenInventoryEntry> inventoryEntries = manager.GetAllHolens();
        int desiredTotal = 0;
        foreach (var entry in inventoryEntries)
            desiredTotal += Mathf.Min(entry.quantity, MAX_INSTANCES);

        int currentCount = spawnedInstances.Count;

        if (currentCount > desiredTotal)
        {
            // Remove excess from the end
            for (int i = currentCount - 1; i >= desiredTotal; i--)
            {
                if (spawnedInstances[i] != null)
                    Destroy(spawnedInstances[i]);
                spawnedInstances.RemoveAt(i);
            }
        }
        else if (currentCount < desiredTotal)
        {
            // Full rebuild is safer when adding — avoids tracking which prefab to add
            ClearSpawnedInstances();
            SpawnFromInventory();
        }
    }

    /// <summary>
    /// Destroys all currently spawned instances.
    /// </summary>
    public void ClearSpawnedInstances()
    {
        foreach (GameObject go in spawnedInstances)
        {
            if (go != null)
                Destroy(go);
        }
        spawnedInstances.Clear();
    }

    // ─────────────────────────────────────────────
    //  PUBLIC UTILITIES
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns how many instances are currently spawned.
    /// </summary>
    public int GetSpawnedCount() => spawnedInstances.Count;

    /// <summary>
    /// Returns a read-only view of the spawned instances list.
    /// </summary>
    public IReadOnlyList<GameObject> GetSpawnedInstances() => spawnedInstances.AsReadOnly();
}