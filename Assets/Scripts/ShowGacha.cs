using UnityEngine;

public class ShowGacha : MonoBehaviour
{
    public GameObject GACHA;

    public void ShowGachaPanel()
    {
        GACHA.SetActive(true);
    }

    public void HideGachaPanel()
    {
        GACHA.SetActive(false);
    }
    
}
