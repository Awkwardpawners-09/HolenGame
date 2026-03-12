using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MythicHolenAchievement : MonoBehaviour
{
    [Header("Achievement Settings")]
    public int coinReward = 50;

    [Header("UI States")]
    public GameObject lockedPanel;
    public GameObject completedPanel;
    public GameObject claimedPanel;

    [Header("Optional")]
    public TextMeshProUGUI rewardText;
    public Button claimButton;

    private bool isCompleted => PlayerDataManager.Instance.playerData.mythicHolenAchievementCompleted;
    private bool isClaimed => PlayerDataManager.Instance.playerData.mythicHolenAchievementClaimed;

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (rewardText != null)
            rewardText.text = $"+{coinReward}";

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
        Debug.Log($"[MythicHolenAchievement] ClaimReward called. isCompleted={isCompleted}, isClaimed={isClaimed}");
        if (!isCompleted || isClaimed) return;

        PlayerDataManager.Instance.AddCoins(coinReward);
        PlayerDataManager.Instance.playerData.mythicHolenAchievementClaimed = true;
        PlayerDataManager.Instance.playerData.Save();

        RefreshUI();
        Debug.Log($"[MythicHolenAchievement] Claimed! +{coinReward} coins.");
    }
}