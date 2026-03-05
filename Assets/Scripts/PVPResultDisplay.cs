using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;

/// <summary>
/// Displays the PVP results by populating grid layouts with knocked out holens.
/// Local player is always displayed on bottom, opponent on top.
/// Attach this to a GameObject in the PVPResult scene.
/// </summary>
public class PVPResultDisplay : MonoBehaviour
{
    [Header("Grid Layout References")]
    public Transform opponentCollectedGrid; // Grid for opponent's collected holens (top)
    public Transform localPlayerCollectedGrid; // Grid for local player's collected holens (bottom)

    [Header("Holen Slot Prefab")]
    public GameObject holenSlotPrefab; // Prefab to display each holen (should have Image, Text components)

    [Header("Optional: Player Names")]
    public TMP_Text opponentNameText; // Top player name (opponent)
    public TMP_Text localPlayerNameText; // Bottom player name (local)

    [Header("Exit Button")]
    public GameObject exitButton; // Button to return to menu

    [Header("Scene Settings")]
    public string menuSceneName = "Demo Menu"; // Scene to load when exiting

    private bool hasAwardedHolens = false; // Ensure holens are only awarded once
    private int localPlayerNumber = 0;
    private int opponentPlayerNumber = 0;

    void Start()
    {
        Debug.Log("[PVPResultDisplay] Start called");

        // Determine local and opponent player numbers
        DeterminePlayerNumbers();

        PopulateResults();

        // Setup exit button
        if (exitButton != null)
        {
            Debug.Log("[PVPResultDisplay] Exit button GameObject found!");
            UnityEngine.UI.Button btnComponent = exitButton.GetComponent<UnityEngine.UI.Button>();
            if (btnComponent != null)
            {
                Debug.Log("[PVPResultDisplay] Button component found! Adding listener...");
                btnComponent.onClick.AddListener(OnExitButtonPressed);
                Debug.Log($"[PVPResultDisplay] Button interactable: {btnComponent.interactable}");
            }
            else
            {
                Debug.LogError("[PVPResultDisplay] Exit button does not have a Button component!");
            }
        }
        else
        {
            Debug.LogError("[PVPResultDisplay] Exit button GameObject is not assigned in the inspector!");
        }
    }

    private void DeterminePlayerNumbers()
    {
        // Get local player number from static holder
        localPlayerNumber = PVPDataHolder.GetLocalPlayerNumber();

        // Opponent is the other player
        opponentPlayerNumber = localPlayerNumber == 1 ? 2 : 1;

        Debug.Log($"[PVPResultDisplay] Local Player: {localPlayerNumber}, Opponent: {opponentPlayerNumber}");
    }

    private void PopulateResults()
    {
        Debug.Log("[PVPResultDisplay] PopulateResults called");

        // Check if we have match data from the static holder
        if (!PVPDataHolder.HasMatchData())
        {
            Debug.LogError("[PVPResultDisplay] No match data found! Make sure PVPScore stores data before scene transition.");
            Debug.LogError($"[PVPResultDisplay] HasMatchData={PVPDataHolder.HasMatchData()}, LocalPlayerNumber={PVPDataHolder.GetLocalPlayerNumber()}");
            return;
        }

        // Get knocked out holens for local player and opponent
        List<PVPDataHolder.KnockedOutHolen> localPlayerHolens = PVPDataHolder.GetPlayerKnockedOutHolens(localPlayerNumber);
        List<PVPDataHolder.KnockedOutHolen> opponentHolens = PVPDataHolder.GetPlayerKnockedOutHolens(opponentPlayerNumber);

        Debug.Log($"[PVPResultDisplay] Local Player (P{localPlayerNumber}) collected {localPlayerHolens.Count} holens");
        Debug.Log($"[PVPResultDisplay] Opponent (P{opponentPlayerNumber}) collected {opponentHolens.Count} holens");

        // Populate Opponent's grid (top)
        PopulateGrid(opponentCollectedGrid, opponentHolens, $"Opponent (Player {opponentPlayerNumber})");

        // Populate Local Player's grid (bottom)
        PopulateGrid(localPlayerCollectedGrid, localPlayerHolens, $"You (Player {localPlayerNumber})");

        // Optional: Update player names
        if (opponentNameText != null)
        {
            opponentNameText.text = $"Opponent ({opponentHolens.Count})";
        }

        if (localPlayerNameText != null)
        {
            localPlayerNameText.text = $"You ({localPlayerHolens.Count})";
        }
    }

