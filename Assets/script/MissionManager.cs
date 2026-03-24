using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("Panels")]
    public GameObject availablePanel;
    public GameObject completedPanel;
    public GameObject mMenuPanel;
    public Transform completedContent;

    [Header("Rewards")]
    public int tutorialReward = 200;
    public int submarineReward = 1000;

    [Header("Tracker")]
    public GameObject trackerPrefab;
    public Transform trackerParent;

    private readonly Dictionary<string, TrackerItem> activeTrackers = new Dictionary<string, TrackerItem>();
    private readonly Dictionary<string, MissionData> missionDatas = new Dictionary<string, MissionData>();

    [Header("Mission Cards")]
    public GameObject tutorialJobCard;
    public GameObject submarineJobCard;

    [Header("Map Groups")]
    public GameObject tutorialMapGroup;
    public GameObject submarineMapGroup;

    public string currentActiveMissionName;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        ShowAvailableJobs();
        if (tutorialMapGroup != null) tutorialMapGroup.SetActive(false);
        if (submarineMapGroup != null) submarineMapGroup.SetActive(false);
    }

    public void ShowAvailableJobs()
    {
        if (availablePanel != null) availablePanel.SetActive(true);
        if (completedPanel != null) completedPanel.SetActive(false);
    }

    public void ShowCompletedTab()
    {
        if (availablePanel != null) availablePanel.SetActive(false);
        if (completedPanel != null) completedPanel.SetActive(true);
    }

    public void HandleMissionClick(string missionName, MissionData.MissionStatus missionStatus)
    {
        if (missionStatus == MissionData.MissionStatus.ReadyToMove) AcceptMission(missionName);
    }

    private void AcceptMission(string missionName)
    {
        if (activeTrackers.ContainsKey(missionName))
        {
            StartMapTransition(missionName);
            return;
        }

        MissionData newData = new MissionData
        {
            missionName = missionName,
            status = MissionData.MissionStatus.InProgress
        };

        string lowerName = missionName.ToLower();
        GameObject mapGroup = null;

        if (lowerName.Contains("tutorial"))
        {
            mapGroup = tutorialMapGroup;
            newData.rewardCoin = tutorialReward;
        }
        else if (lowerName.Contains("newmap") || lowerName.Contains("sub"))
        {
            mapGroup = submarineMapGroup;
            newData.rewardCoin = submarineReward;
        }

        if (mapGroup != null)
        {
            Trash[] trashes = mapGroup.GetComponentsInChildren<Trash>(true);
            Barnacle[] barnacles = mapGroup.GetComponentsInChildren<Barnacle>(true);

            int totalCollectibles = trashes.Length + barnacles.Length;
            newData.totalTrash = totalCollectibles;
            newData.remainingTrash = totalCollectibles;

            Debug.Log($"{missionName} 미션 시작: 일반 쓰레기({trashes.Length}) + 따개비({barnacles.Length}) = 총 {totalCollectibles}개");
        }

        missionDatas[missionName] = newData;

        GameObject trackerObject = Instantiate(trackerPrefab, trackerParent);
        trackerObject.SetActive(true);
        TrackerItem trackerItem = trackerObject.GetComponent<TrackerItem>();
        trackerItem.Setup(newData);
        activeTrackers[missionName] = trackerItem;

        StartMapTransition(missionName);
    }

    public void StartMapTransition(string missionName)
    {
        currentActiveMissionName = missionName;
        string lowerName = missionName.ToLower();

        if (tutorialMapGroup != null) tutorialMapGroup.SetActive(false);
        if (submarineMapGroup != null) submarineMapGroup.SetActive(false);

        if (lowerName.Contains("tutorial"))
        {
            if (tutorialMapGroup != null) tutorialMapGroup.SetActive(true);
        }
        else if (lowerName.Contains("newmap") || lowerName.Contains("sub"))
        {
            if (submarineMapGroup != null) submarineMapGroup.SetActive(true);
            if (SubmarineManager.Instance != null) SubmarineManager.Instance.AppearSubmarine();
        }

        if (mMenuPanel != null) mMenuPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnTrashPickedUp()
    {
        if (!string.IsNullOrEmpty(currentActiveMissionName))
            OnTrashPickedUp(currentActiveMissionName);
    }

    public void OnTrashPickedUp(string missionName)
    {
        if (!missionDatas.ContainsKey(missionName)) return;
        missionDatas[missionName].remainingTrash--;
        UpdateSpecificTrackerUI(missionName);
    }

    public void UpdateProgress(float progress)
    {
        if (!string.IsNullOrEmpty(currentActiveMissionName))
            UpdateProgress(currentActiveMissionName, progress);
    }

    public void UpdateProgress(string missionName, float progress)
    {
        if (!missionDatas.ContainsKey(missionName)) return;
        missionDatas[missionName].currentProgress = progress;
        UpdateSpecificTrackerUI(missionName);
    }

    private void UpdateSpecificTrackerUI(string missionName)
    {
        if (!activeTrackers.ContainsKey(missionName)) return;

        MissionData data = missionDatas[missionName];
        string displayName = GetDisplayMissionName(data.missionName);
        string infoText = $"<b>{displayName}</b>\n" +
                          $"남은 따개비 개수 : {data.remainingTrash}\n" +
                          $"닦기 진행도 : {data.currentProgress:F1}%";

        bool isAllDone = data.remainingTrash <= 0 && data.currentProgress >= 99.0f;
        activeTrackers[missionName].UpdateMissionStatus(infoText, isAllDone, data.rewardCoin);
    }

    public void ClaimReward(string missionName, int rewardAmount, GameObject trackerUI)
    {
        if (CoinManager.instance != null)
        {
            CoinManager.instance.AddCoins(rewardAmount);
            MoveToCompleted(missionName);
            if (activeTrackers.ContainsKey(missionName)) activeTrackers.Remove(missionName);
            if (trackerUI != null) Destroy(trackerUI);
            if (missionDatas.ContainsKey(missionName)) missionDatas.Remove(missionName);
            if (currentActiveMissionName == missionName) currentActiveMissionName = "";
        }
    }

    public void MoveToCompleted(string missionName)
    {
        string lowerName = missionName.ToLower();
        GameObject cardTarget = null;

        if (lowerName.Contains("tutorial")) cardTarget = tutorialJobCard;
        else if (lowerName.Contains("newmap") || lowerName.Contains("sub")) cardTarget = submarineJobCard;

        if (cardTarget != null)
        {
            cardTarget.transform.SetParent(completedContent);
            cardTarget.transform.localScale = Vector3.one;
            JobItemUI itemUI = cardTarget.GetComponent<JobItemUI>();
            if (itemUI != null) itemUI.SetMissionCompleted();
        }
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
