using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    public TextMeshProUGUI coinText; // Drag your coin text here in Inspector

    private void OnEnable()
    {
        // Subscribe to updates
        PlayerDataManager.OnCoinsChanged += UpdateCoinText;
        
        // Also refresh once immediately
        if (PlayerDataManager.Instance != null)
            UpdateCoinText(PlayerDataManager.Instance.playerData.coins);
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent errors
        PlayerDataManager.OnCoinsChanged -= UpdateCoinText;
    }

    private void UpdateCoinText(int newAmount)
    {
        coinText.text = newAmount.ToString();
    }
}
