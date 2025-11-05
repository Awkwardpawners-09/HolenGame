using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================================================
// ShopItem - Attach to each shop item button in your scrollable shop
// =============================================================================
public class ShopItem : MonoBehaviour
{
    [Header("Shop Item Data")]
    public HolenData holenData; // Assign the HolenData ScriptableObject in Inspector
    public int price = 100; // How much this Holen costs

    [Header("UI References")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text priceText;
    public Button buyButton;
    public GameObject ownedLabel; // Optional "OWNED" label/overlay

    private HolenInventoryManager inventoryManager;
    private PlayerDataManager playerDataManager;

    void Start()
    {
        // Get references to managers
        inventoryManager = HolenInventoryManager.Instance;
        playerDataManager = PlayerDataManager.Instance;

        // Setup UI
        SetupUI();

        // Add button listener
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        // Update button state
        UpdateButtonState();
    }

void OnEnable()
{
    // Subscribe to coin changes
    PlayerDataManager.OnCoinsChanged += OnCoinsChanged;
    
    // ✅ ADD THIS - Subscribe to inventory changes
    HolenInventoryManager.OnInventoryChanged += UpdateButtonState;
}

void OnDisable()
{
    // Unsubscribe to prevent errors
    PlayerDataManager.OnCoinsChanged -= OnCoinsChanged;
    
    // ✅ ADD THIS - Unsubscribe
    HolenInventoryManager.OnInventoryChanged -= UpdateButtonState;
}

    void SetupUI()
    {
        if (holenData == null)
        {
            Debug.LogError("HolenData not assigned to ShopItem!");
            return;
        }

        if (iconImage != null) iconImage.sprite = holenData.holenIcon;
        if (nameText != null) nameText.text = holenData.holenName;
        if (priceText != null) priceText.text = $"{price}";
    }

    void OnBuyClicked()
    {
        if (inventoryManager == null || playerDataManager == null)
        {
            Debug.LogError("Managers not found!");
            return;
        }

        // Check if already owned
        var existingEntry = inventoryManager.inventory.Find(e => e.holenID == holenData.holenID);
        if (existingEntry != null && existingEntry.quantity > 0)
        {
            Debug.Log($"Already own {holenData.holenName}!");
            // Optional: Show "already owned" message
            return;
        }

        // Try to spend coins
        if (playerDataManager.SpendCoins(price))
        {
            // Add Holen to inventory
            inventoryManager.AddHolen(holenData.holenID, 1);
            
            Debug.Log($"✅ Purchased {holenData.holenName} for {price} coins!");
            
            // Update button state
            UpdateButtonState();

            // Optional: Play purchase sound/animation here
        }
        else
        {
            Debug.Log("❌ Not enough coins!");
            // Optional: Show "not enough coins" message/shake animation
        }
    }

    void OnCoinsChanged(int newAmount)
    {
        UpdateButtonState();
    }

    void UpdateButtonState()
    {
        if (inventoryManager == null || playerDataManager == null || buyButton == null)
            return;

        // Check if owned
        var existingEntry = inventoryManager.inventory.Find(e => e.holenID == holenData.holenID);
        bool owned = (existingEntry != null && existingEntry.quantity > 0);

        // Check if can afford
        bool canAfford = playerDataManager.playerData.coins >= price;

        if (owned)
        {
            // Show as owned
            buyButton.interactable = false;
            if (ownedLabel != null) ownedLabel.SetActive(true);
            if (priceText != null) priceText.text = "OWNED";
        }
        else
        {
            // Show as available or too expensive
            buyButton.interactable = canAfford;
            if (ownedLabel != null) ownedLabel.SetActive(false);
            if (priceText != null) priceText.text = $"{price}";
        }
    }
}