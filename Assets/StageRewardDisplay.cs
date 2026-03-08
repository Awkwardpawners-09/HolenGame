using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Attach to a reward screen GameObject that gets enabled by another script when a stage is cleared.
///
/// SEQUENCE:
/// 1. coinDisplayText flashes "+[clearReward]" for 3 seconds.
/// 2. Within 2 seconds, counts up from current coins to current + clearReward.
/// 3. Displays the new coin total briefly.
/// 4. IF this is the first clear: shows firstClearRewardObject, flashes "+[firstClearReward]"
///    for 3 seconds, then counts up to the final total within 2 seconds.
///    IF already claimed: skips entirely.
///
/// SETUP:
/// 1. Set "Stage ID" to a unique string per stage (e.g. "Stage1", "Stage2").
/// 2. Set "Clear Reward Value" - coins earned every clear.
/// 3. Set "First Clear Reward Value" - bonus coins for the very first clear only.
/// 4. Assign "Coin Display Text" - TMP text that shows the animated coin value.
/// 5. Assign "First Clear Reward Object" - shown only on first clear, hidden otherwise.
/// </summary>
public class StageRewardDisplay : MonoBehaviour
{
    [Header("Stage Identity")]
    [Tooltip("Unique ID for this stage (e.g. 'Stage1', 'Stage2'). Must be unique per stage.")]
    public string stageID = "Stage1";

    [Header("Reward Values")]
    [Tooltip("Coins awarded every time this stage is cleared.")]
    public int clearRewardValue = 50;

    [Tooltip("Bonus coins awarded only on the very first clear of this stage.")]
    public int firstClearRewardValue = 100;

    [Header("UI References")]
    [Tooltip("TMP text that displays the animated coin value.")]
    public TMPro.TextMeshProUGUI coinDisplayText;

    [Tooltip("GameObject shown only on first clear. Hidden on all repeat clears.")]
    public GameObject firstClearRewardObject;

    [Header("Animation Settings")]
    [Tooltip("How long the '+reward' label is displayed before counting up (seconds).")]
    public float rewardFlashDuration = 3f;

    [Tooltip("How long the count-up animation takes (seconds).")]
    public float countUpDuration = 2f;

    private void OnEnable()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[StageRewardDisplay] PlayerDataManager not found!");
            return;
        }

        // Read first clear flag BEFORE touching any coins
        bool isFirstClear = PlayerDataManager.Instance.playerData.IsFirstClear(stageID);

        // Always hide first clear object immediately - revealed later only if eligible
        if (firstClearRewardObject != null)
            firstClearRewardObject.SetActive(false);

        StartCoroutine(RunRewardSequence(isFirstClear));
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator RunRewardSequence(bool isFirstClear)
    {
        // ── STEP 1: Flash "+50" for 3 seconds ────────────────────────────────
        int coinsBeforeClear = PlayerDataManager.Instance.GetCoins();
        int coinsAfterClear = coinsBeforeClear + clearRewardValue;

        if (coinDisplayText != null)
            coinDisplayText.text = $"+{clearRewardValue}";

        yield return new WaitForSeconds(rewardFlashDuration);

        // ── STEP 2: Add coins + count up to total within 2 seconds ───────────
        PlayerDataManager.Instance.AddCoins(clearRewardValue);
        Debug.Log($"[StageRewardDisplay] Awarded {clearRewardValue} clear reward coins. Total: {coinsAfterClear}");

        yield return StartCoroutine(CountUpCoins(coinsBeforeClear, coinsAfterClear, countUpDuration));

        // ── STEP 3: Show clear reward total briefly ───────────────────────────
        if (coinDisplayText != null)
            coinDisplayText.text = coinsAfterClear.ToString();

        yield return new WaitForSeconds(1f);

        // ── STEP 4: First clear reward (skip if already claimed) ──────────────
        if (isFirstClear)
        {
            // Permanently mark in PlayerData - survives scene reloads and restarts
            PlayerDataManager.Instance.playerData.MarkFirstCleared(stageID);

            int coinsBeforeBonus = PlayerDataManager.Instance.GetCoins();
            int coinsAfterBonus = coinsBeforeBonus + firstClearRewardValue;

            // Show the first clear UI
            if (firstClearRewardObject != null)
                firstClearRewardObject.SetActive(true);

            // Flash "+100" for 3 seconds
            if (coinDisplayText != null)
                coinDisplayText.text = $"+{firstClearRewardValue}";

            yield return new WaitForSeconds(rewardFlashDuration);

            // Add coins + count up to final total within 2 seconds
            PlayerDataManager.Instance.AddCoins(firstClearRewardValue);
            Debug.Log($"[StageRewardDisplay] First clear bonus! Awarded {firstClearRewardValue} coins. Total: {coinsAfterBonus}");

            yield return StartCoroutine(CountUpCoins(coinsBeforeBonus, coinsAfterBonus, countUpDuration));

            if (coinDisplayText != null)
                coinDisplayText.text = coinsAfterBonus.ToString();
        }
        else
        {
            Debug.Log($"[StageRewardDisplay] Stage '{stageID}' already first-cleared - skipping bonus.");
        }
    }

    /// <summary>
    /// Counts the coin display from startValue up to endValue over the given duration.
    /// </summary>
    private IEnumerator CountUpCoins(int startValue, int endValue, float duration)
    {
        if (coinDisplayText == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease out - decelerates near the end
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            int displayValue = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, easedT));
            coinDisplayText.text = displayValue.ToString();

            yield return null;
        }

        // Guarantee exact final value
        coinDisplayText.text = endValue.ToString();
    }

    // ===================== TESTING =====================

    [ContextMenu("Reset First Clear Flag (Testing)")]
    public void ResetFirstClearForTesting()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("[StageRewardDisplay] PlayerDataManager not found.");
            return;
        }

        var data = PlayerDataManager.Instance.playerData;
        var ids = new System.Collections.Generic.HashSet<string>(
            data.firstClearedStages.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
        );
        ids.Remove(stageID);
        data.firstClearedStages = string.Join(",", ids);
        data.Save();

        Debug.Log($"[StageRewardDisplay] First clear flag reset for stage '{stageID}'.");
    }
}