    private void PopulateGrid(Transform gridParent, List<PVPDataHolder.KnockedOutHolen> holens, string playerLabel)
    {
        Debug.Log($"[PVPResultDisplay] PopulateGrid called for {playerLabel} with {holens.Count} holens");

        if (gridParent == null)
        {
            Debug.LogWarning($"[PVPResultDisplay] Grid parent for {playerLabel} is null!");
            return;
        }

        if (holenSlotPrefab == null)
        {
            Debug.LogError("[PVPResultDisplay] Holen slot prefab is not assigned!");
            return;
        }

        // Clear existing children
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        // Group duplicate holens by ID and stack their quantities
        Dictionary<string, int> holenQuantities = new Dictionary<string, int>();
        Dictionary<string, string> holenNames = new Dictionary<string, string>(); // for debug logging

        foreach (var holen in holens)
        {
            if (holenQuantities.ContainsKey(holen.holenID))
            {
                holenQuantities[holen.holenID]++;
            }
            else
            {
                holenQuantities[holen.holenID] = 1;
                holenNames[holen.holenID] = holen.holenName;
            }
        }

        // Create one slot per unique holen ID, with stacked quantity
        foreach (var kvp in holenQuantities)
        {
            string holenID = kvp.Key;
            int quantity = kvp.Value;

            Debug.Log($"[PVPResultDisplay] Creating slot for {playerLabel}: {holenNames[holenID]} (ID: {holenID}) x{quantity}");

            HolenData holenData = LoadHolenDataByID(holenID);

            if (holenData != null)
            {
                GameObject slotObj = Instantiate(holenSlotPrefab, gridParent);
                Debug.Log($"[PVPResultDisplay] Instantiated slot for {holenData.holenName} x{quantity}");

                SetupHolenSlot(slotObj, holenData, quantity);
            }
            else
            {
                Debug.LogWarning($"[PVPResultDisplay] Could not load HolenData for ID: {holenID}");
            }
        }
    }

    private void SetupHolenSlot(GameObject slotObj, HolenData holenData, int quantity = 1)
    {
        // Try to use HolenSlotUI if it exists
        HolenSlotUI slotUI = slotObj.GetComponent<HolenSlotUI>();
        if (slotUI != null)
        {
            slotUI.SetSlot(holenData, quantity); // Pass stacked quantity
            return;
        }

        // Fallback: Manual setup if HolenSlotUI is not available
        // Assumes slot has Image component for icon and TMP_Text for name
        Image iconImage = slotObj.GetComponentInChildren<Image>();
        TMP_Text nameText = slotObj.GetComponentInChildren<TMP_Text>();

        if (iconImage != null && holenData.holenIcon != null)
        {
            iconImage.sprite = holenData.holenIcon;
        }

        if (nameText != null)
        {
            nameText.text = quantity > 1 ? $"{holenData.holenName} x{quantity}" : holenData.holenName;
        }
    }

    /// <summary>
    /// Helper method to load HolenData by ID.
    /// Uses HolenInventoryManager's database.
    /// </summary>
    private HolenData LoadHolenDataByID(string holenID)
    {
        // Use HolenInventoryManager's GetHolenData method
        if (HolenInventoryManager.Instance != null)
        {
            HolenData data = HolenInventoryManager.Instance.GetHolenData(holenID);
            if (data != null)
            {
                return data;
            }
            else
            {
                Debug.LogWarning($"[PVPResultDisplay] Could not find HolenData with ID: {holenID} in HolenInventoryManager database");
            }
        }
        else
        {
            Debug.LogError("[PVPResultDisplay] HolenInventoryManager.Instance is null!");
        }

        return null;
    }

