using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadGacha : MonoBehaviour
{
    public void GoToShopScene()
    {
        SceneManager.LoadScene("Demo Menu"); // replace with your shop scene name
    }

    public void GoToGachaScene()
    {
        SceneManager.LoadScene("Gacha (VIN)"); // replace with your gacha scene name
    }
}
