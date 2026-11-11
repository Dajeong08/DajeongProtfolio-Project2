using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 랜덤 종류를 고르고 SlotManager에 소환을 요청한다.
public class Draw : MonoBehaviour
{
    public List<CharacterData> CharacterList = new List<CharacterData>();
    public SlotManager slotManager;
    public Button SummonButton;
    public TMP_Text CoinText;
    public TMP_Text SummonCostText;
    public TMP_Text SummonLogText;
    [Header("등급별 소환 가중치")]
    [Range(0f, 100f)] public float NormalWeight = 70f;
    [Range(0f, 100f)] public float RareWeight = 25f;
    [Range(0f, 100f)] public float EpicWeight = 5f;
    [Range(0f, 100f)] public float LegendaryWeight = 0f;

    private const int MaxLogLines = 5;
    private List<string> summonLogs = new List<string>();
    private List<CharacterData> availableCharacters = new List<CharacterData>();

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

        if (slotManager.TrySummon(selectedData))
            AddSummonLog(selectedData, gradeChance);

        RefreshButton();
    }

    // out으로 캐릭터와 함께 이번에 뽑힌 등급의 확률도 돌려준다.
    private CharacterData PickSummonCharacter(out float gradeChance)
    {
        gradeChance = 0f;
        CollectAvailableCharacters();
        float normal = GetWeight(CharacterGrade.Normal, NormalWeight);
        float rare = GetWeight(CharacterGrade.Rare, RareWeight);
        float epic = GetWeight(CharacterGrade.Epic, EpicWeight);
        float legendary = GetWeight(CharacterGrade.Legendary, LegendaryWeight);
        float total = normal + rare + epic + legendary;
        if (total <= 0f)
            return null;

        float roll = Random.Range(0f, total);
        CharacterGrade grade;
        float weight;
        if (normal > 0f && roll <= normal)
        {
            grade = CharacterGrade.Normal;
            weight = normal;
        }
        else if (rare > 0f && roll <= normal + rare)
        {
            grade = CharacterGrade.Rare;
            weight = rare;
        }
        else if (epic > 0f && roll <= normal + rare + epic)
        {
            grade = CharacterGrade.Epic;
            weight = epic;
        }
        else
        {
            grade = CharacterGrade.Legendary;
            weight = legendary;
        }

        // 뽑을 수 있는 등급의 가중치 합을 기준으로 실제 확률을 구한다.
        gradeChance = weight / total * 100f;
        return PickFromAvailable(grade);
    }

    private void AddSummonLog(CharacterData data, float gradeChance)
    {
        if (SummonLogText == null)
            return;

        string percent = gradeChance.ToString("0.##", CultureInfo.InvariantCulture);
        summonLogs.Add($"{percent}% 확률로 {data.CharacterName} 소환!");
        if (summonLogs.Count > MaxLogLines)
            summonLogs.RemoveAt(0);

        string message = string.Join("\n", summonLogs);
        if (summonLogs.Count == MaxLogLines)
        {
            // 맨 위의 오래된 기록 한 줄만 반투명하게 표시한다.
            int firstLineEnd = message.IndexOf('\n');
            message = message.Insert(firstLineEnd, "<alpha=#FF>");
            message = "<alpha=#66>" + message;
        }
        SummonLogText.text = message;
    }

    // 캐릭터가 없는 등급은 가중치 합계에서 제외한다.
    private float GetWeight(CharacterGrade grade, float weight)
    {
        for (int i = 0; i < availableCharacters.Count; i++)
        {
            if (availableCharacters[i].Grade == grade)
                return Mathf.Clamp(weight, 0f, 100f);
        }
        return 0f;
    }

    // 합성도 같은 등급별 선택 함수를 사용한다.
    public CharacterData PickCharacterByGrade(CharacterGrade grade)
    {
        CollectAvailableCharacters();
        return PickFromAvailable(grade);
    }

    private CharacterData PickFromAvailable(CharacterGrade grade)
    {
        List<CharacterData> candidates = new List<CharacterData>();
        for (int i = 0; i < availableCharacters.Count; i++)
        {
            if (availableCharacters[i].Grade == grade)
                candidates.Add(availableCharacters[i]);
        }
        if (candidates.Count == 0)
            return null;
        return candidates[Random.Range(0, candidates.Count)];
    }

    // 프리팹과 스크립트가 연결된 데이터만 소환 후보에 넣는다.
    private void CollectAvailableCharacters()
    {
        // Clear는 목록만 비우며, 에셋이나 실제 캐릭터를 삭제하지 않는다.
        availableCharacters.Clear();
        for (int i = 0; i < CharacterList.Count; i++)
        {
            CharacterData data = CharacterList[i];

            // continue는 이번 항목을 건너뛰고 다음 항목을 검사한다.
            if (data == null)
                continue;
            // 신화는 전용 조합으로만 소환한다. 일반 소환과 3명 합성에서는 제외한다.
            if (data.Grade >= CharacterGrade.Mythic)
                continue;
            if (data.Prefab == null)
                continue;

            SummonedCharacter character = data.Prefab.GetComponent<SummonedCharacter>();
            if (character == null)
                continue;
            if (availableCharacters.Contains(data))
                continue;

            availableCharacters.Add(data);
        }
    }

    // 코인, 빈 슬롯, 소환 후보가 모두 있을 때만 버튼을 켠다.
    public void RefreshButton()
    {
        RefreshCoinText();

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

        CollectAvailableCharacters();
        if (GetWeight(CharacterGrade.Normal, NormalWeight) == 0
            && GetWeight(CharacterGrade.Rare, RareWeight) == 0
            && GetWeight(CharacterGrade.Epic, EpicWeight) == 0
            && GetWeight(CharacterGrade.Legendary, LegendaryWeight) == 0)
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
