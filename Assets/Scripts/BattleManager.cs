using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.U2D.Animation;

// 웨이브 시간, 몬스터 생성, 대기 중인 몬스터 풀을 관리한다.
public class BattleManager : MonoBehaviour
{
    // static 참조 하나를 공유하므로 몬스터가 BattleManager.instance로 접근할 수 있다.
    public static BattleManager instance;

    public Transform[] MonaterMovePoint;
    public Transform MonaterSpawonPoint;
    public StageManager stageManager;
    public TMP_Text StageName_OBJ;
    public TMP_Text WaveTime_OBJ;
    public GameObject BossInfo_OBJ;
    public GameObject current_BasicMonster; // 일반 몬스터 원본 프리팹
    public GameObject current_BossMonster;  // 보스 몬스터 원본 프리팹

    // 풀에는 현재 사용하지 않고 대기 중인 개체만 들어 있다.
    private List<GameObject> Basic_MonsterPool = new List<GameObject>();
    private List<GameObject> Boss_MonsterPool = new List<GameObject>();
    private SpriteLibraryAsset WaveSpriteLibrary;
    private SpriteLibrary MonsterSpriteLibrary;
    private string StageName;
    private WaveType currentWaveType;
    public int CurrentWave = 1;
    public bool IsClear = false;
    private const int TotalWaves = 25;
    private const int BossInterval = 5;
    private StageSetting currentStage;
    private float currentMonsterHP;
    private bool IsPlay_Wave = false;
    private GameObject SelectMonster;
    private GameObject CreateMonster;
    private float Timer = 0;
    private float MonsterInterval;
    private bool bossSpawned;
    private bool bossDefeated;
    private SlotManager slotManager;

    // Unity가 초기화할 때 현재 전투 매니저 자신을 공유 참조에 등록한다.
    private void Awake()
    {
        instance = this;
        slotManager = FindFirstObjectByType<SlotManager>(FindObjectsInactive.Include);
    }

