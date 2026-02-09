using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// UPDATED: Spawns wagered holens without requiring HolenPhysicsSync script.
/// Works with MultiplayerHolenController's integrated physics sync.
/// </summary>
public class WagerSpawnPlacer : MonoBehaviour
{
    [Header("Slot settings")]
    public string slotNamePrefix = "Slot";
    public bool randomizeOrder = true;
    public bool destroySlotAfterSpawn = true;
    public bool alignToSlotRotation = true;

    [Header("Player Specific Settings")]
    [Tooltip("Which player's wager to spawn? (1 or 2). Leave 0 to spawn ALL players' wagers.")]
    public int spawnForPlayer = 0;

    [Header("Physics Settings")]
    [Tooltip("Y position for all spawned holens (table height)")]
    public float spawnHeight = 0.5f;

    [Header("Debug")]
    [SerializeField] private List<GameObject> spawnedHolens = new List<GameObject>();
    public bool showDebugInfo = true;

    void Start()
    {
        // CRITICAL: Only Master Client spawns to avoid duplicates!
        if (!PhotonNetwork.IsMasterClient)
        {
            if (showDebugInfo)
                Debug.Log("[WagerSpawnPlacer] Not Master Client - skipping spawn");
            return;
        }

        if (showDebugInfo)
            Debug.Log("[WagerSpawnPlacer] Master Client - will spawn holens");

        // Wait for WagerDataManager to load
        Invoke(nameof(SpawnWagers), 0.5f);
    }

    private void SpawnWagers()
    {
        // Get WagerDataManager
        var wagerData = WagerDataManager.Instance;
        if (wagerData == null)
        {
            Debug.LogError("[WagerSpawnPlacer] WagerDataManager not found!");
            return;
        }

        // Get inventory manager
        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null)
        {
            Debug.LogError("[WagerSpawnPlacer] HolenInventoryManager not found!");
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
            Debug.LogWarning("[WagerSpawnPlacer] No Slot children found!");
            return;
        }

        // Get wager data based on spawnForPlayer setting
        List<WagerManager.SelectedHolenRecord> wagersToSpawn;

