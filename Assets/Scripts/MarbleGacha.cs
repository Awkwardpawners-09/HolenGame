using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarbleGacha : MonoBehaviour
{
    public List<HolenData> marblePool; // ScriptableObjects
    private PlayerData playerData => PlayerDataManager.Instance.playerData;

    [Header("Inventory Reference")]
    public HolenInventoryManager inventoryManager; // 🔗 Drag your InventoryManager here

    [Header("UI References")]
    public GameObject resultPanel;
    public Image marbleImage;
    public TextMeshProUGUI marbleNameText;
    public CoinUIManager coinUI; // 🔗 Drag your CoinUIManager here

    public void TryBuyMarbleBag()
    {
        // Use coinUI if available so UI refreshes instantly
        bool success = (coinUI != null) ? coinUI.SpendCoins(100) : playerData.SpendCoins(100);

        if (success)
        {
            HolenData awardedMarble = GetRandomMarble();

            // ✅ Add to Inventory & Save
            inventoryManager.AddHolen(awardedMarble.holenID, 1);

            // ✅ Show result panel
            ShowMarbleResult(awardedMarble);

            Debug.Log($"🎉 Gacha awarded: {awardedMarble.holenName}");
        }
        else
        {
            Debug.Log("Not enough coins!");
        }
    }

    void ShowMarbleResult(HolenData marble)
    {
        resultPanel.SetActive(true);
        marbleNameText.text = marble.holenName;
        marbleImage.sprite = marble.holenIcon;
    }

    HolenData GetRandomMarble()
    {
        int randomIndex = Random.Range(0, marblePool.Count);
        return marblePool[randomIndex];
    }

    public void CloseResultPanel()
    {
        resultPanel.SetActive(false);
    }
}
