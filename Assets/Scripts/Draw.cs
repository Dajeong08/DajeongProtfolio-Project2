using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 랜덤 종류를 고르고 SlotManager에 소환을 요청한다.
public class Draw : MonoBehaviour
{
    public List<CharacterData> CharacterList = new List<CharacterData>();
    public SlotManager slotManager;
    public Button SummonButton;

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

        CollectAvailableCharacters();
        if (availableCharacters.Count == 0)
        {
            RefreshButton();
            return;
        }

        // 정수 Random.Range는 마지막 숫자를 포함하지 않는다. 2개면 0 또는 1.
        int randomIndex = Random.Range(0, availableCharacters.Count);
        CharacterData selectedData = availableCharacters[randomIndex];
        slotManager.TrySummon(selectedData);
        RefreshButton();
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

    // 빈 슬롯과 소환 후보가 모두 있을 때만 버튼을 켠다.
    public void RefreshButton()
    {
        if (SummonButton == null)
            return;

        // 먼저 끈 뒤 모든 조건을 통과하면 켠다.
        SummonButton.interactable = false;
        if (slotManager == null)
            return;
        if (slotManager.FindEmptySlot() == null)
            return;

        CollectAvailableCharacters();
        if (availableCharacters.Count == 0)
            return;

        SummonButton.interactable = true;
    }
}
