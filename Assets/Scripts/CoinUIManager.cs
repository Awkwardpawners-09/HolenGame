using UnityEngine;
using TMPro; // If you're using TextMeshPro

public class CoinUIManager : MonoBehaviour
{
    public TextMeshProUGUI coinText; // Drag your Coin Text UI here in Inspector
    private PlayerData playerData;

    private void Start()
    {
        // Load player data
        playerData = PlayerData.Load();

        // Update UI immediately
        UpdateCoinUI();
    }

    // Call this whenever coins change
public void UpdateCoinUI()
{
    playerData = PlayerData.Load(); // ✅ refresh from save file each time
    coinText.text = playerData.coins.ToString();
}

    // Optional helpers if you want direct calls
    public void AddCoins(int amount)
    {
        playerData.AddCoins(amount);
        UpdateCoinUI();
    }

    public bool SpendCoins(int amount)
    {
        bool success = playerData.SpendCoins(amount);
        UpdateCoinUI();
        return success;
    }
}
