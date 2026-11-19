using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투 중 공격력과 소환 확률 강화
public class UpGreadManager : MonoBehaviour
{
    public static UpGreadManager instance; // 전투 강화 매니저
    public GameObject UpGreadPanel; // 전투 강화 창
    public GameObject LobbyScreen; // 로비에서는 강화 구매 제한
    public SlotManager slotManager; // 강화 비용 차감
    public Draw draw; // 소환 확률 적용
    public Button NormalRareButton, EpicButton, LegendaryMythicButton, SummonButton; // 등급별 공격력 강화 버튼과 소환 강화 버튼
    public TMP_Text NormalRareText, EpicText, LegendaryMythicText, SummonText; // 강화 레벨과 효과 표시
    public TMP_Text NormalRareCostText, EpicCostText, LegendaryMythicCostText, SummonCostText; // 다음 강화 비용 표시
    public int FirstCost = 20; // 첫 강화 비용
    public int CostIncrease = 10; // 레벨당 비용 증가량

    // get은 읽기, private set은 이 클래스 안에서만 값 변경
    public int NormalRareLevel { get; private set; } // 일반, 희귀 공격력 강화 레벨
    public int EpicLevel { get; private set; } // 에픽 공격력 강화 레벨
    public int LegendaryMythicLevel { get; private set; } // 전설, 신화 공격력 강화 레벨
    public int SummonLevel { get; private set; } // 소환 확률 강화 레벨
    private const int MaxSummonLevel = 10; // 소환 강화 최대 레벨

    // 강화 매니저 참조 등록
    private void Awake()
    {
        instance = this;
    }

    // 강화 창과 UI 초기화
    private void Start()
    {
        UpGreadPanel.SetActive(false);
        RefreshUI();
    }
    // 강화 UI 갱신 후 창 열기
    public void OpenUpGreadPanel()
    {
        RefreshUI();
        UpGreadPanel.SetActive(true);
    }
    // 강화 창 닫기
    public void CloseUpGreadPanel()
    {
        UpGreadPanel.SetActive(false);
    }

    // 새 전투의 강화 4종과 소환 확률 초기화
    public void ResetUpgrades()
    {
        NormalRareLevel = 0;
        EpicLevel = 0;
        LegendaryMythicLevel = 0;
        SummonLevel = 0;
        ApplySummonChance();
        CloseUpGreadPanel();
        RefreshUI();
    }

    // 일반, 희귀 공격력 강화 구매
    public void UpgradeNormalRare()
    {
        if (!TrySpendCoin(NormalRareLevel)) return;
        NormalRareLevel++;
        RefreshUI();
    }

    // 에픽 공격력 강화 구매
    public void UpgradeEpic()
    {
        if (!TrySpendCoin(EpicLevel)) return;
        EpicLevel++;
        RefreshUI();
    }

    // 전설, 신화 공격력 강화 구매
    public void UpgradeLegendaryMythic()
    {
        if (!TrySpendCoin(LegendaryMythicLevel)) return;
        LegendaryMythicLevel++;
        RefreshUI();
    }

    // 최대 10레벨까지 소환 확률 강화
    public void UpgradeSummon()
    {
        if (SummonLevel >= MaxSummonLevel) return;
        if (!TrySpendCoin(SummonLevel)) return;
        SummonLevel++;
        ApplySummonChance();
        RefreshUI();
    }

    // 현재 레벨의 다음 강화 비용 계산
    private int GetCost(int level)
    {
        return FirstCost + level * CostIncrease;
    }

    // 전투 상태와 코인 확인
    private bool CanUpgrade(int level)
    {
        return BattleManager.instance != null && !LobbyScreen.activeSelf && !BattleManager.instance.IsGameEnded
            && slotManager.Coin >= GetCost(level);
    }

    // 구매 가능하면 강화 비용 차감
    private bool TrySpendCoin(int level)
    {
        if (!CanUpgrade(level)) return false;
        slotManager.AddCoin(-GetCost(level));
        return true;
    }

    // 등급별 공격력 배율 반환
    public float GetAttackMultiplier(CharacterGrade grade)
    {
        int level;
        if (grade == CharacterGrade.Normal || grade == CharacterGrade.Rare)
            level = NormalRareLevel;
        else if (grade == CharacterGrade.Epic)
            level = EpicLevel;
        else
            level = LegendaryMythicLevel;
        // 레벨당 기본 공격력의 1% 증가, 20레벨은 1.2배
        return 1f + level * 0.01f;
    }

    // 강화 레벨에 따른 소환 확률 적용
    private void ApplySummonChance()
    {
        // 일반 감소분을 상위 등급에 분배, 합계 100%
        draw.NormalWeight = 65f - SummonLevel * 3f;
        draw.RareWeight = 22f + SummonLevel * 1.5f;
        draw.EpicWeight = 9f + SummonLevel;
        draw.LegendaryWeight = 4f + SummonLevel * 0.5f;
    }

    // 강화 레벨, 효과, 비용, 버튼 상태 갱신
    public void RefreshUI()
    {
        NormalRareText.text = $"일반~희귀\n<size=22>Lv.{NormalRareLevel} (+{NormalRareLevel}%)</size>";
        EpicText.text = $"에픽\n<size=22>Lv.{EpicLevel} (+{EpicLevel}%)</size>";
        LegendaryMythicText.text = $"전설~신화\n<size=22>Lv.{LegendaryMythicLevel} (+{LegendaryMythicLevel}%)</size>";
        SummonText.text = $"소환 확률\n<size=22>Lv.{SummonLevel}/{MaxSummonLevel}</size>";
        NormalRareCostText.text = GetCost(NormalRareLevel).ToString();
        EpicCostText.text = GetCost(EpicLevel).ToString();
        LegendaryMythicCostText.text = GetCost(LegendaryMythicLevel).ToString();
        SummonCostText.text = SummonLevel == MaxSummonLevel ? "MAX" : GetCost(SummonLevel).ToString();
        NormalRareButton.interactable = CanUpgrade(NormalRareLevel);
        EpicButton.interactable = CanUpgrade(EpicLevel);
        LegendaryMythicButton.interactable = CanUpgrade(LegendaryMythicLevel);
        SummonButton.interactable = SummonLevel < MaxSummonLevel && CanUpgrade(SummonLevel);
    }
}
