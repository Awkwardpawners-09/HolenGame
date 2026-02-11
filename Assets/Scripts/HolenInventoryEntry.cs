using System;

/// <summary>
/// Represents a single entry in the player's Holen inventory.
/// Stores the Holen ID and quantity owned.
/// </summary>
[Serializable]
public class HolenInventoryEntry
{
    public string holenID;
    public int quantity;

    public HolenInventoryEntry(string id, int qty)
    {
        holenID = id;
        quantity = qty;
    }
}