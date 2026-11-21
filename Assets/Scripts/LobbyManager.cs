using TMPro;
using UnityEngine;

// 로비 코인과 강화 관리
public class LobbyManager : MonoBehaviour
{
    public TMP_Text CoinText; // 현재 로비 코인
    public TMP_Text MonsterLimit_Level; // 몬스터 제한 강화 레벨
    public TMP_Text MonsterLimit_Text; // 강화된 몬스터 제한 수
    public TMP_Text MonsterLimit_Cost; // 몬스터 제한 강화 비용
    public TMP_Text WaveCoin_Level; // 웨이브 보상 강화 레벨
    public TMP_Text WaveCoin_Text; // 현재 웨이브 코인 보상
    public TMP_Text WaveCoin_Cost; // 웨이브 보상 강화 비용
    public TMP_Text StartToKen_Level; // 시작 토큰 강화 레벨
    public TMP_Text StartToKen_Text; // 현재 시작 토큰 수
    public TMP_Text StartToken_Cost; // 시작 토큰 강화 비용

    public GameObject SkillPanel; // 로비 강화 창
    public GameObject StartPanel;

    [Header("강화 비용")]
    [Min(1)] public int MonsterLimitInitialCost = 1000; // 몬스터 제한 첫 강화 비용
    [Min(1)] public int WaveCoinInitialCost = 1000; // 웨이브 보상 첫 강화 비용
    [Min(1)] public int StartTokenInitialCost = 1000; // 시작 토큰 첫 강화 비용
    [Min(1.01f)] public float UpgradeCostMultiplier = 1.5f; // 강화 후 비용 증가 배율

    // =>는 값을 반환하는 짧은 프로퍼티 문법
    public static int MonsterLimit => currentMonsterLimit; // 외부에서 읽는 몬스터 제한 수
    public static int WaveCoin => currentWaveCoin; // 외부에서 읽는 웨이브 보상
    public static int StartToken => currentStartToKen; // 외부에서 읽는 시작 토큰

    // static 값은 씬 재시작 후에도 유지, 게임 종료 후에는 초기화
    private static int sessionCoin; // 이번 게임 실행 중 모은 로비 코인
    private static int currentMonsterLimit; // 현재 몬스터 제한 수
    private static int currentmonsterLimitLevel; // 몬스터 제한 강화 레벨
    private static int MonsterLimitCost; // 다음 몬스터 제한 강화 비용
    private static int currentWaveCoin; // 현재 웨이브 보상
    private static int currentWaveCoinLevel; // 웨이브 보상 강화 레벨
    private static int WaveCoinCost; // 다음 웨이브 보상 강화 비용
    private static int currentStartToKen; // 현재 시작 토큰 수
    private static int currentStartToKenLevel; // 시작 토큰 강화 레벨
    private static int StartTokenCost; // 다음 시작 토큰 강화 비용
    private static bool costsInitialized; // 첫 강화 비용 설정 여부

    // 새 플레이를 시작할 때 실행, 씬 재시작과는 별개
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    // 새 플레이의 로비 재화와 강화 초기화
    private static void ResetSessionCoin()
    {
        sessionCoin = 0;
        currentMonsterLimit = 100;
        currentWaveCoin = 100;
        currentStartToKen = 0;
        currentmonsterLimitLevel = 0;
        currentWaveCoinLevel = 0;
        currentStartToKenLevel = 0;
        MonsterLimitCost = 0;
        WaveCoinCost = 0;
        StartTokenCost = 0;
        costsInitialized = false;
    }

    // 처음 실행할 때 강화 비용 설정
    private void Awake()
    {
        if (costsInitialized)
        {
            // 로비 복귀나 재시작으로 씬을 다시 열면 시작화면은 생략한다.
            StartPanel.SetActive(false);
            return;
        }
        MonsterLimitCost = Mathf.Max(1, MonsterLimitInitialCost);
        WaveCoinCost = Mathf.Max(1, WaveCoinInitialCost);
        StartTokenCost = Mathf.Max(1, StartTokenInitialCost);
        costsInitialized = true;
        StartPanel.SetActive(true);
    }

    // 로비 창과 강화 UI 초기화
    private void Start()
    {
        CoinText.text = $"{sessionCoin}";
        SkillPanel.SetActive(false);
        Update_UpGread_UI();
    }
    // 로비 강화 창 열기
    public void Open_SkillPanel()
    {
        SkillPanel.SetActive(true);
    }
    // 로비 강화 창 닫기
    public void Close_SkillPanel()
    {
        SkillPanel.SetActive(false);
    }
    // 로비 코인 증감 후 표시 갱신
    public void UpdateCoin(int AddCoin)
    {
        sessionCoin += AddCoin;
        CoinText.text = $"{sessionCoin}";
    }

    // 몬스터 제한 수 강화 구매
    public void UpGread_MonsterLimit()
    {
        if (sessionCoin < MonsterLimitCost)
            return;
        UpdateCoin(-MonsterLimitCost);
        currentMonsterLimit++;
        currentmonsterLimitLevel++;
        MonsterLimitCost = NextUpgradeCost(MonsterLimitCost);
        Update_UpGread_UI();
    }
    // 웨이브 보상 강화 구매
    public void UpGread_WaveCoin()
    {
        if (sessionCoin < WaveCoinCost)
            return;
        UpdateCoin(-WaveCoinCost);
        currentWaveCoin += 10;
        currentWaveCoinLevel++;
        WaveCoinCost = NextUpgradeCost(WaveCoinCost);
        Update_UpGread_UI();
    }
    // 시작 토큰 강화 구매
    public void UpGread_StartToken()
    {
        if (sessionCoin < StartTokenCost)
            return;
        UpdateCoin(-StartTokenCost);
        currentStartToKen++;
        currentStartToKenLevel++;
        StartTokenCost = NextUpgradeCost(StartTokenCost);
        Update_UpGread_UI();
    }

    // 다음 강화 비용 계산
    private int NextUpgradeCost(int cost)
    {
        double next = System.Math.Ceiling(cost * (double)Mathf.Max(1.01f, UpgradeCostMultiplier)); // 소수점 올림
        return (int)System.Math.Min(next, int.MaxValue);
    }
    // 강화 레벨, 효과, 비용 표시
    private void Update_UpGread_UI()
    {
        MonsterLimit_Level.text = $"Lv.{currentmonsterLimitLevel}";
        MonsterLimit_Text.text = $"현재 : {currentMonsterLimit}";
        MonsterLimit_Cost.text = MonsterLimitCost.ToString();
        WaveCoin_Level.text = $"Lv.{currentWaveCoinLevel}";
        WaveCoin_Text.text = $"현재 : {currentWaveCoin}"; 
        WaveCoin_Cost.text = WaveCoinCost.ToString();
        StartToKen_Level.text = $"Lv.{currentStartToKenLevel}";
        StartToKen_Text.text = $"현재 : {currentStartToKen}";
        StartToken_Cost.text = StartTokenCost.ToString();
    }
    public void StartGame()
    {
        StartPanel.SetActive(false);
    }
}
