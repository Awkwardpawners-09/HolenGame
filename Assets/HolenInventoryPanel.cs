using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the InventoryPanel GameObject (which sits under the local player's UI).
/// Reads HolenInventoryManager.Instance and builds one HolenSlotUI per owned holen.
///
/// ══ BUG FIX 1 — Only one player's inventory was populating ══════════════════
///   The old OnEnable() had this guard:
///       if (controller != null && !controller.photonView.IsMine) → SetActive(false)
///
///   This was WRONG. MultiplayerHolenControllerNew is ONE shared scene object (not
///   per-player). Its photonView is owned by whichever client first created it
///   (usually the MasterClient / Player 1). So on Player 2's screen, IsMine = false
///   and their inventory panel was immediately disabled every time it opened.
///
///   Fix: Remove the IsMine guard. Each client runs this script on their own local
///   UI Canvas. HolenInventoryManager is a DontDestroyOnLoad singleton that always
///   holds the LOCAL player's data, so each client correctly shows their own items.
///   The IsTurn() check in OnSlotClicked still prevents the wrong player from acting.
///
/// ══ BUG FIX 2 — Holen 3D model didn't change on selection ══════════════════
///   OnSlotClicked was only passing the network prefab to the controller, which
///   only changed the Photon ball name. The visible 3D model on the ball was never
///   swapped to match the selected holen.
///
///   Fix: Pass the full HolenData to OnHolenSelectedFromInventory so the controller
///   can also swap out the 3D model child using HolenData.holenPrefab.
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

    [Tooltip("The MultiplayerHolenControllerNew in the scene (the one shared controller).")]
    public MultiplayerHolenControllerNew controller;

    [Header("Holen Ball Prefabs")]
    [Tooltip("Drag ALL holenball network prefabs here. Each must have a HolenIdentifier with HolenData assigned.")]
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
        // FIX: Removed the old "if (!controller.photonView.IsMine) SetActive(false)" guard.
        // That guard was silently disabling Player 2's panel because the controller's
        // photonView belongs to Player 1. Each client populates their own panel from
        // their own HolenInventoryManager singleton — no ownership check needed here.
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

        Debug.Log($"[HolenInventoryPanel] Populated {spawnedSlots.Count} slots.");
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

        // Only the player whose turn it is should be able to change their holen.
        // controller.IsTurn() is evaluated locally per-client, so this is safe.
        if (!controller.IsTurn())
        {
            Debug.Log("[HolenInventoryPanel] Not your turn — ignoring slot click.");
            return;
        }

        // Find the Photon network prefab for this holenID.
        // This is the prefab used by PhotonNetwork.Instantiate (must be in a Resources folder).
        GameObject networkPrefab = FindPrefabForHolen(holenID);
        if (networkPrefab == null)
        {
            Debug.LogWarning($"[HolenInventoryPanel] No holenball prefab found for ID '{holenID}'.");
            return;
        }

        // FIX: Pass BOTH the network prefab AND the HolenData to the controller.
        //   - networkPrefab  → used to set holenBallPrefab for PhotonNetwork.Instantiate
        //   - data           → data.holenPrefab is the 3D model to show on the ball
        controller.OnHolenSelectedFromInventory(networkPrefab, data);

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