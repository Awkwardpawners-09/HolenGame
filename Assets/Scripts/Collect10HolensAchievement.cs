using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Collect10HolensAchievement : MonoBehaviour
{
    [Header("Achievement Settings")]
    public int coinReward = 50;

    [Header("UI States")]
    public GameObject lockedPanel;
    public GameObject completedPanel;
    public GameObject claimedPanel;

    [Header("Progress")]
    public TextMeshProUGUI progressText; // Shows "3/10" etc

    [Header("Optional")]
    public TextMeshProUGUI rewardText;
    public Button claimButton;

    private bool isCompleted => PlayerDataManager.Instance.playerData.collect10HolensAchievementCompleted;
    private bool isClaimed => PlayerDataManager.Instance.playerData.collect10HolensAchievementClaimed;

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (rewardText != null)
            rewardText.text = $"+{coinReward}";

        int count = PlayerDataManager.Instance.playerData.totalHolensCollected;

        if (progressText != null)
            progressText.text = $"{Mathf.Min(count, 10)}/10";

        if (isClaimed)
        {
            lockedPanel?.SetActive(false);
            completedPanel?.SetActive(false);
            claimedPanel?.SetActive(true);
            if (claimButton != null) claimButton.interactable = false;
        }
        else if (isCompleted)
        {
            lockedPanel?.SetActive(false);
            completedPanel?.SetActive(true);
            claimedPanel?.SetActive(false);
            if (claimButton != null) claimButton.interactable = true;
        }
        else
        {
            lockedPanel?.SetActive(true);
            completedPanel?.SetActive(false);
            claimedPanel?.SetActive(false);
            if (claimButton != null) claimButton.interactable = false;
        }
    }

    public void ClaimReward()
    {
        Debug.Log($"[Collect10HolensAchievement] ClaimReward called. isCompleted={isCompleted}, isClaimed={isClaimed}");
        if (!isCompleted || isClaimed) return;

        PlayerDataManager.Instance.AddCoins(coinReward);
        PlayerDataManager.Instance.playerData.collect10HolensAchievementClaimed = true;
        PlayerDataManager.Instance.playerData.Save();

        RefreshUI();
        Debug.Log($"[Collect10HolensAchievement] Claimed! +{coinReward} coins.");
    }
}