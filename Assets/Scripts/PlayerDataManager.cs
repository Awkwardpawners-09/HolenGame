using UnityEngine;
using System;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    public PlayerData playerData;

    public static event Action<int> OnCoinsChanged; // 🔔 Event for UI

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            playerData = PlayerData.Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool SpendCoins(int amount)
    {
        if (playerData.SpendCoins(amount))
        {
            OnCoinsChanged?.Invoke(playerData.coins); // 🔔 notify UI
            return true;
        }
        return false;
    }

    public void AddCoins(int amount)
    {
        playerData.AddCoins(amount);
        OnCoinsChanged?.Invoke(playerData.coins); // 🔔 notify UI
    }
}
