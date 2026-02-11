using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages wager data, point calculations, and ready state.
/// UI rendering is now handled by LobbyNetworkManager.
/// </summary>
public class WagerManager : MonoBehaviour
{
    // ===================== POINT RULES =====================
    [System.Serializable]
    public class RarityPoint
    {
        [Tooltip("Must match HolenData.rarity exactly (e.g., Common, Rare, Epic, Legendary)")]
        public string rarity;

        [Tooltip("Point cost for selecting one Holen of this rarity")]
        public int points;
    }

    [Header("Points Rules")]
    [Tooltip("Target threshold for this game mode (e.g., 10).")]
    public int minRequiredPoints = 5;

    [Tooltip("Maximum distinct Holen choices (unique picks) allowed.")]
    public int maxUniqueChoices = 5;

    [Tooltip("Map Holen rarity -> point cost. Configure in Inspector.")]
    public List<RarityPoint> rarityPoints = new List<RarityPoint>()
    {
        new RarityPoint(){rarity="Common",    points=1},
        new RarityPoint(){rarity="Rare",      points=3},
        new RarityPoint(){rarity="Epic",      points=4},
        new RarityPoint(){rarity="Uncommon", points=2},
        new RarityPoint(){rarity="Mythic", points=5},
        new RarityPoint(){rarity="Legendary", points=6}

    };

    // ===================== DATA STORAGE =====================
    [System.Serializable]
    public class SelectedHolenRecord
    {
        public string holenID;
        public int quantity;

        public SelectedHolenRecord(string id, int qty)
        {
            holenID = id;
            quantity = qty;
        }
    }

    [Header("Selected Holens (For Inspector)")]
    [SerializeField] private List<SelectedHolenRecord> selectedHolens = new List<SelectedHolenRecord>();
    public IReadOnlyList<SelectedHolenRecord> SelectedHolens => selectedHolens;

    private int currentPoints = 0;
    public int CurrentPoints => currentPoints;

    // ===================== UI REFERENCES (for display only) =====================
    [Header("UI References (Read-Only Display)")]
    public GameObject wagerContent;
    public GameObject holenUISlotPrefab;
    public Button actionButton;
    public TMP_Text stateText;
    public TMP_Text countdownText;
    public TMP_Text player1PointsText;

    [Header("Button Labels")]
    public string readyLabel = "READY";
    public string cancelLabel = "Preparing....";

    [Header("Countdown")]
    public int startSeconds = 150;
    private int remainingSeconds = 0;
    private float countdownTickTimer = 0f;

    // ===================== STATE =====================
    private bool isReady = false;
    public bool IsReady => isReady;

    // ===================== NETWORK CALLBACKS =====================
    /// <summary>
    /// Invoked when points change. Used by LobbyNetworkManager for network sync.
    /// </summary>
    public System.Action<int> OnPointsChanged;

    /// <summary>
    /// Invoked when wager selection changes. Used by LobbyNetworkManager for network sync.
    /// </summary>
    public System.Action OnWagerSelectionChanged;

    void Start()
    {
        // Countdown init
        remainingSeconds = Mathf.Max(0, Mathf.RoundToInt(startSeconds));
        UpdateCountdownText();
        UpdateStateText();
        UpdatePointsText();
        UpdateButtonInteractable();
    }

    void Update()
    {
        // Countdown tick
        if (remainingSeconds > 0)
        {
            countdownTickTimer += Time.deltaTime;
            if (countdownTickTimer >= 1f)
            {
                countdownTickTimer = 0f;
                remainingSeconds = Mathf.Max(remainingSeconds - 1, 0);
                UpdateCountdownText();
            }
        }

        // Keep button interactivity in sync
        UpdateButtonInteractable();
    }

    // ===================== PUBLIC API FOR LOBBY NETWORK MANAGER =====================

