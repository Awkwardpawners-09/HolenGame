using System.IO;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int coins = 0; // default coins
    public string playerName = ""; // player name

    [Header("Holen Loadout")]
    public string loadoutSlot1 = ""; // HolenID for slot 1
    public string loadoutSlot2 = ""; // HolenID for slot 2
    public string loadoutSlot3 = ""; // HolenID for slot 3

    private static string SavePath => Path.Combine(Application.persistentDataPath, "player_data.json");

    // ✅ Spend coins safely
    public bool SpendCoins(int amount)
    {
        if (coins >= amount)
        {
            coins -= amount;
            Save();
            return true;
        }
        return false;
    }

    // ✅ Add coins (e.g., rewards)
    public void AddCoins(int amount)
    {
        coins += amount;
        Save();
    }

    // ✅ Set player name
    public void SetPlayerName(string name)
    {
        playerName = name;
        Save();
    }

    // ✅ Save loadout (3 Holen IDs)
    public void SaveLoadout(string slot1ID, string slot2ID, string slot3ID)
    {
        loadoutSlot1 = slot1ID ?? "";
        loadoutSlot2 = slot2ID ?? "";
        loadoutSlot3 = slot3ID ?? "";
        Save();
        Debug.Log($"✅ Loadout saved: [{loadoutSlot1}, {loadoutSlot2}, {loadoutSlot3}]");
    }

    // ✅ Check if player has a saved loadout
    public bool HasSavedLoadout()
    {
        return !string.IsNullOrEmpty(loadoutSlot1) ||
               !string.IsNullOrEmpty(loadoutSlot2) ||
               !string.IsNullOrEmpty(loadoutSlot3);
    }

    // ✅ Get loadout as array of IDs
    public string[] GetLoadout()
    {
        return new string[] { loadoutSlot1, loadoutSlot2, loadoutSlot3 };
    }

    // ✅ Save to file
    public void Save()
    {
        string json = JsonUtility.ToJson(this, true);
        File.WriteAllText(SavePath, json);
    }

    // ✅ Load from file
    public static PlayerData Load()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<PlayerData>(json);
        }
        return new PlayerData(); // default values
    }

    public void ResetData()
    {
        coins = 9999;
        playerName = "";
        loadoutSlot1 = "";
        loadoutSlot2 = "";
        loadoutSlot3 = "";
        Save();
    }
}