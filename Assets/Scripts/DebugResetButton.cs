using UnityEngine;

public class DebugResetButton : MonoBehaviour
{
    public void ResetCoins()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.playerData.ResetData();
            Debug.Log("Player data reset! Coins = 9999");
        }

    }
    
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
}
