using UnityEngine;
using UnityEngine.UI; // Needed for UI components
using TMPro;

public class PlayerData : MonoBehaviour
{
    public int coins = 500;
    public TextMeshProUGUI coinsText; // Assign in inspector

    void Start()
    {
        UpdateCoinDisplay();
    }

    public bool SpendCoins(int amount)
    {
        if (coins >= amount)
        {
            coins -= amount;
            UpdateCoinDisplay(); // Update display after spending
            return true;
        }
        return false;
    }

    // Call this method whenever coins change
    public void UpdateCoinDisplay()
    {
        if (coinsText != null)
        {
            coinsText.text = "" + coins.ToString();
        }
    }
}