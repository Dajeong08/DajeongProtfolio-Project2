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
    public GameObject current_BasicMonster; // 생성할 기본 몬스터 오브젝트
    public GameObject current_BossMonster; // 생성할 보스 몬스터 오브젝트

    private List<GameObject> Basic_MonsterPool = new List<GameObject>(); // 일반 몬스터 풀
    private List<GameObject> Boss_MonsterPool = new List<GameObject>(); // 보스 몬스터 풀

    private SpriteLibraryAsset WaveSpriteLibrary;
    private SpriteLibrary MonsterSpriteLibrary;

    private string StageName; // 스테이지 이름
    private WaveType currentWaveType; // 현재 웨이브 타입     
    private List<Wave> WaveList = new List<Wave>(); // 현재 웨이브 리스트
    private int currentWaveIndex; // 현재 웨이브 인덱스 => 몇번째 웨이브인지 알기(1부터 시작)
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
            switch (currentWaveType)
            {
                case WaveType.BasicMonster:
                    if (Timer >= 0)
                    {
                        if (MonsterInterval >= 1f)
                        {
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
                            StartWave();
                            Timer = 30;
                        }
                    }
                        break;
                case WaveType.BossMonster:  
                    GetMonster();
                    WaveTime_OBJ.text = $"{Timer:F0}";

                    if(Timer <= 0)
                    {
                        // 스테이지 종료 
                        Debug.Log("스테이지 종료");
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
        StageName = stageSetting.StageName;
        if (StageName != null)
        {
            StageName_OBJ.text = StageName; // 스테이지 이름 변경
        }
        WaveList.Clear();
        foreach (Wave wave in stageSetting.WaveList)
        {
            WaveList.Add(wave); // 웨이브 쌓기 
        }
        StartWave();
    }
    // 웨이브 시작하기 
    public void StartWave()
    {
        IsPlay_Wave = true;
        Debug.Log(currentWaveIndex);
        currentWaveType = WaveList[currentWaveIndex].monsterType;
        WaveSpriteLibrary = WaveList[currentWaveIndex].MonsterSpriteLibrary;

        Timer = 30;
    }
    // 풀 생성 함수
    public void CreatMonsterPool()
    {

        GameObject New_MonsterPool = new GameObject($"MonsterPool_Basic"); // 스테이지 

        for (int i = 0; i < 120; i++)
        {
            CreateMonster = Instantiate(current_BasicMonster, New_MonsterPool.transform); // 현재 웨이브에 맞는 오브젝트 생성 및 각 웨이브 부모 아래로
            CreateMonster.SetActive(false); // 비활성화 
            Basic_MonsterPool.Add(CreateMonster); // 풀에 넣기 
        }

        New_MonsterPool = new GameObject($"MonsterPool_Boss"); // 스테이지 
        CreateMonster = Instantiate(current_BasicMonster, New_MonsterPool.transform); // 본스몬스터 오브젝트 생성
        CreateMonster.SetActive(false); //비활성화
        Boss_MonsterPool.Add(CreateMonster); // 풀에 넣기
    }
    public void GetMonster()
    {
        switch (currentWaveType)
        {
            case WaveType.BasicMonster:
                SelectMonster = Basic_MonsterPool[0];
                Basic_MonsterPool.Remove(SelectMonster);

                MonsterSpriteLibrary = SelectMonster.GetComponent<SpriteLibrary>();
                MonsterSpriteLibrary.spriteLibraryAsset = WaveSpriteLibrary;
                SelectMonster.gameObject.SetActive(true); //몬스터 활성화 
                SelectMonster.transform.position = MonaterSpawonPoint.position; // 몬스터 이동
                break;
            case WaveType.BossMonster:
                SelectMonster = Boss_MonsterPool[0];
                Boss_MonsterPool.Remove(SelectMonster);

                MonsterSpriteLibrary = SelectMonster.GetComponent<SpriteLibrary>();
                MonsterSpriteLibrary.spriteLibraryAsset = WaveSpriteLibrary;
                SelectMonster.gameObject.SetActive(true); //몬스터 활성화 
                SelectMonster.transform.position = MonaterSpawonPoint.position; // 몬스터 이동
                break;
        }
    }
    public void RemoveMonster(GameObject Monster)
    {
        switch (currentWaveType)
        {
            case WaveType.BasicMonster:
                Basic_MonsterPool.Insert(0,Monster); // 몬스터 풀에 다시 넣기 
                break;
            case WaveType.BossMonster:
                Boss_MonsterPool.Insert(0, Monster);                
                break;
        }
        Monster.SetActive(false);
    }
}
