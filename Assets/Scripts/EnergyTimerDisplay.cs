using UnityEngine;
using TMPro;

/// <summary>
/// Displays current energy and a countdown timer showing when the next energy will regenerate.
/// Attach to a TextMeshProUGUI element.
/// While regenerating shows e.g. "2/5 (9:45)", when full shows e.g. "5/5 (Full)"
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class EnergyTimerDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [Tooltip("Format while energy is regenerating.\n{0} = current energy, {1} = max energy, {2} = minutes, {3} = seconds.\nExample: \"{0}/{1} ({2}:{3:00})\" shows \"2/5 (9:45)\"")]
    public string regenFormat = "{0}/{1} ({2}:{3:00})";

    [Tooltip("Format when energy is full.\n{0} = current energy, {1} = max energy.\nExample: \"{0}/{1} (Full)\" shows \"5/5 (Full)\"")]
    public string fullEnergyFormat = "{0}/{1} (Full)";

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
            textComponent.text = "--/-- (--:--)";
            return;
        }

        int currentEnergy = PlayerDataManager.Instance.GetEnergy();
        int maxEnergy = PlayerData.MAX_ENERGY;

        // Energy is full — no timer needed
        if (currentEnergy >= maxEnergy)
        {
            if (hideWhenFull)
            {
                textComponent.gameObject.SetActive(false);
            }
            else
            {
                textComponent.gameObject.SetActive(true);
                // e.g. "5/5 (Full)"
                textComponent.text = string.Format(fullEnergyFormat, currentEnergy, maxEnergy);
            }
            return;
        }

        // Still regenerating — show energy + countdown, e.g. "2/5 (9:45)"
        textComponent.gameObject.SetActive(true);

        int secondsUntilNext = PlayerDataManager.Instance.GetSecondsUntilNextEnergy();
        int minutes = secondsUntilNext / 60;
        int seconds = secondsUntilNext % 60;

        textComponent.text = string.Format(regenFormat, currentEnergy, maxEnergy, minutes, seconds);
    }
}