using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 토큰 룰렛과 결과 표시
public class RouletteManager : MonoBehaviour
{
    public int Token = 0; // 현재 전투 토큰
    public Draw draw; // 등급별 캐릭터 추첨과 로그 표시
    public SlotManager slotManager; // 룰렛 캐릭터 배치
    public GameObject LobbyScreen; // 로비에서는 룰렛 사용 제한
    public TMP_Text TokenText; // 현재 토큰 표시
    public TMP_Text ResultText; // 이번 룰렛 결과 표시
    public Button RareButton; // 희귀 룰렛 버튼
    public Button EpicButton; // 에픽 룰렛 버튼
    public Button LegendaryButton; // 전설 룰렛 버튼
    public GameObject RoulettePanel; // 룰렛 창

    // 시작 토큰 설정 전 초기값 준비
    private void Awake()
    {
        Token = 0;
    }

    // 결과 폰트를 맞추고 룰렛 UI 초기화
    private void Start()
    {
        if (ResultText != null)
        {
            ResultText.font = draw.SummonLogText.font;
            ResultText.fontSharedMaterial = draw.SummonLogText.fontSharedMaterial;
            ResultText.text = "";
        }
        ClosePanel();
        RefreshUI();
    }

    // 로비 강화에 따른 시작 토큰 설정
    public void SetStartToken(int amount)
    {
        Token = Mathf.Max(0, amount);
        RefreshUI();
    }

    // 전투 중 룰렛 창 열기
    public void OpenPanel()
    {
        if (BattleManager.instance.IsGameEnded || LobbyScreen.activeInHierarchy)
            return;
        if (ResultText != null)
            ResultText.text = "";
        RefreshUI();
        RoulettePanel.SetActive(true);
    }

    // 룰렛 창 닫기
    public void ClosePanel()
    {
        RoulettePanel.SetActive(false);
    }

    // 토큰 증감 후 UI와 도전과제 갱신
    public void AddToken(int amount, bool countAchievement = true)
    {
        Token += amount;
        if (amount > 0 && countAchievement)
            AchievementManager.instance.AddProgress(AchievementType.CollectToken, amount);
        RefreshUI();
        if (amount > 0 && FloatingTextManager.instance != null)
            FloatingTextManager.instance.ShowTokenText(amount);
    }

    // 토큰 증감 후 UI와 도전과제 갱신
    public void AddToken(int amount, Vector3 position)
    {
        AddToken(amount);
        if (amount > 0 && FloatingTextManager.instance != null)
            FloatingTextManager.instance.ShowTokenText(amount, position);
    }

    // 토큰 1개로 희귀 룰렛 도전
    public void OnRareClick()
    {
        Roll(CharacterGrade.Rare, 1, 60, "희귀");
    }

    // 토큰 1개로 에픽 룰렛 도전
    public void OnEpicClick()
    {
        Roll(CharacterGrade.Epic, 1, 30, "에픽");
    }

    // 토큰 2개로 전설 룰렛 도전
    public void OnLegendaryClick()
    {
        Roll(CharacterGrade.Legendary, 2, 10, "전설");
    }

    // 전투 상태와 빈 슬롯 확인
    private bool CanUseRoulette()
    {
        return BattleManager.instance != null && !BattleManager.instance.IsGameEnded
            && !LobbyScreen.activeInHierarchy && slotManager.FindEmptySlot() != null;
    }

    // 토큰 차감, 확률 추첨, 결과와 로그 처리
    private void Roll(CharacterGrade grade, int cost, int chance, string gradeName)
    {
        if (!CanUseRoulette() || Token < cost)
            return;

        Token -= cost;
        bool placementFailed = false;
        bool succeeded = false;
        string message = "룰렛 실패";
        Color resultColor = Color.red;
        if (Random.Range(0, 100) < chance)
        {
            CharacterData data = draw.PickFromList(grade);
            if (slotManager.TryPlaceCharacter(data))
            {
                message = $"{data.CharacterName} 소환 성공!";
                succeeded = true;
                resultColor = Color.white;
            }
            else
            {
                // 배치 실패는 토큰 환불, 도전 횟수에도 포함하지 않음
                Token += cost;
                placementFailed = true;
                message = "캐릭터를 배치할 수 없습니다.";
                resultColor = Color.white;
            }
        }
        if (grade == CharacterGrade.Legendary && !placementFailed)
        {
            AchievementManager.instance.AddProgress(AchievementType.TryLegendaryRoulette, 1);
            if (succeeded)
                AchievementManager.instance.AddProgress(AchievementType.WinLegendaryRoulette, 1);
        }
        if (ResultText != null)
        {
            ResultText.text = message;
            ResultText.color = resultColor;
        }

        draw.AddLog($"[룰렛 {gradeName} {chance}%] {message}");

        slotManager.NotifyCharactersChanged();
        // 성공하면 룰렛 창을 닫아 소환 위치와 이펙트를 보여준다.
        if (succeeded)
            ClosePanel();
    }

    // 토큰 수와 룰렛 버튼 상태 갱신
    public void RefreshUI()
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            ClosePanel();
        TokenText.text = Token.ToString();
        bool canUse = CanUseRoulette();
        RareButton.interactable = canUse && Token >= 1;
        EpicButton.interactable = canUse && Token >= 1;
        LegendaryButton.interactable = canUse && Token >= 2;
    }
}
