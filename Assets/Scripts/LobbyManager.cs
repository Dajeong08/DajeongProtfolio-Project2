using TMPro;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public TMP_Text CoinText;
    public TMP_Text Diamond;
    public TMP_Text MonsterLimit_Level;
    public TMP_Text MonsterLimit_Text;
    public TMP_Text MonsterLimit_Cost;
    public TMP_Text WaveCoin_Level;
    public TMP_Text WaveCoin_Text;
    public TMP_Text WaveCoin_Cost;
    public TMP_Text StartToKen_Level;
    public TMP_Text StartToKen_Text;
    public TMP_Text StartToken_Cost;

    public GameObject SkillPanel;

    [Header("강화 비용")]
    [Min(1)] public int MonsterLimitInitialCost = 1000;
    [Min(1)] public int WaveCoinInitialCost = 1000;
    [Min(1)] public int StartTokenInitialCost = 1000;
    [Min(1.01f)] public float UpgradeCostMultiplier = 1.5f;

    // =>는 값을 반환하는 짧은 프로퍼티 문법. 밖에서는 읽기만 하고 강화 함수에서 값을 바꾼다.
    public static int MonsterLimit => currentMonsterLimit;
    public static int WaveCoin => currentWaveCoin;
    public static int StartToken => currentStartToKen;

    // 씬을 다시 불러와도 이번 실행에서 재화와 업그레이드는 유지한다.
    // static 값은 씬을 다시 열어도 유지된다. 게임을 종료한 뒤에도 저장되는 것은 아니다.
    private static int sessionCoin;
    private static int currentMonsterLimit;
    private static int currentmonsterLimitLevel;
    private static int MonsterLimitCost;
    private static int currentWaveCoin;
    private static int currentWaveCoinLevel;
    private static int WaveCoinCost;
    private static int currentStartToKen;
    private static int currentStartToKenLevel;
    private static int StartTokenCost;
    private static bool costsInitialized;
    

    // 에디터에서 Domain Reload를 꺼도 플레이를 새로 시작하면 0으로 초기화한다.
    // 일반 Start보다 먼저 실행해서, 새 플레이를 시작할 때만 강화와 재화를 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
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

    private void Awake()
    {
        // 씬 재시작 시에는 구매 후 비용을 유지한다.
        if (costsInitialized)
            return;
        MonsterLimitCost = Mathf.Max(1, MonsterLimitInitialCost);
        WaveCoinCost = Mathf.Max(1, WaveCoinInitialCost);
        StartTokenCost = Mathf.Max(1, StartTokenInitialCost);
        costsInitialized = true;
    }

    private void Start()
    {
        CoinText.text = $"{sessionCoin}";
        SkillPanel.SetActive(false);
        Update_UpGread_UI();
    }
    public void Open_SkillPanel()
    {
        SkillPanel.SetActive(true);
    }
    public void Close_SkillPanel()
    {
        SkillPanel.SetActive(false);
    }
    public void UpdateCoin(int AddCoin)
    {
        sessionCoin += AddCoin;
        CoinText.text = $"{sessionCoin}";
    }

    public void UpGread_MonsterLimit()
    {
        if (sessionCoin < MonsterLimitCost)
            return;
        // 음수를 넘기면 그만큼 코인이 줄어든다. 차감 후 다음 강화 비용을 계산한다.
        UpdateCoin(-MonsterLimitCost);
        currentMonsterLimit++;
        currentmonsterLimitLevel++;
        MonsterLimitCost = NextUpgradeCost(MonsterLimitCost);
        Update_UpGread_UI();
    }
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

    private int NextUpgradeCost(int cost)
    {
        // 큰 비용에서도 정수 범위를 넘어 음수가 되지 않게 한다.
        // Ceiling은 소수점을 올림한다. double로 계산하고 int 최대값 안에서 정수로 바꾼다.
        double next = System.Math.Ceiling(cost * (double)Mathf.Max(1.01f, UpgradeCostMultiplier));
        return (int)System.Math.Min(next, int.MaxValue);
    }
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
}
