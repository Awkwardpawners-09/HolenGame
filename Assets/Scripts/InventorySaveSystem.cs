using System.IO;
using UnityEngine;
using System.Collections.Generic;

public static class InventorySaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "holen_inventory.json");

    public static void Save(List<HolenInventoryEntry> entries)
    {
        InventoryWrapper wrapper = new InventoryWrapper { entries = entries };
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"✅ Inventory saved to: {SavePath}");
    }

    public static List<HolenInventoryEntry> Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("🆕 No save file found. Starting with empty inventory.");
            return new List<HolenInventoryEntry>();
        }

        string json = File.ReadAllText(SavePath);
        InventoryWrapper wrapper = JsonUtility.FromJson<InventoryWrapper>(json);
        return wrapper.entries ?? new List<HolenInventoryEntry>();
    }

    [System.Serializable]
    private class InventoryWrapper
    {
        public List<HolenInventoryEntry> entries;
    }
}
