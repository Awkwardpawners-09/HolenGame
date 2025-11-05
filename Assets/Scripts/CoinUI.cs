using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    public TextMeshProUGUI coinText; // Drag your coin text here in Inspector

    private void Start()
    {
        Debug.Log("CoinUI: Start() called");
        
        // Force update on start
        if (PlayerDataManager.Instance != null)
        {
            Debug.Log($"CoinUI Start: Coins = {PlayerDataManager.Instance.playerData.coins}");
            UpdateCoinText(PlayerDataManager.Instance.playerData.coins);
        }
        else
        {
            Debug.LogError("CoinUI Start: PlayerDataManager is NULL!");
        }
    }

    private void OnEnable()
    {
        Debug.Log("CoinUI: OnEnable() called");
        
        // Subscribe to updates
        PlayerDataManager.OnCoinsChanged += UpdateCoinText;
        
        // Also refresh once immediately
        if (PlayerDataManager.Instance != null)
        {
            Debug.Log($"CoinUI OnEnable: Coins = {PlayerDataManager.Instance.playerData.coins}");
            UpdateCoinText(PlayerDataManager.Instance.playerData.coins);
        }
        else
        {
            Debug.LogError("CoinUI OnEnable: PlayerDataManager is NULL!");
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent errors
        PlayerDataManager.OnCoinsChanged -= UpdateCoinText;
    }

    private void UpdateCoinText(int newAmount)
    {
        Debug.Log($"CoinUI: UpdateCoinText called with {newAmount}");
        
        if (coinText != null)
        {
            coinText.text = newAmount.ToString();
            Debug.Log($"CoinUI: Text updated to {newAmount}");
        }
        else
        {
            Debug.LogError("CoinUI: coinText is NULL!");
        }
    }
}