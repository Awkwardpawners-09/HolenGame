using UnityEngine;

[CreateAssetMenu(fileName = "NewHolenData", menuName = "Holens/Holen Data")]
public class HolenData : ScriptableObject
{
    [Header("Basic Info")]
    public string holenID;          // Unique ID for saving/loading
    public string rarity; // e.g., "Common", "Rare", "Epic", "Legendary"
    public string holenName;        // Display name
    public Sprite holenIcon;        // Inventory UI image

    [Header("3D Model")]
    public GameObject holenPrefab;  // Prefab with material assigned

    [Header("Other")]
    public int maxStack = 99;       // Optional: max stack per inventory slot
}