using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the InventoryPanel GameObject (which sits under the local player's UI).
/// Reads HolenInventoryManager.Instance (which is local and persistent) and
/// builds one HolenSlotUI per owned holen.
///
/// MULTIPLAYER NOTE:
///   HolenInventoryManager is a DontDestroyOnLoad singleton — it always holds
///   the LOCAL player's inventory. This panel simply reads from it, so each
///   Photon client automatically shows their own inventory. No special networking
///   is needed here.
///
/// SETUP:
///   1. Assign this script to your InventoryPanel GameObject.
///   2. slotPrefab    → your HolenSlotUI prefab
///   3. gridContainer → the Grid Layout Group Transform inside the panel
///   4. controller    → the MultiplayerHolenControllerNew on THIS player's prefab
///   5. holenBallPrefabs → drag ALL holenball prefabs (each needs a HolenIdentifier)
/// </summary>
public class HolenInventoryPanel : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ─────────────────────────────────────────────
    [Header("References")]
    [Tooltip("The HolenSlotUI prefab to instantiate for each inventory entry.")]
    public HolenSlotUI slotPrefab;

    [Tooltip("The Grid Layout Group Transform that slots will be parented to.")]
    public Transform gridContainer;

    [Tooltip("The MultiplayerHolenControllerNew that owns this panel (local player only).")]
    public MultiplayerHolenControllerNew controller;

    [Header("Holen Ball Prefabs")]
    [Tooltip("Drag ALL holenball prefabs here. Each must have a HolenIdentifier with HolenData assigned.")]
    public List<GameObject> holenBallPrefabs = new List<GameObject>();

    [Header("Selection Visuals")]
    [Tooltip("Border color when a slot is selected.")]
    public Color selectedHighlightColor = new Color(1f, 1f, 0f, 1f);

    [Tooltip("Border color applied to unselected slots during cooldown.")]
    public Color cooldownTintColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────
    private readonly Dictionary<string, HolenSlotUI> spawnedSlots = new Dictionary<string, HolenSlotUI>();
    private readonly Dictionary<string, Color> originalBorderColors = new Dictionary<string, Color>();

    private HolenSlotUI selectedSlot = null;
    private bool isOnCooldown = false;

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────
    private void OnEnable()
    {
        // Safety: only populate if this panel belongs to the local player's controller.
        // The controller reference is set in the Inspector on the local player prefab,
        // so on the remote client this panel won't be active anyway — but this guard
        // adds an extra layer of protection.
        if (controller != null && !controller.photonView.IsMine)
        {
            Debug.Log("[HolenInventoryPanel] Skipping populate — controller is not local player.");
            gameObject.SetActive(false);
            return;
        }

        PopulateGrid();
    }

    private void OnDisable()
    {
        isOnCooldown = false;
    }

    // ─────────────────────────────────────────────
    //  GRID POPULATION
    // ─────────────────────────────────────────────
    public void PopulateGrid()
    {
        if (slotPrefab == null)
        {
            Debug.LogError("[HolenInventoryPanel] slotPrefab is not assigned!");
            return;
        }
        if (gridContainer == null)
        {
            Debug.LogError("[HolenInventoryPanel] gridContainer is not assigned!");
            return;
        }
        if (HolenInventoryManager.Instance == null)
        {
            Debug.LogError("[HolenInventoryPanel] HolenInventoryManager.Instance is null! Make sure it persists across scenes.");
            return;
        }

        // Clear old slots
        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);

        spawnedSlots.Clear();
        originalBorderColors.Clear();
        selectedSlot = null;

        List<HolenInventoryEntry> entries = HolenInventoryManager.Instance.GetAllHolens();
        if (entries == null || entries.Count == 0)
        {
            Debug.Log("[HolenInventoryPanel] Inventory is empty — nothing to show.");
            return;
        }

        foreach (HolenInventoryEntry entry in entries)
        {
            if (string.IsNullOrEmpty(entry.holenID)) continue;

            HolenData data = HolenInventoryManager.Instance.GetHolenData(entry.holenID);
            if (data == null)
            {
                Debug.LogWarning($"[HolenInventoryPanel] No HolenData found for ID '{entry.holenID}'. Skipping.");
                continue;
            }

            HolenSlotUI slot = Instantiate(slotPrefab, gridContainer);
            slot.SetSlot(data, entry.quantity);

            // Store rarity color after SetSlot applies it
            Color rarityColor = (slot.itemBorder != null) ? slot.itemBorder.color : Color.white;
            originalBorderColors[entry.holenID] = rarityColor;

            // Wire button click — capture variables for closure
            string capturedID = entry.holenID;
            HolenSlotUI capturedSlot = slot;
            HolenData capturedData = data;

            Button btn = slot.GetComponent<Button>();
            if (btn == null) btn = slot.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => OnSlotClicked(capturedSlot, capturedID, capturedData));

            spawnedSlots[entry.holenID] = slot;
        }

        // Highlight the currently active holen
        if (controller != null && controller.holenBallPrefab != null)
        {
            HolenIdentifier id = controller.holenBallPrefab.GetComponent<HolenIdentifier>();
            if (id != null && id.holenData != null)
                ApplySelectionHighlight(id.holenData.holenID);
        }

        Debug.Log($"[HolenInventoryPanel] Populated {spawnedSlots.Count} slots for local player.");
    }

    // ─────────────────────────────────────────────
    //  SLOT CLICK HANDLER
    // ─────────────────────────────────────────────
    private void OnSlotClicked(HolenSlotUI slot, string holenID, HolenData data)
    {
        if (isOnCooldown)
        {
            Debug.Log("[HolenInventoryPanel] On cooldown — ignoring click.");
            return;
        }

        if (controller == null)
        {
            Debug.LogError("[HolenInventoryPanel] controller is null!");
            return;
        }

        // Extra multiplayer guard — only respond if this is the local player's controller
        if (!controller.photonView.IsMine)
        {
            Debug.LogWarning("[HolenInventoryPanel] Slot clicked on non-local controller. Ignoring.");
            return;
        }

        GameObject prefab = FindPrefabForHolen(holenID);
        if (prefab == null)
        {
            Debug.LogWarning($"[HolenInventoryPanel] No holenball prefab found for ID '{holenID}'.");
            return;
        }

        controller.OnHolenSelectedFromInventory(prefab);
        ApplySelectionHighlight(holenID);
        StartCoroutine(CooldownVisual(controller.changeHolenCooldown));

        Debug.Log($"[HolenInventoryPanel] Selected: {data.holenName} ({holenID})");
    }

    // ─────────────────────────────────────────────
    //  SELECTION HIGHLIGHT
    // ─────────────────────────────────────────────
    private void ApplySelectionHighlight(string holenID)
    {
        // Restore previous selection to its rarity color
        if (selectedSlot != null && selectedSlot.itemBorder != null)
        {
            HolenData prevData = selectedSlot.GetHolenData();
            if (prevData != null && originalBorderColors.TryGetValue(prevData.holenID, out Color origColor))
                selectedSlot.itemBorder.color = origColor;
        }

        // Apply highlight to new selection
        if (spawnedSlots.TryGetValue(holenID, out HolenSlotUI newSlot))
        {
            if (newSlot.itemBorder != null)
                newSlot.itemBorder.color = selectedHighlightColor;
            selectedSlot = newSlot;
        }
    }

    // ─────────────────────────────────────────────
    //  COOLDOWN VISUAL
    // ─────────────────────────────────────────────
    private IEnumerator CooldownVisual(float duration)
    {
        isOnCooldown = true;

        foreach (var kvp in spawnedSlots)
        {
            if (kvp.Value == selectedSlot) continue;
            if (kvp.Value.itemBorder != null)
                kvp.Value.itemBorder.color = cooldownTintColor;
        }

        yield return new WaitForSeconds(duration);

        isOnCooldown = false;

        foreach (var kvp in spawnedSlots)
        {
            if (kvp.Value == selectedSlot) continue;
            if (kvp.Value.itemBorder != null && originalBorderColors.TryGetValue(kvp.Key, out Color origColor))
                kvp.Value.itemBorder.color = origColor;
        }
    }

    // ─────────────────────────────────────────────
    //  PREFAB LOOKUP
    // ─────────────────────────────────────────────
    private GameObject FindPrefabForHolen(string holenID)
    {
        foreach (GameObject prefab in holenBallPrefabs)
        {
            if (prefab == null) continue;
            HolenIdentifier id = prefab.GetComponent<HolenIdentifier>();
            if (id != null && id.holenData != null && id.holenData.holenID == holenID)
                return prefab;
        }
        Debug.LogWarning($"[HolenInventoryPanel] No prefab in list matches holenID '{holenID}'.");
        return null;
    }
}