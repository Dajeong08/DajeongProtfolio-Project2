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
    public BattleManager battleManager;
    
    private int SelectedStageIndex = 0; // 현재 선택된 스테이지 인덱스 


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Next()
    {
        if (SelectedStageIndex < StageList.Count - 1)
        {
            SelectedStageIndex++;
        }
    }
    public void prev()
    {
        if(SelectedStageIndex > 0)
        {
            SelectedStageIndex--;
        }   
    }
    public void OnBattleClick()
    {
        battleManager.GetBattleData(StageList[SelectedStageIndex].stage_SO);
    }
    public void Unlock(int i)
    {

    }
}
