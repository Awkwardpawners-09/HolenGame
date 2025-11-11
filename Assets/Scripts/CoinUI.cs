using UnityEngine;
using TMPro;

/// <summary>
/// Optimized Coin UI that automatically updates across all instances.
/// Attach this to any TextMeshProUGUI object that should display coins.
/// </summary>
public class CoinUI : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("The TextMeshProUGUI component to display coins (auto-assigned if on same GameObject)")]
    public TextMeshProUGUI coinText;

    [Header("Optional: Formatting")]
    [Tooltip("Prefix to show before coin amount (e.g., 'Coins: ')")]
    public string prefix = "";

    [Tooltip("Suffix to show after coin amount (e.g., ' coins')")]
    public string suffix = "";

    [Tooltip("Use number formatting with commas (e.g., 1,000 instead of 1000)")]
    public bool useThousandsSeparator = false;

    // Track all active instances for efficient updates
    private static System.Collections.Generic.List<CoinUI> activeInstances = new System.Collections.Generic.List<CoinUI>();

    private int lastKnownCoinValue = -1;

    private void Awake()
    {
        // Auto-assign coinText if not set
        if (coinText == null)
        {
            coinText = GetComponent<TextMeshProUGUI>();
        }

        if (coinText == null)
        {
            Debug.LogError($"[CoinUI] No TextMeshProUGUI found on {gameObject.name}! Please assign one.");
        }
    }

    private void OnEnable()
    {
        // Register this instance
        if (!activeInstances.Contains(this))
        {
            activeInstances.Add(this);
        }

        // Subscribe to coin change events
        PlayerDataManager.OnCoinsChanged += OnCoinsChanged;

        // Force immediate update
        ForceUpdate();
    }

    private void OnDisable()
    {
        // Unregister this instance
        activeInstances.Remove(this);

        // Unsubscribe from events
        PlayerDataManager.OnCoinsChanged -= OnCoinsChanged;
    }

    private void OnDestroy()
    {
        // Clean up
        activeInstances.Remove(this);
        PlayerDataManager.OnCoinsChanged -= OnCoinsChanged;
    }

    private void OnCoinsChanged(int newAmount)
    {
        UpdateDisplay(newAmount);
    }

    /// <summary>
    /// Force update the display (useful for manual refreshes)
    /// </summary>
    public void ForceUpdate()
    {
        if (PlayerDataManager.Instance != null)
        {
            int currentCoins = PlayerDataManager.Instance.playerData.coins;
            UpdateDisplay(currentCoins);
        }
        else
        {
            Debug.LogWarning("[CoinUI] PlayerDataManager not ready yet");
        }
    }

    private void UpdateDisplay(int amount)
    {
        // Skip update if value hasn't changed (optimization)
        if (amount == lastKnownCoinValue)
            return;

        lastKnownCoinValue = amount;

        if (coinText == null)
        {
            Debug.LogError($"[CoinUI] coinText is null on {gameObject.name}!");
            return;
        }

        // Format the number
        string formattedAmount;
        if (useThousandsSeparator)
        {
            formattedAmount = amount.ToString("N0"); // e.g., "1,000"
        }
        else
        {
            formattedAmount = amount.ToString();
        }

        // Apply prefix and suffix
        coinText.text = $"{prefix}{formattedAmount}{suffix}";
    }

    // ===================== STATIC UTILITY METHODS =====================

    /// <summary>
    /// Force update all active CoinUI instances (call this if coins change outside PlayerDataManager)
    /// </summary>
    public static void RefreshAllInstances()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("[CoinUI] Cannot refresh - PlayerDataManager not ready");
            return;
        }

        int currentCoins = PlayerDataManager.Instance.playerData.coins;

        foreach (var instance in activeInstances)
        {
            if (instance != null)
            {
                instance.UpdateDisplay(currentCoins);
            }
        }
    }

    /// <summary>
    /// Get the number of active CoinUI instances (for debugging)
    /// </summary>
    public static int GetActiveInstanceCount()
    {
        return activeInstances.Count;
    }
}