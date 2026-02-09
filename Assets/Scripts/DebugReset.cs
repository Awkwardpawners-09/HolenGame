using UnityEngine;

/// <summary>
/// Debug utility for quick keyboard shortcuts during development.
/// Attach to any GameObject in your scene.
/// WARNING: Remove or disable in production builds!
/// </summary>
public class DebugReset : MonoBehaviour
{
    [Header("Keyboard Shortcuts")]
    [Tooltip("Enable/disable debug keyboard shortcuts")]
    public bool enableDebugKeys = true;

    void Update()
    {
        if (!enableDebugKeys) return;

        // Press R to reset ALL player data
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAllData();
        }

        // Press C to add 1000 coins
        if (Input.GetKeyDown(KeyCode.C))
        {
            AddCoins(1000);
        }

        // Press E to add 50 energy
        if (Input.GetKeyDown(KeyCode.E))
        {
            AddEnergy(50);
        }

        // Press P to print player data
        if (Input.GetKeyDown(KeyCode.P))
        {
            PrintPlayerData();
        }

        // Press N to reset player name
        if (Input.GetKeyDown(KeyCode.N))
        {
            ResetPlayerName();
        }

        // Press I to reset inventory
        if (Input.GetKeyDown(KeyCode.I))
        {
            ResetInventory();
        }

        // Press Delete to reset EVERYTHING
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            ResetEverything();
        }
    }

    private void ResetAllData()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.ResetAllDataForTesting();
            Debug.Log("🔄 [DEBUG-R] Player data reset! Coins = 0, Energy = 0, Name = empty");
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    private void AddCoins(int amount)
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddCoinsForTesting(amount);
            int total = PlayerDataManager.Instance.GetCoins();
            Debug.Log($"💰 [DEBUG-C] Added {amount} coins (Total: {total})");
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    private void AddEnergy(int amount)
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddEnergyForTesting(amount);
            int total = PlayerDataManager.Instance.GetEnergy();
            Debug.Log($"⚡ [DEBUG-E] Added {amount} energy (Total: {total})");
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    private void PrintPlayerData()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.PrintDataForTesting();
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    private void ResetPlayerName()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetPlayerName("");
            Debug.Log("👤 [DEBUG-N] Player name reset - PlayerNameSetup will show on restart");
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    private void ResetInventory()
    {
        if (HolenInventoryManager.Instance != null)
        {
            HolenInventoryManager.Instance.ResetInventory();
            Debug.Log("🎒 [DEBUG-I] Inventory reset! All Holens removed");
        }
        else
        {
            Debug.LogWarning("⚠️ HolenInventoryManager not found!");
        }
    }

    private void ResetEverything()
    {
        // Reset player data
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.ResetAllDataForTesting();
        }

        // Reset inventory
        if (HolenInventoryManager.Instance != null)
        {
            HolenInventoryManager.Instance.ResetInventory();
        }

        Debug.Log("🔥 [DEBUG-DELETE] COMPLETE RESET! All player data and inventory cleared!");
    }

    // Optional: Display on-screen help
    private void OnGUI()
    {
        if (!enableDebugKeys) return;

        // Only show in editor
#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        style.fontSize = 12;
        style.padding = new RectOffset(10, 10, 10, 10);

        string helpText = "DEBUG KEYS:\n" +
                         "R - Reset All Data\n" +
                         "C - Add 1000 Coins\n" +
                         "E - Add 50 Energy\n" +
                         "P - Print Data\n" +
                         "N - Reset Name\n" +
                         "I - Reset Inventory\n" +
                         "DELETE - Reset Everything";

        GUI.Label(new Rect(10, 10, 200, 200), helpText, style);
#endif
    }
}