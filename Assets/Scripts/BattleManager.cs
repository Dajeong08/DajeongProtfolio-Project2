using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

// 웨이브 시간, 몬스터 생성, 대기 중인 몬스터 풀을 관리한다.
public class BattleManager : MonoBehaviour
{
    // 공통 참조
    public static BattleManager instance; // 다른 코드에서 현재 전투 매니저에 접근
    private SlotManager slotManager; // 코인 지급과 슬롯 UI 정리
    private RouletteManager rouletteManager;
    public MythicSummonManager mythicSummonManager; // 신화 소환과 슬롯 관리
    public LobbyManager lobbyManager;
    public List<SummonedCharacter> MythicCharacters = new List<SummonedCharacter>(); // 필드의 신화 목록
    public int AngleSteck;
    public TMP_Text SpeedLoss;

    // 전투 UI
    public TMP_Text StageName_OBJ; // 스테이지 이름과 웨이브
    public TMP_Text WaveTime_OBJ; // 남은 시간
    public GameObject ResultImage; // 전투 종료 이미지
    public TMP_Text ResultCoin;

    // 웨이브 진행
    private const int TotalWaves = 25; // 전체 웨이브 수
    private const int BossInterval = 5; // 보스 등장 간격
    public int CurrentWave = 1; // 진행 중인 웨이브
    public int LastClearedWave; // 마지막으로 완료한 웨이브
    private StageSetting currentStage; // 선택한 스테이지 데이터
    private WaveType currentWaveType; // 일반 또는 보스 웨이브
    private bool IsPlay_Wave = false; // 웨이브 진행 여부
    private float Timer = 0; // 남은 웨이브 시간
    private float MonsterInterval; // 마지막 몬스터 생성 후 흐른 시간

    // 몬스터 생성과 풀링
    public Transform[] MonaterMovePoint; // 순서대로 이동할 지점
    public Transform MonaterSpawonPoint; // 출발 위치
    public GameObject current_BasicMonster; // 일반 몬스터 원본 프리팹
    public GameObject current_BossMonster; // 보스 원본 프리팹
    [Min(1)] public int MonsterLimit = 100; // 패배하는 생존 몬스터 수
    private int aliveMonsterCount; // 현재 살아 있는 몬스터 수
    private int waveCoin; // 전투 시작 시 적용한 웨이브 보상
    private float currentMonsterHP; // 이번 웨이브 몬스터의 최대 체력
    private SpriteLibraryAsset WaveSpriteLibrary; // 이번 웨이브 몬스터 외형
    // 풀 목록에는 꺼져 있고 재사용을 기다리는 몬스터를 보관한다.
    private List<GameObject> Basic_MonsterPool = new List<GameObject>();
    private List<GameObject> Boss_MonsterPool = new List<GameObject>();

    // 보스 UI와 등장 상태
    public GameObject BossInfo_OBJ; // 보스 등장 연출
    public Slider BossHPSlider; // 보스 체력바
    public TMP_Text BossHPText; // 현재 체력과 최대 체력
    [Min(0f)] public float BossIntroSeconds = 1f; // 연출 표시 시간
    private bool bossSpawned; // 이번 웨이브에서 보스를 생성했는지
    private bool bossDefeated; // 보스를 처치했는지
    private MonsterManager activeBoss; // 체력바에 표시할 보스
    private float bossIntroEndTime; // 연출을 끌 게임 시간

    // 배속
    public TMP_Text SpeedText; // 현재 배속 표시
    [Range(3, 4)] public int MaxGameSpeed = 4; // 마지막 배속 단계
    private int currentSpeed = 1; // 현재 배속

    // 종료와 보상
    public bool IsClear = false; // 승리 여부
    public Sprite ClearBenner;
    public Sprite LossBenner; // 
    public bool IsGameEnded; // 전투 종료 여부
    [Min(0)] public int CoinPerClearedWave = 10; // 완료 웨이브당 로비 보상
    [Min(0)] public int ClearBonusCoin = 1000; // 모든 웨이브 클리어 시 추가 보상
    private bool isLeavingResult; // 재시작·로비 복귀 중복 요청 방지

