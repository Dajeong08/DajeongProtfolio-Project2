using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 랜덤 종류를 고르고 SlotManager에 소환을 요청한다.
public class Draw : MonoBehaviour
{
    public List<CharacterData> CharacterList = new List<CharacterData>(); // 생성할 수 있는 캐릭터 종류를 에셋에서 드래그한다.
    public SlotManager slotManager;
    public Button SummonButton; // 소환버튼
    public TMP_Text CoinText; // 현재 코인
    public TMP_Text SummonCostText; // 다음 소환 비용
    public TMP_Text SummonLogText; // 소환 로그를 표시할 텍스트
    public RouletteManager rouletteManager;

    [Header("등급별 소환 확률 (%) - 합계 100")]
    [Range(0f, 100f)] public float NormalWeight = 70f;
    [Range(0f, 100f)] public float RareWeight = 25f;
    [Range(0f, 100f)] public float EpicWeight = 5f;
    [Range(0f, 100f)] public float LegendaryWeight = 0f;

    private const int MaxLogLines = 5; 
    private List<string> summonLogs = new List<string>(); // 소환 로그를 최대 5줄까지 저장한다.

    private void OnEnable()
    {
        RefreshButton();
    }

    // 소환 버튼의 On Click에서 호출한다.
    public void OnSummonClick()
    {
        if (slotManager == null)
        {
            RefreshButton();
            return;
        }

        // 같은 종류를 더 쌓을 수 있어도 완전히 빈 슬롯이 하나는 필요하다.
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
            AddSummonLog(selectedData, gradeChance); // 로그띄우기(생성할 캐릭터, 등급) 넘기기

        RefreshButton(); 
    }

    // out으로 캐릭터와 함께 이번에 뽑힌 등급의 확률도 돌려준다.
    private CharacterData PickSummonCharacter(out float gradeChance) // 등급 뽑기
    {
        // 0.00~99.99를 뽑는다. 0%인 전설이 100 때문에 뽑히는 일을 막는다.
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

        return PickFromList(grade); // 등급에 맞는 뽑은 캐릭터 반환
    }

    private void AddSummonLog(CharacterData data, float gradeChance)
    {
        string percent = gradeChance.ToString("0.##"); // 소수점 두 자리까지 표시
        AddLog($"{percent}% 확률로 {data.CharacterName} 소환!");
    }

    // 일반 소환과 룰렛 결과를 같은 최근 5줄 로그에 기록한다.
    public void AddLog(string log)
    {
        if (SummonLogText == null)
            return;

        summonLogs.Add(log);
        if (summonLogs.Count > MaxLogLines) // 최대 로그 라인 수를 초과하면 맨 위의 오래된 기록을 제거한다.
            summonLogs.RemoveAt(0);

        string message = string.Join("\n", summonLogs); // summonLogs 리스트의 모든 문자열 사이에 줄바꿈을 넣어 연결하여 하나의 메시지로 만듬
        if (summonLogs.Count == MaxLogLines) // 로그가 꽉차면
        {
            // 맨 위의 오래된 기록 한 줄만 반투명하게 표시한다.
            int firstLineEnd = message.IndexOf('\n'); // 가장 오래된 로그가 아닌 그 다음 로그
            message = message.Insert(firstLineEnd, "<alpha=#FF>");
            message = "<alpha=#66>" + message;
        }
        SummonLogText.text = message;
    }

    public CharacterData PickFromList(CharacterGrade grade) // 전달받은 등급중에서 랜덤으로  캐릭터 뽑기 하나를 뽑는다.
    {
        List<CharacterData> candidates = new List<CharacterData>(); // 후보 캐릭터를 담을 리스트
        for (int i = 0; i < CharacterList.Count; i++) // 등급에 맞는 캐릭터만 후보 리스트에 담는다.
        {
            if (CharacterList[i].Grade == grade)
                candidates.Add(CharacterList[i]);
        }
        if (candidates.Count == 0) 
            return null;
        return candidates[Random.Range(0, candidates.Count)]; // 리스트중 랜덤으로 하나 반환
    }

    // 코인, 빈 슬롯, 소환 후보가 모두 있을 때만 버튼을 켠다.
    public void RefreshButton()
    {
        RefreshCoinText();
        if (rouletteManager != null)
            rouletteManager.RefreshUI();

        if (SummonButton == null)
            return;

        // 먼저 끈 뒤 모든 조건을 통과하면 켠다.
        SummonButton.interactable = false;
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;
        if (slotManager == null)
            return;
        // 코인이 소환 비용보다 적으면 버튼을 켜지 않는다.
        if (slotManager.Coin < slotManager.SummonCost)
            return;
        if (slotManager.FindEmptySlot() == null)
            return;

        if (CharacterList.Count == 0)
            return;

        SummonButton.interactable = true;
    }

    // 처음 활성화될 때와 소환 후, 현재 코인과 다음 소환 비용을 표시한다.
    public void RefreshCoinText()
    {
        if (slotManager == null)
            return;

        // ToString()은 숫자를 UI에 넣을 수 있는 문자열로 바꾼다.
        if (CoinText != null)
            CoinText.text = slotManager.Coin.ToString();

        if (SummonCostText != null)
            SummonCostText.text = slotManager.SummonCost.ToString();
    }
}
