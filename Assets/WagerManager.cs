using UnityEngine;
using System.Collections.Generic;

public class WagerManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject wagerContent; // The Content GameObject in the P1Wager Scroll View
    public GameObject holenUISlotPrefab; // Reference to the HolenUISlot prefab (same as in the inventory)
    public static WagerManager Instance { get; private set; } // Singleton instance

    private List<GameObject> selectedItems = new List<GameObject>(); // To keep track of selected items

    private bool canClick = true; // To add buffer time before it can be reselected

    private void Awake()
    {
        // Singleton pattern to ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            Instance = this; // Set the singleton instance
            DontDestroyOnLoad(gameObject); // Persist this object across scenes
        }
    }

    void Start()
    {
        // Optional: Initialize with pre-selected items or do additional setup here
    }

    public void HandleWagerItemClick(HolenData holenData, int quantity)
    {
        if (!canClick)
            return; // Prevent immediate re-clicks

        // Set cooldown before allowing the item to be clicked again
        canClick = false;
        Invoke("ResetClickCooldown", 0.5f); // 0.5 seconds cooldown

        // Check if the item is already in the P1Wager content
        GameObject existingItem = selectedItems.Find(item => item.GetComponent<HolenSlotUI>().IsSameItem(holenData));

        if (existingItem != null)
        {
            // Item is already selected, so remove it
            selectedItems.Remove(existingItem);
            Destroy(existingItem);
            Debug.Log($"{holenData.holenName} removed from wager view.");
        }
        else
        {
            // If we have less than 3 selected items, add it to the P1Wager content
            if (selectedItems.Count < 3)
            {
                GameObject newSlot = Instantiate(holenUISlotPrefab, wagerContent.transform);
                var holenUISlot = newSlot.GetComponent<HolenSlotUI>();
                if (holenUISlot != null)
                {
                    holenUISlot.SetSlot(holenData, quantity); // Set the slot data
                    selectedItems.Add(newSlot);
                    Debug.Log($"{holenData.holenName} added to wager view.");
                }
                else
                {
                    Debug.LogError("HolenSlotUI script missing on prefab.");
                }
            }
            else
            {
                Debug.LogWarning("Maximum of 3 items can be selected.");
            }
        }
    }

    private void ResetClickCooldown()
    {
        canClick = true; // Allow the item to be clicked again
    }
}