    /// <summary>
    /// Adds or updates a holen in the wager. Returns true if successful.
    /// </summary>
    public bool AddOrUpdateHolen(string holenID, int quantity, HolenData data)
    {
        // Check if already at threshold
        if (currentPoints >= minRequiredPoints)
        {
            Debug.LogWarning($"Already at or above {minRequiredPoints} points. Remove a Holen before adding another.");
            return false;
        }

        // Check if this is a new selection or update
        var existing = selectedHolens.Find(r => r.holenID == holenID);

        if (existing == null)
        {
            // New selection - check unique limit
            if (selectedHolens.Count >= maxUniqueChoices)
            {
                Debug.LogWarning($"Maximum of {maxUniqueChoices} Holen choices reached.");
                return false;
            }

            selectedHolens.Add(new SelectedHolenRecord(holenID, quantity));
            Debug.Log($"Added {holenID} to wager (quantity: {quantity})");
        }
        else
        {
            // Update existing
            existing.quantity = quantity;
            Debug.Log($"Updated {holenID} quantity to {quantity}");
        }

        RecomputePoints(data);
        OnWagerSelectionChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes a holen from the wager.
    /// </summary>
    public bool RemoveHolen(string holenID)
    {
        var rec = selectedHolens.Find(r => r.holenID == holenID);
        if (rec != null)
        {
            selectedHolens.Remove(rec);
            Debug.Log($"Removed {holenID} from wager");

            // Recompute points (pass null since we don't need specific data)
            RecomputePoints(null);
            OnWagerSelectionChanged?.Invoke();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a holen is currently in the wager.
    /// </summary>
    public bool IsHolenSelected(string holenID)
    {
        return selectedHolens.Exists(r => r.holenID == holenID);
    }

    /// <summary>
    /// Gets a copy of all selected holens.
    /// </summary>
    public List<SelectedHolenRecord> GetSelectedHolensCopy()
    {
        var copy = new List<SelectedHolenRecord>(selectedHolens.Count);
        foreach (var r in selectedHolens)
            copy.Add(new SelectedHolenRecord(r.holenID, r.quantity));
        return copy;
    }

    /// <summary>
    /// Called by LobbyNetworkManager when the action button is pressed.
    /// </summary>
    public void OnActionButtonPressed()
    {
        if (currentPoints < minRequiredPoints)
        {
            Debug.LogWarning($"Cannot set READY. Need at least {minRequiredPoints} points.");
            return;
        }

        isReady = !isReady;
        UpdateStateText();
        Debug.Log($"Ready state changed to: {isReady}");
    }

    /// <summary>
    /// Sets the ready state (used for network sync from opponent).
    /// </summary>
    public void SetReadyState(bool ready)
    {
        isReady = ready;
        UpdateStateText();
    }

    // ===================== INTERNAL METHODS =====================

    private void RecomputePoints(HolenData triggerData)
    {
        // Get inventory manager to look up data
        var inv = FindObjectOfType<HolenInventoryManager>();
        if (inv == null)
        {
            Debug.LogError("HolenInventoryManager not found!");
            return;
        }

        int sum = 0;
        foreach (var record in selectedHolens)
        {
            HolenData data = inv.GetHolenData(record.holenID);
            if (data != null)
            {
                sum += GetRarityPointCost(data);
            }
        }

        int oldPoints = currentPoints;
        currentPoints = sum;

        UpdatePointsText();
        UpdateButtonInteractable();

        // Only trigger callback if points actually changed
        if (oldPoints != currentPoints)
        {
            OnPointsChanged?.Invoke(currentPoints);
        }
    }

    private int GetRarityPointCost(HolenData data)
    {
        if (data == null) return 0;
        var entry = rarityPoints.Find(r => r.rarity == data.rarity);
        if (entry != null) return Mathf.Max(0, entry.points);
        Debug.LogWarning($"No rarity mapping found for '{data.rarity}'. Defaulting cost to 1.");
        return 1;
    }

    private void UpdatePointsText()
    {
        if (player1PointsText != null)
        {
            player1PointsText.text = $"{currentPoints}";
        }
    }

    private void UpdateButtonInteractable()
    {
        if (actionButton != null)
        {
            bool meetsThreshold = (currentPoints >= minRequiredPoints);
            actionButton.interactable = meetsThreshold;
        }
    }

    private void UpdateStateText()
    {
        if (stateText != null)
        {
            stateText.text = isReady ? readyLabel : cancelLabel;
        }
    }

    private void UpdateCountdownText()
    {
        if (countdownText != null)
        {
            countdownText.text = $"{remainingSeconds}";
        }
    }

}