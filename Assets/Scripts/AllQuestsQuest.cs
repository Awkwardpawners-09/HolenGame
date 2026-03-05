using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AllQuestsQuest : MonoBehaviour
{
    [Header("Quest Settings")]
    public int coinReward = 100;

    [Header("UI States")]
    public GameObject lockedPanel;
    public GameObject completedPanel;
    public GameObject claimedPanel;

    [Header("Progress")]
    public TextMeshProUGUI progressText;

    [Header("Optional")]
    public TextMeshProUGUI rewardText;
    public Button claimButton;

    private bool isClaimed => PlayerDataManager.Instance.playerData.allQuestsClaimed;

    private int GetCompletedCount()
    {
        var data = PlayerDataManager.Instance.playerData;
        int count = 0;
        if (data.loginQuestClaimed) count++;
        if (data.gacha1xQuestClaimed) count++;
        if (data.gachaQuestClaimed) count++;
        if (data.arcadeLevel1QuestClaimed) count++;
        return count;
    }

    private bool IsAllCompleted() => GetCompletedCount() >= 4;

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (rewardText != null)
            rewardText.text = $"+{coinReward}";

        int count = GetCompletedCount();

        if (progressText != null)
            progressText.text = $"{count}/4";

        if (isClaimed)
        {
            lockedPanel?.SetActive(false);
            completedPanel?.SetActive(false);
            claimedPanel?.SetActive(true);
            if (claimButton != null) claimButton.interactable = false;
        }
        else if (IsAllCompleted())
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
        Debug.Log($"[AllQuestsQuest] ClaimReward called. IsAllCompleted={IsAllCompleted()}, isClaimed={isClaimed}");
        if (!IsAllCompleted() || isClaimed) return;

        PlayerDataManager.Instance.AddCoins(coinReward);
        PlayerDataManager.Instance.playerData.allQuestsClaimed = true;
        PlayerDataManager.Instance.playerData.Save();

        RefreshUI();
        Debug.Log($"[AllQuestsQuest] Claimed! +{coinReward} coins.");
    }
}
