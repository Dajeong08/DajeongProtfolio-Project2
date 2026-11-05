using System.Collections.Generic;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Timeline;
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
    private List<Wave> WaveList = new List<Wave>();
    private int currentWaveIndex; // 첫 번째 웨이브의 목록 번호는 0이다.
    private bool IsPlay_Wave = false;
    private GameObject SelectMonster;
    private GameObject CreateMonster;
    private float Timer = 0;
    private float MonsterInterval;
    private bool bossSpawned;

    // Unity가 초기화할 때 현재 전투 매니저 자신을 공유 참조에 등록한다.
    private void Awake()
    {
        instance = this;
    }

    // 매 프레임 시간을 계산하고 웨이브 종류에 맞게 생성한다.
    private void Update()
    {
        if (IsPlay_Wave)
        {
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
                        if (currentWaveIndex + 1 < WaveList.Count)
                        {
                            currentWaveIndex++;
                            StartWave();
                            Timer = 30;
                        }
                        else
                        {
                            IsPlay_Wave = false;
                        }
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
                        Debug.Log("스테이지 종료");
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
        currentWaveIndex = 0;
        MonsterInterval = 0f;
        WaveList.Clear();
        // foreach는 목록의 항목을 하나씩 꺼낸다. wave는 이번에 꺼낸 웨이브다.
        foreach (Wave wave in stageSetting.WaveList)
        {
            WaveList.Add(wave);
        }
        StartWave();
    }

    // 현재 목록 번호에 맞는 종류와 외형을 읽고 제한 시간을 설정한다.
    public void StartWave()
    {
        if (currentWaveIndex >= WaveList.Count)
        {
            IsPlay_Wave = false;
            return;
        }
        bossSpawned = false;
        IsPlay_Wave = true;
        Debug.Log(currentWaveIndex);
        currentWaveType = WaveList[currentWaveIndex].monsterType;
        WaveSpriteLibrary = WaveList[currentWaveIndex].MonsterSpriteLibrary;
        Timer = 30;
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
                    return;
                SelectMonster = Basic_MonsterPool[0];
                // Remove는 목록에서만 뺀다. 오브젝트 자체를 삭제하지 않는다.
                Basic_MonsterPool.Remove(SelectMonster);
                MonsterSpriteLibrary = SelectMonster.GetComponent<SpriteLibrary>();
                MonsterSpriteLibrary.spriteLibraryAsset = WaveSpriteLibrary;
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
            if (currentWaveType == WaveType.BossMonster)
            {
                // 웨이브 생성과 타이머만 중단한다. 필드의 모든 동작을 멈추는 것은 아니다.
                IsPlay_Wave = false;
            }
        }
    }
}
