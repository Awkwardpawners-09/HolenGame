using UnityEngine;

public class CoinEnergyShop : MonoBehaviour
{
    public void Buy100Coins()
    {
        PlayerDataManager.Instance.AddCoinsForTesting(100);
        Debug.Log("💰 Bought 100 coins!");
    }

    public void Buy500Coins()
    {
        PlayerDataManager.Instance.AddCoinsForTesting(500);
        Debug.Log("💰 Bought 500 coins!");
    }

    public void Buy1000Coins()
    {
        PlayerDataManager.Instance.AddCoinsForTesting(1000);
        Debug.Log("💰 Bought 1000 coins!");
    }

    public void Refill10Energy()
    {
        PlayerDataManager.Instance.AddEnergyForTesting(10);
        Debug.Log("⚡ Refilled 10 energy!");
    }

}