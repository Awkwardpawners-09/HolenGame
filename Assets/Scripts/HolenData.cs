using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScriptableObject holding all data for a single Holen item.
/// </summary>
[CreateAssetMenu(fileName = "NewHolenData", menuName = "Holens/Holen Data")]
public class HolenData : ScriptableObject
{
    // ─────────────────────────────────────────────
    //  PROPERTY ENUM
    // ─────────────────────────────────────────────

    public enum HolenProperty
    {
        Light,
        Bouncy,
        Heavy
    }

    [Header("Basic Info")]
    public string holenID;          // Unique ID for saving/loading
    public string rarity;           // e.g., "Common", "Rare", "Epic", "Legendary"
    public string holenName;        // Display name
    public Sprite holenIcon;        // Inventory UI image

    [Header("Property")]
    [Tooltip("The physical property of this Holen (Light, Bouncy, or Heavy)")]
    public HolenProperty property;

    [Header("3D Model")]
    public GameObject holenPrefab;  // Prefab with material assigned

    [Header("Inventory")]
    public Sprite InventoryImage;   // Image used in inventory
    [Tooltip("Prefab used to represent this Holen in the inventory scene/world")]
    public GameObject inventoryPrefab; // NEW: Inventory Prefab

    [Header("Detail Display (Optional)")]
    [Tooltip("Custom background sprite for the detail panel (optional)")]
    public Sprite backgroundSprite;

    [Tooltip("Larger or alternate image for detail view (optional - uses InventoryImage if null)")]
    public Sprite detailImage;

    [Tooltip("Description text shown in detail view")]
    [TextArea(3, 6)]
    public string description;

    [Header("Other")]
    public int maxStack = 99;       // Optional: max stack per inventory slot
}