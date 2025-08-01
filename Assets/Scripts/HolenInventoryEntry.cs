using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HolenInventoryEntry
{
    public string holenID;  // Unique string ID (e.g., "gold_holen")
    public int quantity;    // How many of this Holen the player owns

    public HolenInventoryEntry(string id, int qty)
    {
        holenID = id;
        quantity = qty;
    }
}