using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SubmarineManager : MonoBehaviour
{
    public static SubmarineManager Instance; // 어디서든 부를 수 있게 이름표 추가

    [Header("파트 리스트 (껍데기 오브젝트들 드래그)")]
    public List<SubmarinePart> allParts = new List<SubmarinePart>();

    [Header("잠수함 몸체 (의뢰 전엔 꺼둘 오브젝트)")]
    public GameObject submarineBody;

    [Header("완료 판정 기준 (%)")]
    [Range(0f, 100f)]
    public float clearThreshold = 95f;

    private bool _isCleared = false;

    void Awake()
    {
        Instance = this;
        // 게임 시작 시 잠수함을 숨깁니다.
        if (submarineBody != null) submarineBody.SetActive(false);
    }

    void Start()
    {
        foreach (var part in allParts)
            part.onProgressChanged += OnAnyPartChanged;
    }

    // 의뢰 수령 시 호출될 함수
    public void AppearSubmarine()
    {
        if (submarineBody != null) submarineBody.SetActive(true);
    }

    private void OnAnyPartChanged()
    {
        if (allParts.Count == 0) return;

        float sum = 0f;
        foreach (var part in allParts)
            sum += part.partProgress;

        float realOverall = Mathf.Clamp(sum / allParts.Count, 0f, 100f);
        float displayProgress = clearThreshold <= 0f
            ? realOverall
            : Mathf.Clamp((realOverall / clearThreshold) * 100f, 0f, 100f);

        if (MissionManager.Instance != null)
            MissionManager.Instance.UpdateProgress(displayProgress);

        if (!_isCleared && realOverall >= clearThreshold)
        {
            _isCleared = true;
            Debug.Log("잠수함 세척 완료!");
        }
    }
    // SubmarineManager.cs 내부에 추가
    public void RefreshBarnacleCount()
    {
        if (MissionManager.Instance == null) return;

        // 현재 잠수함의 모든 따개비를 다시 세기
        Barnacle[] barnacles = GetComponentsInChildren<Barnacle>(true);

        // 현재 미션의 남은 개수를 강제로 맞춤 (이미 캔 것은 제외되어야 하므로 주의)
        // 이 방법보다는 위의 MissionManager 수정 방식이 더 권장됩니다.
    }

    public void RegisterPart(SubmarinePart part)
    {
        if (!allParts.Contains(part))
        {
            allParts.Add(part);
            part.onProgressChanged += OnAnyPartChanged;
        }
    }

    void OnDestroy()
    {
        foreach (var part in allParts)
            if (part != null)
                part.onProgressChanged -= OnAnyPartChanged;
    }
}
