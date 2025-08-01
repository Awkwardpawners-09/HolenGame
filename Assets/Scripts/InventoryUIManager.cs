using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class InventoryUIManager : MonoBehaviour
{
    public Transform slotParent; // Your Grid Layout Group
    public GameObject slotPrefab; // HolenSlotUI prefab
    private HolenInventoryManager inventoryManager;

    private void Start()
    {
        inventoryManager = FindObjectOfType<HolenInventoryManager>();

        // 🔧 Ensure the manager knows who the UI is
        if (inventoryManager != null)
        {
            inventoryManager.inventoryUI = this;
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        Debug.Log("Refreshing UI...");

        // Clear existing slots
        foreach (Transform child in slotParent)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"Inventory count: {inventoryManager.inventory.Count}");

        foreach (var entry in inventoryManager.inventory)
        {
            var holenData = inventoryManager.GetHolenData(entry.holenID);
            if (holenData == null)
            {
                Debug.LogWarning($"Missing HolenData for ID: {entry.holenID}");
                continue;
            }

            GameObject slotGO = Instantiate(slotPrefab, slotParent);
            Debug.Log($"Instantiated slot for {holenData.name}");

            var slotUI = slotGO.GetComponent<HolenSlotUI>();
            if (slotUI == null)
            {
                Debug.LogError("Missing HolenSlotUI component on slotPrefab!");
            }
            else
            {
                slotUI.SetSlot(holenData, entry.quantity);
            }
        }
    }

    [System.Serializable]
    public class SaveData
    {
        public List<HolenInventoryEntry> inventory;
    }

    public List<HolenData> allHolens = new List<HolenData>();
    //public List<HolenInventoryEntry> inventory = new List<HolenInventoryEntry>();

    private string SavePath => Path.Combine(Application.persistentDataPath, "holen_inventory.json");

    public void SaveInventory()
    {
        SaveData data = new SaveData();
        data.inventory = inventoryManager.inventory; // ✅ Use inventoryManager's list

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"Inventory saved to: {SavePath}");
    }

    public void LoadInventory()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            inventoryManager.inventory = data.inventory ?? new List<HolenInventoryEntry>(); // ✅ Load into manager
            Debug.Log("Inventory loaded.");
        }
        else
        {
            Debug.Log("No save file found, starting fresh.");
            inventoryManager.inventory = new List<HolenInventoryEntry>(); // ✅ Fresh start
        }
    }

    // Optional for testing
    [ContextMenu("Save Inventory")]
    public void TestSave() => SaveInventory();

    [ContextMenu("Load Inventory")]
    public void TestLoad() => LoadInventory();
}
