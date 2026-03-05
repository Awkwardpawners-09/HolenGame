using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginQuest : MonoBehaviour
{
    [Header("Quest Settings")]
    public int coinReward = 10;

    [Header("UI States")]
    public GameObject completedPanel; // Light - ready to claim
    public GameObject claimedPanel;   // Darkened with "Claimed"

    [Header("Optional")]
    public TextMeshProUGUI rewardText;
    public Button claimButton;

    private bool isClaimed => PlayerDataManager.Instance.playerData.loginQuestClaimed;

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
            completedPanel?.SetActive(false);
            claimedPanel?.SetActive(true);
            if (claimButton != null) claimButton.interactable = false;
        }
        else
        {
            // Always claimable on open
            completedPanel?.SetActive(true);
            claimedPanel?.SetActive(false);
            if (claimButton != null) claimButton.interactable = true;
        }
    }

public void ClaimReward()
{
    Debug.Log($"[LoginQuest] ClaimReward called. isClaimed={isClaimed}");
    if (isClaimed) return;

    PlayerDataManager.Instance.AddCoins(coinReward);
    PlayerDataManager.Instance.playerData.loginQuestClaimed = true;
    PlayerDataManager.Instance.playerData.Save();

    RefreshUI();
    Debug.Log($"[LoginQuest] Claimed! +{coinReward} coins.");

    foreach (var q in FindObjectsOfType<AllQuestsQuest>())
        q.RefreshUI();
}
}