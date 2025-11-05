using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class WagerManager : MonoBehaviour
{
    // Singleton pattern
    public static WagerManager Instance { get; private set; }

    private void Awake()
    {
        // Set up singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Header("UI Elements")]
    [Tooltip("The Content GameObject in the Wager Scroll View")]
    public GameObject wagerContent;

    [Tooltip("Reference to the HolenUISlot prefab (same as in the inventory)")]
    public GameObject holenUISlotPrefab;

    // We store both the spawned UI slot and the data for each selected Holen
    private class SelectedUIEntry
    {
        public GameObject go;
        public HolenData data;
        public int quantity;

        public SelectedUIEntry(GameObject go, HolenData data, int qty)
        {
            this.go = go;
            this.data = data;
            this.quantity = qty;
        }
    }

    private readonly List<SelectedUIEntry> selectedEntries = new List<SelectedUIEntry>();
    private bool canClick = true; // small cooldown to avoid double-taps

    // ===================== Persistent "match" memory of selected holens =====================
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

    private void AddSelectedHolen(string holenID, int quantity)
    {
        var rec = selectedHolens.Find(r => r.holenID == holenID);
        if (rec == null)
        {
            selectedHolens.Add(new SelectedHolenRecord(holenID, quantity));
        }
        else
        {
            rec.quantity = quantity; // latest wins
        }
    }

    private void RemoveSelectedHolen(string holenID)
    {
        var rec = selectedHolens.Find(r => r.holenID == holenID);
        if (rec != null) selectedHolens.Remove(rec);
    }

    public List<SelectedHolenRecord> GetSelectedHolensCopy()
    {
        var copy = new List<SelectedHolenRecord>(selectedHolens.Count);
        foreach (var r in selectedHolens)
            copy.Add(new SelectedHolenRecord(r.holenID, r.quantity));
        return copy;
    }
    // =========================================================================================

    void Start()
    {
        // Countdown init
        remainingSeconds = Mathf.Max(0, Mathf.RoundToInt(startSeconds));
        UpdateCountdownText();

        UpdateStateText();
        RecomputePointsAndUI(); // initialize texts and button state
    }

    void Update()
    {
        // --- Countdown tick ---
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

        // Keep button interactivity in sync (if something else changes points)
        UpdateButtonInteractable();
    }

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
    public int minRequiredPoints = 10;

    [Tooltip("Maximum distinct Holen choices (unique picks) allowed.")]
    public int maxUniqueChoices = 5;

    [Tooltip("Map Holen rarity -> point cost. Configure in Inspector.")]
    public List<RarityPoint> rarityPoints = new List<RarityPoint>()
    {
        new RarityPoint(){rarity="Common",    points=1},
        new RarityPoint(){rarity="Rare",      points=2},
        new RarityPoint(){rarity="Epic",      points=3},
        new RarityPoint(){rarity="Legendary", points=5}
    };

    private int GetRarityPointCost(HolenData data)
    {
        if (data == null) return 0;
        var entry = rarityPoints.Find(r => r.rarity == data.rarity);
        if (entry != null) return Mathf.Max(0, entry.points);
        Debug.LogWarning($"No rarity mapping found for '{data.rarity}'. Defaulting cost to 1.");
        return 1;
    }

    private int currentPoints = 0;

    // ===================== NETWORK SYNC CALLBACK =====================
    /// <summary>
    /// Callback invoked when points change. Used by LobbyNetworkManager for network sync.
    /// </summary>
    public System.Action<int> OnPointsChanged;
    // =================================================================

    private void RecomputePointsAndUI()
    {
        int sum = 0;
        foreach (var entry in selectedEntries)
        {
            if (entry != null && entry.data != null)
            {
                sum += GetRarityPointCost(entry.data);
            }
        }
        currentPoints = sum;
        UpdatePointsText();
        UpdateButtonInteractable();

        // Notify network manager of points change
        OnPointsChanged?.Invoke(currentPoints);
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
    // =======================================================

    public void HandleWagerItemClick(HolenData holenData, int quantity)
    {
        if (!canClick) return; // cooldown

        canClick = false;
        Invoke(nameof(ResetClickCooldown), 0.5f);

        int existingIndex = selectedEntries.FindIndex(e => e.data != null && e.data.holenID == holenData.holenID);

        if (existingIndex >= 0)
        {
            // Remove selection
            var existing = selectedEntries[existingIndex];
            if (existing.go != null) Destroy(existing.go);
            selectedEntries.RemoveAt(existingIndex);
            Debug.Log($"{holenData.holenName} removed from wager view.");
            if (holenData != null) RemoveSelectedHolen(holenData.holenID);
            RecomputePointsAndUI();
        }
        else
        {
            // Check if already at threshold
            if (currentPoints >= minRequiredPoints)
            {
                Debug.LogWarning($"Already at or above {minRequiredPoints} points. Remove a Holen before adding another.");
                return;
            }

            // Unique picks cap
            if (selectedEntries.Count >= maxUniqueChoices)
            {
                Debug.LogWarning($"Maximum of {maxUniqueChoices} Holen choices reached.");
                return;
            }

            // Add new selection (spawn UI slot + track data)
            GameObject newSlot = Instantiate(holenUISlotPrefab, wagerContent.transform);
            var holenUISlot = newSlot.GetComponent<HolenSlotUI>();
            if (holenUISlot != null)
            {
                holenUISlot.SetSlot(holenData, quantity);
                selectedEntries.Add(new SelectedUIEntry(newSlot, holenData, quantity));
                Debug.Log($"{holenData.holenName} added to wager view.");

                if (holenData != null) AddSelectedHolen(holenData.holenID, quantity);
                RecomputePointsAndUI();
            }
            else
            {
                Debug.LogError("HolenSlotUI script missing on prefab.");
                Destroy(newSlot);
            }
        }
    }

    private void ResetClickCooldown()
    {
        canClick = true;
    }

    // ========================================================
    // READY / CANCEL Button + TextMeshPro fields and logic
    // ========================================================
    [Header("Wager Action Button")]
    public Button actionButton;
    public TMP_Text stateText;
    public string readyLabel = "READY";
    public string cancelLabel = "CANCEL";

    private bool isReady = false;
    public bool IsReady => isReady; // Expose ready state for LobbyNetworkManager

    private float lastPressTime = -999f;
    private float pressCooldown = 1f;
    private float readySince = -1f;

    // Make this public so LobbyNetworkManager can call it
    public void OnActionButtonPressed()
    {
        // Allow toggling READY once we have at least the threshold
        if (currentPoints < minRequiredPoints)
        {
            Debug.LogWarning($"Cannot set READY. Need at least {minRequiredPoints} points.");
            return;
        }

        if (Time.time - lastPressTime < pressCooldown)
            return; // still cooling down

        lastPressTime = Time.time;

        isReady = !isReady; // toggle
        readySince = isReady ? Time.time : -1f;
        UpdateStateText();
    }

    private void UpdateStateText()
    {
        if (stateText != null)
            stateText.text = isReady ? readyLabel : cancelLabel;
    }

    // ========================================================
    // Countdown UI
    // ========================================================
    [Header("Countdown")]
    [Tooltip("Assign a TMP Text in the Inspector to show the countdown.")]
    public TMP_Text countdownText;

    [Tooltip("Starting seconds for the countdown.")]
    public int startSeconds = 60;

    private int remainingSeconds = 0;
    private float countdownTickTimer = 0f;

    private void UpdateCountdownText()
    {
        if (countdownText != null)
        {
            countdownText.text = $"{remainingSeconds}";
        }
    }

    // ========================================================
    // Player Points Text
    // ========================================================
    [Header("Player Points UI")]
    [Tooltip("Assign a TMP Text that displays the current total selected points.")]
    public TMP_Text player1PointsText;
}