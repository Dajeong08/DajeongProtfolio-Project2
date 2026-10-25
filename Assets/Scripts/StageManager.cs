using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct StageCollection
{
    public StageSetting stage_SO;
    public GameObject StageIcon;
    public bool unlocked;
}

public class StageManager : MonoBehaviour
{
    public List<StageCollection> StageList;
    public GameObject StageCollection_OBJ;
    public GameObject NextButton;
    public GameObject PrevButton;
    public BattleManager battleManager;
    public GameObject LobbyScreen;

    private int SelectedStageIndex = 0; // 현재 선택된 스테이지 인덱스 


    void Start()
    {
        LobbyScreen.SetActive(true);
        PrevButton.SetActive(false);
        if (StageList.Count <= 1)
        {
            NextButton.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    // 다음 버튼을 눌렀을 때
    public void Next()
    {
        if (SelectedStageIndex < StageList.Count - 1)
        {
            SelectedStageIndex++;

            PrevButton.SetActive(true);
            StageCollection_OBJ.transform.position = new Vector3(StageCollection_OBJ.transform.position.x -360f, StageCollection_OBJ.transform.position.y, StageCollection_OBJ.transform.position.z) ;

            if (SelectedStageIndex == StageList.Count - 1)
            {
                NextButton.SetActive(false);
            }
        }
    }
    // 이전 버튼을 눌렀을 때
    public void prev()
    {
        if(SelectedStageIndex > 0)
        {
            SelectedStageIndex--;

            NextButton.SetActive(true);
            StageCollection_OBJ.transform.position = new Vector3(StageCollection_OBJ.transform.position.x +360f, StageCollection_OBJ.transform.position.y, StageCollection_OBJ.transform.position.z);

            if (SelectedStageIndex == 0)
            {
                PrevButton.SetActive(false);
            }
        }
    }
    // 배틀 시작 버튼을 눌렀을 때
    public void OnBattleClick()
    {
        battleManager.GetBattleData(StageList[SelectedStageIndex].stage_SO);
        LobbyScreen.gameObject.SetActive(false);
    }
}
