using UnityEngine;

public class DebugReset : MonoBehaviour
{
    void Update()
    {
        // Press R to reset data
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayerData data = PlayerData.Load();
            data.ResetData();
            Debug.Log("Player data reset! Coins = " + data.coins);
        }
    }
}
