using System;
using System.Collections.Generic;
using UnityEngine;

// Serializable을 붙이면 Inspector에서 구조체 안의 값을 편집할 수 있다.
[Serializable]
public struct StageCollection
{
    public StageSetting stage_SO; // 스테이지 데이터 에셋
}

// 로비의 스테이지 선택과 전투 시작을 담당한다.
public class StageManager : MonoBehaviour
{
    // 재시작 버튼이 다음 씬에 한 번만 전달할 스테이지다.
    public static StageSetting RetryStage; 
    public List<StageCollection> StageList; // 스테이지 데이터 에셋 목록
    public GameObject StageCollection_OBJ;
    public GameObject NextButton; // 다음 버튼
    public GameObject PrevButton; // 이전 버튼
    public BattleManager battleManager; 
    public GameObject LobbyScreen; // 로비 화면 

    private int SelectedStageIndex = 0; // 목록 번호는 0부터 시작한다.

    // Unity가 시작할 때 한 번 호출한다. 로비와 처음 버튼 상태를 준비한다.
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
            RetryStage = null;
            if (battleManager.GetBattleData(stage))
                LobbyScreen.SetActive(false);
        }
    }

    // 다음 버튼에서 호출한다. 선택 번호를 올리고 UI 묶음을 왼쪽으로 옮긴다.
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

    // 이전 버튼에서 호출한다. 선택 번호를 내리고 UI 묶음을 오른쪽으로 옮긴다.
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

    // 전투 버튼에서 선택한 데이터 에셋을 BattleManager에 넘기고 로비를 숨긴다.
    public void OnBattleClick()
    {
        if (battleManager.GetBattleData(StageList[SelectedStageIndex].stage_SO)) // BattleManager에 선택한 스테이지 데이터를 전달
            LobbyScreen.SetActive(false); // 전투 시작
    }
}
