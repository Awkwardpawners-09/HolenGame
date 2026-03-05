using UnityEngine;
using TMPro;
using System;

public class DailyTimer : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI timerText;

    [Header("Settings")]
    [Tooltip("PlayerPrefs key to store the last reset time")]
    public string timerKey = "DailyTimerLastReset";

    private const int HOURS = 24;
    private DateTime nextResetTime;

    private void OnEnable()
    {
        LoadOrInitTimer();
    }

    private void Update()
    {
        UpdateTimerDisplay();
    }

    private void LoadOrInitTimer()
    {
        string saved = PlayerPrefs.GetString(timerKey, "");

        if (string.IsNullOrEmpty(saved))
        {
            // First time — start the timer now
            nextResetTime = DateTime.Now.AddHours(HOURS);
            PlayerPrefs.SetString(timerKey, nextResetTime.ToString("o"));
            PlayerPrefs.Save();
        }
        else
        {
            nextResetTime = DateTime.Parse(saved);
        }
    }

    private void UpdateTimerDisplay()
    {
        TimeSpan remaining = nextResetTime - DateTime.Now;

        if (remaining.TotalSeconds <= 0)
        {
            if (timerText != null)
                timerText.text = "00:00:00";
            return;
        }

        if (timerText != null)
            timerText.text = $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }

    /// <summary>
    /// Call this to manually reset the timer (e.g. when daily quests reset)
    /// </summary>
    public void ResetTimer()
    {
        nextResetTime = DateTime.Now.AddHours(HOURS);
        PlayerPrefs.SetString(timerKey, nextResetTime.ToString("o"));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Returns true if the 24 hours have passed
    /// </summary>
    public bool IsExpired() => DateTime.Now >= nextResetTime;
}