using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
// 선택할 스테이지 데이터
public struct StageCollection
{
    public StageSetting stage_SO; // 스테이지 데이터
}

// 스테이지 선택과 전투 시작
public class StageManager : MonoBehaviour
{
    public static StageSetting RetryStage; // 씬을 다시 열 때 전달할 재도전 스테이지
    public List<StageCollection> StageList; // 선택 가능한 스테이지 목록
    public GameObject StageCollection_OBJ; // 좌우로 이동할 스테이지 UI 묶음
    public GameObject NextButton; // 다음 스테이지 버튼
    public GameObject PrevButton; // 이전 스테이지 버튼
    public BattleManager battleManager; // 전투 시작 요청
    public GameObject LobbyScreen; // 로비 전체 화면

    private int SelectedStageIndex = 0; // 선택한 스테이지 번호, 0부터 시작

    // 로비 UI 준비, 재도전이면 해당 스테이지 시작
    void Start()
    {
        LobbyScreen.SetActive(true); // 로비 화면을 켠다.
        PrevButton.SetActive(false); // 첫 번째 스테이지이므로 이전 버튼을 비활성화
        if (StageList.Count <= 1)
        {
            NextButton.SetActive(false); // 스테이지가 1개 이하이면 다음 버튼을 비활성화
        }

        if (RetryStage != null)
        {
            StageSetting stage = RetryStage;
            // 이번 씬에서 한 번만 재도전하도록 static 값 비우기
            RetryStage = null;
            int stageIndex = 0;
            for (int i = 0; i < StageList.Count; i++)
            {
                if (StageList[i].stage_SO == stage)
                {
                    stageIndex = i;
                    break;
                }
            }
            if (battleManager.GetBattleData(stage, stageIndex))
                LobbyScreen.SetActive(false);
        }
    }

    // 다음 스테이지 선택
    public void Next()
    {
        if (SelectedStageIndex < StageList.Count - 1)
        {
            SelectedStageIndex++; 
            PrevButton.SetActive(true); // 다음 버튼을 누르면 반드시 이전 버튼을 활성화
            StageCollection_OBJ.transform.position = new Vector3(
                StageCollection_OBJ.transform.position.x - 360f,
                StageCollection_OBJ.transform.position.y,
                StageCollection_OBJ.transform.position.z); // UI 묶음을 왼쪽으로 이동

            if (SelectedStageIndex == StageList.Count - 1) // 마지막 스테이지면 다음 버튼 비활성화
            {
                NextButton.SetActive(false);
            }
        }
    }

    // 이전 스테이지 선택
    public void prev()
    {
        if (SelectedStageIndex > 0)
        {
            SelectedStageIndex--; 
            NextButton.SetActive(true); // 이전 버튼을 누르면 반드시 다음 버튼을 활성화
            StageCollection_OBJ.transform.position = new Vector3(
                StageCollection_OBJ.transform.position.x + 360f,
                StageCollection_OBJ.transform.position.y,
                StageCollection_OBJ.transform.position.z); // UI 묶음을 오른쪽으로 이동

            if (SelectedStageIndex == 0) // 첫 번째 스테이지면 이전 버튼 비활성화
            {
                PrevButton.SetActive(false);
            }
        }
    }

    // 선택한 데이터와 배경 번호로 전투 시작
    public void OnBattleClick()
    {
        if (battleManager.GetBattleData(StageList[SelectedStageIndex].stage_SO, SelectedStageIndex)) // 선택한 데이터와 배경 번호 전달
            LobbyScreen.SetActive(false); // 전투 시작
    }
}
