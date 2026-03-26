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
    public bool HasAnyAcceptedMission => activeTrackers.Count > 0;

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
        // 새로운 미션 데이터를 생성
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

        // 미션 이름에 따라 보상을 다르게 설정
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
        // 현재 활성 미션 이름을 저장
        currentActiveMissionName = missionName;
        string lowerName = missionName.ToLower();

        // 기존 맵은 모두 비활성화
        if (tutorialMapGroup != null) tutorialMapGroup.SetActive(false);
        if (submarineMapGroup != null) submarineMapGroup.SetActive(false);

        // 미션 이름에 따라 맞는 맵만 활성화
        if (lowerName.Contains("tutorial"))
        {
            if (tutorialMapGroup != null) tutorialMapGroup.SetActive(true);
        }
        else if (lowerName.Contains("newmap") || lowerName.Contains("sub"))
        {
            if (submarineMapGroup != null) submarineMapGroup.SetActive(true);
            if (SubmarineManager.Instance != null) SubmarineManager.Instance.AppearSubmarine();
        }

        // 의뢰창은 닫고 게임 플레이 상태로 전환
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
        missionDatas[missionName].remainingTrash = Mathf.Max(0, missionDatas[missionName].remainingTrash - 1);
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

        // 미션 이름, 남은 개수, 진행도를 문자열로 구성
        MissionData data = missionDatas[missionName];
        string displayName = GetDisplayMissionName(data.missionName);
        string infoText = $"<b>{displayName}</b>\n" +
                          $"남은 따개비 개수 : {data.remainingTrash}\n" +
                          $"닦기 진행도 : {data.currentProgress:F1}%";

        // 수거와 청소가 모두 끝났을 때만 완료 처리
        bool isAllDone = data.remainingTrash <= 0 && data.currentProgress >= 99.95f;

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
        // 미션 이름을 소문자로 바꿔서 어떤 맵인지 판별하기 쉽게 함
        string lowerName = missionName.ToLower();
        GameObject cardTarget = null;

        // 완료 처리할 의뢰 카드를 선택
        if (lowerName.Contains("tutorial")) cardTarget = tutorialJobCard;
        else if (lowerName.Contains("newmap") || 
            lowerName.Contains("sub")) cardTarget = submarineJobCard;

        // 해당 카드가 존재하면 완료 영역으로 이동
        if (cardTarget != null)
        {
            // 의뢰 카드를 Completed 탭의 부모 오브젝트로 옮김
            cardTarget.transform.SetParent(completedContent);
            // 이동 후 크기가 깨지지 않도록 기본 크기로 맞춤
            cardTarget.transform.localScale = Vector3.one;
            // 카드 UI를 완료 상태로 변경
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
