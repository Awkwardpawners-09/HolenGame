using UnityEngine;

/// <summary>
/// OPTIONAL ENHANCEMENT: Enhanced version of HolenData with additional display fields
/// 
/// If you want more detailed information to display when a Holen is selected,
/// you can replace your existing HolenData.cs with this version.
/// 
/// New fields:
/// - backgroundSprite: Custom background image for the detail display
/// - description: Text description of the Holen
/// - detailImage: Optional larger/alternate image for detail view
/// 
/// This is completely optional - HolenDetailDisplay works fine with the original HolenData!
/// </summary>
[CreateAssetMenu(fileName = "NewHolenData", menuName = "Holens/Holen Data")]
public class HolenData : ScriptableObject
{
    [Header("Basic Info")]
    public string holenID;          // Unique ID for saving/loading
    public string rarity;           // e.g., "Common", "Rare", "Epic", "Legendary"
    public string holenName;        // Display name
    public Sprite holenIcon;        // Inventory UI image

    [Header("3D Model")]
    public GameObject holenPrefab;  // Prefab with material assigned

    [Header("Inventory Image")]
    public Sprite InventoryImage;   // Prefab with material assigned

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