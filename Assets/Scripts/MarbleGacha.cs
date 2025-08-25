using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarbleGacha : MonoBehaviour
{
    public List<HolenData> marblePool; // now uses ScriptableObjects
    public PlayerData playerData;

    // UI references
    public GameObject resultPanel;
    public Image marbleImage;
    public TextMeshProUGUI marbleNameText;

    public void TryBuyMarbleBag()
    {
        if (playerData.SpendCoins(100))
        {
            HolenData awardedMarble = GetRandomMarble();
            ShowMarbleResult(awardedMarble);
        }
        else
        {
            Debug.Log("Not enough coins!");
        }
    }

    void ShowMarbleResult(HolenData marble)
    {
        resultPanel.SetActive(true);
        marbleNameText.text = marble.holenName; // ✅ use holenName
        marbleImage.sprite = marble.holenIcon;  // ✅ use holenIcon
    }

    HolenData GetRandomMarble()
    {
        // ✅ If you want weights, you need to add "public int rarityWeight;" to HolenData.
        // For now, pick random equally
        int randomIndex = Random.Range(0, marblePool.Count);
        return marblePool[randomIndex];
    }

    public void CloseResultPanel()
    {
        resultPanel.SetActive(false);
    }
}
