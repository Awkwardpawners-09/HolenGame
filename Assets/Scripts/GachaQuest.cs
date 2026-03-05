using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaQuest : MonoBehaviour
{
    [Header("Quest Settings")]
    public int coinReward = 10;

    [Header("UI States")]
    public GameObject lockedPanel;   // Darkened - not yet completed
    public GameObject completedPanel; // Light - completed, ready to claim
    public GameObject claimedPanel;  // Darkened with "Claimed" - already claimed

    [Header("Optional")]
    public TextMeshProUGUI rewardText; // Shows coin reward amount
    public Button claimButton;

    private bool isCompleted => PlayerDataManager.Instance.playerData.gachaQuestCompleted;
    private bool isClaimed => PlayerDataManager.Instance.playerData.gachaQuestClaimed;

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
    if (!isCompleted || isClaimed) return;

    PlayerDataManager.Instance.AddCoins(coinReward);
    PlayerDataManager.Instance.playerData.gachaQuestClaimed = true;
    PlayerDataManager.Instance.playerData.Save(); // ✅ fix

    RefreshUI();
    Debug.Log($"[GachaQuest] Claimed! +{coinReward} coins.");
}
}