    // Unity가 초기화할 때 현재 전투 매니저 자신을 공유 참조에 등록한다.
    private void Awake()
    {
        instance = this;
        slotManager = FindFirstObjectByType<SlotManager>(FindObjectsInactive.Include);
        rouletteManager = FindFirstObjectByType<RouletteManager>(FindObjectsInactive.Include);

        HideBossUI();
        ResultImage.SetActive(false);
        SetGameSpeed(1);
        RefreshSpeedLoss();
    }

    // Unity가 시작할 때 한 번 호출한다. 몬스터를 미리 생성해 둔다.
    private void Start()
    {
        CreatMonsterPool();
    }

    // 매 프레임 시간을 계산하고 웨이브 종류에 맞게 생성한다.
    private void Update()
    {
        if (!IsPlay_Wave || bossDefeated)
            return;

        Timer -= Time.deltaTime;
        MonsterInterval += Time.deltaTime;
        WaveTime_OBJ.text = $"{Mathf.Max(0f, Timer):F0}";

        if (currentWaveType == WaveType.BasicMonster)
        {
            if (Timer < 0)
            {
                CompleteWave();
                return;
            }
            if (MonsterInterval >= 1f)
            {
                GetMonster();
                MonsterInterval = 0;
            }
        }
        else
        {
            // 보스는 웨이브마다 한 번만 꺼낸다.
            if (!bossSpawned)
            {
                GetMonster();
                bossSpawned = true;
            }
            if (Timer <= 0)
                EndGame(false);
        }
    }

    // 게임 시간을 기준으로 잡아서 등장 연출도 배속을 따라간다.
    private void LateUpdate()
    {
        if (BossInfo_OBJ.activeSelf && Time.time >= bossIntroEndTime)
            BossInfo_OBJ.SetActive(false);
    }

    private void OnDestroy()
    {
        // 이전 전투가 사라질 때 다음 씬이 멈춰 있지 않도록 시간을 복구한다.
        Time.timeScale = 1f;
    }

    // 배속 버튼의 On Click에서 호출한다.
    public void ChangeSpeed()
    {
        if (IsGameEnded || currentStage == null)
            return;

        if (currentSpeed == 1)
            SetGameSpeed(2);
        else if (currentSpeed == 2)
            SetGameSpeed(MaxGameSpeed);
        else
            SetGameSpeed(1);
    }

    private void SetGameSpeed(int speed)
    {
        currentSpeed = speed;
        Time.timeScale = speed;
        SpeedText.text = $"×{speed}";
    }

    public void RefreshSpeedLoss()
    {
        // 씬이 닫힐 때는 표시 UI가 먼저 사라질 수 있다.
        if (SpeedLoss != null)
            SpeedLoss.text = $"{Mathf.Clamp(AngleSteck * 10, 0, 100)}%";
    }

    // StageManager가 선택한 설정을 전달한다. 첫 웨이브부터 다시 준비한다.
    public bool GetBattleData(StageSetting stageSetting)
    {
        // 25웨이브에 필요한 일반·보스 외형 10개가 있어야 시작한다.
        if (stageSetting == null || currentStage != null || stageSetting.WaveList == null
            || stageSetting.WaveList.Count != TotalWaves / BossInterval * 2)
            return false;

        currentStage = stageSetting;
        CurrentWave = 1;
        LastClearedWave = 0;
        aliveMonsterCount = 0;
        IsClear = false;
        IsGameEnded = false;
        AngleSteck = 0;
        RefreshSpeedLoss();
        // 로비의 강화 값을 이번 전투에 복사한다. 웨이브마다 로비 값을 다시 읽지는 않는다.
        MonsterLimit = LobbyManager.MonsterLimit;
        waveCoin = LobbyManager.WaveCoin;
        if (rouletteManager != null)
            rouletteManager.SetStartToken(LobbyManager.StartToken);
        SetGameSpeed(1);
        StartWave();
        return true;
    }

