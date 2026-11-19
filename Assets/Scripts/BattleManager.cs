using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

// 웨이브, 몬스터 풀, 종료 보상 관리
public class BattleManager : MonoBehaviour
{
    public static BattleManager instance; // 현재 전투 매니저
    private SlotManager slotManager; // 코인 지급과 슬롯 UI 갱신
    private RouletteManager rouletteManager; // 토큰 지급과 룰렛 UI 갱신
    public LobbyManager lobbyManager; // 로비 보상 지급
    public List<SummonedCharacter> MythicCharacters = new List<SummonedCharacter>(); // 필드에 있는 신화 목록
    public int AngleSteck; // 필드의 천사 수
    public TMP_Text SpeedLoss; // 보스 감속률 텍스트
    public List<GameObject> StageBackgrounds = new List<GameObject>(); // 0번부터 스테이지 순서대로 배경 연결

    public TMP_Text StageName_OBJ; // 스테이지와 웨이브 표시
    public TMP_Text WaveTime_OBJ; // 남은 시간 표시
    public GameObject ResultImage; // 결과 창
    public TMP_Text ResultCoin; // 정산한 로비 코인 표시

    private const int TotalWaves = 25; // 전체 웨이브 수
    private const int BossInterval = 5; // 보스가 나오는 웨이브 간격
    public int CurrentWave = 1; // 진행 중인 웨이브
    public int LastClearedWave; // 마지막으로 클리어한 웨이브
    private StageSetting currentStage; // 선택한 스테이지 데이터
    private WaveType currentWaveType; // 현재 일반 또는 보스 웨이브
    private bool IsPlay_Wave = false; // 웨이브 진행 여부
    private float Timer = 0; // 남은 웨이브 시간
    private float MonsterInterval; // 마지막 생성 후 지난 시간

    public Transform[] MonaterMovePoint; // 몬스터 이동 경로
    public Transform MonaterSpawonPoint; // 몬스터 출발 위치
    public GameObject current_BasicMonster; // 일반 몬스터 프리팹
    public GameObject current_BossMonster; // 보스 프리팹
    [Min(1)] public int MonsterLimit = 100; // 패배 기준인 생존 몬스터 수
    private int aliveMonsterCount; // 현재 살아 있는 몬스터 수
    private int waveCoin; // 이번 전투의 웨이브 보상
    private float currentMonsterHP; // 이번 웨이브의 몬스터 체력
    private SpriteLibraryAsset WaveSpriteLibrary; // 이번 웨이브의 몬스터 외형
    private List<GameObject> Basic_MonsterPool = new List<GameObject>(); // 재사용을 기다리는 일반 몬스터
    private List<GameObject> Boss_MonsterPool = new List<GameObject>(); // 재사용을 기다리는 보스

    public GameObject BossInfo_OBJ; // 보스 등장 연출
    public Slider BossHPSlider; // 보스 체력바
    public TMP_Text BossHPText; // 보스 현재 / 최대 체력
    [Min(0f)] public float BossIntroSeconds = 1f; // 등장 연출 시간
    private bool bossSpawned; // 이번 웨이브 보스 생성 여부
    private bool bossDefeated; // 이번 보스 처치 여부
    private MonsterManager activeBoss; // 상단 체력바에 표시할 보스
    private float bossIntroEndTime; // 등장 연출 종료 시각

    public TMP_Text SpeedText; // 현재 배속 표시
    [Range(3, 4)] public int MaxGameSpeed = 4; // 마지막 배속 단계
    private int currentSpeed = 1; // 현재 배속

    public bool IsClear = false; // 클리어 여부
    public Sprite ClearBenner; // 승리 이미지
    public Sprite LossBenner; // 패배 이미지
    public bool IsGameEnded; // 전투 종료 여부
    [Min(0)] public int CoinPerClearedWave = 10; // 클리어 웨이브당 로비 코인
    [Min(0)] public int ClearBonusCoin = 1000; // 전체 클리어 추가 코인
    private bool isLeavingResult; // 재시작, 로비 버튼 중복 입력 방지

