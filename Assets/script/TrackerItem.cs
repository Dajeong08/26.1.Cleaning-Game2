using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrackerItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI allInfoText;
    [SerializeField] private Button rewardButton;

    private MissionData myData;

    public void Setup(MissionData data)
    {
        myData = data;
        if (rewardButton != null) rewardButton.gameObject.SetActive(false);

        string displayName = GetDisplayMissionName(data.missionName);
        UpdateMissionStatus(
            $"<b>{displayName}</b>\n남은 따개비 개수 : {data.remainingTrash}\n닦기 진행도 : 0.0%",
            false,
            data.rewardCoin
        );
    }

    public void OnClickRewardButton()
    {
        if (MissionManager.Instance != null && myData != null)
        {
            MissionManager.Instance.ClaimReward(myData.missionName, myData.rewardCoin, gameObject);
        }
    }

    public void UpdateMissionStatus(string info, bool isComplete, int reward)
    {
        if (allInfoText != null) allInfoText.text = info;
        if (rewardButton != null) rewardButton.gameObject.SetActive(isComplete);
    }

    private string GetDisplayMissionName(string missionName)
    {
        if (string.IsNullOrEmpty(missionName)) return "의뢰";

        string lowerName = missionName.ToLower();
        if (lowerName.Contains("tutorial")) return "튜토리얼";
        if (lowerName.Contains("newmap") || lowerName.Contains("sub")) return "잠수함";

        return missionName;
    }
}
