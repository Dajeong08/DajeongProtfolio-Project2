using TMPro;
using UnityEngine;

// 도전과제 한 줄 UI
public class AchievementItemUI : MonoBehaviour
{
    public TMP_Text TitleText; // 도전과제 이름
    public TMP_Text DescriptionText; // 조건 설명
    public TMP_Text ProgressText; // 현재 진행도 / 목표
    public TMP_Text RewardText; // 코인과 토큰 보상
    public GameObject CompletedMark; // 달성 체크 이미지

    private Achievement achievement; // 이 UI에 연결된 도전과제

    // 표시할 도전과제 연결
    public void SetAchievement(Achievement data)
    {
        achievement = data;
        RefreshUI();
    }

    // 제목, 진행도, 보상, 체크 표시 갱신
    public void RefreshUI()
    {
        if (achievement == null)
            return;
        if (TitleText != null)
            TitleText.text = achievement.Title;
        if (DescriptionText != null)
            DescriptionText.text = achievement.Description;
        if (ProgressText != null)
            ProgressText.text = $"{achievement.CurrentCount} / {achievement.TargetCount}";
        if (RewardText != null)
            RewardText.text = $"코인 {achievement.CoinReward} / 토큰 {achievement.TokenReward}";
        if (CompletedMark != null)
            CompletedMark.SetActive(achievement.IsCompleted);
    }
}
