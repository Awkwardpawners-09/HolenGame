using UnityEngine;
using System.IO;

[System.Serializable]
public class PlayerData
{
    public int coins = 9999; // default value, only used if no save file exists

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
        return new PlayerData(); // default (500 coins)
    }
    public void ResetData()
{
    coins = 9999; // or set back to 500 if you prefer a "default start"
    Save();
}
}
