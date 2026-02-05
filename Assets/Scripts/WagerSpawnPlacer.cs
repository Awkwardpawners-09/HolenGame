using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class WagerSpawnPlacer : MonoBehaviour
{
    [Header("Slot settings")]
    public string slotNamePrefix = "Slot";    // e.g., "Slot1", "Slot2"...
    public bool randomizeOrder = true;
    public bool destroySlotAfterSpawn = true;
    public bool alignToSlotRotation = true;

    [Header("Player Specific Settings")]
    [Tooltip("Which player's wager to spawn? (1 or 2). Leave 0 to spawn ALL players' wagers.")]
    public int spawnForPlayer = 0; // 0 = both, 1 = player 1 only, 2 = player 2 only

    [Header("Debug")]
    [SerializeField] private List<GameObject> spawnedHolens = new List<GameObject>();

    void Start()
    {
        // CRITICAL: Only Master Client spawns to avoid duplicates!
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[WagerSpawnPlacer] Not Master Client - skipping spawn");
            return;
        }

        Debug.Log("[WagerSpawnPlacer] Master Client - will spawn holens");

        // Wait a frame to ensure WagerDataManager has loaded
        Invoke(nameof(SpawnWagers), 0.5f);
    }

    private void SpawnWagers()
    {
        // Get WagerDataManager
        var wagerData = WagerDataManager.Instance;
        if (wagerData == null)
        {
            Debug.LogError("[WagerSpawnPlacer] WagerDataManager not found! Make sure it persists from Lobby scene.");
            return;
        }

        // Get inventory manager
        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null)
        {
            Debug.LogError("[WagerSpawnPlacer] HolenInventoryManager not found in scene.");
            return;
        }

        // Collect all Slot children
        var slots = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith(slotNamePrefix))
                slots.Add(child);
        }

        if (slots.Count == 0)
        {
            Debug.LogWarning("[WagerSpawnPlacer] No Slot children found under PlayField.");
            return;
        }

        // Get wager data based on spawnForPlayer setting
        List<WagerManager.SelectedHolenRecord> wagersToSpawn;

        if (spawnForPlayer == 0)
        {
            // Spawn both players' wagers (each selection spawned individually, duplicates allowed)
            wagersToSpawn = wagerData.GetAllWageredHolensIndividual();
            Debug.Log("[WagerSpawnPlacer] Spawning wagers from BOTH players");
        }
        else if (spawnForPlayer == 1)
        {
            // Spawn only Player 1's wagers
            wagersToSpawn = wagerData.GetPlayerWager(1);
            Debug.Log("[WagerSpawnPlacer] Spawning wagers from Player 1 only");
        }
        else if (spawnForPlayer == 2)
        {
            // Spawn only Player 2's wagers
            wagersToSpawn = wagerData.GetPlayerWager(2);
            Debug.Log("[WagerSpawnPlacer] Spawning wagers from Player 2 only");
        }
        else
        {
            Debug.LogError($"[WagerSpawnPlacer] Invalid spawnForPlayer value: {spawnForPlayer}");
            return;
        }

        if (wagersToSpawn == null || wagersToSpawn.Count == 0)
        {
            Debug.LogWarning("[WagerSpawnPlacer] No wagered Holens to spawn.");
            return;
        }

        Debug.Log($"[WagerSpawnPlacer] Total wager records to process: {wagersToSpawn.Count}");
        foreach (var rec in wagersToSpawn)
        {
            Debug.Log($"[WagerSpawnPlacer] - Wager record: HolenID={rec.holenID}, Quantity={rec.quantity}");
        }

        // Build list of HolenData to spawn (one instance per selection, even if duplicate holenIDs)
        var items = new List<HolenData>();
        foreach (var rec in wagersToSpawn)
        {
            var data = inv.GetHolenData(rec.holenID);
            if (data != null && data.holenPrefab != null)
            {
                // Add each selection as a separate spawn (duplicates allowed)
                items.Add(data);
                Debug.Log($"[WagerSpawnPlacer] Added {data.holenName} (ID: {rec.holenID}) to spawn list");
            }
            else
            {
                Debug.LogWarning($"[WagerSpawnPlacer] Could not find HolenData or prefab for ID: {rec.holenID}");
            }
        }

        if (items.Count == 0)
        {
            Debug.LogWarning("[WagerSpawnPlacer] No valid Holen prefabs found to spawn.");
            return;
        }

        // Check if we have enough slots
        if (items.Count > slots.Count)
        {
            Debug.LogWarning($"[WagerSpawnPlacer] Not enough slots! Need {items.Count} but only have {slots.Count}. Some holens won't spawn.");
        }

        // Randomize if enabled
        if (randomizeOrder)
        {
            Shuffle(slots);
            Shuffle(items);
        }

        // Spawn holens at slot positions
        int spawnCount = Mathf.Min(items.Count, slots.Count);

        Debug.Log($"[WagerSpawnPlacer] Spawning {spawnCount} holens at {slots.Count} slots");

        for (int i = 0; i < spawnCount; i++)
        {
            var slot = slots[i];
            var data = items[i];

            // Use PhotonNetwork.Instantiate for multiplayer sync
            GameObject go = PhotonNetwork.Instantiate(
                data.holenPrefab.name, // Must match Resources folder path
                slot.position,
                alignToSlotRotation ? slot.rotation : Quaternion.identity
            );

            go.name = data.holenName;
            go.transform.SetParent(transform);
            spawnedHolens.Add(go);

            // Initialize HolenIdentifier with data reference
            var identifier = go.GetComponent<HolenIdentifier>();
            if (identifier != null)
            {
                identifier.SetHolenData(data);
            }
            else
            {
                Debug.LogWarning($"[WagerSpawnPlacer] {data.holenName} is missing HolenIdentifier component!");
            }

            Debug.Log($"[WagerSpawnPlacer] Spawned {data.holenName} at {slot.name}");

            if (destroySlotAfterSpawn)
                Destroy(slot.gameObject);
        }

        Debug.Log($"[WagerSpawnPlacer] ✅ Successfully spawned {spawnCount} holens");
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}