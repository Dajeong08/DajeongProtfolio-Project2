using System.Collections.Generic;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.U2D.Animation;

public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;

    // 몬스터 이동 포인트
    public Transform[] MonaterMovePoint; 
    public Transform MonaterSpawonPoint;

    public StageManager stageManager;

    public TMP_Text StageName_OBJ;
    public TMP_Text WaveTime_OBJ;
    public GameObject BossInfo_OBJ;
    public GameObject currentMonster; // 생성할 기본 몬스터 오브젝트
    public Transform MonsterPool_List_prent; //몬스터 풀이 나열될 부모 오브젝트

    private List<GameObject> Basic_MonsterPool = new List<GameObject>(); // 일반 몬스터 풀
    private List<GameObject> Boss_MonsterPool = new List<GameObject>(); // 보스 몬스터 풀

    private List<GameObject> MonsterPool_List = new List<GameObject>(); // 몬스터 풀 리스트

    private string StageName; // 스테이지 이름
    private WaveType currentWaveType; // 현재 웨이브 타입     
    private List<Wave> WaveList = new List<Wave>(); // 현재 웨이브 리스트
    private StageSetting current_Stage;
    private int currentWaveIndex; // 현재 웨이브 인덱스 => 몇번째 웨이브인지 알기(1부터 시작)
    private int currentStageIndex; // 현재 스테이지 인덱스 
    private bool IsPlay_Wave = false; 
    private GameObject SelectMonster; 
    private GameObject CreateMonster; 
    private float Timer = 0;
    private float MonsterInterval;

    private void Awake()
    {
        instance = this;
    }
    private void Update()
    {
        if (IsPlay_Wave)
        {
            Timer -= Time.deltaTime;
            MonsterInterval += Time.deltaTime;
            //Debug.Log($"{currentWaveType}");
            switch (currentWaveType)
            {
                case WaveType.BasicMonster:
                    if (Timer >= 0)
                    {
                        if (MonsterInterval >= 1.3f)
                        {
                            Debug.Log("몬스터 생성");
                            GetMonster();
                            MonsterInterval = 0;
                        }
                        WaveTime_OBJ.text = $"{Timer:F0}";
                    }
                    else
                    {
                        if(currentWaveIndex < 5)
                        {
                            currentWaveIndex++;
                            StartWave(WaveList[currentWaveIndex]);
                            Timer = 30;
                        }
                        else
                        {
                            // 스테이지 종료
                            Debug.Log("스테이지 종료");
                        }
                    }
                        break;
                case WaveType.BossMonster:
                    if (Timer == 30)
                    {
                        Debug.Log("보스 몬스터 생성");
                        GetMonster(); 
                        WaveTime_OBJ.text = $"{Timer:F0}";
                    }
                    else if(Timer <= 0)
                    {
                        if (currentWaveIndex < 5)
                        {
                            currentWaveIndex++;
                            StartWave(WaveList[currentWaveIndex]);
                            Timer = 30;
                        }
                        else
                        {
                            // 스테이지 종료 
                            Debug.Log("스테이지 종료");
                        }
                    }
                    break;
            }
        }
    }
    private void Start()
    {
        CreatMonsterPool(); // 몬스터 풀 생성
    }
    // 스테이지 데이터 받아오기
    public void GetBattleData(StageSetting stageSetting)
    {
        Debug.Log($"스테이지 데이터 받아오기 : {stageSetting}");
        StageName = stageSetting.StageName;
        currentStageIndex = stageSetting.StageNumber-1;
        if (StageName != null)
        {
            StageName_OBJ.text = StageName; // 스테이지 이름 변경
        }
        WaveList.Clear();
        foreach (Wave wave in stageSetting.WaveList)
        {
            WaveList.Add(wave); // 웨이브 쌓기 
        }
        BattleSetting();
    }
    // 배틀 세팅하기
    public void BattleSetting()
    {
        Debug.Log("배틀 세팅");

        foreach (Wave data in WaveList)
        {
            currentWaveType = data.monsterType; //몬스터 타입 
            switch (currentWaveType)
            {
                case WaveType.BasicMonster:
                    break;
                    // 
                case WaveType.BossMonster:
                    break;
            }
        }
        StartWave(WaveList[currentWaveIndex]);
    }
    // 웨이브 시작하기 
    public void StartWave(Wave wave)
    {
        IsPlay_Wave = true;

        Debug.Log($"웨이브 시작 : {IsPlay_Wave}");

        Debug.Log($"{currentStageIndex+1} - {currentWaveIndex+1}");
        Timer = 30;
    }
    // 풀 생성 함수
    public void CreatMonsterPool()
    {
        Debug.Log("풀 생성하기");
        // 모든 스테이지 돌기
        for(int i = 0 ; i < stageManager.StageList.Count; i++)
        {
            current_Stage = stageManager.StageList[i].stage_SO;
            // 각 스테이지의 모든 웨이브 돌기 
            for (int j = 0; j < current_Stage.WaveList.Count; j++)
            {
                Debug.Log($"MonsterPool_{i+1} - {j+1}");
                GameObject New_MonsterPool = new GameObject($"MonsterPool_{i + 1} - {j + 1}"); // 스테이지 
                New_MonsterPool.transform.SetParent(MonsterPool_List_prent);

                switch (current_Stage.WaveList[j].monsterType) // 현재 웨이브 타입에 따라
                {
                    case WaveType.BasicMonster:
                        for (int k = 0; k < 30; k++)
                        {
                            CreateMonster = Instantiate(current_Stage.WaveList[j].Monster, New_MonsterPool.transform); // 현재 웨이브에 맞는 오브젝트 생성 및 각 웨이브 부모 아래로
                            CreateMonster.SetActive(false); // 비활성화 
                            Basic_MonsterPool.Add(CreateMonster); // 풀에 넣기 
                        }
                        break;
                    case WaveType.BossMonster:
                        CreateMonster = Instantiate(current_Stage.WaveList[j].Monster, New_MonsterPool.transform); // 본스몬스터 오브젝트 생성
                        CreateMonster.SetActive(false); //비활성화
                        Boss_MonsterPool.Add(CreateMonster); // 풀에 넣기
                        break;
                }
            }
        }
    }
    public void GetMonster()
    {
        Debug.Log("몬스터 가져오기");
        switch (currentWaveType)
        {
            case WaveType.BasicMonster:
                SelectMonster = Basic_MonsterPool[currentStageIndex*30];
                Basic_MonsterPool.Remove(SelectMonster);
                SelectMonster.gameObject.SetActive(true); //몬스터 활성화 
                SelectMonster.transform.position = MonaterSpawonPoint.position; // 몬스터 이동
                break;
            case WaveType.BossMonster:
                SelectMonster = Boss_MonsterPool[currentStageIndex];
                Boss_MonsterPool.Remove(SelectMonster);
                SelectMonster.gameObject.SetActive(true); //몬스터 활성화 
                SelectMonster.transform.position = MonaterSpawonPoint.position; // 몬스터 이동
                break;
        }
        Debug.Log($"선택된 몬스터 {SelectMonster}");
        // 선택된 몬스터의 스프라이트 라이브러리를 변경
        // 선택된 웨이브의 스프라이트 가져오기
    }
    public void RemoveMonster(GameObject Monster)
    {
        Debug.Log("몬스터 집어넣기");
        switch (currentWaveType)
        {
            case WaveType.BasicMonster:
                Basic_MonsterPool.Insert(currentStageIndex * 30, Monster); // 몬스터 풀에 다시 넣기 
                break;
            case WaveType.BossMonster:
                Boss_MonsterPool.Insert(currentStageIndex, Monster);                
                break;
        }
        Monster.SetActive(false);
    }
}
