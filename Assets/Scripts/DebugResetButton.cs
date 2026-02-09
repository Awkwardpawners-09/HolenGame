using UnityEngine;

/// <summary>
/// Debug utility for resetting player data via UI buttons.
/// Attach to a GameObject and hook up the methods to UI buttons.
/// </summary>
public class DebugResetButton : MonoBehaviour
{
    /// <summary>
    /// Reset all player data (coins, energy, player name)
    /// </summary>
    public void ResetAllPlayerData()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.ResetAllDataForTesting();
            Debug.Log("✅ Player data reset! Coins = 0, Energy = 0, Name = empty");
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    /// <summary>
    /// Reset only coins to 0
    /// </summary>
    public void ResetCoins()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.ResetCoinsForTesting();
            Debug.Log("✅ Coins reset to 0");
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    /// <summary>
    /// Reset only energy to 0
    /// </summary>
    public void ResetEnergy()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.ResetEnergyForTesting();
            Debug.Log("✅ Energy reset to 0");
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    /// <summary>
    /// Give starting coins (default 9999)
    /// </summary>
    public void GiveStartingCoins()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.GiveStartingCoinsForTesting(9999);
            Debug.Log("✅ Coins set to 9999");
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    /// <summary>
    /// Give starting energy (default 100)
    /// </summary>
    public void GiveStartingEnergy()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.GiveStartingEnergyForTesting(100);
            Debug.Log("✅ Energy set to 100");
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    /// <summary>
    /// Add coins (customizable amount in Inspector)
    /// </summary>
    public void AddCoins(int amount)
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddCoinsForTesting(amount);
            Debug.Log($"✅ Added {amount} coins (Total: {PlayerDataManager.Instance.GetCoins()})");
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    /// <summary>
    /// Add energy (customizable amount in Inspector)
    /// </summary>
    public void AddEnergy(int amount)
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddEnergyForTesting(amount);
            Debug.Log($"✅ Added {amount} energy (Total: {PlayerDataManager.Instance.GetEnergy()})");
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    /// <summary>
    /// Print all player data to console
    /// </summary>
    public void PrintPlayerData()
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

    /// <summary>
    /// Reset inventory (if HolenInventoryManager exists)
    /// </summary>
    public void ResetInventory()
    {
        if (HolenInventoryManager.Instance != null)
        {
            HolenInventoryManager.Instance.ResetInventory();
            Debug.Log("✅ Inventory reset! All Holens removed");
        }
        else
        {
            Debug.LogError("❌ HolenInventoryManager not found!");
        }
    }

    /// <summary>
    /// Reset player name (forces PlayerNameSetup to show again)
    /// </summary>
    public void ResetPlayerName()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetPlayerName("");
            Debug.Log("✅ Player name reset - PlayerNameSetup will show on restart");
        }
        else
        {
            Debug.LogError("❌ PlayerDataManager not found!");
        }
    }

    /// <summary>
    /// Complete reset - everything!
    /// </summary>
    public void ResetEverything()
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

        Debug.Log("🔥 COMPLETE RESET! All player data and inventory cleared!");
    }
}