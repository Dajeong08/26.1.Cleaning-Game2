using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TrackerItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI allInfoText;
    [SerializeField] private Button rewardButton;

    private MissionData myData;

    public void Setup(MissionData data)
    {
        myData = data;

        if (rewardButton != null)
        {
            rewardButton.onClick.RemoveAllListeners();
            rewardButton.onClick.AddListener(OnClickRewardButton);
            rewardButton.gameObject.SetActive(false);
            rewardButton.interactable = false;
        }

        string displayName = GetDisplayMissionName(data.missionName);
        UpdateMissionStatus(
            $"<b>{displayName}</b>\n남은 따개비 개수 : {data.remainingTrash}\n닦기 진행도 : 0.0%",
            false,
            data.rewardCoin
        );
    }

    public void OnClickRewardButton()
    {
        if (!IsRewardButtonReady()) return;
        if (MissionManager.Instance == null || myData == null) return;

        MissionManager.Instance.ClaimReward(myData.missionName, myData.rewardCoin, gameObject);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsRewardButtonReady()) return;
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;

        RectTransform buttonRect = rewardButton.transform as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (buttonRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(buttonRect, eventData.position, eventCamera))
        {
            OnClickRewardButton();
        }
    }

    public void UpdateMissionStatus(string info, bool isComplete, int reward)
    {
        if (allInfoText != null) allInfoText.text = info;

        if (rewardButton != null)
        {
            rewardButton.gameObject.SetActive(isComplete);
            rewardButton.interactable = isComplete;
        }
    }

    private bool IsRewardButtonReady()
    {
        return rewardButton != null &&
               rewardButton.gameObject.activeInHierarchy &&
               rewardButton.interactable;
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
