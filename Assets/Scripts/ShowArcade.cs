using UnityEngine;

public class ShowArcade : MonoBehaviour
{
    public GameObject Arcade;

    public void ShowArcadePanel()
    {
        Arcade.SetActive(true);
    }

    public void HideArcadePanel()
    {
        Arcade.SetActive(false);
    }
    
}
