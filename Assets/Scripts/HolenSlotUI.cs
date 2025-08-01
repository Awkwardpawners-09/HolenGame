using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HolenSlotUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text quantityText;

    // Fill data from HolenData + quantity
    public void SetSlot(HolenData data, int quantity)
    {
        iconImage.sprite = data.holenIcon;
        nameText.text = data.holenName;
        quantityText.text = "x" + quantity.ToString();
    }

    public void Setup(HolenData data, int quantity)
    {
        nameText.text = data.holenName;
        quantityText.text = "x" + quantity;
    }
}