    /// <summary>
    /// Called when exit button is pressed. Awards holens, disconnects from Photon, and returns to menu.
    /// </summary>
    public void OnExitButtonPressed()
    {
        Debug.Log("[PVPResultDisplay] Exit button pressed. Awarding holens to inventory...");

        // Award holens to player's inventory before leaving
        AwardHolensToInventory();

        // Clean up PVPScore data
        OnExitResults();

        // Disconnect from Photon room and return to menu
        StartCoroutine(DisconnectAndReturnToMenu());
    }

    /// <summary>
    /// Awards the knocked out holens to the LOCAL player's inventory.
    /// Only awards holens that the LOCAL player knocked out.
    /// </summary>
    private void AwardHolensToInventory()
    {
        if (hasAwardedHolens)
        {
            Debug.Log("[PVPResultDisplay] Holens already awarded. Skipping.");
            return;
        }

        if (!PVPDataHolder.HasMatchData())
        {
            Debug.LogWarning("[PVPResultDisplay] No match data found! Cannot award holens.");
            return;
        }

        if (HolenInventoryManager.Instance == null)
        {
            Debug.LogWarning("[PVPResultDisplay] HolenInventoryManager not found! Cannot award holens.");
            return;
        }

        if (localPlayerNumber == 0)
        {
            Debug.LogWarning("[PVPResultDisplay] Could not determine local player number!");
            return;
        }

        // Get holens knocked out by the local player
        List<PVPDataHolder.KnockedOutHolen> localPlayerHolens = PVPDataHolder.GetPlayerKnockedOutHolens(localPlayerNumber);

        Debug.Log($"[PVPResultDisplay] Awarding {localPlayerHolens.Count} holens to Player {localPlayerNumber}'s inventory");

        // Add each holen to inventory
        foreach (var holen in localPlayerHolens)
        {
            HolenInventoryManager.Instance.AddHolen(holen.holenID, 1);
            Debug.Log($"[PVPResultDisplay] Added {holen.holenName} to inventory");
        }

        // Save inventory
        HolenInventoryManager.Instance.SaveInventory();

        hasAwardedHolens = true;

        Debug.Log($"[PVPResultDisplay] Successfully awarded {localPlayerHolens.Count} holens to inventory!");
    }

    /// <summary>
    /// Disconnects from Photon and returns to main menu.
    /// </summary>
    private IEnumerator DisconnectAndReturnToMenu()
    {
        float timeout = 5f; // Maximum time to wait for disconnect
        float elapsed = 0f;

        // Leave the current room
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("[PVPResultDisplay] Leaving Photon room...");
            PhotonNetwork.LeaveRoom();

            // Wait until we've left the room (with timeout)
            while (PhotonNetwork.InRoom && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[PVPResultDisplay] Timeout waiting to leave room. Forcing disconnect...");
            }
        }

        // Reset timeout
        elapsed = 0f;

        // Disconnect from Photon
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[PVPResultDisplay] Disconnecting from Photon...");
            PhotonNetwork.Disconnect();

            // Wait until disconnected (with timeout)
            while (PhotonNetwork.IsConnected && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("[PVPResultDisplay] Timeout waiting to disconnect. Proceeding to load scene anyway...");
            }
        }

        Debug.Log("[PVPResultDisplay] Disconnected. Loading menu scene...");

        // Load menu scene
        SceneManager.LoadScene(menuSceneName);
    }

    /// <summary>
    /// Call this when leaving the result scene to clean up.
    /// </summary>
    public void OnExitResults()
    {
        // Clear the static data holder
        PVPDataHolder.ClearData();

        // Also clear PVPScore if it still exists
        if (PVPScore.Instance != null)
        {
            PVPScore.Instance.ClearData();

            // Destroy the PVPScore singleton instance
            Destroy(PVPScore.Instance.gameObject);
        }

        // Clean up WagerDataManager
        WagerDataManager.DestroyInstance();

        Debug.Log("[PVPResultDisplay] All game data cleaned up");
    }

    void OnDestroy()
    {
        // Remove button listener
        if (exitButton != null)
        {
            UnityEngine.UI.Button btnComponent = exitButton.GetComponent<UnityEngine.UI.Button>();
            if (btnComponent != null)
            {
                btnComponent.onClick.RemoveListener(OnExitButtonPressed);
            }
        }
    }
}