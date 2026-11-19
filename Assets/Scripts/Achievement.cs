using System;

// 도전과제 조건 종류
public enum AchievementType
{
    CollectCoin,
    TryLegendaryRoulette,
    WinLegendaryRoulette,
    Summon,
    CollectToken,
    SellNormal,
    Merge,
    SummonMythic,
    DefeatWave10Boss
}

[Serializable]
// 도전과제 설정과 진행도
public class Achievement
{
    public string Title; // 도전과제 이름
    public string Description; // 달성 조건 설명
    public AchievementType Type; // 진행도를 올릴 행동 종류
    public int TargetCount; // 목표 횟수 또는 획득량
    public int CoinReward; // 전투 코인 보상
    public int TokenReward; // 토큰 보상
    public int CurrentCount; // 현재 진행도
    public bool IsCompleted; // 달성 여부
}
