using System.Collections.Generic;
using UnityEngine;

public class WagerSpawnPlacer : MonoBehaviour
{
    [Header("Slot settings")]
    public string slotNamePrefix = "Slot";    // e.g., "Slot1", "Slot2"...
    public bool randomizeOrder = true;
    public bool destroySlotAfterSpawn = true;
    public bool alignToSlotRotation = true;

    [Header("Debug")]
    [SerializeField] private List<GameObject> spawnedHolens = new List<GameObject>();

    void Start()
    {
        var wm = WagerManager.Instance;
        if (wm == null || wm.SelectedHolens == null || wm.SelectedHolens.Count == 0)
        {
            Debug.LogWarning("[WagerSpawnPlacer] No wagered Holens to spawn.");
            return;
        }

        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null)
        {
            Debug.LogError("[WagerSpawnPlacer] HolenInventoryManager not found in scene.");
            return;
        }

        // Collect all Slot children
        var slots = new List<Transform>();
        foreach (Transform child in transform)
            if (child.name.StartsWith(slotNamePrefix))
                slots.Add(child);

        if (slots.Count == 0)
        {
            Debug.LogWarning("[WagerSpawnPlacer] No Slot children under PlayField.");
            return;
        }

        // Build list of HolenData to spawn (respecting quantity)
        var items = new List<HolenData>();
        foreach (var rec in wm.SelectedHolens)
        {
            var data = inv.GetHolenData(rec.holenID);
            if (data != null && data.holenPrefab != null)
            {
                int qty = Mathf.Max(1, rec.quantity);
                for (int i = 0; i < qty; i++)
                    items.Add(data);
            }
        }

        if (randomizeOrder)
        {
            Shuffle(slots);
            Shuffle(items);
        }

        int spawnCount = Mathf.Min(items.Count, slots.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            var slot = slots[i];
            var data = items[i];
            var go = Instantiate(
                data.holenPrefab,
                slot.position,
                alignToSlotRotation ? slot.rotation : Quaternion.identity,
                transform
            );
            go.name = data.holenName;
            spawnedHolens.Add(go);

            if (destroySlotAfterSpawn)
                Destroy(slot.gameObject);
        }
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
