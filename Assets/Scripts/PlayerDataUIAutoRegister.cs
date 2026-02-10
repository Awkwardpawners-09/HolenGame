using UnityEngine;
using TMPro;

/// <summary>
/// Attach this component to any TextMeshProUGUI element to automatically
/// register it with PlayerDataManager for updates.
/// Useful for UI elements that appear in different scenes.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class PlayerDataUIAutoRegister : MonoBehaviour
{
    [Header("UI Type")]
    [Tooltip("What type of player data should this UI element display?")]
    public PlayerDataType dataType = PlayerDataType.Coins;

    private TextMeshProUGUI textComponent;
    private bool isRegistered = false;

    public enum PlayerDataType
    {
        PlayerName,
        Coins,
        Energy
    }

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        RegisterWithManager();
    }

    private void OnEnable()
    {
        // Re-register when enabled (in case it was disabled)
        if (!isRegistered)
        {
            RegisterWithManager();
        }
    }

    private void RegisterWithManager()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning($"[PlayerDataUIAutoRegister] PlayerDataManager not found. Retrying in 0.1s...", gameObject);
            Invoke(nameof(RegisterWithManager), 0.1f);
            return;
        }

        if (textComponent == null)
        {
            Debug.LogError($"[PlayerDataUIAutoRegister] TextMeshProUGUI component not found on {gameObject.name}", gameObject);
            return;
        }

        // Register based on type
        switch (dataType)
        {
            case PlayerDataType.PlayerName:
                PlayerDataManager.Instance.RegisterPlayerNameUI(textComponent);
                break;
            case PlayerDataType.Coins:
                PlayerDataManager.Instance.RegisterCoinUI(textComponent);
                break;
            case PlayerDataType.Energy:
                PlayerDataManager.Instance.RegisterEnergyUI(textComponent);
                break;
        }

        isRegistered = true;
        Debug.Log($"[PlayerDataUIAutoRegister] Registered {gameObject.name} for {dataType} updates", gameObject);
    }

    private void OnDisable()
    {
        isRegistered = false;
    }

    private void OnDestroy()
    {
        // Unregister when destroyed
        if (PlayerDataManager.Instance != null && textComponent != null)
        {
            switch (dataType)
            {
                case PlayerDataType.PlayerName:
                    PlayerDataManager.Instance.UnregisterPlayerNameUI(textComponent);
                    break;
                case PlayerDataType.Coins:
                    PlayerDataManager.Instance.UnregisterCoinUI(textComponent);
                    break;
                case PlayerDataType.Energy:
                    PlayerDataManager.Instance.UnregisterEnergyUI(textComponent);
                    break;
            }
        }
    }
}