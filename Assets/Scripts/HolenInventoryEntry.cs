using System;

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