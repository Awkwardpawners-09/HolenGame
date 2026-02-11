using UnityEngine;
using TMPro;

/// <summary>
/// Displays a countdown timer showing when the next energy will regenerate.
/// Attach to a TextMeshProUGUI element.
/// Shows "Full" when energy is maxed, otherwise shows time remaining (e.g., "9:45")
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class EnergyTimerDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [Tooltip("Text to show when energy is full")]
    public string fullEnergyText = "Full";

    [Tooltip("Format for timer display. Use {0} for minutes and {1} for seconds")]
    public string timerFormat = "{0}:{1:00}";

    [Header("Optional: Hide When Full")]
    [Tooltip("Hide this text element when energy is full")]
    public bool hideWhenFull = false;

    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (PlayerDataManager.Instance == null)
        {
            textComponent.text = "--:--";
            return;
        }

        int currentEnergy = PlayerDataManager.Instance.GetEnergy();

        // Check if energy is full
        if (currentEnergy >= PlayerData.MAX_ENERGY)
        {
            if (hideWhenFull)
            {
                textComponent.gameObject.SetActive(false);
            }
            else
            {
                textComponent.gameObject.SetActive(true);
                textComponent.text = fullEnergyText;
            }
            return;
        }

        // Show timer
        textComponent.gameObject.SetActive(true);

        int secondsUntilNext = PlayerDataManager.Instance.GetSecondsUntilNextEnergy();
        int minutes = secondsUntilNext / 60;
        int seconds = secondsUntilNext % 60;

        textComponent.text = string.Format(timerFormat, minutes, seconds);
    }
}