        if (spawnForPlayer == 0)
        {
            wagersToSpawn = wagerData.GetAllWageredHolensIndividual();
            if (showDebugInfo)
                Debug.Log("[WagerSpawnPlacer] Spawning wagers from BOTH players");
        }
        else if (spawnForPlayer == 1)
        {
            wagersToSpawn = wagerData.GetPlayerWager(1);
            if (showDebugInfo)
                Debug.Log("[WagerSpawnPlacer] Spawning wagers from Player 1 only");
        }
        else if (spawnForPlayer == 2)
        {
            wagersToSpawn = wagerData.GetPlayerWager(2);
            if (showDebugInfo)
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

        if (showDebugInfo)
        {
            Debug.Log($"[WagerSpawnPlacer] Total wager records: {wagersToSpawn.Count}");
            foreach (var rec in wagersToSpawn)
            {
                Debug.Log($"[WagerSpawnPlacer] - HolenID={rec.holenID}, Quantity={rec.quantity}");
            }
        }

        // Build list of HolenData to spawn
        var items = new List<HolenData>();
        foreach (var rec in wagersToSpawn)
        {
            var data = inv.GetHolenData(rec.holenID);
            if (data != null && data.holenPrefab != null)
            {
                items.Add(data);
                if (showDebugInfo)
                    Debug.Log($"[WagerSpawnPlacer] Added {data.holenName} (ID: {rec.holenID})");
            }
            else
            {
                Debug.LogWarning($"[WagerSpawnPlacer] Could not find HolenData for ID: {rec.holenID}");
            }
        }

        if (items.Count == 0)
        {
            Debug.LogWarning("[WagerSpawnPlacer] No valid Holen prefabs found.");
            return;
        }

        // Check slots
        if (items.Count > slots.Count)
        {
            Debug.LogWarning($"[WagerSpawnPlacer] Not enough slots! Need {items.Count} but have {slots.Count}");
        }

        // Randomize if enabled
        if (randomizeOrder)
        {
            Shuffle(slots);
            Shuffle(items);
        }

        // Spawn holens
        int spawnCount = Mathf.Min(items.Count, slots.Count);

        if (showDebugInfo)
            Debug.Log($"[WagerSpawnPlacer] Spawning {spawnCount} holens");

        for (int i = 0; i < spawnCount; i++)
        {
            var slot = slots[i];
            var data = items[i];

            // Set spawn position with correct height
            Vector3 spawnPos = slot.position;
            spawnPos.y = spawnHeight;

            // Spawn via Photon
            GameObject go = PhotonNetwork.Instantiate(
                data.holenPrefab.name,
                spawnPos,
                alignToSlotRotation ? slot.rotation : Quaternion.identity
            );

            go.name = data.holenName;
            go.transform.SetParent(transform);
            spawnedHolens.Add(go);

            // Setup components
            SetupHolenComponents(go, data);

            if (showDebugInfo)
                Debug.Log($"[WagerSpawnPlacer] Spawned {data.holenName} at {slot.name}");

            if (destroySlotAfterSpawn)
                Destroy(slot.gameObject);
        }

        if (showDebugInfo)
            Debug.Log($"[WagerSpawnPlacer] ✅ Successfully spawned {spawnCount} holens");

        // Register holens with controller for physics sync
        RegisterHolensWithController();

        // Notify completion
        BroadcastMessage("OnWagerHolensSpawned", SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// Setup necessary components on spawned holen
    /// </summary>
    private void SetupHolenComponents(GameObject holen, HolenData data)
    {
        // Ensure Rigidbody exists
        Rigidbody rb = holen.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = holen.AddComponent<Rigidbody>();
        }

        // Configure Rigidbody (controller will handle detailed setup)
        rb.isKinematic = false;
        rb.useGravity = false; // Billiards-style on flat surface
        rb.mass = 1f;

        // Ensure Collider exists
        if (holen.GetComponent<Collider>() == null)
        {
            holen.AddComponent<SphereCollider>();
        }

        // Ensure PhotonView exists
        PhotonView pv = holen.GetComponent<PhotonView>();
        if (pv == null)
        {
            Debug.LogWarning($"[WagerSpawnPlacer] {holen.name} missing PhotonView!");
        }

        // Setup HolenIdentifier
        HolenIdentifier identifier = holen.GetComponent<HolenIdentifier>();
        if (identifier != null)
        {
            identifier.SetHolenData(data);
        }
        else
        {
            Debug.LogWarning($"[WagerSpawnPlacer] {holen.name} missing HolenIdentifier!");
        }

        // Tag and layer
        if (!holen.CompareTag("Objective"))
        {
            holen.tag = "Objective";
        }
    }

    /// <summary>
    /// Register all spawned holens with MultiplayerHolenController for physics sync
    /// </summary>
    private void RegisterHolensWithController()
    {
        MultiplayerHolenController controller = FindObjectOfType<MultiplayerHolenController>();

        if (controller == null)
        {
            Debug.LogWarning("[WagerSpawnPlacer] No MultiplayerHolenController found - holens won't be synced!");
            return;
        }

        foreach (GameObject holen in spawnedHolens)
        {
            if (holen != null)
            {
                controller.RegisterHolen(holen);
            }
        }

        if (showDebugInfo)
            Debug.Log($"[WagerSpawnPlacer] Registered {spawnedHolens.Count} holens with controller");
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Get all spawned holens
    /// </summary>
    public List<GameObject> GetSpawnedHolens()
    {
        spawnedHolens.RemoveAll(h => h == null);
        return spawnedHolens;
    }

    /// <summary>
    /// Check if all spawned holens have stopped moving
    /// </summary>
    public bool AreAllHolensStopped()
    {
        MultiplayerHolenController controller = FindObjectOfType<MultiplayerHolenController>();
        if (controller != null)
        {
            return controller.AreAllHolensStopped();
        }
        return true;
    }
}