    // 매 프레임 시간을 계산하고 웨이브 종류에 맞게 생성한다.
    private void Update()
    {
        if (IsPlay_Wave)
        {
            if (bossDefeated)
                return;
            Timer -= Time.deltaTime;
            MonsterInterval += Time.deltaTime;

            // switch는 현재 값에 맞는 case 한 곳을 실행한다.
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
                        // $는 변수 삽입, F0는 소수점 없이 숫자를 표시하는 형식이다.
                        WaveTime_OBJ.text = $"{Timer:F0}";
                    }
                    else
                    {
                        CompleteWave();
                    }
                    break;

                case WaveType.BossMonster:
                    // 보스 풀이 하나뿐이므로 같은 웨이브에서 한 번만 꺼낸다.
                    if (!bossSpawned)
                    {
                        GetMonster();
                        bossSpawned = true;
                    }
                    WaveTime_OBJ.text = $"{Timer:F0}";
                    if (Timer <= 0)
                    {
                        IsPlay_Wave = false;
                        WaveTime_OBJ.text = "0";
                        Debug.Log("보스 제한 시간 초과");
                    }
                    break;
            }
        }
    }

    // Unity가 시작할 때 한 번 호출한다. 몬스터를 미리 생성해 둔다.
    private void Start()
    {
        CreatMonsterPool();
    }

    // StageManager가 선택한 설정을 전달한다. 첫 웨이브부터 다시 준비한다.
    public void GetBattleData(StageSetting stageSetting)
    {
        StageName = stageSetting.StageName;
        if (StageName != null)
        {
            StageName_OBJ.text = StageName;
        }
        currentStage = stageSetting;
        IsPlay_Wave = false;
        CurrentWave = 1;
        IsClear = false;
        MonsterInterval = 0f;
        int requiredEntries = TotalWaves / BossInterval * 2;
        if (stageSetting.WaveList == null || stageSetting.WaveList.Count != requiredEntries)
        {
            Debug.LogError($"Wave List에 일반/보스 순서로 외형 {requiredEntries}개를 등록해주세요.");
            return;
        }
        for (int i = 0; i < stageSetting.WaveList.Count; i++)
        {
            // 짝수 항목은 일반, 홀수 항목은 보스로 설정한다.
            WaveType expectedType = WaveType.BasicMonster;
            if (i % 2 == 1)
                expectedType = WaveType.BossMonster;

            if (stageSetting.WaveList[i].monsterType != expectedType)
            {
                Debug.LogError($"Wave List의 Element {i} 타입을 {expectedType}으로 설정해주세요.");
                return;
            }
            if (stageSetting.WaveList[i].MonsterSpriteLibrary == null)
            {
                Debug.LogError($"Wave List의 Element {i}에 몬스터 외형을 연결해주세요.");
                return;
            }
        }
        StartWave();
    }

    // 현재 목록 번호에 맞는 종류와 외형을 읽고 제한 시간을 설정한다.
    public void StartWave()
    {
        if (currentStage == null || CurrentWave > TotalWaves || IsPlay_Wave || IsClear)
        {
            return;
        }
        bossSpawned = false;
        bossDefeated = false;
        MonsterInterval = 0;
        IsPlay_Wave = true;
        // 정수 나눗셈은 소수점을 버린다. 1~5는 0, 6~10은 1이다.
        int groupIndex = (CurrentWave - 1) / BossInterval;
        // 5웨이브 묶음마다 일반/보스 두 칸을 사용한다.
        int listIndex = groupIndex * 2;
        // %는 나머지다. 5로 나눈 나머지가 0이면 보스 웨이브다.
        if (CurrentWave % BossInterval == 0)
        {
            currentWaveType = WaveType.BossMonster;
            listIndex += 1;
            currentMonsterHP = currentStage.BossMonsterHP + groupIndex * currentStage.BossHPIncrease;
        }
        else
        {
            currentWaveType = WaveType.BasicMonster;
            currentMonsterHP = currentStage.BasicMonsterHP + (CurrentWave - 1) * currentStage.BasicHPIncrease;
        }
        WaveSpriteLibrary = currentStage.WaveList[listIndex].MonsterSpriteLibrary;
        StageName_OBJ.text = $"{StageName}  {CurrentWave}/{TotalWaves}";
        Timer = 30;
        WaveTime_OBJ.text = "30";
    }

    // 몬스터가 죽는 순간 호출한다. 풀 반환에서는 처치 보상을 지급하지 않는다.
    public void MonsterDefeated(WaveType monsterType)
    {
        if (!IsPlay_Wave)
            return;
        if (slotManager != null)
        {
            if (monsterType == WaveType.BasicMonster)
                slotManager.AddCoin(1);
            else if (monsterType == WaveType.BossMonster)
                slotManager.AddCoin(50);
        }

        if (IsPlay_Wave && currentWaveType == WaveType.BossMonster
            && monsterType == WaveType.BossMonster && Timer > 0)
            bossDefeated = true;
    }

    // 정상 완료한 웨이브에 한 번 보상하고 다음 웨이브를 시작한다.
    private void CompleteWave()
    {
        if (!IsPlay_Wave)
            return;

        IsPlay_Wave = false;
        if (slotManager != null)
            slotManager.AddCoin(100);

        if (CurrentWave == TotalWaves)
        {
            IsClear = true;
            Debug.Log("모든 웨이브 완료");
            return;
        }
        CurrentWave++;
        StartWave();
    }

    // 풀링은 개체를 매번 삭제하지 않고, 꺼 두었다가 다시 켜서 사용하는 방식이다.
    public void CreatMonsterPool()
    {
        GameObject New_MonsterPool = new GameObject($"MonsterPool_Basic");
        for (int i = 0; i < 120; i++)
        {
            CreateMonster = Instantiate(current_BasicMonster, New_MonsterPool.transform);
            CreateMonster.SetActive(false);
            Basic_MonsterPool.Add(CreateMonster);
        }
        New_MonsterPool = new GameObject($"MonsterPool_Boss");
        CreateMonster = Instantiate(current_BossMonster, New_MonsterPool.transform);
        CreateMonster.SetActive(false);
        Boss_MonsterPool.Add(CreateMonster);
    }

    // 대기 목록에서 개체를 꺼내고 웨이브 외형을 적용해 활성화한다.
    public void GetMonster()
    {
        switch (currentWaveType)
        {
            case WaveType.BasicMonster:
                if (Basic_MonsterPool.Count == 0)
                {
                    // 이전 웨이브 몬스터가 쌓여도 생성을 중단하지 않도록 풀을 늘린다.
                    GameObject extra = Instantiate(current_BasicMonster);
                    extra.SetActive(false);
                    Basic_MonsterPool.Add(extra);
                }
                SelectMonster = Basic_MonsterPool[0];
                // Remove는 목록에서만 뺀다. 오브젝트 자체를 삭제하지 않는다.
                Basic_MonsterPool.Remove(SelectMonster);
                MonsterSpriteLibrary = SelectMonster.GetComponent<SpriteLibrary>();
                MonsterSpriteLibrary.spriteLibraryAsset = WaveSpriteLibrary;
                SelectMonster.GetComponent<MonsterManager>().SetWaveHP(currentMonsterHP);
                // 켜는 순간 MonsterManager.OnEnable이 실행되어 체력과 경로를 초기화한다.
                SelectMonster.gameObject.SetActive(true);
                SelectMonster.transform.position = MonaterSpawonPoint.position;
                break;

            case WaveType.BossMonster:
                if (Boss_MonsterPool.Count == 0)
                    return;
                SelectMonster = Boss_MonsterPool[0];
                Boss_MonsterPool.Remove(SelectMonster);
                MonsterSpriteLibrary = SelectMonster.GetComponent<SpriteLibrary>();
                MonsterSpriteLibrary.spriteLibraryAsset = WaveSpriteLibrary;
                SelectMonster.GetComponent<MonsterManager>().SetWaveHP(currentMonsterHP);
                SelectMonster.gameObject.SetActive(true);
                SelectMonster.transform.position = MonaterSpawonPoint.position;
                break;
        }
    }

    // MonsterManager가 사망 애니메이션 종료 후 호출해 대기 목록으로 돌려보낸다.
    public void RemoveMonster(GameObject Monster)
    {
        if (Monster == null)
            return;
        MonsterManager monster = Monster.GetComponent<MonsterManager>();
        if (monster == null)
            return;

        Monster.SetActive(false);
        // 현재 웨이브가 바뀌었어도 몬스터 자신의 종류로 반환해야 한다.
        if (monster.poolType == WaveType.BasicMonster)
        {
            if (!Basic_MonsterPool.Contains(Monster))
            {
                Basic_MonsterPool.Add(Monster);
            }
        }
        else if (monster.poolType == WaveType.BossMonster)
        {
            if (!Boss_MonsterPool.Contains(Monster))
            {
                Boss_MonsterPool.Add(Monster);
            }
            if (currentWaveType == WaveType.BossMonster && bossDefeated)
            {
                // 보스를 풀에 돌려놓은 뒤 진행해야 다음 보스도 꺼낼 수 있다.
                CompleteWave();
            }
        }
    }
}