    // 매니저 연결, 보스 UI와 배속 초기화
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

    // 재사용할 몬스터 미리 생성
    private void Start()
    {
        CreatMonsterPool();
    }

    // 웨이브 시간과 몬스터 생성 간격 계산
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
            if (!bossSpawned)
            {
                GetMonster();
                bossSpawned = true;
            }
            if (Timer <= 0)
                EndGame(false);
        }
    }

    // 보스 등장 연출 시간이 지나면 끄기
    private void LateUpdate()
    {
        if (BossInfo_OBJ.activeSelf && Time.time >= bossIntroEndTime)
            BossInfo_OBJ.SetActive(false);
    }

    // 씬을 나갈 때 배속 복구
    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    // 배속 버튼을 누르면 다음 배속으로 변경
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

    // 게임 시간과 배속 텍스트 변경
    private void SetGameSpeed(int speed)
    {
        currentSpeed = speed;
        Time.timeScale = speed;
        SpeedText.text = $"×{speed}";
    }

    // 천사 수에 따른 보스 감속률 표시
    public void RefreshSpeedLoss()
    {
        if (SpeedLoss != null)
            SpeedLoss.text = $"{Mathf.Clamp(AngleSteck * 10, 0, 100)}%";
    }

    // 선택한 스테이지로 새 전투 준비
    public bool GetBattleData(StageSetting stageSetting, int stageIndex)
    {
        if (stageSetting == null || currentStage != null || stageSetting.WaveList == null
            || stageSetting.WaveList.Count != TotalWaves / BossInterval * 2)
            return false;

        currentStage = stageSetting;
        SetStageBackground(stageIndex);
        CurrentWave = 1;
        LastClearedWave = 0;
        aliveMonsterCount = 0;
        IsClear = false;
        IsGameEnded = false;
        AngleSteck = 0;
        RefreshSpeedLoss();
        MonsterLimit = LobbyManager.MonsterLimit;
        waveCoin = LobbyManager.WaveCoin;
        UpGreadManager.instance.ResetUpgrades();
        AchievementManager.instance.ResetAchievements();
        if (rouletteManager != null)
            rouletteManager.SetStartToken(LobbyManager.StartToken);
        SetGameSpeed(1);
        StartWave();
        return true;
    }

    // 배경을 모두 끄고 선택한 배경만 켜기
    private void SetStageBackground(int stageIndex)
    {
        for (int i = 0; i < StageBackgrounds.Count; i++)
            StageBackgrounds[i].SetActive(false);
        StageBackgrounds[stageIndex].SetActive(true);
    }

    // 이번 웨이브의 외형, 체력, 제한 시간 설정
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
        // 정수 나눗셈: 1~5웨이브는 0, 6~10웨이브는 1
        int groupIndex = (CurrentWave - 1) / BossInterval;
        // 목록은 일반, 보스 순서로 두 칸씩 사용
        int listIndex = groupIndex * 2;
        // %는 나머지. 5의 배수 웨이브가 보스
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
        Timer = currentWaveType == WaveType.BossMonster ? 60f : 30f;
        WaveTime_OBJ.text = Timer.ToString("F0");
    }

    // 처치 수와 보상 처리, 보스 사망 기록
    public void MonsterDefeated(WaveType monsterType, Vector3 position)
    {
        if (!IsPlay_Wave)
            return;
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
            if (CurrentWave == 10)
                AchievementManager.instance.AddProgress(AchievementType.DefeatWave10Boss, 1);
            HideBossHP();
        }
    }

    // 클리어 웨이브 기록 후 다음 웨이브 진행
    private void CompleteWave()
    {
        if (!IsPlay_Wave)
            return;

        IsPlay_Wave = false;
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

    // 승패 처리와 클리어한 웨이브 기준 보상 정산
    private void EndGame(bool won)
    {
        if (IsGameEnded)
            return;

        IsGameEnded = true;
        AchievementManager.instance.EndBattle();
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
        if (IsClear && LastClearedWave == TotalWaves)
            reward += Mathf.Max(0, ClearBonusCoin);
        ResultCoin.text = $"{reward}";
        lobbyManager.UpdateCoin(reward);

        slotManager.CloseBattleControls();

        Time.timeScale = 0f;
        ResultImage.SetActive(true);
    }

    // 같은 스테이지 재시작
    public void RestartBattle()
    {
        LeaveResult(true);
    }

    // 결과 창에서 로비로 돌아가기
    public void ReturnToLobby()
    {
        LeaveResult(false);
    }

    // 재도전 여부를 기록하고 씬 다시 불러오기
    private void LeaveResult(bool retry)
    {
        if (!IsGameEnded || isLeavingResult)
            return;

        isLeavingResult = true;
        // static에 재도전 스테이지를 남겨 씬 재시작 후 전달
        if (retry)
            StageManager.RetryStage = currentStage;
        else
            StageManager.RetryStage = null;

        Time.timeScale = 1f; // 결과 화면에서 멈춘 시간을 복구한 뒤 씬을 다시 연다.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 꺼진 몬스터를 풀에 미리 보관
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

    // 풀에서 몬스터를 꺼내 외형과 체력 설정
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
            GameObject extra = Instantiate(current_BasicMonster);
            extra.SetActive(false);
            pool.Add(extra);
        }

        GameObject monsterObject = pool[0];
        pool.RemoveAt(0);
        monsterObject.GetComponent<SpriteLibrary>().spriteLibraryAsset = WaveSpriteLibrary;
        MonsterManager monster = monsterObject.GetComponent<MonsterManager>();
        monster.SetWaveHP(currentMonsterHP);
        monsterObject.SetActive(true);

        if (currentWaveType == WaveType.BossMonster)
            ShowBossUI(monster);

        aliveMonsterCount++;
        if (aliveMonsterCount >= Mathf.Max(1, MonsterLimit))
            EndGame(false);
    }

    // 보스 등장 연출과 체력바 표시
    private void ShowBossUI(MonsterManager boss)
    {
        activeBoss = boss;
        RefreshBossHP(boss);

        BossHPSlider.transition = Selectable.Transition.None;
        BossHPSlider.interactable = false;
        BossHPSlider.gameObject.SetActive(true);

        if (BossIntroSeconds > 0)
        {
            CanvasGroup intro = BossInfo_OBJ.GetComponent<CanvasGroup>();
            if (intro == null)
                intro = BossInfo_OBJ.AddComponent<CanvasGroup>();
            intro.blocksRaycasts = false;

            bossIntroEndTime = Time.time + BossIntroSeconds;
            BossInfo_OBJ.transform.SetAsLastSibling();
            BossInfo_OBJ.SetActive(true);
        }
    }

    // 현재 보스의 체력 표시 갱신
    public void RefreshBossHP(MonsterManager monster)
    {
        if (monster == null || monster != activeBoss)
            return;

        BossHPSlider.maxValue = monster.MAX_HP;
        BossHPSlider.SetValueWithoutNotify(monster.HP); // 슬라이더 이벤트 없이 값만 변경
        BossHPText.text = $"{monster.HP:N0}/{monster.MAX_HP:N0}";
    }

    // 보스 연출과 체력바 끄기
    private void HideBossUI()
    {
        BossInfo_OBJ.SetActive(false);
        HideBossHP();
    }

    // 보스 체력바 끄기
    private void HideBossHP()
    {
        activeBoss = null;
        BossHPSlider.gameObject.SetActive(false);
    }

    // 사망 연출이 끝난 몬스터를 풀에 반환
    public void RemoveMonster(GameObject Monster)
    {
        MonsterManager monster = Monster.GetComponent<MonsterManager>();
        Monster.SetActive(false);
        List<GameObject> pool = Basic_MonsterPool;
        if (monster.poolType == WaveType.BossMonster)
            pool = Boss_MonsterPool;
        if (!pool.Contains(Monster))
            pool.Add(Monster);

        if (monster.poolType == WaveType.BossMonster
            && currentWaveType == WaveType.BossMonster && bossDefeated)
            CompleteWave();
    }
}
