using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class BattleManager : MonoBehaviour
{
    /// <summary>
    /// 시작시 현재 스테이지의 스테이지 데이터를 받는다. 
    /// 
    /// 시간에 따른 웨이브 시작하기 
    /// </summary>
    public static BattleManager instance;

    // 몬스터 이동 포인트
    public Transform[] MonaterMovePoint; 
    public Transform MonaterSpawonPoint;


    private int currentWaveinterval; // 현재 웨이브 간격
    private WaveType currentWaveType; // 현재 웨이브 타입    
    private List<Wave> StageList; // 현재 웨이브 리스트
    
    private List<GameObject> MonsterPool = new List<GameObject>(); // 몬스터 풀

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
    }

    // 배틀 데이터 받아오기
    public void GetBattleData(StageSetting stageSetting)
    {

    }
    // 배틀 세팅하기
    public void BattleSetting()
    {

    }
    // 웨이브 시작하기 
    public void StartWave()
    {
        switch (currentWaveType)
        {
            case WaveType.BasicMonster:
                
                break;
            case WaveType.BossMonster:
                
                break;
        }
    }
}
