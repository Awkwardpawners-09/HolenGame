using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AllFiveHolenPanel : MonoBehaviour
{
    [Header("Panel Reference")]
    public GameObject panel;

    [Header("Slot Setup")]
    public GameObject holenSlotPrefab; // Drag your HolenSlotUI prefab here
    public Transform slotContainer;    // Drag your container with Horizontal Layout Group here

    [Header("Rarity Colors")]
    public Color commonColor = Color.gray;
    public Color uncommonColor = new Color(0.145f, 0.588f, 0.745f, 1f);
    public Color rareColor = Color.blue;
    public Color epicColor = Color.magenta;
    public Color legendaryColor = Color.yellow;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

public void ShowPanel(List<HolenData> marbles)
{
    Debug.Log("ShowPanel called");
    
    gameObject.SetActive(true); // activate the GameObject itself
    
    if (panel != null)
        panel.SetActive(true);
        
    Debug.Log($"gameObject active: {gameObject.activeSelf} | panel active: {panel?.activeSelf}");
}

    public void OnPanelClicked()
    {
        // Clear slots on close
        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);

        panel.SetActive(false);
    }

    Color GetRarityColor(string rarity)
    {
        switch (rarity.ToLower())
        {
            case "common":    return commonColor;
            case "uncommon":  return uncommonColor;
            case "rare":      return rareColor;
            case "epic":      return epicColor;
            case "legendary": return legendaryColor;
            default:          return Color.white;
        }
    }
}