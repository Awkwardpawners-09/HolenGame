using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class Marble
{
    public string name; 
    public Sprite marbleSprite; // optional: for showing in scene
    public int rarityWeight;  // higher = more common
}

public class MarbleGacha : MonoBehaviour
{
    public List<Marble> marblePool;
    public PlayerData playerData;

    // UI references
    public GameObject resultPanel;
    public Image marbleImage;
    public TextMeshProUGUI marbleNameText;

    public void TryBuyMarbleBag()
    {
        if (playerData.SpendCoins(100))
        {
            Marble awardedMarble = GetRandomMarble();
            ShowMarbleResult(awardedMarble);
        }
        else
        {
            Debug.Log("Not enough coins!");
        }
    }

    void ShowMarbleResult(Marble marble)
    {
    resultPanel.SetActive(true);
    marbleNameText.text = marble.name;
    marbleImage.sprite = marble.marbleSprite;
    }
    

    Marble GetRandomMarble()
    {
        int totalWeight = marblePool.Sum(m => m.rarityWeight);
        int random = Random.Range(0, totalWeight);
        int current = 0;

        foreach (Marble m in marblePool)
        {
            current += m.rarityWeight;
            if (random < current)
                return m;
        }

        return marblePool[0];
    }

    public void CloseResultPanel()
{
    resultPanel.SetActive(false);
}

}