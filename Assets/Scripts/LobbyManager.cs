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

    // 씬을 다시 불러와도 이번 실행에서 재화와 업그레이드는 유지한다.
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
    

    // 에디터에서 Domain Reload를 꺼도 플레이를 새로 시작하면 0으로 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionCoin()
    {
        sessionCoin = 0;
        currentMonsterLimit = 100;
        currentWaveCoin = 100;
        currentStartToKen = 0;
        
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

        currentMonsterLimit++;
        currentmonsterLimitLevel++;
        MonsterLimitCost += 1000;
        Update_UpGread_UI();
    }
    public void UpGread_WaveCoin()
    {
        currentWaveCoin += 10;
        currentWaveCoinLevel++;
        WaveCoinCost += 1000;
        Update_UpGread_UI();
    }
    public void UpGread_StartToken()
    {
        currentStartToKen++;
        currentStartToKenLevel++;
        StartTokenCost += 1000;
        Update_UpGread_UI();
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
