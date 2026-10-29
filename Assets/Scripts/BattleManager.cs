using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.U2D.Animation;

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

    public TMP_Text StageName_OBJ;
    public TMP_Text WaveTime_OBJ;
    public GameObject BossInfo_OBJ;
    public GameObject currentMonster; // 생성할 몬스터 오브젝트

    private List<GameObject> MonsterPool = new List<GameObject>(); // 몬스터 풀

    private string StageName; // 스테이지 이름
    private WaveType currentWaveType; // 현재 웨이브 타입     
    private List<Wave> WaveList = new List<Wave>(); // 현재 웨이브 리스트
    private int currentWaveIndex;
    private bool IsPlay_Wave = false;
    private GameObject SelectMonster;
    private GameObject CreateMonster;
    private SpriteLibraryAsset currentSpriteLibrary;
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
                        currentWaveIndex++;
                        StartWave();
                        Timer = 30;
                    }
                        break;
                case WaveType.BossMonster:
                    Debug.Log("보스 몬스터 생성");
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
        StageName = stageSetting.StageName;
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
        if(StageName != null)
        {
            StageName_OBJ.text = StageName; // 스테이지 이름 변경
        }
        foreach(Wave data in WaveList)
        {
            Debug.Log($"{data.monsterType} | {data.WaveTimeLimit}");
            switch (currentWaveType)
            {
                case WaveType.BasicMonster:
                    currentSpriteLibrary = data.spriteLibraryAsset;
                    break;
                case WaveType.BossMonster:
                    break;
            }
        }
        StartWave();
    }
    // 웨이브 시작하기 
    public void StartWave()
    {
        IsPlay_Wave = true;
        Timer = 30;



    }
    public void CreatMonsterPool()
    {
        MonsterPool.Clear(); // 풀 비우기 
        for(int i = 0; i < 40; i++)
        {
            CreateMonster = Instantiate(currentMonster);
            CreateMonster.SetActive(false); // 몬스터 비활성화 
            MonsterPool.Add(CreateMonster); // 풀에 넣기 
            Debug.Log($"몬스터 풀생성 : {i}");
        }
    }
    public void GetMonster()
    {
        SelectMonster = MonsterPool[0];
        MonsterPool.Remove(SelectMonster);
        SelectMonster.gameObject.SetActive(true); //몬스터 활성화 
        SelectMonster.transform.position = MonaterSpawonPoint.position; // 몬스터 이동
    }
    public void RemoveMonster(GameObject Monster)
    {
        MonsterPool.Add(Monster); // 몬스터 풀에 다시 넣기 
    }
}