    // 현재 목록 번호에 맞는 종류와 외형을 읽고 제한 시간을 설정한다.
    public void StartWave()
    {
        if (currentStage == null || CurrentWave > TotalWaves || IsPlay_Wave || IsGameEnded)
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
            currentMonsterHP = currentStage.BossHPList[groupIndex];
        }
        else
        {
            currentWaveType = WaveType.BasicMonster;
            currentMonsterHP = currentStage.BasicMonsterHP + (CurrentWave - 1) * currentStage.BasicHPIncrease;
        }
        WaveSpriteLibrary = currentStage.WaveList[listIndex].MonsterSpriteLibrary;
        StageName_OBJ.text = $"{currentStage.StageName}  {CurrentWave}/{TotalWaves}";
        Timer = 30;
        WaveTime_OBJ.text = "30";
    }

    // 몬스터가 죽는 순간 호출한다. 풀 반환에서는 처치 보상을 지급하지 않는다.
    public void MonsterDefeated(WaveType monsterType, Vector3 position)
    {
        if (!IsPlay_Wave)
            return;
        // 사망 애니메이션이 재생되는 적은 생존 수에서 바로 제외한다.
        aliveMonsterCount--;
        if (monsterType == WaveType.BasicMonster)
            slotManager.AddCoin(1, position);
        else
        {
            slotManager.AddCoin(50, position);
            if (rouletteManager != null)
                rouletteManager.AddToken(5, position);
        }

        if (currentWaveType == WaveType.BossMonster
            && monsterType == WaveType.BossMonster && Timer > 0)
        {
            bossDefeated = true;
            // 처치 시 체력바를 숨긴다. 등장 연출은 별도 타이머에서 닫는다.
            HideBossHP();
        }
    }

    // 정상 완료한 웨이브에 한 번 보상하고 다음 웨이브를 시작한다.
    private void CompleteWave()
    {
        if (!IsPlay_Wave)
            return;

        IsPlay_Wave = false;
        // 진행 중인 번호와 완료한 번호를 따로 기록한다.
        LastClearedWave = CurrentWave;
        slotManager.AddCoin(waveCoin);

        if (CurrentWave == TotalWaves)
        {
            EndGame(true);
            return;
        }
        CurrentWave++;
        StartWave();
    }

    // 두 종료 경로가 같은 함수를 사용하므로 결과 보상은 한 번만 지급된다.
    private void EndGame(bool won)
    {
        if (IsGameEnded)
            return;

        IsGameEnded = true;
        IsPlay_Wave = false;
        IsClear = won;
        HideBossUI();

        if (IsClear)
        {
            ResultImage.transform.GetChild(0).GetChild(1).GetComponent<Image>().sprite = ClearBenner;
            Debug.Log("Game Clear");

        }
        else
        {
            ResultImage.transform.GetChild(0).GetChild(1).GetComponent<Image>().sprite = LossBenner;
            Debug.Log("Game Over");
        }

        int reward = LastClearedWave * Mathf.Max(0, CoinPerClearedWave);
        // 마지막 웨이브 진행 중 패배한 경우는 제외하고, 전부 완료한 승리일 때만 보너스를 더한다.
        if (IsClear && LastClearedWave == TotalWaves)
            reward += Mathf.Max(0, ClearBonusCoin);
        // 로비 코인은 이번 게임 실행 중에만 누적한다.
        ResultCoin.text = $"{reward}";
        lobbyManager.UpdateCoin(reward);

        slotManager.CloseBattleControls();

        Time.timeScale = 0f;
        ResultImage.SetActive(true);
    }

    // 씬을 다시 읽으면 슬롯, 풀, 전투 골드가 초기값으로 돌아온다.
    public void RestartBattle()
    {
        LeaveResult(true);
    }

    public void ReturnToLobby()
    {
        LeaveResult(false);
    }

    private void LeaveResult(bool retry)
    {
        if (!IsGameEnded || isLeavingResult)
            return;

        isLeavingResult = true;
        // 씬을 다시 열면 일반 필드는 초기화된다. 재도전할 스테이지만 static 변수로 전달한다.
        if (retry)
            StageManager.RetryStage = currentStage;
        else
            StageManager.RetryStage = null;

        Time.timeScale = 1f; // 결과 화면에서 멈춘 시간을 복구한 뒤 씬을 다시 연다.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 풀링은 개체를 매번 삭제하지 않고, 꺼 두었다가 다시 켜서 사용하는 방식이다.
    public void CreatMonsterPool()
    {
        GameObject poolRoot = new GameObject("MonsterPool_Basic");
        for (int i = 0; i < 120; i++)
        {
            GameObject monster = Instantiate(current_BasicMonster, poolRoot.transform);
            monster.SetActive(false);
            Basic_MonsterPool.Add(monster);
        }
        poolRoot = new GameObject("MonsterPool_Boss");
        GameObject boss = Instantiate(current_BossMonster, poolRoot.transform);
        boss.SetActive(false);
        Boss_MonsterPool.Add(boss);
    }

    // 대기 목록에서 개체를 꺼내고 웨이브 외형을 적용해 활성화한다.
    public void GetMonster()
    {
        if (!IsPlay_Wave)
            return;

        List<GameObject> pool = Basic_MonsterPool;
        if (currentWaveType == WaveType.BossMonster)
            pool = Boss_MonsterPool;

        if (pool.Count == 0)
        {
            if (currentWaveType == WaveType.BossMonster)
                return;
            // 일반 몬스터가 쌓이면 풀을 늘린다.
            GameObject extra = Instantiate(current_BasicMonster);
            extra.SetActive(false);
            pool.Add(extra);
        }

        GameObject monsterObject = pool[0];
        // 출전한 몬스터는 대기 목록에서 뺀다. 사망 애니메이션이 끝나면 다시 목록에 넣는다.
        pool.RemoveAt(0);
        monsterObject.GetComponent<SpriteLibrary>().spriteLibraryAsset = WaveSpriteLibrary;
        MonsterManager monster = monsterObject.GetComponent<MonsterManager>();
        monster.SetWaveHP(currentMonsterHP);
        // OnEnable에서 출발 위치, 체력, 이동 경로를 초기화한다.
        monsterObject.SetActive(true);

        if (currentWaveType == WaveType.BossMonster)
            ShowBossUI(monster);

        aliveMonsterCount++;
        if (aliveMonsterCount >= Mathf.Max(1, MonsterLimit))
            EndGame(false);
    }

    // 보스가 실제로 활성화된 뒤 체력과 등장 연출을 표시한다.
    private void ShowBossUI(MonsterManager boss)
    {
        activeBoss = boss;
        RefreshBossHP(boss);

        BossHPSlider.transition = Selectable.Transition.None;
        BossHPSlider.interactable = false;
        BossHPSlider.gameObject.SetActive(true);

        if (BossIntroSeconds > 0)
        {
            // 연출이 화면을 덮어도 소환 버튼과 슬롯을 누를 수 있게 한다.
            CanvasGroup intro = BossInfo_OBJ.GetComponent<CanvasGroup>();
            if (intro == null)
                intro = BossInfo_OBJ.AddComponent<CanvasGroup>();
            intro.blocksRaycasts = false;

            bossIntroEndTime = Time.time + BossIntroSeconds;
            BossInfo_OBJ.transform.SetAsLastSibling();
            BossInfo_OBJ.SetActive(true);
        }
    }

    // 일반 몬스터는 자기 체력바만 갱신한다. 상단에는 현재 보스만 표시한다.
    public void RefreshBossHP(MonsterManager monster)
    {
        if (monster == null || monster != activeBoss)
            return;

        BossHPSlider.maxValue = monster.MAX_HP;
        // 슬라이더 값만 바꾸고 OnValueChanged 이벤트는 호출하지 않는다.
        BossHPSlider.SetValueWithoutNotify(monster.HP);
        BossHPText.text = $"{monster.HP:N0}/{monster.MAX_HP:N0}";
    }

    private void HideBossUI()
    {
        BossInfo_OBJ.SetActive(false);
        HideBossHP();
    }

    private void HideBossHP()
    {
        activeBoss = null;
        BossHPSlider.gameObject.SetActive(false);
    }

    // MonsterManager가 사망 애니메이션 종료 후 호출해 대기 목록으로 돌려보낸다.
    public void RemoveMonster(GameObject Monster)
    {
        MonsterManager monster = Monster.GetComponent<MonsterManager>();
        Monster.SetActive(false);
        // 현재 웨이브가 바뀌었어도 몬스터 자신의 종류로 반환해야 한다.
        List<GameObject> pool = Basic_MonsterPool;
        if (monster.poolType == WaveType.BossMonster)
            pool = Boss_MonsterPool;
        if (!pool.Contains(Monster))
            pool.Add(Monster);

        // 보스 처치 순간이 아니라 풀에 돌아온 시점에 넘어가므로 사망 연출을 끝까지 보여 준다.
        if (monster.poolType == WaveType.BossMonster
            && currentWaveType == WaveType.BossMonster && bossDefeated)
            CompleteWave();
    }
}
