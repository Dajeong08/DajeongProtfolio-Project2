using System.Collections.Generic;
using UnityEngine;

// 도전과제 진행과 보상 관리
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager instance; // 다른 코드에서 접근할 매니저
    public SlotManager slotManager; // 코인 보상 지급
    public RouletteManager rouletteManager; // 토큰 보상 지급

    public List<Achievement> Achievements = CreateDefaultAchievements(); // 설정과 진행도를 보관하는 목록

    [Header("UI를 만든 후 연결")]
    public GameObject AchievementPanel; // 도전과제 창
    public Transform Content; // 스크롤뷰의 목록 부모
    public AchievementItemUI ItemPrefab; // 도전과제 한 줄 UI

    private List<AchievementItemUI> items = new List<AchievementItemUI>(); // 생성된 목록 UI
    private bool isTracking; // 전투 중 진행도를 집계할지 여부

    // 매니저 참조 등록
    private void Awake()
    {
        instance = this;
    }

    // 창을 닫고 목록 UI 생성
    private void Start()
    {
        ClosePanel();
        CreateUI();
    }

    // 새 전투의 진행도와 달성 여부 초기화
    public void ResetAchievements()
    {
        for (int i = 0; i < Achievements.Count; i++)
        {
            Achievements[i].CurrentCount = 0;
            Achievements[i].IsCompleted = false;
        }
        isTracking = true;
        ClosePanel();
        RefreshUI();
    }

    // 같은 종류의 도전과제 진행도 증가
    public void AddProgress(AchievementType type, int amount)
    {
        if (!isTracking || amount <= 0)
            return;

        for (int i = 0; i < Achievements.Count; i++)
        {
            // 클래스라서 리스트 안의 객체를 그대로 참조
            Achievement achievement = Achievements[i]; // 복사본이 아니라 리스트 안의 객체를 참조한다.
            if (achievement.Type != type || achievement.IsCompleted)
                continue;

            achievement.CurrentCount = Mathf.Min(achievement.CurrentCount + amount, achievement.TargetCount);
            if (achievement.CurrentCount >= achievement.TargetCount)
                CompleteAchievement(achievement);
        }
        RefreshUI();
    }

    // 달성 처리 후 보상 지급
    private void CompleteAchievement(Achievement achievement)
    {
        // 보상 지급 전에 달성 처리해서 중복 지급 방지
        achievement.IsCompleted = true;
        if (achievement.CoinReward > 0)
            // false: 보상 코인은 누적 획득량에서 제외
            slotManager.AddCoin(achievement.CoinReward, false);
        if (achievement.TokenReward > 0)
            // false: 보상 토큰은 누적 획득량에서 제외
            rouletteManager.AddToken(achievement.TokenReward, false);
    }

    // 도전과제 개수만큼 UI 생성
    private void CreateUI()
    {
        if (Content == null || ItemPrefab == null || items.Count > 0)
            return;

        for (int i = 0; i < Achievements.Count; i++)
        {
            AchievementItemUI item = Instantiate(ItemPrefab, Content);
            item.SetAchievement(Achievements[i]);
            items.Add(item);
        }
    }

    // 전체 도전과제 UI 갱신
    public void RefreshUI()
    {
        for (int i = 0; i < items.Count; i++)
            items[i].RefreshUI();
    }

    // 목록을 갱신하고 창 열기
    public void OpenPanel()
    {
        CreateUI();
        RefreshUI();
        if (AchievementPanel != null)
            AchievementPanel.SetActive(true);
    }

    // 도전과제 창 닫기
    public void ClosePanel()
    {
        if (AchievementPanel != null)
            AchievementPanel.SetActive(false);
    }

    // 전투 종료 후 진행도 집계 중단
    public void EndBattle()
    {
        isTracking = false;
        ClosePanel();
    }

    // 기본 도전과제 10개와 보상 설정
    public static List<Achievement> CreateDefaultAchievements()
    {
        return new List<Achievement>
        {
            new Achievement { Title = "소환의 시작", Description = "소환 버튼으로 캐릭터 10회 소환", Type = AchievementType.Summon, TargetCount = 10, CoinReward = 120 },
            new Achievement { Title = "대규모 소환", Description = "소환 버튼으로 캐릭터 30회 소환", Type = AchievementType.Summon, TargetCount = 30, CoinReward = 300 },
            new Achievement { Title = "차곡차곡", Description = "코인 누적 1,000개 획득", Type = AchievementType.CollectCoin, TargetCount = 1000, TokenReward = 2 },
            new Achievement { Title = "토큰 수집가", Description = "토큰 누적 10개 획득", Type = AchievementType.CollectToken, TargetCount = 10, CoinReward = 200 },
            new Achievement { Title = "정리의 달인", Description = "일반 캐릭터 5명 판매", Type = AchievementType.SellNormal, TargetCount = 5, CoinReward = 120 },
            new Achievement { Title = "한 단계 더", Description = "일반 합성 5회 성공", Type = AchievementType.Merge, TargetCount = 5, TokenReward = 2 },
            new Achievement { Title = "전설에 도전", Description = "전설 룰렛 3회 도전", Type = AchievementType.TryLegendaryRoulette, TargetCount = 3, TokenReward = 2 },
            new Achievement { Title = "행운의 주인공", Description = "전설 룰렛 1회 성공", Type = AchievementType.WinLegendaryRoulette, TargetCount = 1, CoinReward = 300 },
            new Achievement { Title = "첫 번째 신화", Description = "신화 캐릭터 1명 조합 소환", Type = AchievementType.SummonMythic, TargetCount = 1, TokenReward = 4 },
            new Achievement { Title = "보스 사냥꾼", Description = "10웨이브 보스 처치", Type = AchievementType.DefeatWave10Boss, TargetCount = 1, CoinReward = 300, TokenReward = 2 }
        };
    }
}
