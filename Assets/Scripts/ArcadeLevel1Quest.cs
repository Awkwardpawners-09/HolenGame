using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArcadeLevel1Quest : MonoBehaviour
{
    [Header("Quest Settings")]
    public int coinReward = 10;

    [Header("UI States")]
    public GameObject lockedPanel;
    public GameObject completedPanel;
    public GameObject claimedPanel;

    [Header("Optional")]
    public TextMeshProUGUI rewardText;
    public Button claimButton;

    private bool isCompleted => PlayerDataManager.Instance.playerData.arcadeLevel1QuestCompleted;
    private bool isClaimed => PlayerDataManager.Instance.playerData.arcadeLevel1QuestClaimed;

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
        Debug.Log($"[ArcadeLevel1Quest] ClaimReward called. isCompleted={isCompleted}, isClaimed={isClaimed}");
        if (!isCompleted || isClaimed) return;

        PlayerDataManager.Instance.AddCoins(coinReward);
        PlayerDataManager.Instance.playerData.arcadeLevel1QuestClaimed = true;
        PlayerDataManager.Instance.playerData.Save();

        RefreshUI();
        Debug.Log($"[ArcadeLevel1Quest] Claimed! +{coinReward} coins.");
    }
}