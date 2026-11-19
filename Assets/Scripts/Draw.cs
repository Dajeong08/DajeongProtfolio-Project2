using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 소환 등급 추첨과 로그 표시
public class Draw : MonoBehaviour
{
    public List<CharacterData> CharacterList = new List<CharacterData>(); // 소환 후보 캐릭터 데이터
    public SlotManager slotManager; // 소환과 배치 요청
    public Button SummonButton; // 소환 버튼
    public TMP_Text CoinText; // 현재 전투 코인
    public TMP_Text SummonCostText; // 다음 소환 비용
    public TMP_Text SummonLogText; // 소환, 룰렛 결과 로그
    public RouletteManager rouletteManager; // 토큰과 룰렛 버튼 갱신

    [Header("등급별 소환 확률 (%) - 합계 100")]
    [Range(0f, 100f)] public float NormalWeight = 65f; // 일반 확률 (%)
    [Range(0f, 100f)] public float RareWeight = 22f; // 희귀 확률 (%)
    [Range(0f, 100f)] public float EpicWeight = 9f; // 에픽 확률 (%)
    [Range(0f, 100f)] public float LegendaryWeight = 4f; // 전설 확률 (%)

    private const int MaxLogLines = 5; // 최대 로그 줄 수
    private List<string> summonLogs = new List<string>(); // 최근 로그 내용

    // 소환 버튼과 재화 표시 갱신
    private void OnEnable()
    {
        RefreshButton();
    }

    // 등급과 캐릭터를 뽑아 소환 요청
    public void OnSummonClick()
    {
        if (slotManager == null)
        {
            RefreshButton();
            return;
        }

        if (slotManager.FindEmptySlot() == null)
        {
            RefreshButton();
            return;
        }

        CharacterData selectedData = PickSummonCharacter(out float gradeChance);  
        if (selectedData == null)
        {
            RefreshButton();
            return;
        }

        if (slotManager.TrySummon(selectedData)) // 소환시도
        {
            AddSummonLog(selectedData, gradeChance); // 소환 결과 기록
            AchievementManager.instance.AddProgress(AchievementType.Summon, 1);
        }

        RefreshButton(); 
    }

    // 확률로 등급을 정하고 해당 등급 캐릭터 선택
    // out: 캐릭터와 함께 뽑힌 등급의 확률도 돌려줌
    private CharacterData PickSummonCharacter(out float gradeChance)
    {
        // 정수 난수의 최댓값은 제외: 0.00~99.99 추첨
        float roll = Random.Range(0, 10000) / 100f; // 확률 뽑기
        CharacterGrade grade; 
        if (roll < NormalWeight) 
        {
            grade = CharacterGrade.Normal;
            gradeChance = NormalWeight;
        }
        else if (roll < NormalWeight + RareWeight) 
        {
            grade = CharacterGrade.Rare;
            gradeChance = RareWeight; 
        }
        else if (roll < NormalWeight + RareWeight + EpicWeight) 
        {
            grade = CharacterGrade.Epic;
            gradeChance = EpicWeight;
        }
        else 
        {
            grade = CharacterGrade.Legendary;
            gradeChance = LegendaryWeight;
        }

        return PickFromList(grade); // 뽑힌 등급에서 캐릭터 선택
    }

    // 뽑힌 등급의 확률과 캐릭터 이름 기록
    private void AddSummonLog(CharacterData data, float gradeChance)
    {
        string percent = gradeChance.ToString("0.##"); // 소수점 두 자리까지 표시
        AddLog($"{percent}% 확률로 {data.CharacterName} 소환!");
    }

    // 최근 로그 5줄 표시, 가장 오래된 줄은 흐리게
    public void AddLog(string log)
    {
        if (SummonLogText == null)
            return;

        summonLogs.Add(log);
        if (summonLogs.Count > MaxLogLines) // 5줄을 넘으면 오래된 기록 삭제
            summonLogs.RemoveAt(0);

        string message = string.Join("\n", summonLogs); // 각 로그 사이에 줄바꿈 넣기
        if (summonLogs.Count == MaxLogLines) // 로그가 꽉차면
        {
            int firstLineEnd = message.IndexOf('\n'); // 첫 줄 끝의 줄바꿈 위치
            // 첫 줄 뒤부터 알파를 FF로 돌려 나머지는 선명하게 표시
            message = message.Insert(firstLineEnd, "<alpha=#FF>");
            message = "<alpha=#66>" + message;
        }
        SummonLogText.text = message;
    }

    // 전달받은 등급의 캐릭터 중 하나 선택
    public CharacterData PickFromList(CharacterGrade grade)
    {
        List<CharacterData> candidates = new List<CharacterData>(); // 후보 캐릭터를 담을 리스트
        for (int i = 0; i < CharacterList.Count; i++) // 같은 등급만 후보로 추가
        {
            if (CharacterList[i].Grade == grade)
                candidates.Add(CharacterList[i]);
        }
        if (candidates.Count == 0) 
            return null;
        return candidates[Random.Range(0, candidates.Count)]; // 후보 중 하나 반환
    }

    // 재화, 빈 슬롯, 종료 여부로 버튼 상태 갱신
    public void RefreshButton()
    {
        RefreshCoinText();
        if (UpGreadManager.instance != null)
            UpGreadManager.instance.RefreshUI();
        if (rouletteManager != null)
            rouletteManager.RefreshUI();

        if (SummonButton == null)
            return;

        SummonButton.interactable = false;
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;
        if (slotManager == null)
            return;
        if (slotManager.Coin < slotManager.SummonCost)
            return;
        if (slotManager.FindEmptySlot() == null)
            return;

        if (CharacterList.Count == 0)
            return;

        SummonButton.interactable = true;
    }

    // 현재 코인과 다음 소환 비용 표시
    public void RefreshCoinText()
    {
        if (slotManager == null)
            return;

        if (CoinText != null)
            CoinText.text = slotManager.Coin.ToString();

        if (SummonCostText != null)
            SummonCostText.text = slotManager.SummonCost.ToString();
    }
}
